﻿//
// Resolver.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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

namespace MonoDevelop.Projects.MSBuild
{
	// This resolver is used to resolve SDKs provided by MonoDevelop add-ins.
	// The class is used for two things:
	// As part of MonoDevelop.Core, it is used by the internal msbuild project evaluator. In this case
	// the resolver is created with an 'sdkFetcher' that returns the sdks currently registered
	// by add-ins.
	// The class is also used to generate a resolver assembly that is copied to the local copy
	// of the msbuild environment that MD uses to build projects. In that case the list of
	// sdks is loaded from a sdks.config file that is generated when the msbuild environment is created.

#if PUBLIC_API
	public
#endif
	class Resolver: SdkResolver
	{
		static IEnumerable<SdkInfo> sdks;

		Func<IEnumerable<SdkInfo>> sdkFetcher;
		Func<string, string> getLocalizedString = s => s;

		public override string Name => "MonoDevelop Resolver";

		// MonoDevelop sdks have the highest priority
		public override int Priority => 0;

		public Resolver ()
		{
			this.sdkFetcher = LoadSdks;
		}

		internal Resolver (Func<IEnumerable<SdkInfo>> sdkFetcher = null, Func<string, string> getLocalizedString = null)
		{
			this.sdkFetcher = sdkFetcher;
			if (getLocalizedString != null)
				this.getLocalizedString = getLocalizedString;
		}

		static IEnumerable<SdkInfo> LoadSdks ()
		{
			if (sdks != null)
				return sdks;
			
			// Load paths from a file located in the same folder of the assembly
			var file = Path.Combine (Path.GetDirectoryName (typeof (Resolver).Assembly.Location), "sdks.config");
			return sdks = SdkInfo.LoadConfig (file);
		}

		public override SdkResult Resolve (SdkReference sdkReference, SdkResolverContext resolverContext, SdkResultFactory factory)
		{
			SdkVersion.TryParse (sdkReference.MinimumVersion, out SdkVersion minVersion);
			SdkInfo bestSdk = null;

			// Pick the SDK with the highest version

			foreach (var sdk in sdkFetcher ()) {
				if (StringComparer.OrdinalIgnoreCase.Equals (sdk.Name, sdkReference.Name)) {
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
				return factory.IndicateFailure (new string [] { getLocalizedString ("SDK not found") });
		}
	}

	class SdkInfo
	{
		public string Name { get; private set; }
		public SdkVersion Version { get; private set; }
		public string Path { get; private set; }

		SdkInfo ()
		{
		}

		public SdkInfo (string name, SdkVersion version, string path)
		{
			Name = name;
			Version = version;
			Path = path;
		}

		internal static void SaveConfig (string file, IEnumerable<SdkInfo> sdks)
		{
			using (var sw = new StreamWriter (file)) {
				foreach (var sdk in sdks)
					sw.WriteLine ($"{sdk.Name},{sdk.Version}:{sdk.Path}");
			}
		}

		internal static SdkInfo[] LoadConfig (string file)
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
					sdkInfo.Name = sdk.Substring (0, i);
					var ver = sdk.Substring (i + 1);
					if (ver.Length > 0) {
						SdkVersion.TryParse (ver, out SdkVersion v);
						sdkInfo.Version = v;
					}
					sdks.Add (sdkInfo);
				}
			}
			return sdks.ToArray ();
		}
	}

}
