// 
// CodeFormattingPanelWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class CodeFormattingPanel: OptionsPanel
	{
		PolicyContainer policyContainer;
		Dictionary<string,MimeTypePanelData> typeSections = new Dictionary<string, MimeTypePanelData> ();
		List<string> globalMimeTypes;
		
		public override void Initialize (MonoDevelop.Ide.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
			
			if (dataObject is SolutionItem) {
				policyContainer = ((SolutionItem)dataObject).Policies;
			} else if (dataObject is Solution) {
				policyContainer = ((Solution)dataObject).Policies;
			} else if (dataObject is PolicySet) {
				policyContainer = ((PolicySet)dataObject);
				globalMimeTypes = new List<string> ();
				string userTypes = PropertyService.Get<string> ("MonoDevelop.Projects.GlobalPolicyMimeTypes", "");
				globalMimeTypes.AddRange (userTypes.Split (new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
			}
			
			foreach (string mt in GetItemMimeTypes ())
				AddPanel (mt);
		}
		
		MimeTypePanelData AddPanel (string mt)
		{
			MimeTypePanelData data = new MimeTypePanelData ();
			OptionsDialogSection sec = new OptionsDialogSection (typeof(MimeTypePolicyOptionsSection));
			sec.Fill = true;
			Gdk.Pixbuf icon = DesktopService.GetPixbufForType (mt, Gtk.IconSize.Menu);
			sec.Icon = ImageService.GetStockId (icon, Gtk.IconSize.Menu);
			data.Section = sec;
			data.MimeType = mt;
			data.TypeDescription = DesktopService.GetMimeTypeDescription (mt);
			if (string.IsNullOrEmpty (data.TypeDescription))
				data.TypeDescription = mt;
			data.DataObject = DataObject;
			data.PolicyContainer = policyContainer;
			sec.Label = data.TypeDescription;
			LoadPolicyTypeData (data, mt);
			typeSections [mt] = data;
			ParentDialog.AddChildSection (this, sec, data);
			return data;
		}
		
		void RemovePanel (string mt)
		{
			MimeTypePanelData data = typeSections [mt];
			typeSections.Remove (mt);
			ParentDialog.RemoveSection (data.Section);
		}
		
		internal MimeTypePanelData AddGlobalMimeType (string mt)
		{
			if (!globalMimeTypes.Contains (mt)) {
				globalMimeTypes.Add (mt);
				return AddPanel (mt);
			}
			return null;
		}
		
		internal void RemoveGlobalMimeType (string mt)
		{
			if (globalMimeTypes.Remove (mt))
				RemovePanel (mt);
		}
		
		public IEnumerable<MimeTypePanelData> GetMimeTypeData ()
		{
			return typeSections.Values;
		}
		
		public PolicyContainer PolicyContainer {
			get { return policyContainer; }
		}

		void LoadPolicyTypeData (MimeTypePanelData data, string mimeType)
		{
			List<string> types = new List<string> ();
			types.AddRange (DesktopService.GetMimeTypeInheritanceChain (mimeType));
			List<IMimeTypePolicyOptionsPanel> panels = new List<IMimeTypePolicyOptionsPanel> ();
			
			bool useParentPolicy = false;
			foreach (MimeTypeOptionsPanelNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/Gui/MimeTypePolicyPanels")) {
				if (!types.Contains (node.MimeType))
					continue;
				
				IMimeTypePolicyOptionsPanel panel = (IMimeTypePolicyOptionsPanel) node.CreateInstance (typeof(IMimeTypePolicyOptionsPanel));
				panel.Initialize (ParentDialog, DataObject);
				panel.InitializePolicy (policyContainer, mimeType, mimeType == node.MimeType);
				panel.Label = GettextCatalog.GetString (node.Label);
				if (!panel.IsVisible ())
					continue;
				
				if (!panel.HasCustomPolicy)
					useParentPolicy = true;
				
				panels.Add (panel);
			}
			data.Panels = panels;
			if (!policyContainer.IsRoot)
				data.UseParentPolicy = useParentPolicy;
		}
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return new CodeFormattingPanelWidget (this, ParentDialog);
		}
		
		public override void ApplyChanges ()
		{
			if (globalMimeTypes != null) {
				string types = string.Join (";", globalMimeTypes.ToArray ());
				PropertyService.Set ("MonoDevelop.Projects.GlobalPolicyMimeTypes", types);
			}
			// If a section is already loaded, changes will be committed in the panel
			foreach (MimeTypePanelData pd in typeSections.Values) {
				if (!pd.SectionLoaded)
					pd.ApplyChanges ();
			}
		}
		
		public IEnumerable<string> GetItemMimeTypes ()
		{
			HashSet<string> types = new HashSet<string> ();
			if (DataObject is Solution)
				GetItemMimeTypes (types, ((Solution)DataObject).RootFolder);
			else if (DataObject is SolutionItem)
				GetItemMimeTypes (types, (SolutionItem)DataObject);
			else {
				types.Add ("application/xml");
				foreach (MimeTypeOptionsPanelNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/Gui/MimeTypePolicyPanels")) {
					types.Add (node.MimeType);
					globalMimeTypes.Remove (node.MimeType);
				}
				types.UnionWith (globalMimeTypes);
			}
			return types;
		}
		
		public bool IsUserMimeType (string type)
		{
			return globalMimeTypes != null && globalMimeTypes.Contains (type);
		}
		
		void GetItemMimeTypes (HashSet<string> types, SolutionItem item)
		{
			if (item is SolutionFolder) {
				foreach (SolutionItem it in ((SolutionFolder)item).Items)
					GetItemMimeTypes (types, it);
			}
			else if (item is Project) {
				foreach (ProjectFile pf in ((Project)item).Files) {
					string mt = DesktopService.GetMimeTypeForUri (pf.FilePath);
					foreach (string mth in DesktopService.GetMimeTypeInheritanceChain (mt))
						types.Add (mth);
				}
			}
		}
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	partial class CodeFormattingPanelWidget : Gtk.Bin
	{
		CodeFormattingPanel panel;
		Gtk.ListStore store;
		OptionsDialog dialog;
		
		public CodeFormattingPanelWidget (CodeFormattingPanel panel, OptionsDialog dialog)
		{
			this.Build();
			this.panel = panel;
			this.dialog = dialog;
			
			store = new Gtk.ListStore (typeof(MimeTypePanelData), typeof(Gdk.Pixbuf), typeof(string));
			tree.Model = store;
			
			boxButtons.Visible = panel.DataObject is PolicySet;
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			Gtk.CellRendererPixbuf crp = new Gtk.CellRendererPixbuf ();
			
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("File Type");
			col.PackStart (crp, false);
			col.PackStart (crt, true);
			col.AddAttribute (crp, "pixbuf", 1);
			col.AddAttribute (crt, "text", 2);
			tree.AppendColumn (col);
			store.SetSortColumnId (2, Gtk.SortType.Ascending);
			
			CellRendererComboBox comboCell = new CellRendererComboBox ();
			comboCell.Changed += OnPolicySelectionChanged;
			Gtk.TreeViewColumn polCol = tree.AppendColumn (GettextCatalog.GetString ("Policy"), comboCell, new Gtk.TreeCellDataFunc (OnSetPolicyData));
			
			tree.Selection.Changed += delegate {
				Gtk.TreeIter it;
				tree.Selection.GetSelected (out it);
				Gtk.TreeViewColumn ccol;
				Gtk.TreePath path;
				tree.GetCursor (out path, out ccol);
				if (ccol == polCol)
					tree.SetCursor (path, ccol, true);
			};

			Fill ();
			UpdateButtons ();
			
			tree.Selection.Changed += delegate {
				UpdateButtons ();
			};
		}
		
		static readonly string parentPolicyText = GettextCatalog.GetString ("(Inherited Policy)");
		static readonly string customPolicyText = GettextCatalog.GetString ("(Custom)");
		
		void OnSetPolicyData (Gtk.TreeViewColumn treeColumn, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			MimeTypePanelData mt = (MimeTypePanelData) store.GetValue (iter, 0);
			
			string selection;
			if (mt.UseParentPolicy)
				selection = parentPolicyText;
			else {
				PolicySet matchingSet = mt.GetMatchingSet ();
				if (matchingSet != null)
					selection = matchingSet.Name;
				else
					selection = customPolicyText;
			}
			
			CellRendererComboBox comboCell = (CellRendererComboBox) cell;
			comboCell.Values = GetComboOptions (mt);
			comboCell.Text = selection;
		}
		
		string[] GetComboOptions (MimeTypePanelData mt)
		{
			List<string> values = new List<string> ();
			
			if (!this.panel.PolicyContainer.IsRoot)
				values.Add (parentPolicyText);
			
			foreach (PolicySet set in mt.GetSupportedPolicySets ())
				values.Add (set.Name);

			values.Add (customPolicyText);
			return values.ToArray ();
		}
		
		void OnPolicySelectionChanged (object s, ComboSelectionChangedArgs args)
		{
			Gtk.TreeIter iter;
			if (store.GetIter (out iter, new Gtk.TreePath (args.Path))) {
				MimeTypePanelData mt = (MimeTypePanelData) store.GetValue (iter, 0);
				if (args.Active != -1) {
					string sel = args.ActiveText;
					if (sel == parentPolicyText)
						mt.UseParentPolicy = true;
					else if (sel != customPolicyText) {
						PolicySet pset = PolicyService.GetPolicySet (sel);
						mt.AssignPolicies (pset);
					}
				}
			}
		}
		
		void Fill ()
		{
			foreach (MimeTypePanelData mt in panel.GetMimeTypeData ()) {
				store.AppendValues (mt, DesktopService.GetPixbufForType (mt.MimeType, Gtk.IconSize.Menu), mt.TypeDescription);
			}
		}

		protected void OnButtonEditClicked (object sender, System.EventArgs e)
		{
			Gtk.TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				MimeTypePanelData mt = (MimeTypePanelData) store.GetValue (iter, 0);
				dialog.ShowPage (mt.Section);
			}
		}

		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			AddMimeTypeDialog dlg = new AddMimeTypeDialog (panel.GetItemMimeTypes ());
			try {
				if (MessageService.RunCustomDialog (dlg, this.Toplevel as Gtk.Window) == (int) Gtk.ResponseType.Ok) {
					MimeTypePanelData mt = panel.AddGlobalMimeType (dlg.MimeType);
					store.AppendValues (mt, DesktopService.GetPixbufForType (mt.MimeType, Gtk.IconSize.Menu), mt.TypeDescription);
				}
			} finally {
				dlg.Destroy ();
			}
		}

		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			Gtk.TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				MimeTypePanelData mt = (MimeTypePanelData) store.GetValue (iter, 0);
				if (MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to remove the formatting policy for the type '{0}'?", mt.TypeDescription), AlertButton.Delete)) {
					panel.RemoveGlobalMimeType (mt.MimeType);
					store.Remove (ref iter);
				}
			}
		}
		
		void UpdateButtons ()
		{
			Gtk.TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				MimeTypePanelData mt = (MimeTypePanelData) store.GetValue (iter, 0);
				if (panel.IsUserMimeType (mt.MimeType)) {
					buttonRemove.Sensitive = buttonEdit.Sensitive = true;
					return;
				}
			}
			buttonRemove.Sensitive = buttonEdit.Sensitive = false;
		}
	}
}
