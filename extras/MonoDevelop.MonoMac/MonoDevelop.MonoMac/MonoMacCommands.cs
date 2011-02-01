// 
// MonoMacCommands.cs
//  
// Author:
//       Duane Wandless
// 
// Copyright (c) 2009-2010 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.MonoMac
{
	public enum MonoMacCommands
	{
		MonoMacAddFramework,
	}
	
	public class MonoMacAddFrameworkCommand : NodeCommandHandler
	{
		[CommandHandler (MonoDevelop.MonoMac.MonoMacCommands.MonoMacAddFramework)]
		public void AddFramework()
		{
		}
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
			var mmProject = IdeApp.ProjectOperations.CurrentSelectedItem as MonoMacProject;			
			if (mmProject == null) 
				return;
			
			var addFrameworkDlg = new AddFrameworksDialog () {
				Modal = true
			};
			
			MonoMacFrameworkItem ef = null;
			
			try {				
				if (MessageService.RunCustomDialog (addFrameworkDlg) == (int)Gtk.ResponseType.Ok) {
					string pathToFramework = addFrameworkDlg.Framework;
					
					if (addFrameworkDlg.IsRelative) {
						var path = new FilePath (addFrameworkDlg.Framework);
						pathToFramework = path.ToRelative (mmProject.BaseDirectory);
					}
					
					ef = new MonoMacFrameworkItem (pathToFramework) {
						Relative = addFrameworkDlg.IsRelative
					};
				}
			}
			finally {
				addFrameworkDlg.Destroy ();
			}
			
			if (ef != null) {
				mmProject.Items.Add (ef);
				IdeApp.ProjectOperations.Save (mmProject);
				IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetString ("Added framework"));
				
				NotifyFrameworksChanged (mmProject);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			var entry = IdeApp.ProjectOperations.CurrentSelectedItem as MonoMacProject;
			
			if (entry == null) {
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