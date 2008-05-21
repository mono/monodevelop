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
	public partial class ProjectOptionPanelWidget : Gtk.Bin
	{		
		ChangeLogData changeLogData;
		
		public ProjectOptionPanelWidget (SolutionItem entry)
		{
			this.Build();
			
			changeLogData = ChangeLogService.GetChangeLogData (entry);
			
			if (entry.ParentFolder == null)
				parentRadioButton.Sensitive = false;
			
			switch (changeLogData.Policy)
			{
				case ChangeLogPolicy.NoChangeLog:
					noneRadioButton.Active = true;				
					break;
				case ChangeLogPolicy.UseParentPolicy:
					parentRadioButton.Active = true;				
					break;
				case ChangeLogPolicy.UpdateNearestChangeLog:
					nearestRadioButton.Active = true;				
					break;
				case ChangeLogPolicy.OneChangeLogInProjectRootDirectory:
					oneChangeLogInProjectRootDirectoryRadioButton.Active = true;				
					break;
				case ChangeLogPolicy.OneChangeLogInEachDirectory:
					oneChangeLogInEachDirectoryRadioButton.Active = true;				
					break;
			}									
		}
		
		public void Store ()
		{
			if (noneRadioButton.Active)
				changeLogData.Policy =  ChangeLogPolicy.NoChangeLog;
			
			if (parentRadioButton.Active)
				changeLogData.Policy =  ChangeLogPolicy.UseParentPolicy;
			
			if (nearestRadioButton.Active)
				changeLogData.Policy =  ChangeLogPolicy.UpdateNearestChangeLog;
			
			if (oneChangeLogInProjectRootDirectoryRadioButton.Active)
				changeLogData.Policy = ChangeLogPolicy.OneChangeLogInProjectRootDirectory;
			
			if (oneChangeLogInEachDirectoryRadioButton.Active)
				changeLogData.Policy = ChangeLogPolicy.OneChangeLogInEachDirectory;
		}
	}
}
