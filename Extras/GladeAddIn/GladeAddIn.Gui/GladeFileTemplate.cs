//
// GladeFileTemplate.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;

namespace GladeAddIn.Gui
{
	public class GladeFileTemplate: FileTemplate
	{
		protected override void CreateProjectFile (Project project, string fileName, string fileContent)
		{
			GuiBuilderProject[] projects = GladeService.GetGuiBuilderProjects (project);
			GuiBuilderProject gproject = null;
			if (projects.Length == 0) {
				// Create an empty glade file
				string gladeFile = Path.Combine (project.BaseDirectory, "gui.glade");
				StreamWriter sw = new StreamWriter (gladeFile);
				sw.WriteLine ("<?xml version=\"1.0\" standalone=\"no\"?> <!--*- mode: xml -*-->");
				sw.WriteLine ("<!DOCTYPE glade-interface SYSTEM \"http://glade.gnome.org/glade-2.0.dtd\">");
				sw.WriteLine ("<glade-interface>");
				sw.WriteLine ("</glade-interface>");
				sw.Close ();
				ProjectFile file = new ProjectFile (gladeFile, BuildAction.EmbedAsResource);
				project.ProjectFiles.Add (file);
				projects = GladeService.GetGuiBuilderProjects (project);
				if (projects.Length > 0)
					gproject = projects [0];
				else
					throw new UserException ("A new glade file for the window could not be created.");
			} else {
				gproject = projects [0];
			}
			
			fileContent = fileContent.Replace ("${GladeFile}", System.IO.Path.GetFileName (gproject.File));
		
			base.CreateProjectFile (project, fileName, fileContent);
			
			ProjectFile pf = project.ProjectFiles.GetFile (fileName);
			if (pf == null || pf.BuildAction != BuildAction.Compile)
				return;

			IParseInformation pinfo = IdeApp.ProjectOperations.ParserDatabase.UpdateFile (project, fileName, fileContent);
			if (pinfo == null) return;
			
			ICompilationUnit cunit = (ICompilationUnit)pinfo.BestCompilationUnit;
			foreach (IClass cls in cunit.Classes) {
				string id = ClassUtils.GetWindowId (cls);
				if (id != null) {
					gproject.NewDialog (id);
				}
			}
		}
	}
}
