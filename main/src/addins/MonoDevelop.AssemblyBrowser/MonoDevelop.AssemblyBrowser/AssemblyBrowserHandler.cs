// 
// AssemblyBrowserHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.Linq;

namespace MonoDevelop.AssemblyBrowser
{
	public class AssemblyBrowserHandler : CommandHandler
	{
		readonly static string[] defaultAssemblies = new string[] { "mscorlib", "System", "System.Core", "System.Xml" };
		
		protected override void Run ()
		{
			foreach (var view in IdeApp.Workbench.Documents) {
				if (view.GetContent<AssemblyBrowserViewContent> () != null) {
					view.Window.SelectWindow ();
					return;
				}
			}
			var binding = DisplayBindingService.GetBindings<AssemblyBrowserDisplayBinding> ().FirstOrDefault ();
			var assemblyBrowserView = binding != null ? binding.GetViewContent () : new AssemblyBrowserViewContent ();
			
			if (Ide.IdeApp.ProjectOperations.CurrentSelectedSolution == null) {
				foreach (var assembly in defaultAssemblies) {
					assemblyBrowserView.Widget.AddReferenceByAssemblyName (assembly, assembly == defaultAssemblies [0]); 
				}
			} else {
				foreach (var project in Ide.IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
					assemblyBrowserView.Widget.AddProject (project, false);
					
					var netProject = project as DotNetProject;
					if (netProject == null)
						continue;
					foreach (string file in netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {
						if (!File.Exists (file))
							continue;
						assemblyBrowserView.Widget.AddReferenceByFileName (file, false); 
					}
				}
			}
			
			Ide.IdeApp.Workbench.OpenDocument (assemblyBrowserView, true);
		}
	}
}

