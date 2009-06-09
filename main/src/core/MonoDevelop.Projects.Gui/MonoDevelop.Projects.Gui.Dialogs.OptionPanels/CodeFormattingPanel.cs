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
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui.Codons;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Gui.Extensions;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	class CodeFormattingPanel: OptionsPanel
	{
		IPolicyContainer policyContainer;
		Dictionary<string,MimeTypePanelData> typeSections = new Dictionary<string, MimeTypePanelData> ();
		
		public override void Initialize (MonoDevelop.Core.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
			
			if (dataObject is SolutionItem) {
				policyContainer = ((SolutionItem)dataObject).Policies;
			} else if (dataObject is Solution) {
				policyContainer = ((Solution)dataObject).Policies;
			} else if (dataObject is PolicySet) {
				policyContainer = ((PolicySet)dataObject);
			}
			
			foreach (string mt in GetItemMimeTypes ()) {
				MimeTypePanelData data = new MimeTypePanelData ();
				OptionsDialogSection sec = new OptionsDialogSection (typeof(MimeTypePolicyOptionsSection));
				sec.Fill = true;
				sec.Label = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeDescription (mt);
				Gdk.Pixbuf icon = MonoDevelop.Core.Gui.Services.PlatformService.GetPixbufForType (mt, Gtk.IconSize.Menu);
				sec.Icon = ImageService.GetStockId (icon, Gtk.IconSize.Menu);
				data.Section = sec;
				data.MimeType = mt;
				data.DataObject = DataObject;
				data.PolicyContainer = policyContainer;
				LoadPolicyTypeData (data, mt);
				typeSections [mt] = data;
				dialog.AddChildSection (this, sec, data);
			}
		}
		
		public IEnumerable<MimeTypePanelData> GetMimeTypeData ()
		{
			return typeSections.Values;
		}
		
		public IPolicyContainer PolicyContainer {
			get { return policyContainer; }
		}

		void LoadPolicyTypeData (MimeTypePanelData data, string mimeType)
		{
			List<string> types = new List<string> ();
			types.AddRange (MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeInheritanceChain (mimeType));
			List<IMimeTypePolicyOptionsPanel> panels = new List<IMimeTypePolicyOptionsPanel> ();
			
			bool useParentPolicy = false;
			foreach (MimeTypeOptionsPanelNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/ProjectModel/Gui/MimeTypePolicyPanels")) {
				if (!types.Contains (node.MimeType))
					continue;
				
				IMimeTypePolicyOptionsPanel panel = (IMimeTypePolicyOptionsPanel) node.CreateInstance (typeof(IMimeTypePolicyOptionsPanel));
				panel.Initialize (ParentDialog, DataObject);
				panel.InitializePolicy (policyContainer, mimeType, mimeType == node.MimeType);
				panel.Label = node.Label;
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
			else
				return new string[] { "text/plain", "application/xml", "text/x-csharp" };
			return types;
		}
		
		void GetItemMimeTypes (HashSet<string> types, SolutionItem item)
		{
			if (item is SolutionFolder) {
				foreach (SolutionItem it in ((SolutionFolder)item).Items)
					GetItemMimeTypes (types, it);
			}
			else if (item is Project) {
				foreach (ProjectFile pf in ((Project)item).Files) {
					string mt = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (pf.FilePath);
					foreach (string mth in MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeInheritanceChain (mt))
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
			
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			Gtk.CellRendererPixbuf crp = new Gtk.CellRendererPixbuf ();
			
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("File Type");
			col.PackStart (crp, false);
			col.PackStart (crt, true);
			col.AddAttribute (crp, "pixbuf", 1);
			col.AddAttribute (crt, "text", 2);
			tree.AppendColumn (col);
			
			CellRendererComboBox comboCell = new CellRendererComboBox ();
			comboCell.Changed += OnPolicySelectionChanged;
			tree.AppendColumn (GettextCatalog.GetString ("Policy"), comboCell, new Gtk.TreeCellDataFunc (OnSetPolicyData));
			
			Fill ();
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
			PlatformService pf = MonoDevelop.Core.Gui.Services.PlatformService;
			foreach (MimeTypePanelData mt in panel.GetMimeTypeData ()) {
				store.AppendValues (mt, pf.GetPixbufForType (mt.MimeType, Gtk.IconSize.Menu), pf.GetMimeTypeDescription (mt.MimeType));
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
	}
}
