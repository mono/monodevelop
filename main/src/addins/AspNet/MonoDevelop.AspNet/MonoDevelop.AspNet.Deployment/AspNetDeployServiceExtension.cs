// 
// AspNetDeployServiceExtension.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Deployment;

namespace MonoDevelop.AspNet.Deployment
{
	
	
	public class AspNetDeployServiceExtension : DeployServiceExtension
	{
		
		public override DeployFileCollection GetProjectDeployFiles (DeployContext ctx, Project project, 
		                                                            string solutionConfiguration)
		{
			DeployFileCollection files = base.GetProjectDeployFiles (ctx, project, solutionConfiguration);
			
			AspNetAppProject aspProj = project as AspNetAppProject;
			
			//none of this needs to happen if it's just happening during solution build
			if (aspProj == null || ctx.ProjectBuildOnly)
				return files;
			
			//audit the DeployFiles to make sure they're allowed and laid out for ASP.NET
			foreach (DeployFile df in files) {
				//rewrite ProgramFiles target to ASP.NET's "bin" target
				if (df.TargetDirectoryID == TargetDirectory.ProgramFiles) {
					df.RelativeTargetPath = Path.Combine ("bin", df.RelativeTargetPath);
				}
			}
			
			//add all ASP.NET files marked as content. They go relative to the application root, which we assume to be ProgramFiles
			foreach (ProjectFile pf in aspProj.Files) {
				if (pf.BuildAction == BuildAction.Content)
					files.Add (new DeployFile (aspProj, pf.FilePath, pf.RelativePath, TargetDirectory.ProgramFiles));
			}
			
			return files;
		}
	}
}
