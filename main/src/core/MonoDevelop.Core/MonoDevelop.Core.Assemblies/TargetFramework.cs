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

using Mono.Addins;
using Mono.PkgConfig;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;
using System.Reflection;

namespace MonoDevelop.Core.Assemblies
{
	public class TargetFramework
	{
		[ItemProperty(SerializationDataType=typeof(TargetFrameworkMonikerDataType))]
		TargetFrameworkMoniker id;
		
		[ItemProperty ("_name")]
		string name;
		
#pragma warning disable 0649
		[ItemProperty]
		bool hidden;
#pragma warning restore 0649
		
		[ItemProperty]
		ClrVersion clrVersion;

		List<TargetFrameworkMoniker> includedFrameworks = new List<TargetFrameworkMoniker> ();
		List<SupportedFramework> supportedFrameworks = new List<SupportedFramework> ();

		internal bool RelationsBuilt;
		
		string corlibVersion;
		TargetFrameworkToolsVersion toolsVersion;

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
			clrVersion = ClrVersion.Default;
			Assemblies = new AssemblyInfo[0];
		}
		
		public bool Hidden {
			get { return hidden; }
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
		
		public ClrVersion ClrVersion {
			get {
				// Always return a concrete ClrVersion, nothing that uses this can deal with ClrVersion.Default
				// If the framework didn't specify one, assume the same default as the ToolsVersion.
				if (clrVersion == ClrVersion.Default) {
					return ClrVersion.Net_4_0;
				}
				return clrVersion;
			}
		}
		
		//FIXME: this isn't really valid/useful. anything using MSBuild custom frameworks should use 4.0 tools
		internal TargetFrameworkToolsVersion GetToolsVersion ()
		{
			if (toolsVersion != TargetFrameworkToolsVersion.Unspecified)
				return toolsVersion;
			
			if (Id.Identifier == TargetFrameworkMoniker.ID_NET_FRAMEWORK) {
				switch (id.Version) {
				case "4.0":
					return TargetFrameworkToolsVersion.V4_0;
				case "3.5":
					return TargetFrameworkToolsVersion.V3_5;
				case "3.0":
				case "2.0":
					return TargetFrameworkToolsVersion.V2_0;
				case "1.1":
					return TargetFrameworkToolsVersion.V1_1;
				}
			}
			
			switch (clrVersion) {
			case MonoDevelop.Core.ClrVersion.Net_1_1:
				return TargetFrameworkToolsVersion.V1_1;
			case MonoDevelop.Core.ClrVersion.Net_2_0:
				return TargetFrameworkToolsVersion.V2_0;
			case MonoDevelop.Core.ClrVersion.Net_4_0:
				return TargetFrameworkToolsVersion.V4_0;
			}
			
			return TargetFrameworkToolsVersion.V4_0;
		}

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
				return profile.StartsWith (prefix);
			}

