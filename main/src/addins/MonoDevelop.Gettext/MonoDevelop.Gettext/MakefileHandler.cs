// MakefileHandler.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Autotools;
using MonoDevelop.Projects;
using MonoDevelop.Deployment;

namespace MonoDevelop.Gettext
{
	class MakefileHandler: IMakefileHandler
	{
		public bool CanDeploy (SolutionFolderItem entry, MakefileType type)
		{
			return entry is TranslationProject;
		}

		public Makefile Deploy (AutotoolsContext ctx, SolutionFolderItem entry, ProgressMonitor monitor)
		{
			Makefile mkfile = new Makefile ();
			TranslationProject project = (TranslationProject) entry;
			
			StringBuilder files = new StringBuilder ();
			foreach (Translation t in project.Translations) {
				files.Append ("\\\n\t").Append (t.FileName);
			}
			
			string dir;
			if (project.OutputType == TranslationOutputType.SystemPath) {
				dir = ctx.DeployContext.GetResolvedPath (TargetDirectory.CommonApplicationDataRoot, "locale");
			} else {
				dir = ctx.DeployContext.GetResolvedPath (TargetDirectory.ProgramFiles, project.RelPath);
			}
			dir = dir.Replace ("@prefix@", "$(prefix)");
			dir = dir.Replace ("@PACKAGE@", "$(PACKAGE)");
			
			TemplateEngine templateEngine = new TemplateEngine ();
			templateEngine.Variables ["TOP_SRCDIR"] = FileService.AbsoluteToRelativePath (project.BaseDirectory, ctx.TargetSolution.BaseDirectory);
			templateEngine.Variables ["FILES"] = files.ToString ();
			templateEngine.Variables ["BUILD_DIR"] = ".";
			templateEngine.Variables ["INSTALL_DIR"] = "$(DESTDIR)" + dir;
			templateEngine.Variables ["ALL_TARGET"] = (ctx.TargetSolution.BaseDirectory == project.BaseDirectory) ? "all-local" : "all";
			
			StringWriter sw = new StringWriter ();
			
			string mt;
			if (ctx.MakefileType == MakefileType.AutotoolsMakefile)
				mt = "Makefile_am.template";
			else
				mt = "Makefile.template";

			using (Stream stream = GetType().Assembly.GetManifestResourceStream (mt)) {
				StreamReader reader = new StreamReader (stream);

				templateEngine.Process (reader, sw);
				reader.Close ();
			}

			mkfile.Append (sw.ToString ());
			
			return mkfile;
		}
	}
}
