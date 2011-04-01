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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.Projects
{
	partial class ExportProjectPolicyDialog : Gtk.Dialog
	{
		IPolicyProvider policyProvider;
		
		public ExportProjectPolicyDialog (IPolicyProvider policyProvider)
		{
			this.Build ();
			this.policyProvider = policyProvider;
			
			fileEntry.DefaultPath = DefaultFileDialogPolicyDir;
			if (policyProvider is SolutionItem)
				fileEntry.Path = ((SolutionItem)policyProvider).Name + ".mdpolicy";
			else if (policyProvider is Solution)
				fileEntry.Path = ((Solution)policyProvider).Name + ".mdpolicy";
			
			UpdateWidgets ();
			
			labelPolicies.Text = ApplyPolicyDialog.GetPoliciesDescription (policyProvider.Policies);
		}
		
		public static FilePath DefaultFileDialogPolicyDir {
			get { return PropertyService.Get<string> ("MonoDevelop.Ide.Projects.PolicyLocation", Environment.GetFolderPath (Environment.SpecialFolder.Personal)); }
			set { PropertyService.Set ("MonoDevelop.Ide.Projects.PolicyLocation", value.ToString ()); }
		}

		void UpdateWidgets ()
		{
			boxCustom.Sensitive = radioCustom.Active;
			boxFile.Sensitive = !radioCustom.Active;
		}
		
		protected void OnRadioCustomToggled (object sender, System.EventArgs e)
		{
			UpdateWidgets ();
		}

		protected void OnButtonOkClicked (object sender, System.EventArgs e)
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
				pset.SaveToFile (file);
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

