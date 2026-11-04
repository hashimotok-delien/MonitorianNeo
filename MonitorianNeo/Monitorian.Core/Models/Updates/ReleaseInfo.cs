using System;
using System.Linq;
using System.Text.Json;

namespace Monitorian.Core.Models.Updates;

/// <summary>
/// Information of a GitHub release
/// </summary>
public class ReleaseInfo
{
	/// <summary>
	/// Token used to prefer an asset of this product when multiple installers exist
	/// </summary>
	private const string ProductToken = "MonitorianNeo";

	public Version Version { get; private init; }

	/// <summary>
	/// Whether this release is a pre-release
	/// </summary>
	public bool IsPrerelease { get; private init; }

	/// <summary>
	/// Tag name (e.g. v0.1.3)
	/// </summary>
	public string TagName { get; private init; }

	/// <summary>
	/// Displayable version (e.g. 0.1.3)
	/// </summary>
	public string DisplayVersion { get; private init; }

	/// <summary>
	/// URL of the release page
	/// </summary>
	public string HtmlUrl { get; private init; }

	/// <summary>
	/// Release notes
	/// </summary>
	public string Body { get; private init; }

	/// <summary>
	/// Download URL of the installer asset (null if not available)
	/// </summary>
	public string InstallerUrl { get; private init; }

	/// <summary>
	/// File name of the installer asset
	/// </summary>
	public string InstallerName { get; private init; }

	/// <summary>
	/// Download URL of the checksum file (null if not available)
	/// </summary>
	public string ChecksumUrl { get; private init; }

	/// <summary>
	/// Parses a JSON document of a GitHub release.
	/// </summary>
	public static bool TryParse(JsonElement root, out ReleaseInfo release)
	{
		release = null;

		var tagName = root.TryGetProperty("tag_name", out var tagElement) ? tagElement.GetString() : null;
		if (!TryParseVersion(tagName, out var version))
			return false;

		if (root.TryGetProperty("draft", out var draftElement) && draftElement.GetBoolean())
			return false;

		string installerUrl = null;
		string installerName = null;
		string checksumUrl = null;

		if (root.TryGetProperty("assets", out var assetsElement) && (assetsElement.ValueKind is JsonValueKind.Array))
		{
			foreach (var asset in assetsElement.EnumerateArray())
			{
				var name = asset.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
				var url = asset.TryGetProperty("browser_download_url", out var urlElement) ? urlElement.GetString() : null;
				if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
					continue;

				if (name.EndsWith("-setup.exe", StringComparison.OrdinalIgnoreCase))
				{
					// Prefer the asset which contains the product name.
					if ((installerUrl is null) || name.Contains(ProductToken, StringComparison.OrdinalIgnoreCase))
					{
						installerUrl = url;
						installerName = name;
					}
				}
				else if (string.Equals(name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase))
				{
					checksumUrl = url;
				}
			}
		}

		release = new ReleaseInfo
		{
			Version = version,
			IsPrerelease = root.TryGetProperty("prerelease", out var prereleaseElement) && prereleaseElement.GetBoolean(),
			TagName = tagName,
			DisplayVersion = tagName.TrimStart('v', 'V'),
			HtmlUrl = root.TryGetProperty("html_url", out var htmlElement) ? htmlElement.GetString() : null,
			Body = root.TryGetProperty("body", out var bodyElement) ? bodyElement.GetString() : null,
			InstallerUrl = installerUrl,
			InstallerName = installerName,
			ChecksumUrl = checksumUrl,
		};
		return true;
	}

	/// <summary>
	/// Parses a tag name such as "v0.1.3" or "0.1.3" into a version.
	/// </summary>
	public static bool TryParseVersion(string text, out Version version)
	{
		version = null;
		if (string.IsNullOrWhiteSpace(text))
			return false;

		var value = text.Trim().TrimStart('v', 'V');

		// Drop a pre-release suffix such as "0.2.0-rc.1".
		var hyphenIndex = value.IndexOf('-');
		if (hyphenIndex >= 0)
			value = value[..hyphenIndex];

		if (!Version.TryParse(value, out var parsed))
			return false;

		version = Normalize(parsed);
		return true;
	}

	/// <summary>
	/// Normalizes a version to 3 components so that versions can be compared.
	/// </summary>
	public static Version Normalize(Version version) =>
		new(version.Major, version.Minor, Math.Max(version.Build, 0));
}
