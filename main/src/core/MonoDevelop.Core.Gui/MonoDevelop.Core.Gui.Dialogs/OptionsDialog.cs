// OptionsDialog.cs
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
using System.Collections.Generic;
using Gtk;
using Mono.Addins;
using MonoDevelop.Core.Gui.Codons;

namespace MonoDevelop.Core.Gui.Dialogs
{
	public partial class OptionsDialog : Gtk.Dialog
	{
		TreeStore store;
		Dictionary<OptionsDialogSection, SectionPage> pages = new Dictionary<OptionsDialogSection, SectionPage> ();
		Dictionary<OptionsPanelNode, PanelInstance> panels = new Dictionary<OptionsPanelNode,PanelInstance> ();
		object mainDataObject;
		string extensionPath;
		ExtensionContext extensionContext;
		
		public object DataObject {
			get {
				return mainDataObject;
			}
		}

		public ExtensionContext ExtensionContext {
			get {
				return extensionContext;
			}
		}
		
		public OptionsDialog (string extensionPath): this (null, null, extensionPath)
		{
		}
		
		public OptionsDialog (Gtk.Window parentWindow, object dataObject, string extensionPath)
		{
			this.Build();
			
			extensionContext = AddinManager.CreateExtensionContext ();
			
			this.mainDataObject = dataObject;
			this.extensionPath = extensionPath;
			
			if (parentWindow != null)
				TransientFor = parentWindow;
			
			store = new TreeStore (typeof(OptionsDialogSection), typeof(string), typeof(string), typeof(bool));
			tree.Model = store;
			tree.HeadersVisible = false;
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			crp.StockSize = (uint) IconSize.Menu;
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock-id", 1);
			col.AddAttribute (crp, "visible", 3);
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", 2);
			tree.AppendColumn (col);
			
			tree.Selection.Changed += OnSelectionChanged;
			
			InitializeContext (extensionContext);
			
			FillTree ();
			
			TreeIter it;
			if (store.GetIterFirst (out it)) {
				tree.Selection.SelectIter (it);
				do {
					tree.ExpandRow (store.GetPath (it), false);
				} while (store.IterNext (ref it));
			}
		}
		
		public void FillTree ()
		{
			DetachWidgets ();
			pages.Clear ();
			store.Clear ();
			foreach (OptionsDialogSection section in extensionContext.GetExtensionNodes (extensionPath)) {
				AddSection (section, mainDataObject);
			}
		}
		
		protected virtual void InitializeContext (ExtensionContext extensionContext)
		{
		}
		
		public override void Dispose ()
		{
			foreach (PanelInstance pi in panels.Values)
				if (pi.Widget != null)
					pi.Widget.Destroy ();
			base.Dispose ();
		}
		
		public void SelectPanel (string id)
		{
			foreach (OptionsDialogSection section in pages.Keys) {
				if (section.Id == id) {
					ShowPage (section);
					return;
				}
			}
		}
		
		protected void AddSection (OptionsDialogSection section, object dataObject)
		{
			AddSection (TreeIter.Zero, section, dataObject);
		}
		
		protected void AddSection (TreeIter parentIter, OptionsDialogSection section, object dataObject)
		{
			TreeIter it;
			if (parentIter.Equals (TreeIter.Zero)) {
				it = store.AppendValues (section, null, section.Label, false);
			}
			else {
				string icon = string.IsNullOrEmpty (section.Icon) ? "gtk-preferences" : section.Icon;
				it = store.AppendValues (parentIter, section, icon, section.Label, true);
			}
			
			AddChildSections (it, section, dataObject);
			
			// Remove the section if it doesn't have children nor panels
			
			SectionPage page = CreatePage (section, dataObject);
			TreeIter cit;
			if (page.Panels.Count == 0 && !store.IterChildren (out cit, it))
				store.Remove (ref it);
		}
		
