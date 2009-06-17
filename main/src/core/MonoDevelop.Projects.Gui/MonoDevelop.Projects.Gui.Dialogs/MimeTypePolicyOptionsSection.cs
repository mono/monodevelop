// 
// IMimeTypePolicyOptionsPanel.cs
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
using System.Collections.Generic;
using Gtk;
using System.Linq;
using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Gui.Codons;

namespace MonoDevelop.Projects.Gui.Dialogs
{
	class MimeTypePolicyOptionsSection: OptionsPanel
	{
		ComboBox policyCombo;
		ListStore store;
		PolicyBag bag;
		PolicySet polSet;
		bool loading = true;
		string mimeType;
		List<IMimeTypePolicyOptionsPanel> panels;
		MimeTypePanelData panelData;
		Notebook notebook;
		bool isRoot;
		
		public MimeTypePolicyOptionsSection ()
		{
		}
		
		public override void Initialize (MonoDevelop.Core.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
			panelData = (MimeTypePanelData) dataObject;
			
			if (panelData.DataObject is SolutionItem) {
				bag = ((SolutionItem)panelData.DataObject).Policies;
			} else if (panelData.DataObject is Solution) {
				bag = ((Solution)panelData.DataObject).Policies;
			} else if (panelData.DataObject is PolicySet) {
				polSet = ((PolicySet)panelData.DataObject);
			}
			mimeType = panelData.MimeType;
			panelData.SectionPanel = this;
			isRoot = polSet != null || bag.IsRoot;
		}
		
		public override Widget CreatePanelWidget ()
		{
			HBox hbox = new HBox (false, 6);
			Label label = new Label ();
			label.MarkupWithMnemonic = GettextCatalog.GetString ("_Policy:");
			hbox.PackStart (label, false, false, 0);
			
			store = new ListStore (typeof (string), typeof (PolicySet));
			policyCombo = new ComboBox (store);
			CellRenderer renderer = new CellRendererText ();
			policyCombo.PackStart (renderer, true);
			policyCombo.AddAttribute (renderer, "text", 0);
			
			label.MnemonicWidget = policyCombo;
			policyCombo.RowSeparatorFunc = (TreeModel model, TreeIter iter) =>
				((string) model.GetValue (iter, 0)) == "--";
			hbox.PackStart (policyCombo, false, false, 0);
			
			VBox vbox = new VBox (false, 6);
			vbox.PackStart (hbox, false, false, 0);
			vbox.ShowAll ();
			
			notebook = new Notebook ();

			// Get the panels for all mime types
			
			List<string> types = new List<string> ();
			types.AddRange (MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeInheritanceChain (mimeType));
			
			panelData.SectionLoaded = true;
			panels = panelData.Panels;
			foreach (IMimeTypePolicyOptionsPanel panel in panelData.Panels) {
				panel.SetParentSection (this);
				Widget child = panel.CreateMimePanelWidget ();
				//HACK: work around bug 469427 - broken themes match on widget names
				if (child.Name.IndexOf ("Panel") > 0)
					child.Name = child.Name.Replace ("Panel", "_");
				
				Label tlabel = new Label (panel.Label);
				label.Show ();
				child.Show ();
				Alignment align = new Alignment (0.5f, 0.5f, 1f, 1f);
				align.BorderWidth = 6;
				align.Add (child);
				align.Show ();
				
				notebook.AppendPage (align, tlabel);
				panel.LoadCurrentPolicy ();
			}
			
			notebook.Show ();
			vbox.PackEnd (notebook, true, true, 0);
			
			FillPolicies ();
			policyCombo.Active = 0;
			
			loading = false;
			
			if (!isRoot && panelData.UseParentPolicy) {
				//in this case "parent" is always first in the list
				policyCombo.Active = 0;
				notebook.Sensitive = false;
			} else {
				UpdateSelectedNamedPolicy ();
			}
			
			policyCombo.Changed += HandlePolicyComboChanged;
			
			return vbox;
		}

		void HandlePolicyComboChanged (object sender, EventArgs e)
		{
			loading = true;
			
			if (policyCombo.Active == 0 && !isRoot) {
				panelData.UseParentPolicy = true;
				notebook.Sensitive = false;
			}
			else {
				string activeName = policyCombo.ActiveText;
				PolicySet pset = PolicyService.GetPolicySet (activeName);
				if (pset != null)
					panelData.AssignPolicies (pset);
				else
					panelData.UseParentPolicy = false;
				notebook.Sensitive = true;
			}
			loading = false;
		}
		
