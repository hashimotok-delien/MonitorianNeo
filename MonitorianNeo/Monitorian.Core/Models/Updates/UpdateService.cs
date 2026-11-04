using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Monitorian.Core.Models.Updates;

/// <summary>
/// State of update check
/// </summary>
public enum UpdateState
{
	/// <summary>
	/// Not checked yet
	/// </summary>
	None,

	/// <summary>
	/// Checking
	/// </summary>
	Checking,

	/// <summary>
	/// The current version is the latest
	/// </summary>
	UpToDate,

	/// <summary>
	/// A newer version is available
	/// </summary>
	Available,

	/// <summary>
	/// Failed to check
	/// </summary>
	Failed,
}

/// <summary>
/// Checks, downloads and starts installing updates published on GitHub Releases.
/// </summary>
public class UpdateService : INotifyPropertyChanged, IDisposable
{
	private const string RepositoryOwner = "hashimotok-delien";
	private const string RepositoryName = "MonitorianNeo";

	// Pre-releases are included so that a pre-release can also be installed.
	private static readonly Uri ReleasesUri = new($"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases?per_page=20");

	// The uninstall key that Inno Setup creates for this product.
	private static readonly string[] UninstallKeys =
	[
		@"Software\Microsoft\Windows\CurrentVersion\Uninstall\{7F3A5C1E-9B62-4E48-93D0-5A8C2B6E4F17}_is1",
	];

	private readonly HttpClient _client;
	private readonly SemaphoreSlim _checkGate = new(1, 1);

	private UpdateState _state = UpdateState.None;
	public UpdateState State
	{
		get => _state;
		private set => SetProperty(ref _state, value);
	}

	private ReleaseInfo _latest;
	/// <summary>
	/// The latest release which is newer than the current version
	/// </summary>
	public ReleaseInfo Latest
	{
		get => _latest;
		private set
		{
			if (SetProperty(ref _latest, value))
				OnPropertyChanged(nameof(IsUpdateAvailable));
		}
	}

	public bool IsUpdateAvailable => (_latest is not null);

	private bool _isDownloading;
	public bool IsDownloading
	{
		get => _isDownloading;
		private set => SetProperty(ref _isDownloading, value);
	}

	private double _downloadProgress;
	public double DownloadProgress
	{
		get => _downloadProgress;
		private set => SetProperty(ref _downloadProgress, value);
	}

	/// <summary>
	/// Whether this application has been installed by the installer
	/// </summary>
	public static bool IsInstalled => UninstallKeys.Any(IsUninstallKeyPresent);

	private static bool IsUninstallKeyPresent(string key)
	{
		using (var currentUser = Registry.CurrentUser.OpenSubKey(key))
		{
			if (currentUser is not null)
				return true;
		}

		using (var localMachine = Registry.LocalMachine.OpenSubKey(key))
		{
			return (localMachine is not null);
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public UpdateService()
	{
		_client = new HttpClient(new HttpClientHandler())
		{
			Timeout = TimeSpan.FromSeconds(30),
		};
		_client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ProductInfo.Product, ProductInfo.DisplayVersion));
		_client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
	}

	/// <summary>
	/// Checks the latest release.
	/// </summary>
	/// <returns>True if a newer version is available</returns>
	public async Task<bool> CheckAsync(CancellationToken cancellationToken = default)
	{
		if (!await _checkGate.WaitAsync(0, cancellationToken))
			return IsUpdateAvailable;

		try
		{
			State = UpdateState.Checking;
			Latest = null;

			var (success, release) = await FetchLatestReleaseAsync(cancellationToken);
			if (!success)
			{
				State = UpdateState.Failed;
				return false;
			}

			if (release is null)
			{
				Debug.WriteLine("No release with a valid version was found.");
				State = UpdateState.UpToDate;
				return false;
			}

			if (IsNewer(release))
			{
				Latest = release;
				State = UpdateState.Available;
				OperationRecorder.Record($"Update available: {release.DisplayVersion}");
				return true;
			}

			State = UpdateState.UpToDate;
			return false;
		}
		catch (OperationCanceledException)
		{
			State = UpdateState.Failed;
			return false;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("Failed to check the latest release." + Environment.NewLine + ex);
			Logger.SaveException(ex);
			State = UpdateState.Failed;
			return false;
		}
		finally
		{
			_checkGate.Release();
		}
	}

	/// <summary>
	/// Fetches the newest release including pre-releases.
	/// </summary>
	/// <returns>success: False if the request failed. release: Null if no release exists.</returns>
	private async Task<(bool success, ReleaseInfo release)> FetchLatestReleaseAsync(CancellationToken cancellationToken)
	{
		using var response = await _client.GetAsync(ReleasesUri, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			Debug.WriteLine($"Failed to get releases. Status: {(int)response.StatusCode}");
			return (false, null);
		}

		await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
		using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

		if (document.RootElement.ValueKind is not JsonValueKind.Array)
			return (false, null);

		// Drafts are excluded by ReleaseInfo.TryParse.
		var release = document.RootElement
			.EnumerateArray()
			.Select(x => ReleaseInfo.TryParse(x, out var parsed) ? parsed : null)
			.Where(x => x is not null)
			.OrderByDescending(x => x, Comparer<ReleaseInfo>.Create(ReleaseInfo.Compare))
			.FirstOrDefault();

		return (true, release);
	}

