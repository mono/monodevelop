// WebDeployLaunchDialog.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using Gtk;

using MonoDevelop.AspNet;
using MonoDevelop.Ide;

namespace MonoDevelop.AspNet.Deployment
{
	
	public partial class WebDeployLaunchDialog : Dialog
	{
		Gtk.ListStore targetStore = new Gtk.ListStore (typeof (string), typeof(WebDeployTarget), typeof (bool));
		const int LISTCOL_Text = 0;
		const int LISTCOL_Target = 1;
		const int LISTCOL_Checked = 2;
		AspNetAppProject project;
		
		public WebDeployLaunchDialog (AspNetAppProject project)
		{
			this.Build();
			
			this.project = project;
			
			//set up the sort order 
			targetStore.SetSortFunc (LISTCOL_Text, delegate (TreeModel m, TreeIter a, TreeIter b) {
				return string.Compare ((string) m.GetValue (a, LISTCOL_Text), (string) m.GetValue (b, LISTCOL_Text));
			});
			targetStore.SetSortColumnId (LISTCOL_Text, SortType.Ascending);
			
			//set up the view
			targetView.Model = targetStore;
			targetView.HeadersVisible = false;
			
			CellRendererToggle toggleRenderer = new CellRendererToggle ();
			toggleRenderer.Activatable = true;
			toggleRenderer.Xpad = 6;
			TreeViewColumn checkCol = new TreeViewColumn ("", toggleRenderer, "active", LISTCOL_Checked);
			checkCol.Expand = false;
			targetView.AppendColumn (checkCol);
			toggleRenderer.Toggled += HandleToggle;
			
			CellRendererText textRenderer = new CellRendererText ();
			textRenderer.WrapMode = Pango.WrapMode.WordChar;
			targetView.AppendColumn ("", textRenderer, "markup", LISTCOL_Text);
			
			fillStore ();
		}
		
		void fillStore ()
		{
			int count = 0;
			TreeIter lastIter = TreeIter.Zero;
			foreach (WebDeployTarget target in project.WebDeployTargets) {
				if (target.ValidForDeployment) {
					lastIter = targetStore.AppendValues (target.GetMarkup (), target, false);
					count++;
				}
			}
			
			//select some targets by default if appropriate
			if (count == 1) {
				targetStore.SetValue (lastIter, LISTCOL_Checked, true);
			}
			//FIXME: store/load other selections in .userprefs file
			UpdateButtonState ();
		}
		
		public ICollection<WebDeployTarget> GetSelectedTargets ()
		{
			List<WebDeployTarget> targets = new List<WebDeployTarget> ();
			foreach (object[] row in targetStore)
				if (((bool)row [LISTCOL_Checked]) == true)
					targets.Add ((WebDeployTarget) row [LISTCOL_Target]);
			return targets;
		}
		
		void HandleToggle (object sender, ToggledArgs e)
		{
			TreeIter iter; 
			if (targetStore.GetIter (out iter, new TreePath (e.Path))) {
				bool currentVal = (bool) targetStore.GetValue (iter, LISTCOL_Checked);
				targetStore.SetValue (iter, LISTCOL_Checked, !currentVal);
			}
			UpdateButtonState ();
		}
		
		void UpdateButtonState ()
		{
			bool targetSelected = false;
			foreach (object[] row in targetStore)
				if (((bool)row [LISTCOL_Checked]) == true)
					targetSelected = true;
			buttonDeploy.Sensitive = targetSelected;
		}
		
		protected virtual void editTargetsClicked (object sender, System.EventArgs e)
		{
			IdeApp.ProjectOperations.ShowOptions (project, "MonoDevelop.AspNet.Deployment");
			targetStore.Clear ();
			fillStore ();
		}
	}
}
