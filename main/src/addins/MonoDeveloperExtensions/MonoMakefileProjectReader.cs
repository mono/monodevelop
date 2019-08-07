//
// MonoMakeFileProjectServiceExtension.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Projects.MSBuild;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MonoDeveloper
{
	public class MonoMakefileProjectReader: WorkspaceObjectReader
	{
		public override bool CanRead (FilePath file, Type expectedType)
		{
			if (file.FileName != "Makefile") return false;
			MonoMakefile mkfile = new MonoMakefile (file);
			if (mkfile.Content.IndexOf ("build/rules.make") == -1) return false;

			if (mkfile.GetVariable ("LIBRARY") != null) return expectedType.IsAssignableFrom (typeof(DotNetProject));
			if (mkfile.GetVariable ("PROGRAM") != null) return expectedType.IsAssignableFrom (typeof(DotNetProject));
			string subdirs = mkfile.GetVariable ("SUBDIRS");
			if (subdirs != null && subdirs.Trim (' ','\t') != "")
				return expectedType.IsAssignableFrom (typeof(Solution)) || expectedType.IsAssignableFrom (typeof(SolutionFolder));

			return false;
		}

		public override Task<SolutionItem> LoadSolutionItem (ProgressMonitor monitor, SolutionLoadContext ctx, string fileName, MSBuildFileFormat expectedFormat, string typeGuid, string itemGuid)
		{
			return Task.FromResult ((SolutionItem) ReadFile (fileName, false, monitor));
		}

		public override Task<WorkspaceItem> LoadWorkspaceItem (ProgressMonitor monitor, string fileName)
		{
			return Task.FromResult ((WorkspaceItem) ReadFile (fileName, false, monitor));
		}

		public object ReadFile (FilePath fileName, bool hasParentSolution, ProgressMonitor monitor)
		{
			FilePath basePath = fileName.ParentDirectory;
			MonoMakefile mkfile = new MonoMakefile (fileName);
			string aname = mkfile.GetVariable ("LIBRARY");
			if (aname == null)
				aname = mkfile.GetVariable ("PROGRAM");

			if (!string.IsNullOrEmpty (aname)) {
				monitor.BeginTask ("Loading '" + fileName + "'", 0);
				var project = Services.ProjectService.CreateProject ("C#");
				project.FileName = fileName;

				var ext = new MonoMakefileProjectExtension ();
				project.AttachExtension (ext);
				ext.Read (mkfile);

				monitor.EndTask ();
				return project;
			}

			string subdirs;
			StringBuilder subdirsBuilder = new StringBuilder ();
			subdirsBuilder.Append (mkfile.GetVariable ("common_dirs"));
			if (subdirsBuilder.Length != 0) {
				subdirsBuilder.Append ("\t");
				subdirsBuilder.Append (mkfile.GetVariable ("net_2_0_dirs"));
			}
			if (subdirsBuilder.Length == 0)
				subdirsBuilder.Append (mkfile.GetVariable ("SUBDIRS"));

			subdirs = subdirsBuilder.ToString ();
			if (subdirs != null && (subdirs = subdirs.Trim (' ', '\t')) != "") {
				object retObject;
				SolutionFolder folder;
				if (!hasParentSolution) {
					Solution sol = new Solution ();
					sol.AttachExtension (new MonoMakefileSolutionExtension ());
					sol.FileName = fileName;
					folder = sol.RootFolder;
					retObject = sol;

					foreach (string conf in MonoMakefile.MonoConfigurations) {
						SolutionConfiguration sc = new SolutionConfiguration (conf);
						sol.Configurations.Add (sc);
					}
				} else {
					folder = new SolutionFolder ();
					folder.Name = Path.GetFileName (Path.GetDirectoryName (fileName));
					retObject = folder;
				}

				subdirs = subdirs.Replace ('\t', ' ');
				string[] dirs = subdirs.Split (' ');

				monitor.BeginTask ("Loading '" + fileName + "'", dirs.Length);
				HashSet<string> added = new HashSet<string> ();
				foreach (string dir in dirs) {
					if (!added.Add (dir))
						continue;
					monitor.Step (1);
					if (dir == null)
						continue;
					string tdir = dir.Trim ();
					if (tdir == "")
						continue;
					string mfile = Path.Combine (Path.Combine (basePath, tdir), "Makefile");
					if (File.Exists (mfile) && CanRead (mfile, typeof(SolutionItem))) {
						SolutionFolderItem it = (SolutionFolderItem) ReadFile (mfile, true, monitor);
						folder.Items.Add (it);
					}
				}
				monitor.EndTask ();
				return retObject;
			}
			return null;
		}
	}
}