			return profile == pattern;
		}

		[Obsolete ("Use CanReferenceAssembliesTargetingFramework() instead")]
		public bool IsCompatibleWithFramework (TargetFrameworkMoniker fxId)
		{
			return CanReferenceAssembliesTargetingFramework (fxId);
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

			// FIXME: this is a hack for systems w/o Portable Class Library definitions
			if (fx.Id.Identifier == TargetFrameworkMoniker.ID_PORTABLE) {
				switch (id.Identifier) {
				case TargetFrameworkMoniker.ID_NET_FRAMEWORK:
					return new Version (fx.Id.Version).CompareTo (new Version (id.Version)) <= 0;
				case TargetFrameworkMoniker.ID_MONOTOUCH:
				case TargetFrameworkMoniker.ID_MONODROID:
					return true;
				}
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

		internal TargetFrameworkNode FrameworkNode { get; set; }
		
		internal TargetFrameworkBackend CreateBackendForRuntime (TargetRuntime runtime)
		{
			if (FrameworkNode == null)
				return null;
			
			lock (FrameworkNode) {
				if (FrameworkNode.ChildNodes == null)
					return null;
			}
			
			foreach (TypeExtensionNode node in FrameworkNode.ChildNodes) {
				TargetFrameworkBackend backend = (TargetFrameworkBackend) node.CreateInstance (typeof (TargetFrameworkBackend));
				if (backend.SupportsRuntime (runtime))
					return backend;
			}
			return null;
		}
		
		public bool IncludesFramework (TargetFrameworkMoniker id)
		{
			return id == this.id || includedFrameworks.Contains (id);
		}

		internal List<TargetFrameworkMoniker> IncludedFrameworks {
			get { return includedFrameworks; }
		}
				
		[ItemProperty (Name="IncludesFramework")]
		string includesFramework;
		
		internal TargetFrameworkMoniker GetIncludesFramework ()
		{
			if (string.IsNullOrEmpty (includesFramework))
				return null;
			string version = includesFramework[0] == 'v'?
				includesFramework.Substring (1) : includesFramework;
			if (version.Length == 0)
				throw new InvalidOperationException ("Invalid include version in framework " + id);
			
			return new TargetFrameworkMoniker (id.Identifier, version);	
		}
		
		public List<SupportedFramework> SupportedFrameworks {
			get { return supportedFrameworks; }
		}
		
		[ItemProperty]
		[ItemProperty ("Assembly", Scope="*")]
		internal AssemblyInfo[] Assemblies {
			get;
			set;
		}
		
		internal AssemblyInfo[] AssembliesExpanded {
			get;
			set;
		}
		
		public override string ToString ()
		{
			return string.Format ("[TargetFramework: Hidden={0}, Name={1}, Id={2}, ClrVersion={3}]",
				Hidden, Name, Id, ClrVersion);
		}
		
		public static TargetFramework FromFrameworkDirectory (TargetFrameworkMoniker moniker, FilePath dir)
		{
			var fxList = dir.Combine ("RedistList", "FrameworkList.xml");
			if (!File.Exists (fxList))
				return null;
			
			var fx = new TargetFramework (moniker);
			
			using (var reader = System.Xml.XmlReader.Create (fxList)) {
				if (!reader.ReadToDescendant ("FileList"))
					throw new Exception ("Missing FileList element");
				
				//not sure what this is for
				//if (reader.MoveToAttribute ("Redist") && reader.ReadAttributeValue ())
				//	redist = reader.ReadContentAsString ();
				
				if (reader.MoveToAttribute ("Name") && reader.ReadAttributeValue ())
					fx.name = reader.ReadContentAsString ();
				
				if (reader.MoveToAttribute ("RuntimeVersion") && reader.ReadAttributeValue ()) {
					string runtimeVersion = reader.ReadContentAsString ();
					switch (runtimeVersion) {
					case "2.0":
						fx.clrVersion = ClrVersion.Net_2_0;
						break;
					case "4.0":
						fx.clrVersion = ClrVersion.Net_4_0;
						break;
					case "4.5":
						fx.clrVersion = ClrVersion.Net_4_5;
						break;
					default:
						throw new Exception ("Unknown RuntimeVersion '" + runtimeVersion + "'");
					}
				}
				
				if (reader.MoveToAttribute ("ToolsVersion") && reader.ReadAttributeValue ()) {
					string runtimeVersion = reader.ReadContentAsString ();
					switch (runtimeVersion) {
					case "2.0":
						fx.toolsVersion = TargetFrameworkToolsVersion.V2_0;
						break;
					case "3.5":
						fx.toolsVersion = TargetFrameworkToolsVersion.V3_5;
						break;
					case "4.0":
						fx.toolsVersion = TargetFrameworkToolsVersion.V4_0;
						break;
					case "4.5":
						fx.toolsVersion = TargetFrameworkToolsVersion.V4_5;
						break;
					default:
						throw new Exception ("Unknown ToolsVersion '" + runtimeVersion + "'");
					}
				}
				
				if (reader.MoveToAttribute ("IncludeFramework") && reader.ReadAttributeValue ()) {
					string include = reader.ReadContentAsString ();
					if (!string.IsNullOrEmpty (include)) {
						fx.IncludedFrameworks.Add (new TargetFrameworkMoniker (fx.Id.Identifier, include));
					}
				}
				
				//this is a Mono-specific extension
				if (reader.MoveToAttribute ("TargetFrameworkDirectory") && reader.ReadAttributeValue ()) {
					string targetDir = reader.ReadContentAsString ();
					if (!string.IsNullOrEmpty (targetDir)) {
						targetDir = targetDir.Replace ('\\', System.IO.Path.DirectorySeparatorChar);
						dir = fxList.ParentDirectory.Combine (targetDir).FullPath;
					}
				}
				
				var assemblies = new List<AssemblyInfo> ();
				if (reader.ReadToFollowing ("File")) {
					do {
						var ainfo = new AssemblyInfo ();
						assemblies.Add (ainfo);
						if (reader.MoveToAttribute ("AssemblyName") && reader.ReadAttributeValue ())
							ainfo.Name = reader.ReadContentAsString ();
						if (string.IsNullOrEmpty (ainfo.Name))
							throw new Exception ("Missing AssemblyName attribute");
						if (reader.MoveToAttribute ("Version") && reader.ReadAttributeValue ())
							ainfo.Version = reader.ReadContentAsString ();
						if (reader.MoveToAttribute ("PublicKeyToken") && reader.ReadAttributeValue ())
							ainfo.PublicKeyToken = reader.ReadContentAsString ();
						if (reader.MoveToAttribute ("Culture") && reader.ReadAttributeValue ())
							ainfo.Culture = reader.ReadContentAsString ();
						if (reader.MoveToAttribute ("ProcessorArchitecture") && reader.ReadAttributeValue ())
							ainfo.ProcessorArchitecture = (ProcessorArchitecture)
								Enum.Parse (typeof (ProcessorArchitecture), reader.ReadContentAsString (), true);
						if (reader.MoveToAttribute ("InGac") && reader.ReadAttributeValue ())
							ainfo.InGac = reader.ReadContentAsBoolean ();
					} while (reader.ReadToFollowing ("File"));
				} else {

					// HACK: we were using EnumerateFiles but it's broken in some Mono releases
					// https://bugzilla.xamarin.com/show_bug.cgi?id=2975
					var files = System.IO.Directory.GetFiles (dir, "*.dll");
					foreach (var f in files) {
						try {
							var an = SystemAssemblyService.GetAssemblyNameObj (dir.Combine (f));
							var ainfo = new AssemblyInfo ();
							ainfo.Update (an);
							assemblies.Add (ainfo);
						} catch (Exception ex) {
							LoggingService.LogError ("Error reading name for assembly '{0}' in framework '{1}':\n{2}",
								f, fx.Id, ex.ToString ());
						}
					}
				}
				
				fx.Assemblies = assemblies.ToArray ();
			}
			
			var supportedFrameworksDir = dir.Combine ("SupportedFrameworks");
			if (Directory.Exists (supportedFrameworksDir)) {
				foreach (var sfx in Directory.GetFiles (supportedFrameworksDir))
					fx.SupportedFrameworks.Add (SupportedFramework.Load (fx, sfx));
			}
			
			return fx;
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
			int i = fn.ToLower().IndexOf (key) + key.Length;
			int j = fn.IndexOf (',', i);
			if (j == -1) j = fn.Length;
			PublicKeyToken = fn.Substring (i, j - i);
		}
		
		public AssemblyInfo Clone ()
		{
			return (AssemblyInfo) MemberwiseClone ();
		}
	}
	
	public enum TargetFrameworkToolsVersion
	{
		Unspecified,
		V1_1, //not a real MSBuild ToolsVersion, but MD internal build supports it
		V2_0,
		V3_5,
		V4_0,
		V4_5
	}
}
