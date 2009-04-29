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

namespace MonoDevelop.Core.Assemblies
{
	public class SystemAssemblyService
	{
		List<TargetFramework> frameworks;
		List<TargetRuntime> runtimes;
		TargetRuntime defaultRuntime;
		internal static string ReferenceFrameworksFile;
		internal static string GeneratedFrameworksFile;
		
		public TargetRuntime CurrentRuntime { get; private set; }
		
		public event EventHandler DefaultRuntimeChanged;
		public event EventHandler RuntimesChanged;
		
		internal SystemAssemblyService ()
		{
			ReferenceFrameworksFile = Environment.GetEnvironmentVariable ("MONODEVELOP_FRAMEWORKS_FILE");
			GeneratedFrameworksFile = Environment.GetEnvironmentVariable ("MONODEVELOP_FRAMEWORKS_OUTFILE");
		}
		
		internal void Initialize ()
		{
			CreateFrameworks ();
			runtimes = new List<TargetRuntime> ();
			foreach (ITargetRuntimeFactory factory in AddinManager.GetExtensionObjects ("/MonoDevelop/Core/Runtimes", typeof(ITargetRuntimeFactory))) {
				foreach (TargetRuntime runtime in factory.CreateRuntimes ()) {
					RegisterRuntime (runtime);
					if (runtime.IsRunning)
						DefaultRuntime = CurrentRuntime = runtime;
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
		
		public void RegisterRuntime (TargetRuntime runtime)
		{
			runtime.StartInitialization ();
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
		
		public IEnumerable<TargetFramework> GetTargetFrameworks ()
		{
			return frameworks;
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

		public TargetFramework GetTargetFramework (string id)
		{
			foreach (TargetFramework fx in frameworks)
				if (fx.Id == id)
					return fx;
			TargetFramework f = new TargetFramework (id);
			frameworks.Add (f);
			return f;
		}
		
		public SystemPackage GetPackageFromPath (string assemblyPath)
		{
			foreach (TargetRuntime r in runtimes) {
				SystemPackage p = r.GetPackageFromPath (assemblyPath);
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
				return System.Reflection.AssemblyName.GetAssemblyName (file);
			} catch (FileNotFoundException) {
				// GetAssemblyName is not case insensitive in mono/windows. This is a workaround
				foreach (string f in Directory.GetFiles (Path.GetDirectoryName (file), Path.GetFileName (file))) {
					if (f != file)
						return GetAssemblyNameObj (f);
				}
				throw;
			}
		}
		
		public static string GetAssemblyName (string file)
		{
			return TargetRuntime.NormalizeAsmName (GetAssemblyNameObj (file).ToString ());
		}
		
		internal static bool UseExpandedFrameworksFile {
			get { return ReferenceFrameworksFile == null; }
		}
		
		internal static bool UpdateExpandedFrameworksFile {
			get { return GeneratedFrameworksFile != null; }
		}
		
		protected void CreateFrameworks ()
		{
			Stream s;
			if (UseExpandedFrameworksFile)
				s = AddinManager.CurrentAddin.GetResource ("frameworks.xml");
			else {
				Console.WriteLine ("Loading frameworks file: " + ReferenceFrameworksFile);
				s = File.OpenRead (ReferenceFrameworksFile);
			}
			
			using (s) {
				XmlTextReader reader = new XmlTextReader (s);
				XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
				frameworks = (List<TargetFramework>) ser.Deserialize (reader, typeof(List<TargetFramework>));
			}

			// Find framework realtions
			foreach (TargetFramework fx in frameworks)
				BuildFrameworkRelations (fx);
		}
		
		void BuildFrameworkRelations (TargetFramework fx)
		{
			if (fx.RelationsBuilt)
				return;
			
			fx.ExtendedFrameworks.Add (fx.Id);
			fx.CompatibleFrameworks.Add (fx.Id);
			
			if (!string.IsNullOrEmpty (fx.CompatibleWithFramework)) {
				TargetFramework compatFx = GetTargetFramework (fx.CompatibleWithFramework);
				BuildFrameworkRelations (compatFx);
				if (!UseExpandedFrameworksFile) {
					List<AssemblyInfo> allAsm = new List<AssemblyInfo> (fx.Assemblies);
					foreach (AssemblyInfo ai in compatFx.Assemblies)
						allAsm.Add (ai.Clone ());
					fx.Assemblies = allAsm.ToArray ();
				}
				fx.CompatibleFrameworks.AddRange (compatFx.CompatibleFrameworks);
				fx.BaseCoreFramework = fx.Id;
			}
			else if (!string.IsNullOrEmpty (fx.ExtendsFramework)) {
				TargetFramework compatFx = GetTargetFramework (fx.ExtendsFramework);
				BuildFrameworkRelations (compatFx);
				fx.CompatibleFrameworks.AddRange (compatFx.CompatibleFrameworks);
				fx.ExtendedFrameworks.AddRange (compatFx.ExtendedFrameworks);
				fx.BaseCoreFramework = compatFx.BaseCoreFramework;
			}
			
			// Find subsets of this framework
			foreach (TargetFramework sfx in frameworks) {
				if (sfx.SubsetOfFramework == fx.Id)
					fx.CompatibleFrameworks.Add (sfx.Id);
			}
			
			fx.RelationsBuilt = true;
		}
		
		internal void SaveGeneratedFrameworkInfo ()
		{
			if (GeneratedFrameworksFile != null) {
				Console.WriteLine ("Saving frameworks file: " + GeneratedFrameworksFile);
				using (StreamWriter sw = new StreamWriter (GeneratedFrameworksFile)) {
					XmlTextWriter tw = new XmlTextWriter (sw);
					tw.Formatting = Formatting.Indented;
					XmlDataSerializer ser = new XmlDataSerializer (new DataContext ());
					ser.Serialize (tw, frameworks);
				}
					
				XmlDocument doc = new XmlDocument ();
				doc.Load (GeneratedFrameworksFile);
				doc.DocumentElement.InsertBefore (doc.CreateComment ("This file has been autogenerated. DO NOT MODIFY!"), doc.DocumentElement.FirstChild);
				doc.Save (GeneratedFrameworksFile);
			}
		}
	}
}