		void FillPolicies ()
		{
			if (!isRoot) {
				store.AppendValues (GettextCatalog.GetString ("Inherited Policy"), null);
				store.AppendValues ("--", null);
			}
			
			bool added = false;
			foreach (PolicySet set in panelData.GetSupportedPolicySets ()) {
				store.AppendValues (set.Name, set);
				added = true;
			}
			
			if (added)
				store.AppendValues ("--", null);
			
			store.AppendValues (GettextCatalog.GetString ("Custom"), null);
		}
		
		public void UpdateSelectedNamedPolicy ()
		{
			if (loading)
				return;
			
			// Find a policy set which is common to all policy types
			
			if (!isRoot && panelData.UseParentPolicy) {
				policyCombo.Active = 0;
				return;
			}
			
			PolicySet matchedSet = panelData.GetMatchingSet ();
			
			TreeIter iter;
			int i = 0;
			if (matchedSet != null && store.GetIterFirst (out iter)) {
				do {
					PolicySet s2 = store.GetValue (iter, 1) as PolicySet;
					if (s2 != null && s2.Id == matchedSet.Id) {
						policyCombo.Active = i;
						return;
					}
					i++;
				} while (store.IterNext (ref iter));
			}
			
			policyCombo.Active = store.IterNChildren () - 1;
		}
		
		public override bool IsVisible ()
		{
			return bag != null || polSet != null;
		}
		
		public override bool ValidateChanges ()
		{
			foreach (IMimeTypePolicyOptionsPanel panel in panels)
				if (!panel.ValidateChanges ())
					return false;
			return true;
		}

		
		public override void ApplyChanges ()
		{
			panelData.ApplyChanges ();
		}
	}
	
	class MimeTypePanelData
	{
		public string MimeType;
		public object DataObject;
		public string TypeDescription;
		public OptionsDialogSection Section;
		public MimeTypePolicyOptionsSection SectionPanel;
		public List<IMimeTypePolicyOptionsPanel> Panels;
		public bool SectionLoaded;
		public IPolicyContainer PolicyContainer;
		
		public void ApplyChanges ()
		{
			if (UseParentPolicy) {
				foreach (IMimeTypePolicyOptionsPanel panel in Panels)
					panel.RemovePolicy (PolicyContainer);
			} else {
				foreach (IMimeTypePolicyOptionsPanel panel in Panels) {
					if (SectionLoaded)
						panel.ApplyChanges ();
					panel.StorePolicy ();
				}
			}	
		}
		
		public PolicySet GetMatchingSet ()
		{
			// Find a policy set which is common to all policy types
			
			PolicySet matchedSet = null;
			bool firstMatch = true;
			foreach (IMimeTypePolicyOptionsPanel panel in Panels) {
				PolicySet s = panel.GetMatchingSet ();
				if (firstMatch) {
					matchedSet = s;
					firstMatch = false;
				}
				else if (matchedSet != s) {
					matchedSet = null;
					break;
				}
			}
			return matchedSet;
		}
		
		public IEnumerable<PolicySet> GetSupportedPolicySets ()
		{
			HashSet<PolicySet> commonSets = null;
			foreach (IMimeTypePolicyOptionsPanel panel in Panels) {
				HashSet<PolicySet> sets = new HashSet<PolicySet> ();
				foreach (PolicySet pset in panel.GetPolicySets ())
					sets.Add (pset);
				if (commonSets == null)
					commonSets = sets;
				else
					commonSets.IntersectWith (sets);
			}
			if (commonSets != null)
				return commonSets;
			else
				return new PolicySet[0];
		}
		
		bool useParentPolicy;
		
		public bool UseParentPolicy {
			get {
				return useParentPolicy;
			}
			set {
				if (useParentPolicy != value) {
					useParentPolicy = value;
					if (useParentPolicy) {
						foreach (IMimeTypePolicyOptionsPanel panel in Panels)
							panel.LoadParentPolicy ();
					}
					if (SectionLoaded)
						SectionPanel.UpdateSelectedNamedPolicy ();
				}
			}
		}
		
		public void AssignPolicies (PolicySet pset)
		{
			useParentPolicy = false;
			foreach (IMimeTypePolicyOptionsPanel panel in Panels)
				panel.LoadSetPolicy (pset);
			if (SectionLoaded)
				SectionPanel.UpdateSelectedNamedPolicy ();
		}
	}
}
