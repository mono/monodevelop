//
// GitHubPad.cs
//
// Author:
//       Praveena <>
//
// Copyright (c) 2014 Praveena
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

using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using GitHub.Repository;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;
using Gtk;
using MonoDevelop.Components;
using Xwt.Drawing;
using GitHub.Repository.Services;

namespace GitHub.Repository.Gui
{
	public class GitHubPad : TreeViewPad
	{
		GitHubRepoService gitHubRepoService = GitHubRepoService.Instance;
		ListStore detailsStore;
		VPaned paned;
		TreeView detailsTree;
		VBox detailsPad;
		EventHandler repoChangedHandler;

		public override void Initialize (NodeBuilder[] builders, TreePadOption[] options, string menuPath)
		{
			base.Initialize (builders, options, menuPath);

			repoChangedHandler = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnGitHubRepoListChanged));

			paned = new VPaned ();

			VBox vbox = new VBox ();

			DockItemToolbar topToolbar = Window.GetToolbar (PositionType.Top);
			topToolbar.ShowAll ();

			vbox.PackEnd (base.Control, true, true, 0);
			vbox.FocusChain = new Gtk.Widget [] { base.Control };

			paned.Pack1 (vbox, true, true);


			Frame tf = new Frame ();
			ScrolledWindow sw = new ScrolledWindow ();
			detailsTree = new TreeView ();

			detailsTree.HeadersVisible = true;
			detailsTree.RulesHint = true;
			detailsStore = new ListStore (typeof(object), typeof(string), typeof (string), typeof (string), typeof (string));

			CellRendererText trtest = new CellRendererText ();
			CellRendererText tr;

			TreeViewColumn col5 = new TreeViewColumn ();
			col5.Expand = false;
			col5.Alignment = 0.5f;

			col5.Widget = new ImageView (Xwt.Drawing.Image.FromResource ("pad-github-16.png"));
			col5.Widget.Show ();
			tr = new CellRendererText ();
			tr.Xalign = 0.5f;
			col5.PackStart (tr, false);
			col5.AddAttribute (tr, "markup", 4);
			detailsTree.AppendColumn (col5);

			detailsTree.Model = detailsStore;

			sw.Add (detailsTree);
			tf.Add (sw);
			tf.ShowAll ();

			foreach (GitHubRepo r in gitHubRepoService.RepoList) 
			{
				TreeView.AddChild (r);
			}
		}


		GitHubRepo GetSelectedRepo ()
		{
			ITreeNavigator nav = TreeView.GetSelectedNode ();
			if (nav == null)
				return null;
			return nav.DataItem as GitHubRepo;
		}

		void OnGitHubRepoListChanged (object sender, EventArgs e)
		{
			if (gitHubRepoService.RepoList.Length > 0) {
				TreeView.Clear ();
				foreach (GitHubRepo r in gitHubRepoService.RepoList)
					TreeView.AddChild (r);
			}
			else {
				TreeView.Clear ();
			}
		}

		public void SelectTest (GitHubRepo t)
		{
			ITreeNavigator node = FindTestNode (t);
			if (node != null) {
				node.ExpandToNode ();
				node.Selected = true;
			}
		}

		ITreeNavigator FindTestNode (GitHubRepo t)
		{
			ITreeNavigator nav = TreeView.GetNodeAtObject (t);
			if (nav != null)
				return nav;
			else return null;
		}

		public override Gtk.Widget Control {
			get {
				return paned;
			}
		}

	}
}

