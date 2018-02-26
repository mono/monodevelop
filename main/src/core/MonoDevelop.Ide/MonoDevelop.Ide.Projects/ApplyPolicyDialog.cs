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
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Projects
{
	partial class ApplyPolicyDialog : Gtk.Dialog
	{
		IPolicyProvider policyProvider;
		
		PoliciesListSummaryTree tree;
		
		public ApplyPolicyDialog (IPolicyProvider policyProvider)
		{
			this.policyProvider = policyProvider;
			
			this.Build ();
			tree = new PoliciesListSummaryTree ();
			policiesScroll.Add (tree);
			tree.Show ();
			
			foreach (PolicySet pset in PolicyService.GetPolicySets ())
				if (pset.Visible)
					combPolicies.AppendText (pset.Name);
			
			fileEntry.DefaultPath = ExportProjectPolicyDialog.DefaultFileDialogPolicyDir;
			fileEntry.FileFilters.AddFilter (BrandingService.BrandApplicationName (GettextCatalog.GetString ("MonoDevelop policy files")), "*.mdpolicy");
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
			PolicySet pset = null;
			try {
				pset = GetPolicySet (true);
				policyProvider.Policies.Import (pset, false);
			} catch (Exception ex) {
				string msg;
				if (pset == null) {
					msg = GettextCatalog.GetString ("The policy set could not be loaded");
				} else {
					msg = GettextCatalog.GetString ("The policy set could not be applied");
				}
				MessageService.ShowError (msg, ex);
				Respond (Gtk.ResponseType.Cancel);
				return;
			}
			Respond (Gtk.ResponseType.Ok);
		}
		
		PolicySet GetPolicySet (bool notifyErrors)
		{
			if (radioCustom.Active) {
				return PolicyService.GetPolicySet (combPolicies.ActiveText);
			}
			
			var f = fileEntry.Path;
			if (string.IsNullOrEmpty (f) || !System.IO.File.Exists (f)) {
				return null;
			}
			
			var pset = new PolicySet ();
			pset.LoadFromFile (fileEntry.Path);
			ExportProjectPolicyDialog.DefaultFileDialogPolicyDir = System.IO.Path.GetDirectoryName (fileEntry.Path);
			return pset;
		}
		
		void UpdateContentLabels ()
		{
			PolicySet pset = null;
			try {
				pset = GetPolicySet (false);
			} catch (Exception ex) {
				LoggingService.LogError ("Policy file could not be loaded", ex);
			}
			tree.SetPolicies (pset);
			if (tree.HasPolicies) {
				buttonOk.Sensitive = true;
				return;
			}
			
			if (pset != null) {
				tree.Message = GettextCatalog.GetString ("The selected policy is empty");
			} else if (radioFile.Active) {
				if (string.IsNullOrEmpty (fileEntry.Path) || !System.IO.File.Exists (fileEntry.Path)) {
					tree.Message = GettextCatalog.GetString ("Please select a valid policy file");
				} else {
					tree.Message = GettextCatalog.GetString ("The selected file is not a valid policies file");
				}
			} else {
				tree.Message = GettextCatalog.GetString ("Please select a policy");
			}
			buttonOk.Sensitive = false;
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
			StringBuilder sb = StringBuilderCache.Allocate ();
			
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
			return StringBuilderCache.ReturnAndFree (sb);
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
	
	internal class PoliciesListSummaryTree : Gtk.TreeView
	{
		Gtk.ListStore store;
		string message;
		
		public PoliciesListSummaryTree () : base (new Gtk.ListStore (typeof (string)))
		{
			CanFocus = false;
			HeadersVisible = false;
			store = (Gtk.ListStore) Model;
			this.AppendColumn ("", new Gtk.CellRendererText (), "text", 0);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			if (HasPolicies) {
				return base.OnExposeEvent (evnt);
			}
			
			var win = evnt.Window;
			win.Clear ();
			if (string.IsNullOrEmpty (message)) {
				return true;
			}
			
			using (var layout = PangoUtil.CreateLayout (this)) {
				layout.SetMarkup ("<i>" + GLib.Markup.EscapeText (message) + "</i>");
				int w, h;
				layout.GetPixelSize (out w, out h);
				var a = Allocation;
				var x = (a.Width - w) / 2;
				var y = (a.Height - h ) / 2;
				win.DrawLayout (Style.TextGC (Gtk.StateType.Normal), x, y, layout);
			}
			return true;
		}
		
		public bool HasPolicies { get; private set; }
		
		/// <summary>
		/// Message to be shown if there are no policies.
		/// </summary>
		public string Message {
			get { return message; }
			set {
				message = value;
				if (!HasPolicies) {
					QueueDraw ();
				}
			}
		}
		
		public void SetPolicies (PolicyContainer pset)
		{
			if (pset == null) {
				store.Clear ();
				HasPolicies = false;
				return;
			}
			
			var content = new Dictionary<string, List<string>> ();
			foreach (var p in pset.DirectGetAll ()) {
				string name = PolicyService.GetPolicyTypeDescription (p.PolicyType);
				List<string> scopes;
				if (!content.TryGetValue (name, out scopes))
					scopes = content [name] = new List<string> ();
				scopes.Add (p.Scope ?? "");
			}
			
			var sorted = content.ToList ();
			sorted.Sort ((x, y) => x.Key.CompareTo(y.Key));
			
			store.Clear ();
			
			var sb = StringBuilderCache.Allocate ();
			foreach (var pol in sorted) {
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
				store.AppendValues (sb.ToString ());
				sb.Length = 0;
			}
			StringBuilderCache.Free (sb);
			HasPolicies = sorted.Count > 0;
		}
	}
}