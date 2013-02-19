//
// SystemAssemblyService.cs
//
// Author:
//   Todd Berman <tberman@sevenl.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (C) 2004 Todd Berman
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using MonoDevelop.Core.AddIns;
using Mono.Addins;
using System.Reflection;
using System.Linq;

namespace MonoDevelop.Core.Assemblies
{
	public sealed class SystemAssemblyService
	{
		object frameworkWriteLock = new object ();
		Dictionary<TargetFrameworkMoniker,TargetFramework> frameworks;
		List<TargetFrameworkMoniker> coreFrameworks;
		List<TargetRuntime> runtimes;
		TargetRuntime defaultRuntime;
		DirectoryAssemblyContext userAssemblyContext = new DirectoryAssemblyContext ();
		
		public TargetRuntime CurrentRuntime { get; private set; }
		
		public event EventHandler DefaultRuntimeChanged;
		public event EventHandler RuntimesChanged;
		
		internal void Initialize ()
		{
			CreateFrameworks ();
			runtimes = new List<TargetRuntime> ();
			foreach (ITargetRuntimeFactory factory in AddinManager.GetExtensionObjects ("/MonoDevelop/Core/Runtimes", typeof(ITargetRuntimeFactory))) {
				foreach (TargetRuntime runtime in factory.CreateRuntimes ()) {
					runtimes.Add (runtime);
					if (runtime.IsRunning)
						DefaultRuntime = CurrentRuntime = runtime;
				}
			}
			
			// Don't initialize until Current and Default Runtimes are set
			foreach (TargetRuntime runtime in runtimes) {
				runtime.Initialized += HandleRuntimeInitialized;
			}

			if (CurrentRuntime == null)
				LoggingService.LogFatalError ("Could not create runtime info for current runtime");

			CurrentRuntime.StartInitialization ();
			
			LoadUserAssemblyContext ();
			userAssemblyContext.Changed += delegate {
				SaveUserAssemblyContext ();
			};
		}
		
		void InitializeRuntime (TargetRuntime runtime)
		{
			runtime.Initialized += HandleRuntimeInitialized;
			runtime.StartInitialization ();
		}
		
		void HandleRuntimeInitialized (object sender, EventArgs e)
		{
			var runtime = (TargetRuntime) sender;
			lock (frameworkWriteLock) {
				if (runtime.CustomFrameworks.Any ()) {
					var newFxList = new Dictionary<TargetFrameworkMoniker,TargetFramework> (frameworks);
					foreach (var fx in runtime.CustomFrameworks) {
						TargetFramework existing;
						if (!newFxList.TryGetValue (fx.Id, out existing) || existing.Assemblies.Length == 0) {
							newFxList [fx.Id] = fx;
						}
					}
					BuildFrameworkRelations (newFxList);
					frameworks = newFxList;
				}
			}
		}
		
		public TargetRuntime DefaultRuntime {
			get {
				return defaultRuntime;
			}
			set {
				defaultRuntime = value;
				if (DefaultRuntimeChanged != null)
					DefaultRuntimeChanged (this, EventArgs.Empty);
			}
		}
		
		public DirectoryAssemblyContext UserAssemblyContext {
			get { return userAssemblyContext; }
		}
		
		public IAssemblyContext DefaultAssemblyContext {
			get { return DefaultRuntime.AssemblyContext; }
		}
		
		public void RegisterRuntime (TargetRuntime runtime)
		{
			runtime.Initialized += HandleRuntimeInitialized;
			runtimes.Add (runtime);
			if (RuntimesChanged != null)
				RuntimesChanged (this, EventArgs.Empty);
		}
		
		public void UnregisterRuntime (TargetRuntime runtime)
		{
			if (runtime == CurrentRuntime)
				return;
			DefaultRuntime = CurrentRuntime;
			runtimes.Remove (runtime);
			runtime.Initialized -= HandleRuntimeInitialized;
			if (RuntimesChanged != null)
				RuntimesChanged (this, EventArgs.Empty);
		}
		
		internal IEnumerable<TargetFramework> GetKnownFrameworks ()
		{
			return frameworks.Values;
		}

		internal bool IsKnownFramework (TargetFrameworkMoniker moniker)
		{
			return frameworks.ContainsKey (moniker);
		}
		
		public IEnumerable<TargetFramework> GetTargetFrameworks ()
		{
			return frameworks.Values;
		}
		
		public IEnumerable<TargetRuntime> GetTargetRuntimes ()
		{
			return runtimes;
		}
		
