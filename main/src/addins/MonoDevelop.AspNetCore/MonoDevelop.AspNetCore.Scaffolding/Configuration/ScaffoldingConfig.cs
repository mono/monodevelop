using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.WebTools.Scaffolding.Core.Config
{
	class ScaffoldingConfig
	{
		public static string ConfigPath { get; private set; } = Path.GetDirectoryName (typeof (ScaffoldingConfig).Assembly.Location);

		public string Version { get; set; }

		// LTS10, FTS11, NetStandard20, NetStandard21, and Net22 packages are set up as they are to maintain backwards compat.
		// They were (are) explicitly named sections before the config file format was generalized to support arbitrary support policy versions.
		public PackageDescription [] LTS10Packages { get; set; }

		public PackageDescription [] FTS11Packages { get; set; }

		public PackageDescription [] NetStandard20Packages { get; set; }

		public PackageDescription [] NetStandard21Packages { get; set; }

		public PackageDescription [] Net22Packages { get; set; }

		// This is public so the Json deserialization works (and for testing).
		// The data should be accessed via TryGetPackagesForSupportPolicyVersion
		[JsonProperty]
		public Dictionary<string, PackageDescription []> DynamicVersionedPackages { get; set; }

		public bool TryGetPackagesForSupportPolicyVersion (SupportPolicyVersion supportPolicyVersion, out PackageDescription [] packageDescriptions)
		{
			if (supportPolicyVersion == null || supportPolicyVersion.Version == null) {
				packageDescriptions = null;
				return false;
			}

			if (supportPolicyVersion == SupportPolicyVersion.LTS10) {
				packageDescriptions = LTS10Packages;
				return true;
			}
			if (supportPolicyVersion == SupportPolicyVersion.FTS11) {
				packageDescriptions = FTS11Packages;
				return true;
			}
			if (supportPolicyVersion == SupportPolicyVersion.NetStandard20) {
				packageDescriptions = NetStandard20Packages;
				return true;
			}
			if (supportPolicyVersion == SupportPolicyVersion.NetStandard21) {
				packageDescriptions = NetStandard21Packages;
				return true;
			}
			if (supportPolicyVersion == SupportPolicyVersion.Net220) {
				packageDescriptions = Net22Packages;
				return true;
			}

			if (DynamicVersionedPackages != null && DynamicVersionedPackages.TryGetValue (supportPolicyVersion.Version.ToString (), out packageDescriptions)) {
				return true;
			}

			packageDescriptions = null;
			return false;
		}

		static ScaffoldingConfig fetchedConfig;
		// This url will go live for 16.4
		static string packageVersionsUrl = "https://webpifeed.blob.core.windows.net/webpifeed/partners/scaffoldingpackageversions_2108718.json";

		public static async Task<ScaffoldingConfig> LoadFromJsonAsync ()
		{
			if(fetchedConfig == null) {
				Stream stream;
				using var httpClient = new HttpClient ();

				try {
					stream = await httpClient.GetStreamAsync (packageVersionsUrl);
				} catch {
					// fallback to embedded resource
					stream = typeof (ScaffoldingConfig).Assembly.GetManifestResourceStream ("ScaffoldingPackageVersions.json");
				}
				
				using var streamReader = new StreamReader (stream);
				var json = await streamReader.ReadToEndAsync ();
				fetchedConfig = JsonConvert.DeserializeObject<ScaffoldingConfig> (json);
			}
			return fetchedConfig;
		}
	}
}
