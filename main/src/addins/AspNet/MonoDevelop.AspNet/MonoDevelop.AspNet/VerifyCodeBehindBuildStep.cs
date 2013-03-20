// 
// VerifyCodeBehindBuildStep.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2006-2007 Michael Hutchinson
// Copyright (C) 2007-2008 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.AspNet
{
	
	public class VerifyCodeBehindBuildStep : ProjectServiceExtension
	{
		public override bool SupportsItem (IBuildTarget item)
		{
			var aspProject = item as AspNetAppProject;
			return aspProject != null && aspProject.LanguageBinding != null;
		}
		
		protected override BuildResult Build (IProgressMonitor monitor, SolutionEntityItem project, ConfigurationSelector configuration)
		{
			var aspProject = project as AspNetAppProject;
			
			//get the config object and validate
			AspNetAppProjectConfiguration config = (AspNetAppProjectConfiguration)aspProject.GetConfiguration (configuration);
			if (config == null || config.DisableCodeBehindGeneration) {
				return base.Build (monitor, project, configuration);
			}
			
			var writer = CodeBehindWriter.CreateForProject (monitor, aspProject);
			if (!writer.SupportsPartialTypes) {
				return base.Build (monitor, project, configuration);
			}

			var result = new BuildResult ();

			monitor.BeginTask ("Updating CodeBehind designer files", 0);

			foreach (var file in aspProject.Files) {
				ProjectFile designerFile = CodeBehind.GetDesignerFile (file);
				if (designerFile == null)
					continue;

				if (File.GetLastWriteTimeUtc (designerFile.FilePath) > File.GetLastWriteTimeUtc (file.FilePath))
					continue;

				monitor.Log.WriteLine ("Updating CodeBehind for file '{0}'", file);
				result.Append (CodeBehind.UpdateDesignerFile (writer, aspProject, file, designerFile));
			}
			
			writer.WriteOpenFiles ();

			monitor.EndTask ();

			if (writer.WrittenCount > 0)
				monitor.Log.WriteLine ("{0} CodeBehind designer classes updated.", writer.WrittenCount);
			else
				monitor.Log.WriteLine ("No changes made to CodeBehind classes.");
			
			if (result.Failed)
				return result;

			return result.Append (base.Build (monitor, project, configuration));
		}
	}
}