		public TargetRuntime GetTargetRuntime (string id)
		{
			foreach (TargetRuntime r in runtimes) {
				if (r.Id == id)
					return r;
			}
			return null;
		}

		public IEnumerable<TargetRuntime> GetTargetRuntimes (string runtimeId)
		{
			foreach (TargetRuntime r in runtimes) {
				if (r.RuntimeId == runtimeId)
					yield return r;
			}
		}
		
		public TargetFramework GetTargetFramework (TargetFrameworkMoniker id)
		{
			return GetTargetFramework (id, frameworks);
		}
		
		//HACK: this is so that MonoTargetRuntime can access the core frameworks while it's doing its broken assembly->framework mapping
		internal TargetFramework GetCoreFramework (TargetFrameworkMoniker id)
		{
			return GetTargetFramework (id, frameworks);
		}
		
		static TargetFramework GetTargetFramework (TargetFrameworkMoniker id, Dictionary<TargetFrameworkMoniker, TargetFramework> frameworks)
		{
			TargetFramework fx;
			if (frameworks.TryGetValue (id, out fx))
				return fx;
			LoggingService.LogWarning ("Unregistered TargetFramework '{0}' is being requested from SystemAssemblyService", id);
			fx = new TargetFramework (id);
			frameworks[id] = fx;
			return fx;
		}
		
		public SystemPackage GetPackageFromPath (string assemblyPath)
		{
			foreach (TargetRuntime r in runtimes) {
				SystemPackage p = r.AssemblyContext.GetPackageFromPath (assemblyPath);
				if (p != null)
					return p;
			}
			return null;
		}

		public static System.Reflection.AssemblyName ParseAssemblyName (string fullname)
		{
			var aname = new System.Reflection.AssemblyName ();
			int i = fullname.IndexOf (',');
			if (i == -1) {
				aname.Name = fullname.Trim ();
				return aname;
			}
			
			aname.Name = fullname.Substring (0, i).Trim ();
			i = fullname.IndexOf ("Version", i+1);
			if (i == -1)
				return aname;
			i = fullname.IndexOf ('=', i);
			if (i == -1) 
				return aname;
			int j = fullname.IndexOf (',', i);
			if (j == -1)
				aname.Version = new Version (fullname.Substring (i+1).Trim ());
			else
				aname.Version = new Version (fullname.Substring (i+1, j - i - 1).Trim ());
			return aname;
		}
		
		static Dictionary<string, AssemblyName> assemblyNameCache = new Dictionary<string, AssemblyName> ();
		internal static AssemblyName GetAssemblyNameObj (string file)
		{
			AssemblyName name;

			lock (assemblyNameCache) {
				if (assemblyNameCache.TryGetValue (file, out name))
					return name;
			}

			try {
				name = AssemblyName.GetAssemblyName (file);
				lock (assemblyNameCache) {
					assemblyNameCache [file] = name;
				}
				return name;
			} catch (FileNotFoundException) {
				// GetAssemblyName is not case insensitive in mono/windows. This is a workaround
				foreach (string f in Directory.GetFiles (Path.GetDirectoryName (file), Path.GetFileName (file))) {
					if (f != file) {
						GetAssemblyNameObj (f);
						return assemblyNameCache [file];
					}
				}
				throw;
			}
		}
		
		public static string GetAssemblyName (string file)
		{
			return AssemblyContext.NormalizeAsmName (GetAssemblyNameObj (file).ToString ());
		}
		
		void CreateFrameworks ()
		{
			frameworks = new Dictionary<TargetFrameworkMoniker, TargetFramework> ();
			coreFrameworks = new List<TargetFrameworkMoniker> ();
			foreach (TargetFrameworkNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/Core/Frameworks")) {
				try {
					TargetFramework fx = node.CreateFramework ();
					if (frameworks.ContainsKey (fx.Id)) {
						LoggingService.LogError ("Duplicate framework '" + fx.Id + "'");
						continue;
					}
					coreFrameworks.Add (fx.Id);
					frameworks[fx.Id] = fx;
				} catch (Exception ex) {
					LoggingService.LogError ("Could not load framework '" + node.Id + "'", ex);
				}
			}
			
			BuildFrameworkRelations (frameworks);
		}
		
		static void BuildFrameworkRelations (Dictionary<TargetFrameworkMoniker, TargetFramework> frameworks)
		{
			foreach (TargetFramework fx in frameworks.Values)
				BuildFrameworkRelations (fx, frameworks);
		}
		
