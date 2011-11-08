// 
// ApplyPolicyDialog.cs
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
using MonoDevelop.Projects.Policies;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.Ide.Projects
{
	partial class ApplyPolicyDialog : Gtk.Dialog
	{
		IPolicyProvider policyProvider;
		
		public ApplyPolicyDialog (IPolicyProvider policyProvider)
		{
			this.Build ();
			this.policyProvider = policyProvider;
			
			foreach (PolicySet pset in PolicyService.GetPolicySets ())
				if (pset.Visible)
					combPolicies.AppendText (pset.Name);
			
			fileEntry.DefaultPath = ExportProjectPolicyDialog.DefaultFileDialogPolicyDir;
			fileEntry.FileFilters.AddFilter (GettextCatalog.GetString ("MonoDevelop policy files"), "*.mdpolicy");
			fileEntry.FileFilters.AddAllFilesFilter ();
			combPolicies.Active = 0;
			OnRadioCustomToggled (null, null);
			UpdateContentLabels ();
		}

		protected void OnRadioCustomToggled (object sender, System.EventArgs e)
		{
			boxCustom.Sensitive = radioCustom.Active;
			boxFile.Sensitive = !radioCustom.Active;
			UpdateContentLabels ();
		}
		 
		protected void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			PolicySet pset = GetPolicySet (true);
			if (pset != null) {
				policyProvider.Policies.Import (pset, false);
				Respond (Gtk.ResponseType.Ok);
			}
		}
		
		PolicySet GetPolicySet (bool notifyErrors)
		{
			if (radioCustom.Active) {
				return PolicyService.GetPolicySet (combPolicies.ActiveText);
			}
			else {
				if (string.IsNullOrEmpty (fileEntry.Path)) {
					if (notifyErrors)
						MessageService.ShowError (GettextCatalog.GetString ("File name not specified"));
					return null;
				}
				try {
					PolicySet pset = new PolicySet ();
					pset.LoadFromFile (fileEntry.Path);
					ExportProjectPolicyDialog.DefaultFileDialogPolicyDir = System.IO.Path.GetDirectoryName (fileEntry.Path);
					return pset;
				} catch (Exception ex) {
					if (notifyErrors)
						MessageService.ShowException (ex, GettextCatalog.GetString ("The policy set could not be loaded"));
					return null;
				}
			}
		}
		
		void UpdateContentLabels ()
		{
			PolicySet pset = GetPolicySet (false);
			if (pset == null) {
				labelChangesTitle.Hide ();
				if (radioFile.Active) {
					if (string.IsNullOrEmpty (fileEntry.Path) || !System.IO.File.Exists (fileEntry.Path)) {
						labelChanges.Text = GettextCatalog.GetString ("Please select a valid policy file");
					}
					else {
						labelChanges.Text = GettextCatalog.GetString ("The selected file is not a valid policies file");
					}
				}
				else
					labelChanges.Text = string.Empty;
				return;
			}
			labelChangesTitle.Show ();
			labelChanges.Text = GetPoliciesDescription (pset);
		}
		
		public static string GetPoliciesDescription (PolicyContainer pset)
		{
			Dictionary<string,List<string>> content = new Dictionary<string, List<string>> ();
			foreach (var p in pset.DirectGetAll ()) {
				string name = PolicyService.GetPolicyTypeDescription (p.PolicyType);
				List<string> scopes;
				if (!content.TryGetValue (name, out scopes))
					scopes = content [name] = new List<string> ();
				scopes.Add (p.Scope ?? "");
			}
			
			var sorted = content.ToList ();
			sorted.Sort ((x, y) => x.Key.CompareTo(y.Key));
			StringBuilder sb = new StringBuilder ();
			
			foreach (var pol in sorted) {
				if (sb.Length > 0)
					sb.Append ('\n');
				sb.Append (pol.Key);
				if (pol.Value.Count != 1 || pol.Value[0].Length != 0) {
					sb.Append (" (");
					bool first = true;
					if (pol.Value.Remove ("")) {
						sb.Append (GettextCatalog.GetString ("default settings"));
						first = false;
					}
					foreach (var s in pol.Value) {
						if (!first)
							sb.Append (", ");
						sb.Append (s);
						first = false;
					}
					sb.Append (")");
				}
			}
			return sb.ToString ();
		}

		protected void OnCombPoliciesChanged (object sender, System.EventArgs e)
		{
			UpdateContentLabels ();
		}

		protected void OnFileEntryPathChanged (object sender, System.EventArgs e)
		{
			UpdateContentLabels ();
		}
	}
}

