// ProjectOptionPanelWidget.cs
//
// Author:
//   Jacob Ilsø Christensen
//
// Copyright (C) 2007  Jacob Ilsø Christensen
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
//
//

using System;
using MonoDevelop.Projects;

namespace MonoDevelop.ChangeLogAddIn
{	
	partial class ProjectOptionPanelWidget : Gtk.Bin
	{
		ProjectOptionPanel parent;
		
		public ProjectOptionPanelWidget (ProjectOptionPanel parent)
		{
			this.parent = parent;
			this.Build ();
		}
		
		public void LoadFrom (ChangeLogPolicy policy)
		{
			switch (policy.UpdateMode) {
			case ChangeLogUpdateMode.None:
				noneRadioButton.Active = true;				
				break;
			case ChangeLogUpdateMode.Nearest:
				nearestRadioButton.Active = true;				
				break;
			case ChangeLogUpdateMode.ProjectRoot:
				oneChangeLogInProjectRootDirectoryRadioButton.Active = true;				
				break;
			case ChangeLogUpdateMode.Directory:
				oneChangeLogInEachDirectoryRadioButton.Active = true;				
				break;
			}
			
			this.checkVersionControl.Active = policy.VcsIntegration != VcsIntegration.None;
			this.checkRequireOnCommit.Active = policy.VcsIntegration == VcsIntegration.RequireEntry;
		}
		
		public ChangeLogPolicy GetPolicy ()
		{
			ChangeLogUpdateMode mode = ChangeLogUpdateMode.None;
			if (nearestRadioButton.Active)
				mode =  ChangeLogUpdateMode.Nearest;
			else if (oneChangeLogInProjectRootDirectoryRadioButton.Active)
				mode = ChangeLogUpdateMode.ProjectRoot;
			else if (oneChangeLogInEachDirectoryRadioButton.Active)
				mode = ChangeLogUpdateMode.Directory;
			
			VcsIntegration vcs = VcsIntegration.None;
			if (checkVersionControl.Active) {
				vcs = VcsIntegration.Enabled;
				if (checkRequireOnCommit.Active)
					vcs = VcsIntegration.RequireEntry;
			}
			
			return new ChangeLogPolicy (mode, vcs);
		}

		protected virtual void ValueChanged (object sender, System.EventArgs e)
		{
			//update sensitivity
			checkVersionControl.Sensitive = !noneRadioButton.Active;
			checkRequireOnCommit.Sensitive = (checkVersionControl.Sensitive && checkVersionControl.Active);
			
			parent.UpdateSelectedNamedPolicy ();
		}
	}
}