		protected virtual void AddChildSections (TreeIter parentIter, OptionsDialogSection section, object dataObject)
		{
			foreach (ExtensionNode nod in section.ChildNodes) {
				if (nod is OptionsDialogSection)
					AddSection (parentIter, (OptionsDialogSection) nod, dataObject);
			}
		}
		
		void DetachWidgets ()
		{
			foreach (Gtk.Widget w in pageFrame.Children)
				pageFrame.Remove (w);

			foreach (SectionPage sp in pages.Values) {
				foreach (PanelInstance pi in sp.Panels) {
					if (pi.Widget != null && pi.Widget.Parent != null)
						((Gtk.Container)pi.Widget.Parent).Remove (pi.Widget);
				}
				if (sp.Widget != null) {
					sp.Widget.Destroy ();
					sp.Widget = null;
				}
			}
		}
		
		protected override void OnResponse (ResponseType resp)
		{
			if (resp == ResponseType.Ok) {
				
				// Validate changes before saving
				if (!ValidateChanges ())
					return;
				
				// Now save
				ApplyChanges ();
			}
			base.OnResponse (resp);
			DetachWidgets ();
		}
		
		protected virtual bool ValidateChanges ()
		{
			foreach (SectionPage sp in pages.Values) {
				if (sp.Widget == null)
					continue;
				foreach (PanelInstance pi in sp.Panels) {
					if (!pi.Panel.ValidateChanges ())
						return false; // Not valid
				}
			}
			return true;
		}
		
		protected virtual void ApplyChanges ()
		{
			foreach (SectionPage sp in pages.Values) {
				if (sp.Widget == null)
					continue;
				foreach (PanelInstance pi in sp.Panels)
					pi.Panel.ApplyChanges ();
			}
		}
		
		
		void OnSelectionChanged (object s, EventArgs a)
		{
			TreeIter it;
			if (tree.Selection.GetSelected (out it)) {
				OptionsDialogSection section = (OptionsDialogSection) store.GetValue (it, 0);
				ShowPage (section);
			}
		}
		
		protected void ShowPage (OptionsDialogSection section)
		{
			SectionPage page;
			if (!pages.TryGetValue (section, out page))
				return;
			
			if (page.Panels.Count == 0) {
				foreach (ExtensionNode node in section.ChildNodes) {
					if (node is OptionsDialogSection) {
						ShowPage ((OptionsDialogSection) node);
						return;
					}
				}
			}
			
			foreach (Gtk.Widget w in pageFrame.Children) {
				Container cc = w as Gtk.Container;
				if (cc != null) {
					foreach (Gtk.Widget cw in cc)
						cw.Hide ();
				}
				pageFrame.Remove (w);
			}
			
			if (page.Widget == null)
				CreatePageWidget (page);
			
			labelTitle.Markup = "<span weight=\"bold\" size=\"x-large\">" + GLib.Markup.EscapeText (section.Label) + "</span>";
			if (!string.IsNullOrEmpty (section.Icon))
				image.Stock = section.Icon;
			else
				image.Stock = "gtk-preferences";
			pageFrame.PackStart (page.Widget, true, true, 0);
			
			// Ensures that the Shown event is fired for each panel
			Container c = page.Widget as Gtk.Container;
			if (c != null) {
				foreach (Gtk.Widget cw in c)
					cw.Show ();
			}
		}
		
