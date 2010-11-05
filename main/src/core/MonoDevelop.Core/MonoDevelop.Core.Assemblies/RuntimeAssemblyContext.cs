// 
// RuntimeAssemblyContext.cs
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

namespace MonoDevelop.Core.Assemblies
{
	public class RuntimeAssemblyContext: AssemblyContext
	{
		TargetRuntime runtime;
		
		public RuntimeAssemblyContext (TargetRuntime runtime)
		{
			this.runtime = runtime;
		}
		
		protected override void Initialize ()
		{
			runtime.Initialize ();
		}
		
		public override bool AssemblyIsInGac (string aname)
		{
			return GetGacFile (aname, false) != null;
		}
		
		public override string GetAssemblyLocation (string assemblyName, string package, TargetFramework fx)
		{
			string loc = base.GetAssemblyLocation (assemblyName, package, fx);
			if (loc != null)
				return loc;
			
			string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
			
			string name;
			
			int i = assemblyName.IndexOf (',');
			if (i == -1)
				name = assemblyName;
			else
				name = assemblyName.Substring (0,i).Trim ();

			// Look in initial path
			if (!string.IsNullOrEmpty (baseDirectory)) {
				string localPath = Path.Combine (baseDirectory, name);
				if (File.Exists (localPath))
					return localPath;
			}
			
			// Look in assembly directories
			foreach (string path in GetAssemblyDirectories ()) {
				string localPath = Path.Combine (path, name);
				if (File.Exists (localPath))
					return localPath;
			}

			// Look in the gac
			return GetGacFile (assemblyName, true);
		}
		
		string GetGacFile (string aname, bool allowPartialMatch)
		{
			// Look for the assembly in the GAC.
			
			string name, version, culture, token;
			ParseAssemblyName (aname, out name, out version, out culture, out token);
			if (name == null)
				return null;
			
			if (!allowPartialMatch) {
				if (name == null || version == null || culture == null || token == null)
					return null;
			
				foreach (string gacDir in runtime.GetGacDirectories ()) {
					string file = Path.Combine (gacDir, name);
					file = Path.Combine (file, version + "_" + culture + "_" + token);
					file = Path.Combine (file, name + ".dll");
					if (File.Exists (file))
					    return file;
				}
			}
			else {
				string pattern = (version ?? "*") + "_" + (culture ?? "*") + "_" + (token ?? "*");
				foreach (string gacDir in runtime.GetGacDirectories ()) {
					string asmDir = Path.Combine (gacDir, name);
					if (Directory.Exists (asmDir)) {
						foreach (string dir in Directory.GetDirectories (asmDir, pattern)) {
							string file = Path.Combine (dir, name + ".dll");
							if (File.Exists (file))
								return file;
							file = Path.Combine (dir, name + ".exe");
							if (File.Exists (file))
								return file;
						}
					}
				}
			}
			return null;
		}
	}
}
