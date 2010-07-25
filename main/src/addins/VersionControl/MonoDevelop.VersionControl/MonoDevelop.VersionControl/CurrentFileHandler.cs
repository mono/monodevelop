// 
// CurrentFileHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide;
using MonoDevelop.VersionControl.Views;

namespace MonoDevelop.VersionControl
{
	abstract class CurrentFileHandler: CommandHandler
	{
		public VersionControlItem CreateItem ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var project = doc.Project ?? IdeApp.ProjectOperations.CurrentSelectedProject;
			if (doc == null || project == null)
				return null;
			
			var path = doc.FileName;
			var repo = VersionControlService.GetRepository (project);
			return new VersionControlItem (repo, project, path, false);
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = CreateItem () != null;
		}
	}
	
	class CurrentFileDiffHandler : CurrentFileHandler
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			ComparisonView.AttachViewContents (doc, CreateItem ());
			doc.Window.SwitchView (1);
		}
	}
	
	class CurrentFileBlameHandler : CurrentFileHandler
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			ComparisonView.AttachViewContents (doc, CreateItem ());
			doc.Window.SwitchView (3);
		}
	}
	
	class CurrentFileLogHandler : CurrentFileHandler
	{
		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			ComparisonView.AttachViewContents (doc, CreateItem ());
			doc.Window.SwitchView (4);
		}
	}
}

