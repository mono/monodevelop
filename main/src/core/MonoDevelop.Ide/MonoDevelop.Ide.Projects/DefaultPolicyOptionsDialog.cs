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
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Projects
{
	public class DefaultPolicyOptionsDialog : OptionsDialog
	{
		ComboBox policiesCombo;
		MenuButton newButton;
		Button deleteButton;
		MenuButton exportButton;
		List<PolicySet> sets = new List<PolicySet> ();
		Dictionary<PolicySet,PolicySet> originalSets = new Dictionary<PolicySet, PolicySet> ();
		
		PolicySet editingSet;
		PolicySet currentSet;
		bool loading;
		
		public DefaultPolicyOptionsDialog (MonoDevelop.Components.Window parentWindow)
			: base (parentWindow, new PolicySet (),
			        "/MonoDevelop/ProjectModel/Gui/DefaultPolicyPanels")
		{
			this.Title = GettextCatalog.GetString ("Policies");
			editingSet = (PolicySet) DataObject;
			
			HBox topBar = new HBox ();
			topBar.Spacing = 3;
			topBar.PackStart (new Label (GettextCatalog.GetString ("Editing Policy:")), false, false, 0);
			
			policiesCombo = ComboBox.NewText ();
			topBar.PackStart (policiesCombo, false, false, 0);
			
			deleteButton = new Button (GettextCatalog.GetString ("Delete Policy"));
			topBar.PackEnd (deleteButton, false, false, 0);
			
			exportButton = new MenuButton ();
			exportButton.Label = GettextCatalog.GetString ("Export");
			exportButton.ContextMenuRequested = delegate {
				ContextMenu menu = new ContextMenu ();

				ContextMenuItem item = new ContextMenuItem (GettextCatalog.GetString ("To file..."));
				item.Clicked += HandleToFile;
				menu.Items.Add (item);

				item = new ContextMenuItem (GettextCatalog.GetString ("To project or solution..."));
				item.Clicked += HandleToProject;
				if (!IdeApp.Workspace.IsOpen) {
					item.Sensitive = false;
				}
				menu.Items.Add (item);

				return menu;
			};
			topBar.PackEnd (exportButton, false, false, 0);
			
			newButton = new MenuButton ();
			newButton.Label = GettextCatalog.GetString ("Add Policy");
			newButton.ContextMenuRequested = delegate {
				ContextMenu menu = new ContextMenu ();

				ContextMenuItem item = new ContextMenuItem (GettextCatalog.GetString ("New policy..."));
				item.Clicked += HandleNewButtonClicked;
				menu.Items.Add (item);

				item = new ContextMenuItem (GettextCatalog.GetString ("From file..."));
				item.Clicked += HandleFromFile;
				menu.Items.Add (item);

				item = new ContextMenuItem (GettextCatalog.GetString ("From project or solution..."));
				item.Clicked += HandleFromProject;
				if (!IdeApp.Workspace.IsOpen) {
					item.Sensitive = false;
				}
				menu.Items.Add (item);

				return menu;
			};
			topBar.PackEnd (newButton, false, false, 0);
			
			Alignment align = new Alignment (0f, 0f, 1f, 1f);
			align.LeftPadding = 9;
			align.TopPadding = 9;
			align.RightPadding = 9;
			align.BottomPadding = 9;
			align.Add (topBar);
			
			HeaderBox ebox = new HeaderBox ();
			ebox.GradientBackground = true;
			ebox.SetMargins (0, 1, 0, 0);
			ebox.Add (align);
			
			ebox.ShowAll ();
			
			VBox.PackStart (ebox, false, false, 0);
			VBox.BorderWidth = 0;
			Box.BoxChild c = (Box.BoxChild) VBox [ebox];
			c.Position = 0;
			
			foreach (PolicySet ps in PolicyService.GetUserPolicySets ()) {
				PolicySet copy = ps.Clone ();
				originalSets [copy] = ps;
				sets.Add (copy);
			}
			FillPolicySets ();
			
			policiesCombo.Changed += HandlePoliciesComboChanged;
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
			
			PolicyService.SavePolicies ();
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
			try {
				if (MessageService.RunCustomDialog (dlg, this) == (int) ResponseType.Ok) {
					PolicySet pset = new PolicySet ();
					pset.CopyFrom (dlg.SourceSet);
					pset.Name = GetUnusedName (dlg.PolicyName);
					sets.Add (pset);
					FillPolicySets ();
					policiesCombo.Active = sets.IndexOf (pset);
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		void HandleFromProject (object sender, EventArgs e)
		{
			ImportProjectPolicyDialog dlg = new ImportProjectPolicyDialog ();
			try {
				if (MessageService.RunCustomDialog (dlg, this) == (int) Gtk.ResponseType.Ok) {
					PolicySet pset = new PolicySet ();
					pset.CopyFrom (dlg.SelectedItem.Policies);
					pset.Name = GetUnusedName (dlg.PolicyName);
					sets.Add (pset);
					FillPolicySets ();
					policiesCombo.Active = sets.IndexOf (pset);
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		void HandleFromFile (object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog (GettextCatalog.GetString ("Select Policy File"));
			dlg.Action = MonoDevelop.Components.FileChooserAction.Open;
			dlg.TransientFor = this;
			dlg.AddFilter (BrandingService.BrandApplicationName (GettextCatalog.GetString ("MonoDevelop policy files")), "*.mdpolicy");
			dlg.AddAllFilesFilter ();
			dlg.CurrentFolder = ExportProjectPolicyDialog.DefaultFileDialogPolicyDir;
			if (dlg.Run ()) {
				try {
					PolicySet pset = new PolicySet ();
					pset.LoadFromFile (dlg.SelectedFile);
					if (string.IsNullOrEmpty (pset.Name))
						pset.Name = dlg.SelectedFile.FileNameWithoutExtension;
					pset.Name = GetUnusedName (pset.Name);
					sets.Add (pset);
					ExportProjectPolicyDialog.DefaultFileDialogPolicyDir = dlg.SelectedFile.ParentDirectory;
					FillPolicySets ();
					policiesCombo.Active = sets.IndexOf (pset);
				} catch (Exception ex) {
					MessageService.ShowError (GettextCatalog.GetString ("The policy set could not be loaded"), ex);
				}
			}
		}
		
		string GetUnusedName (string name)
		{
			string finalName = name;
			int n = 1;
			while (sets.Any (ps => ps.Name == finalName)) {
				n++;
				finalName = name + n;
			}
			return finalName;
		}

		void HandleToProject (object sender, EventArgs e)
		{
			ProjectSelectorDialog dlg = new ProjectSelectorDialog ();
			try {
				dlg.Title = GettextCatalog.GetString ("Apply to Project");
				dlg.RootItem = IdeApp.Workspace;
				dlg.SelectedItem = IdeApp.ProjectOperations.CurrentSelectedObject;
				dlg.SelectableItemTypes = new Type[] { typeof(Solution), typeof(SolutionFolderItem) };
				if (MessageService.RunCustomDialog (dlg, this) == (int) Gtk.ResponseType.Ok) {
					((IPolicyProvider)dlg.SelectedItem).Policies.Import (currentSet, true);
					if (dlg.SelectedItem is IWorkspaceFileObject)
						IdeApp.ProjectOperations.SaveAsync (dlg.SelectedItem);
					else
						IdeApp.ProjectOperations.SaveAsync (((SolutionFolderItem)dlg.SelectedItem).ParentSolution);
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		void HandleToFile (object sender, EventArgs e)
		{
			OpenFileDialog dlg = new OpenFileDialog (GettextCatalog.GetString ("Select Policy File"));
			dlg.TransientFor = this;
			dlg.InitialFileName = currentSet.Id + ".mdpolicy";
			dlg.Action = MonoDevelop.Components.FileChooserAction.Save;
			dlg.AddFilter (BrandingService.BrandApplicationName (GettextCatalog.GetString ("MonoDevelop policy files")), "*.mdpolicy");
			dlg.AddAllFilesFilter ();
			dlg.CurrentFolder = ExportProjectPolicyDialog.DefaultFileDialogPolicyDir;
			if (dlg.Run ()) {
				try {
					currentSet.SaveToFile (dlg.SelectedFile);
					ExportProjectPolicyDialog.DefaultFileDialogPolicyDir = dlg.SelectedFile.ParentDirectory;
				} catch (Exception ex) {
					MessageService.ShowError (GettextCatalog.GetString ("The policy set could not be saved"), ex.Message, ex);
				}
			}
		}
		
		void FillPolicySets ()
		{
			loading = true;
			int current = policiesCombo.Active;
			
			((ListStore)policiesCombo.Model).Clear ();
			policiesCombo.WidthRequest = -1;
			
			sets.Sort ((p1, p2) => string.Compare (p1.Name, p2.Name, StringComparison.CurrentCulture));
			
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
			Gtk.Widget mainBox = MainBox;
			if (sets.Count == 0) {
				deleteButton.Sensitive = exportButton.Sensitive = false;
				mainBox.Sensitive = false;
				((ListStore)policiesCombo.Model).Clear ();
				policiesCombo.Sensitive = false;
				policiesCombo.AppendText (GettextCatalog.GetString ("No Selection"));
				policiesCombo.Active = 0;
			}
			else {
				deleteButton.Sensitive = exportButton.Sensitive = true;
				mainBox.Sensitive = true;
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
						Application.Invoke ((o, args) => {
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
