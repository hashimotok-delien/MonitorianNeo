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
	/// Pre-release label (e.g. "rc.1"). Null for a stable release.
	/// </summary>
	public string PreRelease { get; private init; }

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
		if (!TryParseTag(tagName, out var version, out var preRelease))
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
			PreRelease = preRelease,
			IsPrerelease = (preRelease is not null) ||
				(root.TryGetProperty("prerelease", out var prereleaseElement) && prereleaseElement.GetBoolean()),
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
	public static bool TryParseVersion(string text, out Version version) =>
		TryParseTag(text, out version, out _);

	/// <summary>
	/// Parses a tag name such as "v0.1.3" or "0.2.0-rc.1" into a version and a pre-release label.
	/// </summary>
	public static bool TryParseTag(string text, out Version version, out string preRelease)
	{
		version = null;
		preRelease = null;
		if (string.IsNullOrWhiteSpace(text))
			return false;

		var value = text.Trim().TrimStart('v', 'V');

		// Split a pre-release suffix such as "0.2.0-rc.1".
		var hyphenIndex = value.IndexOf('-');
		if (hyphenIndex >= 0)
		{
			var suffix = value[(hyphenIndex + 1)..].Trim();
			if (suffix.Length > 0)
				preRelease = suffix;

			value = value[..hyphenIndex];
		}

		if (!Version.TryParse(value, out var parsed))
			return false;

		version = Normalize(parsed);
		return true;
	}

	/// <summary>
	/// Compares two releases following the precedence rules of Semantic Versioning.
	/// </summary>
	/// <returns>Negative if x is older, positive if x is newer, zero if identical</returns>
	public static int Compare(ReleaseInfo x, ReleaseInfo y)
	{
		ArgumentNullException.ThrowIfNull(x);
		ArgumentNullException.ThrowIfNull(y);

		var result = x.Version.CompareTo(y.Version);
		if (result != 0)
			return result;

		return ComparePreRelease(x.PreRelease, y.PreRelease);
	}

	private static int ComparePreRelease(string x, string y)
	{
		var xIsEmpty = string.IsNullOrEmpty(x);
		var yIsEmpty = string.IsNullOrEmpty(y);

		// A stable release has a higher precedence than a pre-release of the same version.
		if (xIsEmpty && yIsEmpty)
			return 0;
		if (xIsEmpty)
			return 1;
		if (yIsEmpty)
			return -1;

		var xIdentifiers = x.Split('.');
		var yIdentifiers = y.Split('.');

		for (var i = 0; i < Math.Max(xIdentifiers.Length, yIdentifiers.Length); i++)
		{
			if (i >= xIdentifiers.Length)
				return -1;
			if (i >= yIdentifiers.Length)
				return 1;

			var xIsNumeric = int.TryParse(xIdentifiers[i], out var xNumber);
			var yIsNumeric = int.TryParse(yIdentifiers[i], out var yNumber);

			// Numeric identifiers always have lower precedence than alphanumeric ones.
			var result = (xIsNumeric, yIsNumeric) switch
			{
				(true, true) => xNumber.CompareTo(yNumber),
				(true, false) => -1,
				(false, true) => 1,
				_ => string.CompareOrdinal(xIdentifiers[i], yIdentifiers[i]),
			};

			if (result != 0)
				return result;
		}

		return 0;
	}

	/// <summary>
	/// Normalizes a version to 3 components so that versions can be compared.
	/// </summary>
	public static Version Normalize(Version version) =>
		new(version.Major, version.Minor, Math.Max(version.Build, 0));
}
