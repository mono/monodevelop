//
// ProjectSolutionPad.cs
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
using System.Resources;

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Core.Properties;

namespace MonoDevelop.Gui.Pads.ProjectPad
{
	public class ProjectSolutionPad: SolutionPad
	{
		public ProjectSolutionPad ()
		{
			Runtime.Gui.Workbench.ActiveWorkbenchWindowChanged += new EventHandler (OnWindowChanged);
		}
		
		protected override void OnSelectionChanged (object sender, EventArgs args)
		{
			base.OnSelectionChanged (sender, args);
			ITreeNavigator nav = GetSelectedNode ();
			if (nav != null) {
				Project p = (Project) nav.GetParentDataItem (typeof(Project), true);
				Runtime.ProjectService.CurrentSelectedProject = p;
				Combine c = (Combine) nav.GetParentDataItem (typeof(Combine), true);
				Runtime.ProjectService.CurrentSelectedCombine = c;
			}
		}
		
		protected override void OnCloseCombine (object sender, CombineEventArgs e)
		{
			base.OnCloseCombine (sender, e);
			Runtime.ProjectService.CurrentSelectedProject = null;
			Runtime.ProjectService.CurrentSelectedCombine = null;
		}
		
		void OnWindowChanged (object ob, EventArgs args)
		{
			IWorkbenchWindow win = Runtime.Gui.Workbench.ActiveWorkbenchWindow;
			if (win != null && win.ViewContent != null && win.ViewContent.Project != null) {
				string file = win.ViewContent.ContentName;
				if (file != null) {
					ProjectFile pf = win.ViewContent.Project.ProjectFiles.GetFile (win.ViewContent.ContentName);
					if (pf != null) {
						ITreeNavigator nav = GetNodeAtObject (pf, true);
						if (nav != null) {
							nav.ExpandToNode ();
							nav.Selected = true;
						}
					}
				}
			}
		}
	}
}

