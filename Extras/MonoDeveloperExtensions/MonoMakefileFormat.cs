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
using System.IO;
using MonoDevelop.Services;
using System.Text.RegularExpressions;
using MonoDevelop.Internal.Project;

namespace MonoDeveloper
{
	public class MonoMakefileFormat: IFileFormat
	{
		public string Name {
			get { return "Mono Makefile"; }
		}
		
		public string GetValidFormatName (string fileName)
		{
			return Path.Combine (Path.GetDirectoryName (fileName), "Makefile");
		}
		
		public bool CanReadFile (string file)
		{
			if (Path.GetFileName (file) != "Makefile") return false;
			MonoMakefile mkfile = new MonoMakefile (file);
			if (mkfile.Content.IndexOf ("build/rules.make") == -1) return false;
			
			if (mkfile.GetVariable ("LIBRARY") != null) return true;
			if (mkfile.GetVariable ("PROGRAM") != null) return true;
			string subdirs = mkfile.GetVariable ("SUBDIRS");
			if (subdirs != null && subdirs.Trim (' ','\t') != "")
				return true;
			
			return false;
		}
		
		public bool CanWriteFile (object obj)
		{
			return (obj is Project) || (obj is Combine);
		}
		
		public void WriteFile (string file, object node, IProgressMonitor monitor)
		{
		}
		
		public object ReadFile (string fileName, IProgressMonitor monitor)
		{
			string basePath = Path.GetDirectoryName (fileName);
			MonoMakefile mkfile = new MonoMakefile (fileName);
			string aname = mkfile.GetVariable ("LIBRARY");
			if (aname == null) aname = mkfile.GetVariable ("PROGRAM");
			
			if (aname != null) {
				// It is a project
				monitor.BeginTask ("Loading '" + fileName + "'", 0);
				MonoProject project = new MonoProject (mkfile);
				monitor.EndTask ();
				return project;
			} else {
				string subdirs = mkfile.GetVariable ("SUBDIRS");
				if (subdirs != null && (subdirs = subdirs.Trim (' ','\t')) != "")
				{
					Combine combine = new MonoCombine ();
					combine.FileName = fileName;
					combine.Name = Path.GetFileName (basePath);
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
						if (File.Exists (mfile) && CanReadFile (mfile))
							combine.AddEntry (mfile, monitor);
					}
					monitor.EndTask ();
					return combine;
				}
			}
			return null;
		}
	}
}
