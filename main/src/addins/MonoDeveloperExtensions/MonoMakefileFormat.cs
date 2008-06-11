//
// MonoMakefileFormat.cs
//
// Author:
//   Lluis Sanchez Gual
//
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoDevelop.Core;
using System.Text.RegularExpressions;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Extensions;

namespace MonoDeveloper
{
	public class MonoMakefileFormat: IFileFormat
	{
		public static readonly string[] Configurations = new string [] {
			"default", "net_2_0"
		};
		
		public string Name {
			get { return "Mono Makefile"; }
		}
		
		public string GetValidFormatName (object obj, string fileName)
		{
			return Path.Combine (Path.GetDirectoryName (fileName), "Makefile");
		}
		
		public bool CanReadFile (string file, Type expectedType)
		{
			if (Path.GetFileName (file) != "Makefile") return false;
			MonoMakefile mkfile = new MonoMakefile (file);
			if (mkfile.Content.IndexOf ("build/rules.make") == -1) return false;
			
			if (mkfile.GetVariable ("LIBRARY") != null) return expectedType.IsAssignableFrom (typeof(DotNetProject));
			if (mkfile.GetVariable ("PROGRAM") != null) return expectedType.IsAssignableFrom (typeof(DotNetProject));
			string subdirs = mkfile.GetVariable ("SUBDIRS");
			if (subdirs != null && subdirs.Trim (' ','\t') != "")
				return expectedType.IsAssignableFrom (typeof(Solution)) || expectedType.IsAssignableFrom (typeof(SolutionFolder));
			
			return false;
		}
		
		public bool CanWriteFile (object obj)
		{
			return (obj is SolutionFolder) || IsMonoProject (obj);
		}
		
		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
		}
		
		public List<string> GetItemFiles (object obj)
		{
			List<string> col = new List<string> ();
			DotNetProject mp = obj as DotNetProject;
			if (mp != null) {
				MonoSolutionItemHandler handler = ProjectExtensionUtil.GetItemHandler (mp) as MonoSolutionItemHandler;
				if (handler != null && File.Exists (handler.SourcesFile)) {
					col.Add (mp.FileName);
					col.Add (handler.SourcesFile);
				}
			}
			return col;
		}
		
		public object ReadFile (string fileName, Type expectedType, IProgressMonitor monitor)
		{
			return ReadFile (fileName, false, monitor);
		}
		
		public object ReadFile (string fileName, bool hasParentSolution, IProgressMonitor monitor)
		{
			string basePath = Path.GetDirectoryName (fileName);
			MonoMakefile mkfile = new MonoMakefile (fileName);
			string aname = mkfile.GetVariable ("LIBRARY");
			if (aname == null) aname = mkfile.GetVariable ("PROGRAM");
			
			if (aname != null) {
				// It is a project
				monitor.BeginTask ("Loading '" + fileName + "'", 0);
				DotNetProject project = new DotNetProject ("C#");
				MonoSolutionItemHandler handler = new MonoSolutionItemHandler (project);
				ProjectExtensionUtil.InstallHandler (handler, project);
				project.Name = Path.GetFileName (basePath);
				handler.Read (mkfile);
				monitor.EndTask ();
				return project;
			} else {
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
				if (subdirs != null && (subdirs = subdirs.Trim (' ','\t')) != "")
				{
					object retObject;
					SolutionFolder folder;
					if (!hasParentSolution) {
						Solution sol = new Solution ();
						sol.FileFormat = Services.ProjectService.FileFormats.GetFileFormat ("MonoMakefile");
						sol.FileName = fileName;
						folder = sol.RootFolder;
						retObject = sol;
						
						foreach (string conf in MonoMakefileFormat.Configurations) {
							SolutionConfiguration sc = new SolutionConfiguration (conf);
							sol.Configurations.Add (sc);
						}
					} else {
						folder = new SolutionFolder ();
						folder.Name = Path.GetFileName (Path.GetDirectoryName (fileName));
						retObject = folder;
					}
					
					subdirs = subdirs.Replace ('\t',' ');
					string[] dirs = subdirs.Split (' ');
					
					monitor.BeginTask ("Loading '" + fileName + "'", dirs.Length);
					Hashtable added = new Hashtable ();
					foreach (string dir in dirs) {
						if (added.Contains (dir)) continue;
						added.Add (dir, dir);
						monitor.Step (1);
						if (dir == null) continue;
						string tdir = dir.Trim ();
						if (tdir == "") continue;
						string mfile = Path.Combine (Path.Combine (basePath, tdir), "Makefile");
						if (File.Exists (mfile) && CanReadFile (mfile, typeof(SolutionItem))) {
							SolutionItem it = (SolutionItem) ReadFile (mfile, true, monitor);
							folder.Items.Add (it);
						}
					}
					monitor.EndTask ();
					return retObject;
				}
			}
			return null;
		}
		
		public static bool IsMonoProject (object obj)
		{
			DotNetProject p = obj as DotNetProject;
			return p != null && (ProjectExtensionUtil.GetItemHandler (p) is MonoSolutionItemHandler);
		}

		public void ConvertToFormat (object obj)
		{
			// Nothing can be converted to this format.
		}
		
		public bool SupportsMixedFormats {
			get { return false; }
		}

	}
}
