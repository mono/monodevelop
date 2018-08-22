// TargetFramework.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.IO;
using System.Collections.Generic;

using Mono.PkgConfig;
using MonoDevelop.Core.Serialization;
using System.Reflection;

namespace MonoDevelop.Core.Assemblies
{
	public class TargetFramework
	{
		TargetFrameworkMoniker id;
		string name;

		List<TargetFrameworkMoniker> includedFrameworks = new List<TargetFrameworkMoniker> ();
		List<SupportedFramework> supportedFrameworks = new List<SupportedFramework> ();

		internal bool RelationsBuilt;

		string corlibVersion;

		public static TargetFramework Default {
			get { return Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.Default); }
		}

		internal TargetFramework ()
		{
		}

		internal TargetFramework (TargetFrameworkMoniker id)
		{
			this.id = id;
			this.name = id.Profile == null
				? string.Format ("{0} {1}", id.Identifier, id.Version)
				: string.Format ("{0} {1} {2} Profile", id.Identifier, id.Version, id.Profile);
			Assemblies = new AssemblyInfo [0];
		}

		public string Name {
			get {
				if (string.IsNullOrEmpty (name)) {
					return string.IsNullOrEmpty (id.Profile)
						? string.Format ("{0} {1}", id.Identifier, id.Version)
						: string.Format ("{0} {1} ({2})", id.Identifier, id.Version, id.Profile);
				}
				return name;
			}
		}

		public TargetFrameworkMoniker Id {
			get {
				return id;
			}
		}

		[Obsolete ("It is no longer possible to define a hidden framework")]
		public bool Hidden { get; } = false;

		[Obsolete ("This value is no longer meaningful")]
		public ClrVersion ClrVersion { get; } = ClrVersion.Net_4_0;

		static bool ProfileMatchesPattern (string profile, string pattern)
		{
			if (string.IsNullOrEmpty (pattern))
				return string.IsNullOrEmpty (profile);

			int star = pattern.IndexOf ('*');

			if (star != -1) {
				if (star == 0)
					return true;

				if (string.IsNullOrEmpty (profile))
					return false;

				var prefix = pattern.Substring (0, star);
				return profile.StartsWith (prefix, StringComparison.Ordinal);
			}

			return profile == pattern;
		}

		public bool CanReferenceAssembliesTargetingFramework (TargetFrameworkMoniker fxId)
		{
			var fx = Runtime.SystemAssemblyService.GetTargetFramework (fxId);

			return fx != null && CanReferenceAssembliesTargetingFramework (fx);
		}

		/// <summary>
		/// Determines whether projects targeting this framework can reference assemblies targeting the framework specified by fx.
		/// </summary>
		/// <returns><c>true</c> if projects targeting this framework can reference assemblies targeting the framework specified by fx; otherwise, <c>false</c>.</returns>
		/// <param name="fx">The target framework</param>
		public bool CanReferenceAssembliesTargetingFramework (TargetFramework fx)
		{
			foreach (var sfx in fx.SupportedFrameworks) {
				if (sfx.Identifier != id.Identifier)
					continue;

				if (!ProfileMatchesPattern (id.Profile, sfx.Profile))
					continue;

				var version = new Version (id.Version);

				if (version >= sfx.MinimumVersion && version <= sfx.MaximumVersion)
					return true;
			}

			// HACK: allow referencing NetStandard projects
			//
			//.NETPortable,Version=v5.0 is a dummy framework. In this case, the TFM is not available
			//to MSBuild, its is only available to NuGet from the project.json.
			//
			//Additionally, there is no equivalent of SupportedFrameworks for these TFMs, the
			//relationships are hardcoded into NuGet.
			//
			//Until this is fixed, we will be very lax about what we consider compatible.
			//
			if (fx.Id.Identifier == TargetFrameworkMoniker.ID_PORTABLE && fx.Id.Version == "5.0") {
				//.NetFramework < 4.5 isn't compatible with any netstandard version
				if (Id.Identifier == TargetFrameworkMoniker.ID_NET_FRAMEWORK) {
					return new Version (Id.Version).CompareTo (new Version (4, 5)) >= 0;
				}

				//PCL < 4.5 isn't compatible with any netstandard version
				if (Id.Identifier == TargetFrameworkMoniker.ID_PORTABLE) {
					return new Version (Id.Version).CompareTo (new Version (4, 5)) >= 0;
				}

				return true;
			}

			return fx.Id.Identifier == id.Identifier && new Version (fx.Id.Version).CompareTo (new Version (id.Version)) <= 0;
		}

		internal string GetCorlibVersion ()
		{
			if (corlibVersion != null)
				return corlibVersion;

			foreach (AssemblyInfo asm in Assemblies) {
				if (asm.Name == "mscorlib")
					return corlibVersion = asm.Version;
			}
			return corlibVersion = string.Empty;
		}

		public bool IncludesFramework (TargetFrameworkMoniker id)
		{
			return id == this.id || includedFrameworks.Contains (id);
		}

		internal List<TargetFrameworkMoniker> IncludedFrameworks {
			get { return includedFrameworks; }
		}

#pragma warning disable 649
		string includesFramework;
#pragma warning restore 649

		internal TargetFrameworkMoniker GetIncludesFramework ()
		{
			if (string.IsNullOrEmpty (includesFramework))
				return null;
			string version = includesFramework [0] == 'v' ?
				includesFramework.Substring (1) : includesFramework;
			if (version.Length == 0)
				throw new InvalidOperationException ("Invalid include version in framework " + id);

			return new TargetFrameworkMoniker (id.Identifier, version);
		}

		public List<SupportedFramework> SupportedFrameworks {
			get { return supportedFrameworks; }
		}

		internal AssemblyInfo [] Assemblies {
			get;
			set;
		}

