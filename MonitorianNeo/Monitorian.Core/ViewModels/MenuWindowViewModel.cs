using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Monitorian.Core.Models;
using Monitorian.Core.Models.Updates;

namespace Monitorian.Core.ViewModels;

public class MenuWindowViewModel : ViewModelBase
{
	private readonly AppController _controller;
	public Settings Settings => _controller.Settings;

	public MenuWindowViewModel(AppController controller)
	{
		this._controller = controller ?? throw new ArgumentNullException(nameof(controller));

		_controller.Updates.PropertyChanged += OnUpdatesChanged;

		CleanLicense();
	}

	#region License

	private const string LicenseFileName = "License.txt";
	private static bool _licenseFileExists = true; // Default

	public void OpenLicense()
	{
		Task.Run(() =>
		{
			var licenseFileBody = DocumentService.ReadEmbeddedFile(LicenseFileName);
			var licenseFileHtml = DocumentService.BuildHtml(LicenseFileName, ProductInfo.Product, licenseFileBody);

			(_licenseFileExists, var licenseFilePath) = TempService.SaveFile(LicenseFileName, "html", licenseFileHtml);
			if (!_licenseFileExists)
				return;

			Process.Start(licenseFilePath);
		});
	}

	private void CleanLicense()
	{
		if (!_licenseFileExists)
			return;

		Task.Run(() => _licenseFileExists = TempService.DeleteFile(LicenseFileName, "html", TimeSpan.FromHours(1)));
	}

	#endregion

	#region Startup

	public bool CanRegister => _controller.StartupAgent.CanRegister();

	public bool IsRegistered
	{
		get
		{
			if (!_isRegistered.HasValue)
			{
				_isRegistered = _controller.StartupAgent.IsRegistered();
			}
			return _isRegistered.GetValueOrDefault();
		}
		set
		{
			if (_isRegistered == value)
				return;

			if (value)
			{
				_controller.StartupAgent.Register();
			}
			else
			{
				_controller.StartupAgent.Unregister();
			}
			_isRegistered = value;
			OnPropertyChanged();
		}
	}
	private bool? _isRegistered;

	#endregion

	#region Accent color

	public bool IsAccentColorSupported => _controller.WindowPainter.IsAccentColorSupported;

	#endregion

	#region Update

	private UpdateService Updates => _controller.Updates;

	/// <summary>
	/// Text of the update menu item
	/// </summary>
	public string UpdateStatusText
	{
		get
		{
			if (Updates.IsDownloading)
				return string.Format(Properties.Resources.DownloadingUpdate, (int)(Updates.DownloadProgress * 100));

			if (Updates.Latest is { } release)
				return string.Format(Properties.Resources.UpdateAvailable, $"v{release.DisplayVersion}");

			return Updates.State switch
			{
				UpdateState.Checking => Properties.Resources.CheckingUpdate,
				UpdateState.UpToDate => Properties.Resources.UpToDate,
				UpdateState.Failed => Properties.Resources.UpdateCheckFailed,
				_ => Properties.Resources.CheckUpdate,
			};
		}
	}

	/// <summary>
	/// Whether the update menu item can be clicked
	/// </summary>
	public bool CanUpdate => (Updates.State is not UpdateState.Checking) && !Updates.IsDownloading;

	/// <summary>
	/// Checks for updates, or installs the available update.
	/// </summary>
	public async Task CheckOrInstallUpdateAsync()
	{
		if (!CanUpdate)
			return;

		if (Updates.Latest is not null)
		{
			await _controller.InstallUpdateAsync();
			return;
		}

		await Updates.CheckAsync();
	}

	private void OnUpdatesChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		// The update service notifies from a background thread.
		var dispatcher = System.Windows.Application.Current?.Dispatcher;
		if ((dispatcher is null) || dispatcher.CheckAccess())
		{
			Raise();
		}
		else
		{
			dispatcher.Invoke(Raise);
		}

		void Raise()
		{
			OnPropertyChanged(nameof(UpdateStatusText));
			OnPropertyChanged(nameof(CanUpdate));
		}
	}

	#endregion

	public event EventHandler CloseAppRequested;

	/// <summary>
	/// Closes this application.
	/// </summary>
	public void CloseApp() => CloseAppRequested?.Invoke(this, EventArgs.Empty);

	#region IDisposable

	private bool _isDisposed = false;

	protected override void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;

		if (disposing)
		{
			_controller.Updates.PropertyChanged -= OnUpdatesChanged;
			CloseAppRequested = null;
		}

		_isDisposed = true;

		base.Dispose(disposing);
	}

	#endregion
}