// 
// ExtensionModelBrowserWidget.cs
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
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.AddinAuthoring.NodeBuilders;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using Mono.Addins.Description;
using MonoDevelop.AddinAuthoring.Gui;
using MonoDevelop.Projects;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ExtensionModelBrowserWidget : Gtk.Bin
	{
		ExtensibleTreeView tree;
		Gtk.Widget docView;
		Dictionary<AddinRegistry, RegistryInfo> registries = new Dictionary<AddinRegistry, RegistryInfo> ();
		Dictionary<Solution, RegistryInfo> solutions = new Dictionary<Solution, RegistryInfo> ();
		
		public ExtensionModelBrowserWidget ()
		{
			this.Build ();
			NodeBuilder[] builders = new NodeBuilder [] {
				new ExtensionNodeBuilder (),
				new ExtensionNodeNodeBuilder (),
				new ExtensionPointNodeBuilder (),
				new AddinNodeBuilder (),
				new SolutionNodeBuilder (true),
				new RegistryNodeBuilder (),
				new AddinCategoryNodeBuilder (),
				new MonoDevelop.Ide.Gui.Pads.ProjectPad.WorkspaceNodeBuilder (),
				new MonoDevelop.Ide.Gui.Pads.ProjectPad.SolutionFolderNodeBuilder ()
			};
			TreePadOption[] options = new TreePadOption [] {
				new TreePadOption ("ShowExistingNodes", GettextCatalog.GetString ("Show existing nodes"), true)
			};
			
			tree = new ExtensibleTreeView (builders, options);
			tree.ShowAll ();
			paned.Add1 (tree);
			
			foreach (Solution sol in IdeApp.Workspace.GetAllSolutions ())
				AddSolution (sol);
			
			docView = new Gtk.Label ();
			paned.Add2 (docView);
			
			tree.ShadowType = Gtk.ShadowType.In;
			tree.Tree.Selection.Changed += HandleSelectionChanged;
			
			AddinAuthoringService.RegistryChanged += OnRegistryChanged;
			IdeApp.Workspace.WorkspaceItemLoaded += OnSolutionLoaded;
			IdeApp.Workspace.WorkspaceItemUnloaded += OnSolutionUnloaded;
		}
		
		protected override void OnDestroyed ()
		{
			AddinAuthoringService.RegistryChanged -= OnRegistryChanged;
			IdeApp.Workspace.WorkspaceItemLoaded -= OnSolutionLoaded;
			IdeApp.Workspace.WorkspaceItemUnloaded -= OnSolutionUnloaded;
			base.OnDestroyed ();
		}
		
		void AddSolution (Solution sol)
		{
			SolutionAddinData data = sol.GetAddinData ();
			if (data != null) {
				RegistryInfo reg = new RegistryInfo ();
				reg.ApplicationName = data.ApplicationName;
				reg.CachedRegistry = data.Registry;
				tree.AddChild (reg);
				registries [reg.CachedRegistry] = reg;
				solutions [sol] = reg;
			}
		}

		void OnSolutionLoaded (object sender, WorkspaceItemEventArgs e)
		{
			if (e.Item is Solution)
				AddSolution ((Solution)e.Item);
		}

		void OnSolutionUnloaded (object sender, WorkspaceItemEventArgs e)
		{
			if (e.Item is Solution) {
				RegistryInfo reg;
				if (solutions.TryGetValue ((Solution) e.Item, out reg)) {
					var nav = tree.BuilderContext.GetTreeBuilder (reg);
					if (nav != null)
						nav.Remove ();
				}
			}
		}

		void OnRegistryChanged (object sender, RegistryEventArgs e)
		{
			RegistryInfo reg;
			if (registries.TryGetValue (e.Registry, out reg)) {
				var nav = tree.BuilderContext.GetTreeBuilder (reg);
				if (nav != null)
					nav.UpdateAll ();
			}
		}

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			ITreeNavigator nav = tree.GetSelectedNode ();
			if (nav != null)
				ShowDocumentation (nav.DataItem);
		}
		
		void ShowDocumentation (object ob)
		{
			if (docView != null) {
				paned.Remove (docView);
				docView.Destroy ();
				docView = null;
			}
			
			ITreeNavigator nav = tree.GetSelectedNode ();
			RegistryInfo regInfo = (RegistryInfo) nav.GetParentDataItem (typeof(RegistryInfo), true);
			AddinRegistry reg = regInfo.CachedRegistry;
			
			if (ob is AddinDescription) {
				AddinView view = new AddinView ();
				view.Fill ((AddinDescription) ob);
				docView = view;
			}
			else if (ob is ExtensionPoint) {
				ExtensionPointView view = new ExtensionPointView ();
				view.Fill ((ExtensionPoint) ob, reg);
				docView = view;
			}
			else if (ob is Extension) {
				ExtensionView view = new ExtensionView ();
				view.Fill ((Extension) ob, nav);
				docView = view;
			}
			else if (ob is ExtensionNodeInfo) {
				ExtensionNodeView view = new ExtensionNodeView ();
				view.Fill (((ExtensionNodeInfo) ob).Node);
				docView = view;
			}
			
			if (docView == null)
				docView = new Gtk.Label ();
			
			docView.ShowAll ();
			paned.Add2 (docView);
		}
		
		protected void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			SelectRepositoryDialog dlg = new SelectRepositoryDialog (null);
			dlg.TransientFor = this.Toplevel as Gtk.Window;
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				RegistryInfo reg = dlg.SelectedApplication;
				tree.AddChild (reg);
			}
			dlg.Destroy ();
		}
	}
}

