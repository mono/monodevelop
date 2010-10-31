// 
// GitNodeBuilderExtension.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using System.Collections.Generic;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Git
{
	public class GitNodeBuilderExtension: NodeBuilderExtension
	{
		Dictionary<FilePath,IWorkspaceObject> repos = new Dictionary<FilePath, IWorkspaceObject> ();
		
		protected override void Initialize ()
		{
			base.Initialize ();
			IdeApp.CommandService.ApplicationFocusIn += HandleApplicationFocusIn;
			GitRepository.BranchSelectionChanged += HandleBranchSelectionChanged;
		}
		
		public override void Dispose ()
		{
			IdeApp.CommandService.ApplicationFocusIn -= HandleApplicationFocusIn;
			GitRepository.BranchSelectionChanged -= HandleBranchSelectionChanged;
			base.Dispose ();
		}
		
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(IWorkspaceObject).IsAssignableFrom (dataType);
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IWorkspaceObject ob = (IWorkspaceObject) dataObject;
			GitRepository rep = VersionControlService.GetRepository (ob) as GitRepository;
			if (rep != null) {
				IWorkspaceObject rob;
				if (repos.TryGetValue (rep.RootPath.CanonicalPath, out rob)) {
					if (ob == rob)
						label += " (" + rep.GetCurrentBranch () + ")";
				}
			}
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			IWorkspaceObject ob = (IWorkspaceObject) dataObject;
			GitRepository rep = VersionControlService.GetRepository (ob) as GitRepository;
			if (rep != null && !repos.ContainsKey (rep.RootPath.CanonicalPath)) {
				repos [rep.RootPath] = ob;
			}
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			IWorkspaceObject ob = (IWorkspaceObject) dataObject;
			GitRepository rep = VersionControlService.GetRepository (ob) as GitRepository;
			IWorkspaceObject rob;
			if (rep != null && repos.TryGetValue (rep.RootPath.CanonicalPath, out rob)) {
				if (ob == rob)
					repos.Remove (rep.RootPath.CanonicalPath);
			}
		}

		void HandleApplicationFocusIn (object sender, EventArgs e)
		{
			foreach (object ob in repos.Values) {
				ITreeBuilder tb = Context.GetTreeBuilder (ob);
				if (tb != null)
					tb.Update ();
			}
		}

		void HandleBranchSelectionChanged (object sender, EventArgs e)
		{
			HandleApplicationFocusIn (null, null);
		}
	}
}

