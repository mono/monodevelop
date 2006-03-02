//
// GeneratorBuildStep.cs
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
using System.Collections;
using System.IO;
using System.CodeDom;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.GtkCore.WidgetLibrary;

namespace MonoDevelop.GtkCore
{
	class GeneratorBuildStep: IBuildStep
	{
		public ICompilerResult Build (IProgressMonitor monitor, Project project)
		{
			GtkDesignInfo info = GtkCoreService.GetGtkInfo (project);
			if (info == null)
				return null;
				
			if (info.IsWidgetLibrary)
				GtkCoreService.UpdateObjectsFile (project);

			if (!info.GuiBuilderProject.IsEmpty) {
				monitor.Log.WriteLine ("Generating stetic code...");
				DotNetProject netproject = (DotNetProject) project;
				
				ArrayList projects = new ArrayList ();
				projects.Add (info.GuiBuilderProject.File);
				
				ArrayList libs = new ArrayList ();
				foreach (BaseWidgetLibrary lib in info.GetReferencedWidgetLibraries ()) {
					if (lib == info.ProjectWidgetLibrary) continue;
					libs.Add (lib.AssemblyPath);
				}
				
				CodeGeneratorProcess cob = (CodeGeneratorProcess) Runtime.ProcessService.CreateExternalProcessObject (typeof (CodeGeneratorProcess), false);
				using (cob) {
					cob.GenerateCode (Path.Combine (project.BaseDirectory, info.SteticGeneratedFile), projects, libs, netproject.LanguageName);
				}
			}
			
			return null;
		}
		
		public bool NeedsBuilding (Project project)
		{
			return false;
		}
	}
	
	[AddinDependency ("MonoDevelop.Projects")]
	public class CodeGeneratorProcess: RemoteProcessObject
	{
		public void GenerateCode (string targetFile, ArrayList projectFiles, ArrayList libraries, string langName)
		{
			Console.WriteLine ("p1");
			Gtk.Application.Init ();
			
			CodeDomProvider provider;
			
			if (langName == "C#") {
				// FIXME: This is a fast path. Do not add other languages here.
				Console.WriteLine ("p2");
				provider = new Microsoft.CSharp.CSharpCodeProvider ();
			} else {
				IDotNetLanguageBinding binding = Services.Languages.GetBindingPerLanguageName (langName) as IDotNetLanguageBinding;
				provider = binding.GetCodeDomProvider ();
				if (provider == null)
					throw new UserException ("Code generation not supported in language: " + langName);
			}
			
			Stetic.Project[] projects = new Stetic.Project [projectFiles.Count];
			for (int n=0; n < projectFiles.Count; n++) {
				projects [n] = new Stetic.Project ();
				projects [n].Load ((string) projectFiles [n]);
			}
			
			foreach (string lib in libraries) {
				Stetic.AssemblyWidgetLibrary alib = new Stetic.AssemblyWidgetLibrary (lib);
				Stetic.Registry.RegisterWidgetLibrary (alib);
			}

			Console.WriteLine ("p3");
			Stetic.CodeGenerator.GenerateProjectCode (targetFile, "Stetic", provider, projects);
			Console.WriteLine ("p4");
		}
	}
}
