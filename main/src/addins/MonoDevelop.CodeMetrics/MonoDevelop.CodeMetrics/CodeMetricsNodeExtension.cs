//
// CodeMetricsNodeExtension.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.CodeMetrics
{
	public class CodeMetricsNodeExtension: NodeBuilderExtension
	{
		public override Type CommandHandlerType {
			get { return typeof(CodeMetricsCommandHandler); }
		}
		
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFile).IsAssignableFrom (dataType)
				|| typeof(ProjectFolder).IsAssignableFrom (dataType)
				|| typeof(Project).IsAssignableFrom (dataType)
				|| typeof(SolutionFolder).IsAssignableFrom (dataType)
				|| typeof(Solution).IsAssignableFrom (dataType);
		}
		
		public CodeMetricsNodeExtension()
		{
		}
	}
	
	class CodeMetricsCommandHandler: NodeCommandHandler 
	{
		[CommandHandler (Commands.ShowMetrics)]
		protected void OnShowMetrics () 
		{
			CodeMetricsView view = new CodeMetricsView ();
			
			ProjectFile file   = CurrentNode.DataItem as ProjectFile;
			if (file != null)
				view.Add (file);
			
			ProjectFolder folder = CurrentNode.DataItem as ProjectFolder;
			if (folder != null) {
				foreach (string fileName in System.IO.Directory.GetFiles (folder.Path, "*", SearchOption.AllDirectories)) {
					view.Add (fileName);
				}
			}
			
			SolutionFolder combine = CurrentNode.DataItem as SolutionFolder;
			if (combine != null) 
				view.Add (combine);
			
			Solution solution = CurrentNode.DataItem as Solution;
			if (combine != null) 
				view.Add (solution);
			
			Project project = CurrentNode.DataItem as Project;
			if (project != null) 
				view.Add (project);
			IdeApp.Workbench.OpenDocument (view, true);
			view.Run ();
		}
	}

}
