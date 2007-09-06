//
// CProjectServiceExtension.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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
using System.Text;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Execution;

namespace CBinding
{
	public class CProjectServiceExtension : ProjectServiceExtension
	{
		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry entry)
		{
			CProject project = entry as CProject;
			
			if (project == null)
				return base.Build (monitor, entry);
			
			foreach (CProject p in project.DependedOnProjects ()) {
				p.Build (monitor, true);
			}
			
			if (((CProjectConfiguration)project.ActiveConfiguration).CompileTarget != CompileTarget.Bin)
				project.WriteMDPkgPackage ();
			
			return base.Build (monitor, entry);
		}
		
		public override void Clean (IProgressMonitor monitor, CombineEntry entry)
		{
			base.Clean (monitor, entry);
			
			CProject project = entry as CProject;
			
			if (project == null) return;
			
			StringBuilder cleanFiles = new StringBuilder ();
			foreach (ProjectFile file in project.ProjectFiles) {
				if (file.BuildAction == BuildAction.Compile) {
					cleanFiles.Append (Path.ChangeExtension (file.Name, ".o") + " ");
					cleanFiles.Append (Path.ChangeExtension (file.Name, ".d") + " ");
				}
			}
			
			ProcessWrapper p = Runtime.ProcessService.StartProcess ("rm", cleanFiles.ToString (), null, null);
			p.WaitForExit ();
			p.Close ();
		}
	}
}