		SectionPage CreatePage (OptionsDialogSection section, object dataObject)
		{
			SectionPage page;
			if (pages.TryGetValue (section, out page))
				return page;
			
			page = new SectionPage ();
			page.Panels = new List<PanelInstance> ();
			pages [section] = page;
			
			List<OptionsPanelNode> nodes = new List<OptionsPanelNode> ();
			if (!string.IsNullOrEmpty (section.TypeName))
				nodes.Add (section);
			foreach (ExtensionNode nod in section.ChildNodes) {
				OptionsPanelNode node = nod as OptionsPanelNode;
				if (node != null && !(node is OptionsDialogSection))
					nodes.Add (node);
			}
			
			foreach (OptionsPanelNode node in nodes)
			{
				PanelInstance pi = null;
				if (panels.TryGetValue (node, out pi)) {
					if (pi.DataObject == dataObject) {
						// The panel can be reused
						if (!pi.Panel.IsVisible ())
							continue;
					} else {
						// If the data object has changed, this panel instance can't be
						// reused anymore. Destroy it.
						panels.Remove (node);
						if (pi.Widget != null)
							pi.Widget.Destroy ();
						pi = null;
					}
				}
				if (pi == null) {
					IOptionsPanel panel = node.CreatePanel ();
					panel.Initialize (this, dataObject);
					if (!panel.IsVisible ())
						continue;
					pi = new PanelInstance ();
					pi.Panel = panel;
					pi.Node = node;
					pi.DataObject = dataObject;
					panels [node] = pi;
				}
				page.Panels.Add (pi);
			}
			return page;
		}
		
		void CreatePageWidget (SectionPage page)
		{
			List<PanelInstance> boxPanels = new List<PanelInstance> ();
			List<PanelInstance> tabPanels = new List<PanelInstance> ();
			
			foreach (PanelInstance pi in page.Panels) {
				if (pi.Widget == null)
					pi.Widget = pi.Panel.CreatePanelWidget ();
				else if (pi.Widget.Parent != null)
					((Gtk.Container) pi.Widget.Parent).Remove (pi.Widget);
					
				if (pi.Node.Grouping == PanelGrouping.Tab)
					tabPanels.Add (pi);
				else
					boxPanels.Add (pi);
			}
			
			// Try to fit panels with grouping=box or auto in the main page.
			// If they don't fit. Move auto panels to its own tab page.
			
			int mainPageSize;
			bool fits;
			do {
				PanelInstance lastAuto = null;
				mainPageSize = 0;
				foreach (PanelInstance pi in boxPanels) {
					if (pi.Node.Grouping == PanelGrouping.Auto)
						lastAuto = pi;
					mainPageSize += pi.Widget.SizeRequest ().Height + 6;
				}
				fits = mainPageSize <= pageFrame.Allocation.Height;
				if (!fits) {
					if (lastAuto != null && boxPanels.Count > 1) {
						boxPanels.Remove (lastAuto);
						tabPanels.Insert (0, lastAuto);
					} else {
						fits = true;
					}
				}
			} while (!fits);
			
			Gtk.VBox box = new VBox (false, 12);
			box.Show ();
			for (int n=0; n<boxPanels.Count; n++) {
				if (n != 0) {
					HSeparator sep = new HSeparator ();
					sep.Show ();
					box.PackStart (sep, false, false, 0);
				}
				PanelInstance pi = boxPanels [n];
				box.PackStart (pi.Widget, pi.Node.Fill, pi.Node.Fill, 0);
				pi.Widget.Show ();
			}
			
			if (tabPanels.Count > 0) {
				Gtk.Notebook nb = new Notebook ();
				nb.Show ();
				Gtk.Label blab = new Gtk.Label (GettextCatalog.GetString ("General"));
				blab.Show ();
				box.BorderWidth = 9;
				nb.InsertPage (box, blab, -1);
				foreach (PanelInstance pi in tabPanels) {
					Gtk.Label lab = new Gtk.Label (GettextCatalog.GetString (pi.Node.Label));
					lab.Show ();
					Gtk.Alignment a = new Alignment (0, 0, 1, 1);
					a.BorderWidth = 9;
					a.Show ();
					a.Add (pi.Widget);
					nb.InsertPage (a, lab, -1);
					pi.Widget.Show ();
				}
				page.Widget = nb;
			} else {
				page.Widget = box;
			}
		}
		
		class PanelInstance
		{
			public IOptionsPanel Panel;
			public Gtk.Widget Widget;
			public OptionsPanelNode Node;
			public object DataObject;
		}
		
		class SectionPage
		{
			public List<PanelInstance> Panels;
			public Gtk.Widget Widget;
		}
	}
}
