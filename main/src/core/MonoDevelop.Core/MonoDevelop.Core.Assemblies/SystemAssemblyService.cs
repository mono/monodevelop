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
using System.Threading;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using Mono.Cecil;

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
				InitializeRuntime (runtime);
			}
			
			if (CurrentRuntime == null)
				LoggingService.LogFatalError ("Could not create runtime info for current runtime");
			
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
			runtime.Initialized -= HandleRuntimeInitialized;
			lock (frameworkWriteLock) {
				var newFxList = new Dictionary<TargetFrameworkMoniker,TargetFramework> (frameworks);
				foreach (var fx in runtime.CustomFrameworks) {
					if (!newFxList.ContainsKey (fx.Id))
						newFxList[fx.Id] = fx;
				}
				BuildFrameworkRelations (newFxList);
				frameworks = newFxList;
			}
		}
		
		//we initialize runtimes in threads, but consumers of this service aren't aware that runtimes
		//can be in an uninialized state, so we consider the initialization as purely an opportunistic
		//attempt at startup parallization, and block as soon as anything actually tries to access the
		//runtime objects
		void CheckRuntimesInitialized ()
		{
			foreach (var r in runtimes) {
				if (!r.IsInitialized)
					r.Initialize ();
			}
		}
		
		public TargetRuntime DefaultRuntime {
			get {
				CheckRuntimesInitialized ();
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
			InitializeRuntime (runtime);
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
			if (RuntimesChanged != null)
				RuntimesChanged (this, EventArgs.Empty);
		}
		
		internal IEnumerable<TargetFramework> GetCoreFrameworks ()
		{
			foreach (var id in coreFrameworks)
				yield return frameworks[id];
		}
		
		public IEnumerable<TargetFramework> GetTargetFrameworks ()
		{
			CheckRuntimesInitialized ();
			return frameworks.Values;
		}
		
		public IEnumerable<TargetRuntime> GetTargetRuntimes ()
		{
			CheckRuntimesInitialized ();
			return runtimes;
		}
		
		public TargetRuntime GetTargetRuntime (string id)
		{
			CheckRuntimesInitialized ();
			foreach (TargetRuntime r in runtimes) {
				if (r.Id == id)
					return r;
			}
			return null;
		}

		public IEnumerable<TargetRuntime> GetTargetRuntimes (string runtimeId)
		{
			CheckRuntimesInitialized ();
			foreach (TargetRuntime r in runtimes) {
				if (r.RuntimeId == runtimeId)
					yield return r;
			}
		}
		
		public TargetFramework GetTargetFramework (TargetFrameworkMoniker id)
		{
			CheckRuntimesInitialized ();
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
			CheckRuntimesInitialized ();
			foreach (TargetRuntime r in runtimes) {
				SystemPackage p = r.AssemblyContext.GetPackageFromPath (assemblyPath);
				if (p != null)
					return p;
			}
			return null;
		}

		public static AssemblyName ParseAssemblyName (string fullname)
		{
			AssemblyName aname = new AssemblyName ();
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
		
		internal static System.Reflection.AssemblyName GetAssemblyNameObj (string file)
		{
			try {
				AssemblyDefinition asm = AssemblyDefinition.ReadAssembly (file);
				return new AssemblyName (asm.Name.FullName);
				
				// Don't use reflection to get the name since it is a common cause for deadlocks
				// in Mono < 2.6.
				// return System.Reflection.AssemblyName.GetAssemblyName (file);
				
			} catch (FileNotFoundException) {
				// GetAssemblyName is not case insensitive in mono/windows. This is a workaround
				foreach (string f in Directory.GetFiles (Path.GetDirectoryName (file), Path.GetFileName (file))) {
					if (f != file)
						return GetAssemblyNameObj (f);
				}
				throw;
			} catch (BadImageFormatException) {
				AssemblyDefinition asm = AssemblyDefinition.ReadAssembly (file);
				return new AssemblyName (asm.Name.FullName);
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
		// also, it currently only resolves assemblies against the core frameworks
		public TargetFrameworkMoniker GetTargetFrameworkForAssembly (TargetRuntime tr, string file)
		{
			try {
				AssemblyDefinition asm = AssemblyDefinition.ReadAssembly (file);

				foreach (AssemblyNameReference aname in asm.MainModule.AssemblyReferences) {
					if (aname.Name == "mscorlib") {
						foreach (TargetFramework tf in GetCoreFrameworks ()) {
							if (tf.GetCorlibVersion () == aname.Version.ToString ())
								return tf.Id;
						}
						break;
					}
				}
			} catch {
				// Ignore
			}
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
	}
}