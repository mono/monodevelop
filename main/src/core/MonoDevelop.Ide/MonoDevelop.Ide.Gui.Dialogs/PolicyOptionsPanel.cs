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

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public abstract class PolicyOptionsPanel<T> : ItemOptionsPanel, IOptionsPanel where T : class, IEquatable<T>, new ()
	{
		ComboBox policyCombo;
		ListStore store;
		PolicyBag bag;
		PolicySet polSet;
		PolicyContainer policyContainer;
		PolicyContainer defaultPolicyContainer;
		bool loading = true;
		HBox warningMessage;
		bool isGlobalPolicy;
		Widget policyPanel;
		bool policyUndefined;
		List<PolicySet> setsInCombo = new List<PolicySet> ();
		
		public PolicyOptionsPanel ()
		{
		}
		
		Control IOptionsPanel.CreatePanelWidget ()
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

			// Message to be shown when the user changes default policies
			
			warningMessage = new HBox ();
			warningMessage.Spacing = 6;
			var img = new ImageView (Stock.Warning, IconSize.LargeToolbar);
			img.SetCommonAccessibilityAttributes ("PolicyOptionsPanel.Warning",
			                                      GettextCatalog.GetString ("Warning"),
			                                      null);
			warningMessage.PackStart (img, false, false, 0);
			Label wl = new Label (GettextCatalog.GetString ("Changes done in this section will only be applied to new projects. " +
				"Settings for existing projects can be modified in the project (or solution) options dialog."));
			wl.Xalign = 0;
			wl.Wrap = true;
			wl.WidthRequest = 450;
			warningMessage.PackStart (wl, true, true, 0);
			warningMessage.ShowAll ();
			warningMessage.Visible = false;
			vbox.PackEnd (warningMessage, false, false, 0);
			
			policyPanel = CreatePanelWidget ();
			//HACK: work around bug 469427 - broken themes match on widget names
			if (policyPanel.Name.IndexOf ("Panel") > 0)
				policyPanel.Name = policyPanel.Name.Replace ("Panel", "_");
			vbox.PackEnd (policyPanel, true, true, 0);
			
			InitializePolicy ();

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



		
		void LoadPolicy (T policy)
		{
			if (policy == null) {
				policyPanel.Sensitive = false;
				// Policy is not being set, which means the default value will be used.
				// Show that default value in the panel, so user van see the settings that
				// are going to be applied.
				LoadFrom (GetDefaultValue ());
				return;
			}
			policyPanel.Sensitive = true;
			LoadFrom (policy);
		}

		void HandlePolicyComboChanged (object sender, EventArgs e)
		{
			policyPanel.Sensitive = true;
			T selected = GetSelectedPolicy ();
			policyUndefined = IsCustomUserPolicy && policyCombo.Active == 0;
			if (selected != null || policyUndefined) {
				loading = true;
				LoadPolicy (selected);
				loading = false;
			}
		}

		T GetDefaultValue ()
		{
			if (defaultPolicyContainer != null)
				return defaultPolicyContainer.Get<T> ();
			else
				return PolicyService.GetDefaultPolicy<T> ();
		}
		
		T GetCurrentValue ()
		{
			if (policyUndefined)
				return null;
			return policyContainer.Get<T> () ?? GetDefaultValue ();
		}
		
		void FillPolicies ()
		{
			((ListStore)store).Clear ();
			
			if (IsCustomUserPolicy) {
				store.AppendValues (GettextCatalog.GetString ("System Default"), null);
				store.AppendValues ("--", null);
			}
			else if (!IsRoot) {
				store.AppendValues (GettextCatalog.GetString ("Parent Policy"), null);
				store.AppendValues ("--", null);
			}
			
			setsInCombo.Clear ();
			foreach (PolicySet set in PolicyService.GetPolicySets<T> ()) {
				if (polSet != null && set.Name == polSet.Name)
					continue;
				if (IsCustomUserPolicy && set.Name == "Default") // There is already a System Default entry
					continue;
				store.AppendValues (set.Name, set);
				setsInCombo.Add (set);
			}
			
			if (setsInCombo.Count > 0)
				store.AppendValues ("--", null);
			
			store.AppendValues (GettextCatalog.GetString ("Custom"), null);
		}
		
		T GetSelectedPolicy ()
		{
			int active = policyCombo.Active;
			
			if (active == 0 && !IsRoot)
				return bag.Owner.ParentFolder.Policies.Get<T> ();
			if (active == 0 && IsCustomUserPolicy)
				return null;
			
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
			
			if (policyUndefined) {
				policyCombo.Active = 0;
				return;
			}
			
			T pol = GetPolicy ();
			
			PolicySet s = PolicyService.GetMatchingSet (pol, setsInCombo, false);
			
			int active = -1;
			TreeIter iter;
			int i = 0;
			if (s != null && store.GetIterFirst (out iter)) {
				do {
					PolicySet s2 = store.GetValue (iter, 1) as PolicySet;
					if (s2 == s) {
						active = i;
						break;
					}
					i++;
				} while (store.IterNext (ref iter));
			}
			
			if (active != -1)
				policyCombo.Active = active;
			else
				policyCombo.Active = store.IterNChildren () - 1;
			warningMessage.Visible = isGlobalPolicy && !((IEquatable<T>)pol).Equals (GetCurrentValue ());
		}
		
		protected abstract string PolicyTitleWithMnemonic { get; }
		
		public override bool IsVisible ()
		{
			return bag != null || polSet != null;
		}
		
		bool IsRoot {
			get {
				return policyContainer.IsRoot;
			}
		}
			
		bool UseParentPolicy {
			get { return !IsRoot && policyCombo.Active == 0; }
		}
		
		bool IsCustomUserPolicy {
			get { return ParentDialog is MonoDevelop.Ide.Projects.DefaultPolicyOptionsDialog; }
		}
		
		protected abstract void LoadFrom (T policy);
		protected abstract T GetPolicy ();
		
		public override void Initialize (MonoDevelop.Ide.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
			IPolicyProvider provider = dataObject as IPolicyProvider;
			if (provider == null) {
				provider = PolicyService.GetUserDefaultPolicySet ();
				// When editing the global user preferences, the default values for policies are the IDE default values.
				defaultPolicyContainer = PolicyService.SystemDefaultPolicies;
				isGlobalPolicy = true;
			}
			policyContainer = provider.Policies;
			bag = policyContainer as PolicyBag;
			polSet = policyContainer as PolicySet;
			
			policyContainer.PolicyChanged += HandlePolicyContainerPolicyChanged;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			policyContainer.PolicyChanged -= HandlePolicyContainerPolicyChanged;
		}

		void HandlePolicyContainerPolicyChanged (object sender, PolicyChangedEventArgs e)
		{
			if (!loading && e.PolicyType == typeof(T)) {
				FillPolicies ();
				InitializePolicy ();
				UpdateSelectedNamedPolicy ();
			}
		}
		
		void InitializePolicy ()
		{
			policyUndefined = IsCustomUserPolicy && !polSet.DirectHas<T> ();
			LoadPolicy (GetCurrentValue ());
		}
		
		public override void ApplyChanges ()
		{
			loading = true;
			try {
				if (polSet != null) {
					if (IsCustomUserPolicy && policyUndefined)
						polSet.Remove<T> ();
					else
						polSet.Set (GetPolicy ());
					return;
				}
				
				if (UseParentPolicy) {
					bag.Remove<T> ();
				} else {
					bag.Set (GetPolicy ());
				}
			} finally {
				loading = false;
			}
		}
	}
}
