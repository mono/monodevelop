// 
// PolicyOptionsPanel.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Gtk;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Projects.Gui.Dialogs
{
	
	
	public abstract class PolicyOptionsPanel<T> : ItemOptionsPanel, IOptionsPanel where T : class, IEquatable<T>, new ()
	{
		ComboBox policyCombo;
		ListStore store;
		PolicyBag bag;
		PolicySet polSet;
		PolicyContainer policyContainer;
		bool loading = true;
		
		public PolicyOptionsPanel ()
		{
		}
		
		Widget IOptionsPanel.CreatePanelWidget ()
		{
			HBox hbox = new HBox (false, 6);
			Label label = new Label ();
			string displayName = polSet != null? GettextCatalog.GetString ("_Policy") : PolicyTitleWithMnemonic;
			label.MarkupWithMnemonic = "<b>" + displayName + ":</b>";
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
			
			FillPolicies ();
			policyCombo.Active = 0;
			
			Widget child = CreatePanelWidget ();
			//HACK: work around bug 469427 - broken themes match on widget names
			if (child.Name.IndexOf ("Panel") > 0)
				child.Name = child.Name.Replace ("Panel", "_");
			vbox.PackEnd (child, true, true, 0);
			
			LoadFrom (GetCurrentValue ());
			loading = false;
			
			if (!IsRoot && !policyContainer.DirectHas<T> ()) {
				//in this case "parent" is always first in the list
				policyCombo.Active = 0;
			} else {
				UpdateSelectedNamedPolicy ();
			}
			
			policyCombo.Changed += HandlePolicyComboChanged;
			
			return vbox;
		}

		void HandlePolicyComboChanged (object sender, EventArgs e)
		{
			T selected = GetSelectedPolicy ();
			if (selected != null) {
				loading = true;
				LoadFrom (selected);
				loading = false;
			}
		}
		
		T GetCurrentValue ()
		{
			if (polSet != null)
				return polSet.Get<T> () ?? new T ();
			else
				return bag.Get<T> ();
		}	
		
		void FillPolicies ()
		{
			if (!IsRoot) {
				store.AppendValues (GettextCatalog.GetString ("Parent Policy"), null);
				store.AppendValues ("--", null);
			}
			
			bool added = false;
			foreach (PolicySet set in PolicyService.GetPolicySets<T> ()) {
				store.AppendValues (set.Name, set);
				added = true;
			}
			
			if (added)
				store.AppendValues ("--", null);
			
			store.AppendValues (GettextCatalog.GetString ("Custom"), null);
		}
		
		T GetSelectedPolicy ()
		{
			int active = policyCombo.Active;
			
			if (active == 0 && !IsRoot)
				return bag.Owner.ParentFolder.Policies.Get<T> ();
			
			TreeIter iter;
			int i = 0;
			if (store.GetIterFirst (out iter)) {
				do {
					if (active == i) {
						PolicySet s = store.GetValue (iter, 1) as PolicySet;
						if (s != null)
							return s.Get<T> ();
						else return null;
					}
					i++;
				} while (store.IterNext (ref iter));
			}
			
			return null;
		}
		
		public void UpdateSelectedNamedPolicy ()
		{
			if (loading)
				return;
			
			T pol = GetPolicy ();
			
			PolicySet s = PolicyService.GetMatchingSet (pol);
			
			TreeIter iter;
			int i = 0;
			if (s != null && store.GetIterFirst (out iter)) {
				do {
					PolicySet s2 = store.GetValue (iter, 1) as PolicySet;
					if (s2 != null && s2.Id == s.Id) {
						policyCombo.Active = i;
						return;
					}
					i++;
				} while (store.IterNext (ref iter));
			}
			
			policyCombo.Active = store.IterNChildren () - 1;
		}
		
		protected abstract string PolicyTitleWithMnemonic { get; }
		
		public override bool IsVisible ()
		{
			return bag != null || polSet != null;
		}
		
		bool IsRoot {
			get {
				return polSet != null || bag.IsRoot;
			}
		}
			
		bool UseParentPolicy {
			get { return !IsRoot && policyCombo.Active == 0; }
		}
		
		protected abstract void LoadFrom (T policy);
		protected abstract T GetPolicy ();
		
		public override void Initialize (MonoDevelop.Core.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
			if (dataObject is SolutionItem) {
				policyContainer = bag = ((SolutionItem)dataObject).Policies;
			} else if (dataObject is Solution) {
				policyContainer = bag = ((Solution)dataObject).Policies;
			} else if (dataObject is PolicySet) {
				policyContainer = polSet = ((PolicySet)dataObject);
			}
		}
		
		public override void ApplyChanges ()
		{
			if (polSet != null) {
				polSet.Set (GetPolicy ());
				return;
			}
			
			if (UseParentPolicy) {
				bag.Remove<T> ();
			} else {
				bag.Set (GetPolicy ());
			}	
		}
	}
}