		static void BuildFrameworkRelations (TargetFramework fx, Dictionary<TargetFrameworkMoniker, TargetFramework> frameworks)
		{
			if (fx.RelationsBuilt)
				return;
			
			var includesFramework = fx.GetIncludesFramework ();
			if (includesFramework != null) {
				fx.IncludedFrameworks.Add (includesFramework);
				TargetFramework compatFx = GetTargetFramework (includesFramework, frameworks);
				BuildFrameworkRelations (compatFx, frameworks);
				fx.IncludedFrameworks.AddRange (compatFx.IncludedFrameworks);
			}
			
			fx.RelationsBuilt = true;
		}
		
		//FIXME: this is totally broken. assemblies can't just belong to one framework
		public TargetFrameworkMoniker GetTargetFrameworkForAssembly (TargetRuntime tr, string file)
		{
			var universe = new IKVM.Reflection.Universe ();
			universe.EnableMissingMemberResolution ();
			try {
				IKVM.Reflection.Assembly assembly = universe.LoadFile (file);
				var att = assembly.CustomAttributes.FirstOrDefault (a =>
					a.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute"
				);
				if (att != null) {
					if (att.ConstructorArguments.Count == 1) {
						var v = att.ConstructorArguments[0].Value as string;
						TargetFrameworkMoniker m;
						if (v != null && TargetFrameworkMoniker.TryParse (v, out m)) {
							return m;
						}
					}
					LoggingService.LogError ("Invalid TargetFrameworkAttribute in assembly {0}", file);
				}

				foreach (var r in assembly.GetReferencedAssemblies ()) {
					if (r.Name == "mscorlib") {
						TargetFramework compatibleFramework = null;
						// If there are several frameworks that can run the file, pick one that is installed
						foreach (TargetFramework tf in GetKnownFrameworks ()) {
							if (tf.GetCorlibVersion () == r.Version.ToString ()) {
								compatibleFramework = tf;
								if (tr.IsInstalled (tf))
									return tf.Id;
							}
						}
						if (compatibleFramework != null)
							return compatibleFramework.Id;
						break;
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error to determine target framework for assembly {0}: {1}", file, ex);
				return TargetFrameworkMoniker.UNKNOWN;
			} finally {
				universe.Dispose ();
			}
			LoggingService.LogError ("Failed to determine target framework for assembly {0}", file);
			return TargetFrameworkMoniker.UNKNOWN;
		}
		
		void SaveUserAssemblyContext ()
		{
			List<string> list = new List<string> (userAssemblyContext.Directories);
			PropertyService.Set ("MonoDevelop.Core.Assemblies.UserAssemblyContext", list);
			PropertyService.SaveProperties ();
		}
		
		void LoadUserAssemblyContext ()
		{
			List<string> dirs = PropertyService.Get<List<string>> ("MonoDevelop.Core.Assemblies.UserAssemblyContext");
			if (dirs != null)
				userAssemblyContext.Directories = dirs;
		}

		/// <summary>
		/// Simply get all assembly reference names from an assembly given it's file name.
		/// </summary>
		public static IEnumerable<string> GetAssemblyReferences (string fileName)
		{
			using (var universe = new IKVM.Reflection.Universe ()) {
				IKVM.Reflection.Assembly assembly;
				try {
					assembly = universe.LoadFile (fileName);
				} catch {
					yield break;
				}
				foreach (var r in assembly.GetReferencedAssemblies ()) {
					yield return r.Name;
				}
			}
		}

		public class ManifestResource
		{
			public string Name {
				get; private set;
			}

			Func<Stream> streamCallback;
			public Stream Open ()
			{
				return streamCallback ();
			}

			public ManifestResource (string name, Func<Stream> streamCallback)
			{
				this.streamCallback = streamCallback;
				Name = name;
			}
		}

		/// <summary>
		/// Simply get all assembly manifest resources from an assembly given it's file name.
		/// </summary>
		public static IEnumerable<ManifestResource> GetAssemblyManifestResources (string fileName)
		{
			using (var universe = new IKVM.Reflection.Universe ()) {
				IKVM.Reflection.Assembly assembly;
				try {
					assembly = universe.LoadFile (fileName);
				} catch {
					yield break;
				}
				foreach (var _r in assembly.GetManifestResourceNames ()) {
					var r = _r;
					yield return new ManifestResource (r, () => assembly.GetManifestResourceStream (r));
				}
			}
		}

	}
}
