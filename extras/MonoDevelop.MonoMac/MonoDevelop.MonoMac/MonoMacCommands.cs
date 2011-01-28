// 
// MonoMacCommands.cs
//  
// Author:
//       dwandless <${AuthorEmail}>
// 
// Copyright (c) 2011 dwandless
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using System;

namespace MonoDevelop.MonoMac
{
	public enum MonoMacCommands
	{
		MonoMacAddFrameworksHandler,
	}
	
	public class MonoMacAddFrameworksHandler : CommandHandler
	{
		public static event EventHandler<FrameworksChangedArgs> FrameworksChanged;
		
		public static void NotifyFrameworksChanged (MonoMacProject project)
		{
			if (FrameworksChanged != null)
				FrameworksChanged (null, new FrameworksChangedArgs (project));
		}
		
		protected override void Run ()
		{
			var addFrameworkDlg = new AddFrameworksDialog () {
				Modal = true
			};
			
			try {				
				if (MessageService.RunCustomDialog (addFrameworkDlg) == (int)Gtk.ResponseType.Ok) {
					var entry = (SolutionItem)IdeApp.ProjectOperations.CurrentSelectedItem;
					var mmProject = (MonoMacProject)entry;
					
					if (mmProject != null) {	
						string pathToFramework = addFrameworkDlg.Framework;
						
						if (addFrameworkDlg.IsRelative) {
							var path = new FilePath (addFrameworkDlg.Framework);
							pathToFramework = path.ToRelative (mmProject.BaseDirectory);
						}
						
						var ef = new MonoMacFrameworkItem (pathToFramework) {
							Relative = addFrameworkDlg.IsRelative
						};

						mmProject.Items.Add (ef);
						IdeApp.ProjectOperations.Save (mmProject);
						IdeApp.Workbench.StatusBar.ShowMessage ( GettextCatalog.GetString ("Added framework"));
						
						NotifyFrameworksChanged (mmProject);
					}
				}
			}
			finally {
				addFrameworkDlg.Destroy ();
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			var entry = (SolutionItem)IdeApp.ProjectOperations.CurrentSelectedItem;
			
			if (entry == null || !(entry is MonoMacProject)) {
				info.Visible = false;
				return;
			}
			
			info.Enabled = true;
		}
	}
	
	public class FrameworksChangedArgs: EventArgs
	{
		public MonoMacProject Project { get; private set; }

		public FrameworksChangedArgs (MonoMacProject project)
		{
			Project = project;
		}		
	}
}