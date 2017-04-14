// 
// MsNetFrameworkBackend.cs
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
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;

namespace MonoDevelop.Core.Assemblies
{
	public class MsNetFrameworkBackend: TargetFrameworkBackend<MsNetTargetRuntime>
	{
		string ref_assemblies_folder;

		string GetReferenceAssembliesFolder ()
		{
			if (ref_assemblies_folder != null)
				return ref_assemblies_folder;
			
			var fxDir = framework.Id.GetAssemblyDirectoryName ();
			foreach (var rootDir in runtime.GetReferenceFrameworkDirectories ()) {
				var dir = rootDir.Combine (fxDir);
				var frameworkList = dir.Combine ("RedistList", "FrameworkList.xml");
				if (File.Exists (frameworkList))
					return ref_assemblies_folder = dir;
			}
			return null;
		}
		
		public override IEnumerable<string> GetFrameworkFolders ()
		{
			var dir = GetReferenceAssembliesFolder ();
			if (dir != null) {
				yield return dir;
			}
		}
		
		public override Dictionary<string, string> GetToolsEnvironmentVariables ()
		{
			var vars = new Dictionary<string, string> ();
			var path = new System.Text.StringBuilder ();
			path.Append (Environment.GetEnvironmentVariable ("PATH"));
			vars["PATH"] = path.ToString ();
			return vars;
		}

		public override IEnumerable<string> GetToolsPaths ()
		{
			foreach (string s in BaseGetToolsPaths ())
				yield return s;
			yield return PropertyService.EntryAssemblyPath;
		}

		// ProgramFilesX86 is broken on 32-bit WinXP, this is a workaround
		static string GetProgramFilesX86 ()
		{
			return Environment.GetFolderPath (IntPtr.Size == 8?
				Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);
		}

		//base isn't verifiably accessible from the enumerator so use this private helper
		IEnumerable<string> BaseGetToolsPaths ()
		{
			return base.GetToolsPaths ();
		}
	}
}
