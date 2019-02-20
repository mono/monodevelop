// 
// ExportProjectPolicyDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Projects
{
	partial class ExportProjectPolicyDialog : Gtk.Dialog
	{
		IPolicyProvider policyProvider;
		PoliciesListSummaryTree tree;
		
		public ExportProjectPolicyDialog (IPolicyProvider policyProvider)
		{
			this.Build ();
			this.policyProvider = policyProvider;

			fileEntry.Action = FileChooserAction.Save;
			fileEntry.DefaultPath = DefaultFileDialogPolicyDir;
			if (policyProvider is SolutionFolderItem)
				fileEntry.Path = ((SolutionFolderItem)policyProvider).Name + ".mdpolicy";
			else if (policyProvider is Solution)
				fileEntry.Path = ((Solution)policyProvider).Name + ".mdpolicy";
			
			fileEntry.FileFilters.AddFilter (BrandingService.BrandApplicationName (GettextCatalog.GetString ("MonoDevelop policy files")), "*.mdpolicy");
			fileEntry.FileFilters.AddAllFilesFilter ();
			
			fileEntry.PathChanged += delegate {
				UpdateWidgets ();
			};
			entryName.Changed += delegate {
				UpdateWidgets ();
			};
			
			tree = new PoliciesListSummaryTree ();
			policiesScroll.Add (tree);
			tree.Show ();
			
			tree.SetPolicies (policyProvider.Policies);
			if (!tree.HasPolicies) {
				buttonOk.Sensitive = false;
			}
			
			UpdateWidgets ();
		}
		
		public static FilePath DefaultFileDialogPolicyDir {
			get { return PropertyService.Get<string> ("MonoDevelop.Ide.Projects.PolicyLocation", Environment.GetFolderPath (Environment.SpecialFolder.Personal)); }
			set { PropertyService.Set ("MonoDevelop.Ide.Projects.PolicyLocation", value.ToString ()); }
		}

		void UpdateWidgets ()
		{
			bool custom = radioCustom.Active;
			boxCustom.Sensitive = custom;
			boxFile.Sensitive = !custom;
			
			bool valid;
			if (custom) {
				valid = !string.IsNullOrWhiteSpace (entryName.Text);
			} else {
				valid = !string.IsNullOrWhiteSpace (fileEntry.Path);
			}
			
			buttonOk.Sensitive = tree.HasPolicies && valid;
		}
		
		protected void OnRadioCustomToggled (object sender, System.EventArgs e)
		{
			UpdateWidgets ();
		}

		protected void OnButtonOkClicked (object sender, EventArgs e)
		{
			if (radioCustom.Active) {
				if (entryName.Text.Length == 0) {
					MessageService.ShowError (GettextCatalog.GetString ("Policy name not specified"));
					return;
				}
				PolicySet pset = CreatePolicySet ();
				pset.Name = entryName.Text;
				PolicyService.AddUserPolicySet (pset);
				PolicyService.SavePolicies ();
			}
			else {
				if (fileEntry.Path == null || fileEntry.Path.Length == 0) {
					MessageService.ShowError (GettextCatalog.GetString ("File name not specified"));
					return;
				}
				FilePath file = fileEntry.Path;
				if (file.Extension != ".mdpolicy")
					file = file + ".mdpolicy";
				DefaultFileDialogPolicyDir = file.ParentDirectory;
				
				if (System.IO.File.Exists (file) && !MessageService.Confirm (GettextCatalog.GetString ("The file {0} already exists. Do you want to replace it?", file), AlertButton.Replace))
					return;
				
				PolicySet pset = CreatePolicySet ();
				pset.Name = file.FileName;
				try {
					pset.SaveToFile (file);
				} catch (Exception ex) {
					MessageService.ShowError (GettextCatalog.GetString ("The policy file could not be saved"), ex);
					return;
				}
			}
			Respond (Gtk.ResponseType.Ok);
		}
		
		PolicySet CreatePolicySet ()
		{
			PolicySet pset = new PolicySet ();
			pset.Import (policyProvider.Policies, true);
			return pset;
		}
	}
}

