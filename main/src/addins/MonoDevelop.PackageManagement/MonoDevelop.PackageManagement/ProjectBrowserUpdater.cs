// 
// ProjectBrowserUpdater.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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

namespace ICSharpCode.PackageManagement
{
	public class ProjectBrowserUpdater : IProjectBrowserUpdater
	{
//		ProjectBrowserControl projectBrowser;
//		
//		public ProjectBrowserUpdater()
//			: this(ProjectBrowserPad.Instance.ProjectBrowserControl)
//		{
//		}
//		
//		public ProjectBrowserUpdater(ProjectBrowserControl projectBrowser)
//		{
//			this.projectBrowser = projectBrowser;
//			ProjectService.ProjectItemAdded += ProjectItemAdded;
//		}
//
//		protected virtual void ProjectItemAdded(object sender, ProjectItemEventArgs e)
//		{
//			if (e.ProjectItem is FileProjectItem) {
//				AddFileProjectItemToProjectBrowser(e);
//			}
//		}
//		
//		void AddFileProjectItemToProjectBrowser(ProjectItemEventArgs e)
//		{
//			var visitor = new UpdateProjectBrowserFileNodesVisitor(e);
//			foreach (AbstractProjectBrowserTreeNode node in projectBrowser.TreeView.Nodes) {
//				node.AcceptVisitor(visitor, null);
//			}
//		}
		
		public void Dispose()
		{
		//	ProjectService.ProjectItemAdded -= ProjectItemAdded;
		}
	}
}
