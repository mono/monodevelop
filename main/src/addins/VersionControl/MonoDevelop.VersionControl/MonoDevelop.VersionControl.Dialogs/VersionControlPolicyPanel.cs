// CommitMessageStylePanel.cs
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
using Gtk;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl
{
	public class VersionControlPolicyPanel: PolicyOptionsPanel<VersionControlPolicy>
	{
		CommitMessageStylePanelWidget widget;
		CommitMessageFormat format;
		
		public override Widget CreatePanelWidget ()
		{
			format = new CommitMessageFormat ();
			widget = new CommitMessageStylePanelWidget ();
			widget.Changed += delegate {
				UpdateSelectedNamedPolicy ();
			};
			widget.Show ();
			return widget;
		}
		
		protected override string PolicyTitleWithMnemonic {
			get {
				return GettextCatalog.GetString ("Version Control _Policy");
			}
		}
		
		protected override VersionControlPolicy GetPolicy ()
		{
			VersionControlPolicy policy = new VersionControlPolicy ();
			policy.CommitMessageStyle.CopyFrom (format.Style);
			return policy;
		}
		
		protected override void LoadFrom (VersionControlPolicy policy)
		{
			format.Style = new CommitMessageStyle ();
			format.Style.CopyFrom (policy.CommitMessageStyle);
			AuthorInformation uinfo;
			if (ConfiguredSolutionItem != null)
				uinfo = IdeApp.Workspace.GetAuthorInformation (ConfiguredSolutionItem);
			else
				uinfo = IdeApp.Workspace.GetAuthorInformation (ConfiguredSolution);
			widget.Load (format, uinfo);
		}
	}
}
