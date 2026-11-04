using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Monitorian.Core.Models.Updates;

namespace Monitorian.Test;

[TestClass]
public class UpdateReleaseInfoTest
{
	[TestMethod]
	[DataRow("v0.1.3", 0, 1, 3)]
	[DataRow("0.1.3", 0, 1, 3)]
	[DataRow("V1.2.10", 1, 2, 10)]
	[DataRow("4.16.9-Installer", 4, 16, 9)]
	[DataRow("0.2.0-rc.1", 0, 2, 0)]
	public void TestTryParseVersion_Valid(string text, int major, int minor, int build)
	{
		var success = ReleaseInfo.TryParseVersion(text, out var version);

		Assert.IsTrue(success);
		Assert.AreEqual(new System.Version(major, minor, build), version);
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("latest")]
	[DataRow("v")]
	public void TestTryParseVersion_Invalid(string text)
	{
		Assert.IsFalse(ReleaseInfo.TryParseVersion(text, out _));
	}

	[TestMethod]
	public void TestNormalize()
	{
		Assert.AreEqual(new System.Version(0, 1, 2), ReleaseInfo.Normalize(new System.Version(0, 1, 2, 0)));
		Assert.AreEqual(new System.Version(0, 1, 0), ReleaseInfo.Normalize(new System.Version(0, 1)));
	}

	[TestMethod]
	[DataRow("v0.1.3", 0, 1, 3, null)]
	[DataRow("0.2.0-rc.1", 0, 2, 0, "rc.1")]
	[DataRow("V1.2.10-beta", 1, 2, 10, "beta")]
	[DataRow("4.16.9-Installer", 4, 16, 9, "Installer")]
	public void TestTryParseTag(string text, int major, int minor, int build, string preRelease)
	{
		var success = ReleaseInfo.TryParseTag(text, out var version, out var actualPreRelease);

		Assert.IsTrue(success);
		Assert.AreEqual(new System.Version(major, minor, build), version);
		Assert.AreEqual(preRelease, actualPreRelease);
	}

	private static ReleaseInfo CreateRelease(string tagName, bool prerelease)
	{
		using var document = JsonDocument.Parse($$"""
			{ "tag_name": "{{tagName}}", "prerelease": {{(prerelease ? "true" : "false")}}, "assets": [] }
			""");

		Assert.IsTrue(ReleaseInfo.TryParse(document.RootElement, out var release));
		return release;
	}

	[TestMethod]
	public void TestCompare_PrereleaseIsOlderThanStable()
	{
		var stable = CreateRelease("v0.1.3", false);
		var rc = CreateRelease("v0.1.3-rc.1", true);

		Assert.IsTrue(ReleaseInfo.Compare(rc, stable) < 0);
		Assert.IsTrue(ReleaseInfo.Compare(stable, rc) > 0);
		Assert.AreEqual(0, ReleaseInfo.Compare(stable, CreateRelease("0.1.3", false)));
	}

	[TestMethod]
	public void TestCompare_PrereleaseOrdering()
	{
		var alpha = CreateRelease("v0.1.3-alpha", true);
		var beta = CreateRelease("v0.1.3-beta", true);
		var rc1 = CreateRelease("v0.1.3-rc.1", true);
		var rc2 = CreateRelease("v0.1.3-rc.2", true);
		var numeric = CreateRelease("v0.1.3-1", true);

		Assert.IsTrue(ReleaseInfo.Compare(alpha, beta) < 0);
		Assert.IsTrue(ReleaseInfo.Compare(beta, rc1) < 0);
		Assert.IsTrue(ReleaseInfo.Compare(rc1, rc2) < 0);
		Assert.IsTrue(ReleaseInfo.Compare(numeric, alpha) < 0); // numeric < alphanumeric
	}

	[TestMethod]
	public void TestCompare_NewerPreleaseOfHigherVersionWins()
	{
		var current = CreateRelease("v0.1.2", false);
		var nextPre = CreateRelease("v0.1.3-rc.1", true);

		Assert.IsTrue(ReleaseInfo.Compare(nextPre, current) > 0);
	}

	[TestMethod]
	public void TestTryParse_PrereleaseIsDetected()
	{
		var release = CreateRelease("v0.1.3-rc.1", true);

		Assert.IsTrue(release.IsPrerelease);
		Assert.AreEqual("rc.1", release.PreRelease);
		Assert.AreEqual("0.1.3-rc.1", release.DisplayVersion);
	}

	private const string SampleJson = """
		{
		  "tag_name": "v0.1.3",
		  "html_url": "https://github.com/hashimotok-delien/MonitorianNeo/releases/tag/v0.1.3",
		  "body": "- 変更点",
		  "draft": false,
		  "assets": [
		    {
		      "name": "MonitorianNeo-win-x64.zip",
		      "browser_download_url": "https://example.com/MonitorianNeo-win-x64.zip"
		    },
		    {
		      "name": "MonitorianNeo-0.1.3-setup.exe",
		      "browser_download_url": "https://example.com/MonitorianNeo-0.1.3-setup.exe"
		    },
		    {
		      "name": "SHA256SUMS.txt",
		      "browser_download_url": "https://example.com/SHA256SUMS.txt"
		    }
		  ]
		}
		""";

	[TestMethod]
	public void TestTryParse_SelectsInstallerAndChecksum()
	{
		using var document = JsonDocument.Parse(SampleJson);

		var success = ReleaseInfo.TryParse(document.RootElement, out var release);

		Assert.IsTrue(success);
		Assert.AreEqual(new System.Version(0, 1, 3), release.Version);
		Assert.AreEqual("0.1.3", release.DisplayVersion);
		Assert.AreEqual("https://example.com/MonitorianNeo-0.1.3-setup.exe", release.InstallerUrl);
		Assert.AreEqual("MonitorianNeo-0.1.3-setup.exe", release.InstallerName);
		Assert.AreEqual("https://example.com/SHA256SUMS.txt", release.ChecksumUrl);
		Assert.AreEqual("https://github.com/hashimotok-delien/MonitorianNeo/releases/tag/v0.1.3", release.HtmlUrl);
	}

	[TestMethod]
	public void TestTryParse_DraftIsRejected()
	{
		using var document = JsonDocument.Parse("""
			{ "tag_name": "v0.1.3", "draft": true, "assets": [] }
			""");

		Assert.IsFalse(ReleaseInfo.TryParse(document.RootElement, out _));
	}

	[TestMethod]
	public void TestTryParse_NoInstaller()
	{
		using var document = JsonDocument.Parse("""
			{
			  "tag_name": "v0.1.3",
			  "draft": false,
			  "assets": [ { "name": "MonitorianNeo-win-x64.zip", "browser_download_url": "https://example.com/a.zip" } ]
			}
			""");

		Assert.IsTrue(ReleaseInfo.TryParse(document.RootElement, out var release));
		Assert.IsNull(release.InstallerUrl);
		Assert.IsNull(release.ChecksumUrl);
	}
}