		internal string FrameworkAssembliesDirectory { get; set; }

		public override string ToString ()
		{
			return $"[TargetFramework: Name={Name}, Id={Id}]";
		}

		public static TargetFramework FromFrameworkDirectory (TargetFrameworkMoniker moniker, FilePath dir)
		{
			var fxListFile = dir.Combine ("RedistList", "FrameworkList.xml");
			var fxListInfo = new FileInfo (fxListFile);
			if (!fxListInfo.Exists)
				return null;

			var fxCacheDir = UserProfile.Current.CacheDir.Combine ("FrameworkInfo");

			var cacheKey = moniker.Identifier + "_" + moniker.Version;
			if (!string.IsNullOrEmpty (moniker.Profile)) {
				cacheKey += "_" + moniker.Profile;
			}

			FrameworkInfo fxInfo = null;

			var cachedListFile = fxCacheDir.Combine (cacheKey + ".xml");
			var cachedListInfo = new FileInfo (cachedListFile);
			if (cachedListInfo.Exists && cachedListInfo.LastWriteTime == fxListInfo.LastWriteTime) {
				fxInfo = FrameworkInfo.Load (moniker, cachedListFile);
				//if Mono was upgraded since caching, the cached location may no longer be valid
				if (!Directory.Exists (fxInfo.TargetFrameworkDirectory)) {
					fxInfo = null;
				} else if (fxInfo.SupportedFrameworks.Count > 0) {
					// Ensure DisplayName was saved for the SupportedFrameworks. If missing invalidate the
					// cache to ensure DisplayName is saved. Only check the first framework since the
					// DisplayName was not being saved previously. The DisplayName will not be empty when
					// saved even if the framework .xml file does not define it since the filename will be
					// used as the DisplayName in that case.
					if (string.IsNullOrEmpty (fxInfo.SupportedFrameworks [0].DisplayName)) {
						fxInfo = null;
					}
				}
			}

			if (fxInfo == null) {
				fxInfo = FrameworkInfo.Load (moniker, fxListFile);
				var supportedFrameworksDir = dir.Combine ("SupportedFrameworks");
				if (Directory.Exists (supportedFrameworksDir)) {
					foreach (var sfx in Directory.EnumerateFiles (supportedFrameworksDir))
						fxInfo.SupportedFrameworks.Add (SupportedFramework.Load (sfx));
				}
				if (fxInfo.Assemblies.Count == 0) {
					fxInfo.Assemblies = ScanAssemblyDirectory (moniker, fxInfo.TargetFrameworkDirectory);
				}
				Directory.CreateDirectory (fxCacheDir);
				fxInfo.Save (cachedListFile);
				File.SetLastWriteTime (cachedListFile, fxListInfo.LastWriteTime);
			}

			return new TargetFramework (moniker) {
				name = fxInfo.Name,
				includesFramework = fxInfo.IncludeFramework,
				Assemblies = fxInfo.Assemblies.ToArray (),
				supportedFrameworks = fxInfo.SupportedFrameworks,
				FrameworkAssembliesDirectory = fxInfo.TargetFrameworkDirectory
			};
		}

		static List<AssemblyInfo> ScanAssemblyDirectory (TargetFrameworkMoniker tfm, FilePath dir)
		{
			var assemblies = new List<AssemblyInfo> ();
			foreach (var f in Directory.EnumerateFiles (dir, "*.dll")) {
				try {
					var an = SystemAssemblyService.GetAssemblyNameObj (dir.Combine (f));
					var ainfo = new AssemblyInfo ();
					ainfo.Update (an);
					assemblies.Add (ainfo);
				} catch (BadImageFormatException ex) {
					LoggingService.LogError ("Invalid assembly in framework '{0}': {1}{2}{3}", tfm, f, Environment.NewLine, ex.ToString ());
				} catch (Exception ex) {
					LoggingService.LogError ("Error reading assembly '{0}' in framework '{1}':{2}{3}",
						f, tfm, Environment.NewLine, ex.ToString ());
				}
			}
			return assemblies;
		}
	}

	class AssemblyInfo
	{
		[ItemProperty ("name")]
		public string Name = null;
		
		[ItemProperty ("version")]
		public string Version = null;
		
		[ItemProperty ("publicKeyToken", DefaultValue="null")]
		public string PublicKeyToken = null;
		
		[ItemProperty ("package")]
		public string Package = null;
		
		[ItemProperty ("culture")]
		public string Culture = null;
		
		[ItemProperty ("processorArchitecture")]
		public ProcessorArchitecture ProcessorArchitecture = ProcessorArchitecture.MSIL;
		
		[ItemProperty ("inGac")]
		public bool InGac = false;
		
		public AssemblyInfo ()
		{
		}
		
		public AssemblyInfo (PackageAssemblyInfo info)
		{
			Name = info.Name;
			Version = info.Version;
			PublicKeyToken = info.PublicKeyToken;
		}
		
		public void UpdateFromFile (string file)
		{
			Update (SystemAssemblyService.GetAssemblyNameObj (file));
		}
		
		public void Update (AssemblyName aname)
		{
			Name = aname.Name;
			Version = aname.Version.ToString ();
			ProcessorArchitecture = aname.ProcessorArchitecture;
			Culture = aname.CultureInfo.Name;
			string fn = aname.ToString ();
			string key = "publickeytoken=";
			int i = fn.IndexOf (key, StringComparison.OrdinalIgnoreCase) + key.Length;
			int j = fn.IndexOf (',', i);
			if (j == -1) j = fn.Length;
			PublicKeyToken = fn.Substring (i, j - i);
		}
		
		public AssemblyInfo Clone ()
		{
			return (AssemblyInfo) MemberwiseClone ();
		}
	}
}
