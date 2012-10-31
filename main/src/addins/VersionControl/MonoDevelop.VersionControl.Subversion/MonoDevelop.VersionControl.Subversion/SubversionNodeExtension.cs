// SubversionNodeExtension.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
 
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.VersionControl.Subversion
{
	public class SubversionNodeExtension: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			//Console.Error.WriteLine(dataType);
			return typeof(ProjectFile).IsAssignableFrom (dataType)
				|| typeof(SystemFile).IsAssignableFrom (dataType)
				|| typeof(ProjectFolder).IsAssignableFrom (dataType)
				|| typeof(IWorkspaceObject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(SubversionCommandHandler); }
		}
	}
	
	class SubversionCommandHandler : VersionControlCommandHandler 
	{
		[CommandHandler (Commands.Resolve)]
		protected void OnResolve()
		{
			foreach (VersionControlItemList items in GetItems ().SplitByRepository ()) {
				FilePath[] files = new FilePath[items.Count];
				for (int n=0; n<files.Length; n++)
					files [n] = items [n].Path;
				((SubversionRepository)items[0].Repository).Resolve (files, true, new NullProgressMonitor ());
			}
		}
		
		[CommandUpdateHandler (Commands.Resolve)]
		protected void UpdateResolve (CommandInfo item)
		{
			foreach (VersionControlItem vit in GetItems ()) {
				if (!(vit.Repository is SubversionRepository)) {
					item.Visible = false;
					return;
				}
				
				if (!vit.IsDirectory) {
					VersionInfo vi = vit.Repository.GetVersionInfo (vit.Path);
					if (vi != null && (vi.Status & VersionStatus.Conflicted) == 0) {
						item.Visible = false;
						return;
					}
				}
			}
		}
	}
}