	/// <summary>
	/// Current version of this application
	/// </summary>
	public static Version CurrentVersion => ReleaseInfo.Normalize(ProductInfo.Version);

	/// <summary>
	/// Whether the specified release is newer than the current version.
	/// </summary>
	/// <remarks>
	/// The current version comes from the assembly version which has no pre-release label.
	/// Therefore a pre-release of the same numeric version is not treated as newer
	/// to avoid offering the same version repeatedly.
	/// </remarks>
	public static bool IsNewer(ReleaseInfo release) =>
		(release is not null) && (release.Version > CurrentVersion);

	/// <summary>
	/// Downloads the installer of the specified release and verifies its checksum.
	/// </summary>
	/// <returns>Path of the downloaded installer</returns>
	public async Task<string> DownloadInstallerAsync(ReleaseInfo release, CancellationToken cancellationToken = default)
	{
		if (release is null)
			throw new ArgumentNullException(nameof(release));
		if (string.IsNullOrWhiteSpace(release.InstallerUrl))
			throw new InvalidOperationException("The release does not contain an installer.");

		var folderPath = Path.Combine(Path.GetTempPath(), ProductInfo.Product, "update");
		Directory.CreateDirectory(folderPath);

		var fileName = string.IsNullOrWhiteSpace(release.InstallerName)
			? "MonitorianNeo-setup.exe"
			: Path.GetFileName(release.InstallerName);
		var filePath = Path.Combine(folderPath, fileName);

		IsDownloading = true;
		DownloadProgress = 0;
		try
		{
			using var response = await _client.GetAsync(release.InstallerUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			response.EnsureSuccessStatusCode();

			var totalLength = response.Content.Headers.ContentLength;

			await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
			await using (var destination = File.Create(filePath))
			{
				var buffer = new byte[81920];
				long totalRead = 0;
				int read;
				while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
				{
					await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
					totalRead += read;

					if (totalLength is > 0)
						DownloadProgress = (double)totalRead / totalLength.Value;
				}
			}

			await VerifyChecksumAsync(release, fileName, filePath, cancellationToken);

			DownloadProgress = 1;
			OperationRecorder.Record($"Update downloaded: {fileName}");
			return filePath;
		}
		finally
		{
			IsDownloading = false;
		}
	}

	private async Task VerifyChecksumAsync(ReleaseInfo release, string fileName, string filePath, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(release.ChecksumUrl))
		{
			Debug.WriteLine("The release does not contain a checksum file. Skipped verification.");
			return;
		}

		var sums = await _client.GetStringAsync(release.ChecksumUrl, cancellationToken);

		var expected = sums
			.Split('\n', StringSplitOptions.RemoveEmptyEntries)
			.Select(x => x.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
			.Where(x => x.Length >= 2)
			.Where(x => string.Equals(x[^1].TrimStart('*'), fileName, StringComparison.OrdinalIgnoreCase))
			.Select(x => x[0])
			.FirstOrDefault();

		if (string.IsNullOrWhiteSpace(expected))
		{
			Debug.WriteLine($"The checksum file does not contain {fileName}. Skipped verification.");
			return;
		}

		await using var stream = File.OpenRead(filePath);
		var actual = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));

		if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
		{
			File.Delete(filePath);
			throw new InvalidDataException("The checksum of the downloaded installer does not match.");
		}
	}

	/// <summary>
	/// Starts the downloaded installer silently. This application should exit right after this.
	/// </summary>
	public void LaunchInstaller(string installerPath)
	{
		if (string.IsNullOrWhiteSpace(installerPath))
			throw new ArgumentNullException(nameof(installerPath));

		// Inno Setup closes this application through Restart Manager and restarts it after installation.
		var startInfo = new ProcessStartInfo(installerPath)
		{
			Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
			UseShellExecute = true,
		};
		Process.Start(startInfo);
		OperationRecorder.Record("Update installer started.");
	}

	/// <summary>
	/// Opens the release page in the default browser.
	/// </summary>
	public static Task<bool> OpenReleasePageAsync(ReleaseInfo release)
	{
		var url = release.HtmlUrl ?? $"https://github.com/{RepositoryOwner}/{RepositoryName}/releases/latest";
		return Launcher.LaunchAsync(new Uri(url));
	}

	#region INotifyPropertyChanged

	protected bool SetProperty<T>(ref T storage, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
	{
		if (Equals(storage, value))
			return false;

		storage = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	#endregion

	#region IDisposable

	private bool _isDisposed;

	public void Dispose()
	{
		if (_isDisposed)
			return;

		_client.Dispose();
		_checkGate.Dispose();
		_isDisposed = true;
	}

	#endregion
}
