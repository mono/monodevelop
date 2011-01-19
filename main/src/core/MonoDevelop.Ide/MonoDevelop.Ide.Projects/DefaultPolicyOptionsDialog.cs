// 
// DefaultPolicyOptionsDialog.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;
using MonoDevelop.Ide.Projects.OptionPanels;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Components;
using Gtk;

namespace MonoDevelop.Ide.Projects
{
	public class DefaultPolicyOptionsDialog : OptionsDialog
	{
		ComboBox policiesCombo;
		Button newButton;
		Button deleteButton;
		List<PolicySet> sets = new List<PolicySet> ();
		Dictionary<PolicySet,PolicySet> originalSets = new Dictionary<PolicySet, PolicySet> ();
		
		PolicySet editingSet;
		PolicySet currentSet;
		bool loading;
		
		public DefaultPolicyOptionsDialog (Gtk.Window parentWindow)
			: base (parentWindow, new PolicySet (),
			        "/MonoDevelop/ProjectModel/Gui/DefaultPolicyPanels")
		{
			this.Title = GettextCatalog.GetString ("Custom Policies");
			editingSet = (PolicySet) DataObject;
			
			HBox topBar = new HBox ();
			topBar.Spacing = 3;
			topBar.PackStart (new Label (GettextCatalog.GetString ("Editing Policy:")), false, false, 0);
			
			policiesCombo = ComboBox.NewText ();
			topBar.PackStart (policiesCombo, false, false, 0);
			
			newButton = new Button (GettextCatalog.GetString ("Add Policy"));
			topBar.PackEnd (newButton, false, false, 0);
			
			deleteButton = new Button (GettextCatalog.GetString ("Delete Policy"));
			topBar.PackEnd (deleteButton, false, false, 0);
			
			Alignment align = new Alignment (0f, 0f, 1f, 1f);
			align.LeftPadding = 12;
			align.TopPadding = 12;
			align.RightPadding = 12;
			align.BottomPadding = 12;
			align.Add (topBar);
			
			EventBox ebox = new EventBox ();
			ebox.Add (align);
			
			ebox.ShowAll ();
			ebox.ModifyBg (StateType.Normal, ebox.Style.Background (StateType.Normal).AddLight (-0.2));
			
			VBox.PackStart (ebox, false, false, 0);
			Box.BoxChild c = (Box.BoxChild) VBox [ebox];
			c.Position = 0;
			
			foreach (PolicySet ps in PolicyService.GetUserPolicySets ()) {
				PolicySet copy = ps.Clone ();
				originalSets [copy] = ps;
				sets.Add (copy);
			}
			FillPolicySets ();
			
			policiesCombo.Changed += HandlePoliciesComboChanged;
			newButton.Clicked += HandleNewButtonClicked;
			deleteButton.Clicked += HandleDeleteButtonClicked;
		}
		
		protected override void ApplyChanges ()
		{
			base.ApplyChanges ();
			ApplyPolicyChanges ();
			
			HashSet<PolicySet> usets = new HashSet<PolicySet> (PolicyService.GetUserPolicySets ());
			foreach (PolicySet ps in sets) {
				PolicySet orig;
				if (originalSets.TryGetValue (ps, out orig)) {
					orig.CopyFrom (ps);
					usets.Remove (orig);
				} else {
					orig = ps.Clone ();
					PolicyService.AddUserPolicySet (orig);
					originalSets [ps] = orig;
				}
			}
			foreach (PolicySet ps in usets)
				PolicyService.RemoveUserPolicySet (ps);
			
			PolicyService.SaveDefaultPolicies ();
		}

		void HandleDeleteButtonClicked (object sender, EventArgs e)
		{
			if (!MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the policy '{0}'?", currentSet.Name), AlertButton.Delete))
				return;
			
			sets.Remove (currentSet);
			currentSet = null;
			FillPolicySets ();
		}

		void HandleNewButtonClicked (object sender, EventArgs e)
		{
			HashSet<PolicySet> esets = new HashSet<PolicySet> (PolicyService.GetPolicySets ());
			esets.ExceptWith (PolicyService.GetUserPolicySets ());
			esets.UnionWith (sets);
			esets.RemoveWhere (p => !p.Visible);
			
			NewPolicySetDialog dlg = new NewPolicySetDialog (new List<PolicySet> (esets));
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				PolicySet pset = new PolicySet ();
				pset.CopyFrom (dlg.SourceSet);
				pset.Name = dlg.PolicyName;
				sets.Add (pset);
				FillPolicySets ();
				policiesCombo.Active = sets.IndexOf (pset);
			}
			dlg.Destroy ();
		}
		
		void FillPolicySets ()
		{
			loading = true;
			int current = policiesCombo.Active;
			
			((ListStore)policiesCombo.Model).Clear ();
			policiesCombo.WidthRequest = -1;
			
			sets.Sort ((p1,p2) => p1.Name.CompareTo(p2.Name));
			
			foreach (PolicySet pset in sets) {
				policiesCombo.AppendText (pset.Name ?? "");
			}
			if (current == -1 && sets.Count > 0)
				policiesCombo.Active = 0;
			else if (current >= sets.Count)
				policiesCombo.Active = sets.Count - 1;
			else
				policiesCombo.Active = current;
			
			if (policiesCombo.SizeRequest ().Width < 200)
				policiesCombo.WidthRequest = 200;
			
			loading = false;
			
			if (policiesCombo.Active != -1 && sets [policiesCombo.Active] != currentSet) {
				currentSet = sets [policiesCombo.Active];
				editingSet.Name = currentSet.Name;
				editingSet.CopyFrom (currentSet);
			}
			UpdateStatus ();
		}
		
		void UpdateStatus ()
		{
			if (sets.Count == 0) {
				deleteButton.Sensitive = false;
				MainBox.Sensitive = false;
				((ListStore)policiesCombo.Model).Clear ();
				policiesCombo.Sensitive = false;
				policiesCombo.AppendText (GettextCatalog.GetString ("No Selection"));
				policiesCombo.Active = 0;
			}
			else {
				deleteButton.Sensitive = true;
				MainBox.Sensitive = true;
				policiesCombo.Sensitive = true;
			}
		}

		void HandlePoliciesComboChanged (object sender, EventArgs e)
		{
			if (!loading) {
				if (currentSet != null) {
					// Save current values
					if (ValidateChanges ()) {
						base.ApplyChanges ();
						currentSet.CopyFrom (editingSet);
					} else {
						// There are validation errors. Cancel the policy switch
						int last = policiesCombo.Active;
						Application.Invoke (delegate {
							loading = true;
							policiesCombo.Active = last;
							loading = false;
						});
						return;
					}
				}
			
				if (policiesCombo.Active != -1 && policiesCombo.Active < sets.Count) {
					// Load the new values
					currentSet = sets [policiesCombo.Active];
					editingSet.Name = currentSet.Name;
					editingSet.CopyFrom (currentSet);
				}
			}
		}
		
		void ApplyPolicyChanges ()
		{
			if (policiesCombo.Active != -1 && sets.Count > 0)
				sets [policiesCombo.Active].CopyFrom (editingSet);
		}
	}
}
