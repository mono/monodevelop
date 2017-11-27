// 
// TargetFrameworkBackend.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.Core.Assemblies
{
	public abstract class TargetFrameworkBackend
	{
		protected TargetRuntime runtime;
		protected TargetFramework framework;
		
		public abstract bool SupportsRuntime (TargetRuntime runtime);
		
		internal protected virtual void Initialize (TargetRuntime runtime, TargetFramework framework)
		{
			this.runtime = runtime;
			this.framework = framework;
		}

		bool? isInstalled;
		public virtual bool IsInstalled {
			get {
				if (isInstalled != null)
					return isInstalled.Value;
				
				foreach (string dir in GetFrameworkFolders ()) {
					if (Directory.Exists (dir)) {
						string manifest = Path.Combine (dir, "RedistList", "FrameworkList.xml");
						if (File.Exists (manifest)) {
							isInstalled = true;
							return true;
						}
						if (framework.Assemblies.Length > 0) {
							string firstAsm = Path.Combine (dir, framework.Assemblies [0].Name) + ".dll";
							if (File.Exists (firstAsm)) {
								isInstalled = true;
								return true;
							}
						}
					}
				}
				isInstalled = false;
				return false;
			}
		}
		
		public abstract IEnumerable<string> GetFrameworkFolders ();
		
		public virtual Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			return new Dictionary<string,string> ();
		}
		
		public virtual string GetToolPath (string toolName)
		{
			foreach (string path in runtime.GetToolsPaths (framework)) {
				string toolPath = Path.Combine (path, toolName);
				if (Platform.IsWindows) {
					if (File.Exists (toolPath + ".bat"))
						return toolPath + ".bat";
				}
				if (File.Exists (toolPath))
					return toolPath;
				if (File.Exists (toolPath + ".exe"))
					return toolPath + ".exe";
			}
			return null;
		}
		
		public virtual IEnumerable<string> GetToolsPaths ()
		{
			string paths;
			if (!runtime.GetToolsExecutionEnvironment (framework).Variables.TryGetValue ("PATH", out paths))
				yield break;
			foreach (string path in paths.Split (new char[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)) {
				if (path.Length > 0 && path [0] == '"' && path [path.Length - 1] == '"')
					yield return path.Substring (1, path.Length-2);
				else
					yield return path;
			}
		}
		
		public virtual IEnumerable<string> GetAssemblyDirectories ()
		{
			yield break;
		}
		
		public virtual SystemPackageInfo GetFrameworkPackageInfo (string packageName)
		{
			string name = !string.IsNullOrEmpty (packageName)? packageName : framework.Name;
			SystemPackageInfo info = new SystemPackageInfo ();
			info.Name = name;
			info.Description = name;
			info.IsFrameworkPackage = true;
			info.IsCorePackage = true;
			info.IsGacPackage = true;
			info.Version = framework.Id.Version;
			info.TargetFramework = framework.Id;
			return info;
		}
	}
	
	public abstract class TargetFrameworkBackend<T>: TargetFrameworkBackend where T:TargetRuntime
	{
		protected T targetRuntime;
		
		public override bool SupportsRuntime (TargetRuntime runtime)
		{
			return runtime is T;
		}
		
		internal protected override void Initialize (TargetRuntime runtime, TargetFramework framework)
		{
			base.Initialize (runtime, framework);
			this.targetRuntime = (T) runtime;
		}
	}
	
	class NotSupportedFrameworkBackend: TargetFrameworkBackend
	{
		public override bool SupportsRuntime (TargetRuntime runtime)
		{
			return false;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			return null;
		}
		
		public override bool IsInstalled {
			get {
				return false;
			}
		}
	}
}
