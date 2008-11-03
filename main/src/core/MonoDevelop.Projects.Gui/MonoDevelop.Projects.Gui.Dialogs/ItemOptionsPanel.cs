// SolutionItemOptionsPanel.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Projects.Gui.Dialogs
{
	public abstract class ItemOptionsPanel: OptionsPanel
	{
		SolutionEntityItem solutionItem;
		WorkspaceItem workspaceItem;
		
		public SolutionEntityItem ConfiguredSolutionItem {
			get {
				return solutionItem;
			}
		}
		
		public Project ConfiguredProject {
			get { return solutionItem as Project; }
		}
		
		public WorkspaceItem ConfiguredWorkspaceItem {
			get {
				return workspaceItem;
			}
		}
		
		public Solution ConfiguredSolution {
			get {
				return workspaceItem as Solution;
			}
		}
		
		public System.Collections.Generic.IEnumerable<ItemConfiguration> ItemConfigurations {
			get {
				if (ParentDialog is MultiConfigItemOptionsDialog)
					return ((MultiConfigItemOptionsDialog)ParentDialog).Configurations;
				else if (solutionItem != null)
					return solutionItem.Configurations;
				else if (ConfiguredSolution != null)
					return ConfiguredSolution.Configurations;
				else
					return new ItemConfiguration [0];
			}
		}

		public override void Initialize (OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
			
			solutionItem = dataObject as SolutionEntityItem;
			if (solutionItem != null)
				workspaceItem = solutionItem.ParentSolution;
			else
				workspaceItem = dataObject as WorkspaceItem;
		}
	}
}
