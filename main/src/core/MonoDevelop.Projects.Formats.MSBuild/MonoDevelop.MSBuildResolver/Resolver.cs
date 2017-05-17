//
// MyClass.cs
//
// Author:
//       lluis <>
//
// Copyright (c) 2017 ${CopyrightHolder}
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Microsoft.Build.Framework;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.MSBuildResolver
{
	class Resolver: SdkResolver
	{
		static IEnumerable<SdkInfo> sdks;

		Func<IEnumerable<SdkInfo>> sdkFetcher;

		public override string Name => "MonoDevelop Resolver";

		public override int Priority => 0;

		public Resolver (Func<IEnumerable<SdkInfo>> sdkFetcher = null)
		{
			this.sdkFetcher = sdkFetcher ?? LoadSdks;
		}

		static IEnumerable<SdkInfo> LoadSdks ()
		{
			if (sdks != null)
				return sdks;
			
			// Load paths from a file located in the same folder of the assembly
			var file = Path.Combine (Path.GetDirectoryName (typeof (Resolver).Assembly.Location), "sdks.txt");
			return sdks = SdkInfo.Load (file);
		}

		public override SdkResult Resolve (SdkReference sdkReference, SdkResolverContext resolverContext, SdkResultFactory factory)
		{
			Version.TryParse (sdkReference.MinimumVersion, out Version minVersion);
			SdkInfo bestSdk = null;

			// Pick the SDK with the highest version

			foreach (var sdk in sdkFetcher ()) {
				if (sdk.Sdk == sdkReference.Name) {
					if (sdk.Version != null) {
						// If the sdk has a version, it must satisfy the min version requirement
						if (minVersion != null && sdk.Version < minVersion)
							continue;
						if (bestSdk?.Version == null || bestSdk.Version < sdk.Version)
							bestSdk = sdk;
					} else {
						// Pick this sdk for now, even if it has no version info
						if (bestSdk == null)
							bestSdk = sdk;
					}
				}
			}
			if (bestSdk != null)
				return factory.IndicateSuccess (bestSdk.Path, bestSdk.Version?.ToString ());
			else
				return factory.IndicateFailure (new string [] { "SDK not found" });
		}
	}

	class SdkInfo
	{
		public string Sdk { get; set; }
		public Version Version { get; set; }
		public string Path { get; set; }

		public static void Save (string file, SdkInfo[] sdks)
		{
			using (var sw = new StreamWriter (file)) {
				foreach (var sdk in sdks)
					sw.WriteLine ($"{sdk.Sdk},{sdk.Version}:{sdk.Path}");
			}
		}

		public static SdkInfo[] Load (string file)
		{
			if (!File.Exists (file))
				return new SdkInfo [0];
			
			var sdks = new List<SdkInfo> ();
			using (var sr = new StreamReader (file)) {
				string line;
				while ((line = sr.ReadLine ()) != null) {
					var sdkInfo = new SdkInfo ();
					int i = line.IndexOf (':');
					sdkInfo.Path = line.Substring (i + 1);
					var sdk = line.Substring (0, i);
					i = sdk.IndexOf (',');
					sdkInfo.Sdk = sdk.Substring (0, i);
					var ver = sdk.Substring (i + 1);
					if (ver.Length > 0) {
						Version.TryParse (ver, out Version v);
						sdkInfo.Version = v;
					}
					sdks.Add (sdkInfo);
				}
			}
			return sdks.ToArray ();
		}
	}

}
