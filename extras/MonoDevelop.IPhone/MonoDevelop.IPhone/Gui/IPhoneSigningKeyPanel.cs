// 
// IPhoneSigningKeyPanelWidget.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Linq;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects;
using System.Security.Cryptography.X509Certificates;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.IPhone.Gui
{
	class IPhoneSigningKeyPanel : MultiConfigItemOptionsPanel
	{
		IPhoneSigningKeyPanelWidget widget;
		
		public override void Initialize (OptionsDialog dialog, object dataObject)
		{
			base.Initialize (dialog, dataObject);
		}
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is IPhoneProject
				&& (((IPhoneProject)ConfiguredProject).CompileTarget == CompileTarget.Exe);
		}
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			AllowMixedConfigurations = false;
			return (widget = new IPhoneSigningKeyPanelWidget ((IPhoneProject)ConfiguredProject));
		}
		
		public override void LoadConfigData ()
		{
			widget.LoadPanelContents ((IPhoneProjectConfiguration)CurrentConfiguration);
		}

		public override void ApplyChanges ()
		{
			widget.StorePanelContents ((IPhoneProjectConfiguration)CurrentConfiguration);
		}
		
		protected override IEnumerable<ItemConfiguration> FilterConfigurations (IEnumerable<ItemConfiguration> configurations)
		{
			foreach (var conf in configurations) {
				IPhoneProjectConfiguration ipconf = conf as IPhoneProjectConfiguration;
				if (ipconf != null && ipconf.Platform == IPhoneProject.PLAT_IPHONE)
					yield return conf;
			}
		}
	}
	
	partial class IPhoneSigningKeyPanelWidget : Gtk.Bin
	{
		IList<MobileProvision> profiles;
		
		ListStore identityStore = new ListStore (typeof (string), typeof (string), typeof (X509Certificate2));
		ListStore profileStore = new ListStore (typeof (string), typeof (string), typeof (MobileProvision));
		
		public IPhoneSigningKeyPanelWidget (IPhoneProject project)
		{
			this.Build ();
			
			resourceRulesEntry.DefaultFilter = "*.plist";
			resourceRulesEntry.Project = project;
			resourceRulesEntry.EntryIsEditable = true;
			
			entitlementsEntry.DefaultFilter = "*.plist";
			entitlementsEntry.Project = project;
			entitlementsEntry.EntryIsEditable = true;
			
			additionalArgsEntry.AddOptions (IPhoneBuildOptionsWidget.menuOptions);
			
			profiles = MobileProvision.GetAllInstalledProvisions ();
			
			var txtRenderer = new CellRendererText ();
			txtRenderer.Ellipsize = Pango.EllipsizeMode.End;
			
			identityCombo.Model = identityStore;
			identityCombo.PackStart (txtRenderer, true);
			identityCombo.AddAttribute (txtRenderer, "markup", 0);
			
			identityCombo.RowSeparatorFunc = delegate (TreeModel model, TreeIter iter) {
				return (string)model.GetValue (iter, 0) == "-";
			};
			
			identityCombo.Changed += delegate { UpdateProfiles (); };
			
			provisioningCombo.Model = profileStore;
			provisioningCombo.PackStart (txtRenderer, true);
			provisioningCombo.AddAttribute (txtRenderer, "markup", 0);
			
			var signingCerts = Keychain.FindNamedSigningCertificates (x => x.StartsWith ("iPhone")).ToList ();
			signingCerts.Sort ((x , y) => Keychain.GetCertificateCommonName (x).CompareTo (Keychain.GetCertificateCommonName (x)));
			
			identityStore.AppendValues ("<b>Developer (Automatic)</b>", Keychain.DEV_CERT_PREFIX, null);
			identityStore.AppendValues ("<b>Distribution (Automatic)</b>", Keychain.DIST_CERT_PREFIX, null);
			
			int trimStart = "iPhone ".Length;
			
			identityStore.AppendValues ("-", "-", null);
			foreach (var cert in signingCerts) {
				string cn = Keychain.GetCertificateCommonName (cert);
				if (cn.StartsWith (Keychain.DEV_CERT_PREFIX))
					identityStore.AppendValues (GLib.Markup.EscapeText (cn.Substring (trimStart, cn.Length - trimStart)), cn, cert);
			}
			
			identityStore.AppendValues ("-", "-", null);
			foreach (var cert in signingCerts) {
				string cn = Keychain.GetCertificateCommonName (cert);
				if (cn.StartsWith (Keychain.DIST_CERT_PREFIX))
					identityStore.AppendValues (GLib.Markup.EscapeText (cn.Substring (trimStart, cn.Length - trimStart)), cn, cert);
			}
			
			this.ShowAll ();
		}
		
		public void LoadPanelContents (IPhoneProjectConfiguration cfg)
		{
			signingTable.Sensitive = cfg.Platform == IPhoneProject.PLAT_IPHONE;
			
			SigningKey = cfg.CodesignKey;
			ProvisionFingerprint = cfg.CodesignProvision;
			entitlementsEntry.SelectedFile = cfg.CodesignEntitlements;
			resourceRulesEntry.SelectedFile = cfg.CodesignResourceRules;
			additionalArgsEntry.Text = cfg.CodesignExtraArgs ?? "";
		}
		
		public void StorePanelContents (IPhoneProjectConfiguration cfg)
		{
			cfg.CodesignKey = SigningKey;
			cfg.CodesignProvision = ProvisionFingerprint;
			cfg.CodesignEntitlements = entitlementsEntry.SelectedFile;
			cfg.CodesignResourceRules = resourceRulesEntry.SelectedFile;
			cfg.CodesignExtraArgs = NullIfEmpty (additionalArgsEntry.Entry.Text);
		}
		
		string SigningKey {
			get {
				TreeIter iter;
				int active = identityCombo.Active;
				if (active >= 0 && identityStore.GetIter (out iter, new TreePath (new int[] { active })))
					return (string) identityStore.GetValue (iter, 1);
				return null;
			}
			set {
				if (string.IsNullOrEmpty (value) && identityStore.IterNChildren () > 0) {
					identityCombo.Active = 0;
				} else {
					if (!SelectMatchingItem (identityCombo, 1, value)) {
						var name = GettextCatalog.GetString ("Unknown ({0})", value);
						identityStore.AppendValues (GLib.Markup.EscapeText (name), value, new object ());
						SelectMatchingItem (identityCombo, 1, value);
					}
				}
				UpdateProfiles ();
			}
		}
		
		string ProvisionFingerprint {
			get {
				if (!provisioningCombo.Sensitive)
					return null;
				
				TreeIter iter;
				int active = provisioningCombo.Active;
				if (active >= 0 && profileStore.GetIter (out iter, new TreePath (new int[] { active })))
					return (string) profileStore.GetValue (iter, 1);
				return null;
			}
			set {
				if (string.IsNullOrEmpty (value) && profileStore.IterNChildren () > 0) {
					provisioningCombo.Active = 0;
				} else {
					if (!SelectMatchingItem (provisioningCombo, 1, value)) {
						var name = GettextCatalog.GetString ("Unknown ({0})", value);
						profileStore.AppendValues (GLib.Markup.EscapeText (name), value, new object ());
						SelectMatchingItem (provisioningCombo, 1, value);
						provisioningCombo.Sensitive = true;
					}
				}
			}
		}
		
		void UpdateProfiles ()
		{
			profileStore.Clear ();
			
			TreeIter iter;
			int active = identityCombo.Active;
			if (active >= 0 && identityStore.GetIter (out iter, new TreePath (new int[] { active }))) {
				var name = (string) identityStore.GetValue (iter, 1);
				var identityObj = identityStore.GetValue (iter, 2);
				var cert = identityObj as X509Certificate2;
				
				Func<X509Certificate2, bool> matchIdentity;
				//known identity
				if (cert != null) {
					matchIdentity = c => c.Thumbprint == cert.Thumbprint;
				}
				//unknown identity
				else if (identityObj != null) {
					matchIdentity = c => false;
				}
				//auto identity
				else {
					string autoPrefix = name.StartsWith (Keychain.DIST_CERT_PREFIX)?
						Keychain.DIST_CERT_PREFIX : Keychain.DEV_CERT_PREFIX;
					matchIdentity = c => Keychain.GetCertificateCommonName (c).StartsWith (autoPrefix);
				}
				
				var isDuplicate = new Dictionary<string, bool> ();
				var filtered = profiles.Where (p => p.DeveloperCertificates.Any (matchIdentity)).Where (p => {
					if (string.IsNullOrEmpty (p.Uuid)) {
						LoggingService.LogWarning ("Provisioning Profile '{0}' has no UUID", p.Name);
						return false;
					}
					isDuplicate[p.Name] = isDuplicate.ContainsKey (p.Name);
					return true;
				}).ToList ();
				
				if (filtered.Any ()) {
					profileStore.AppendValues (GettextCatalog.GetString ("<b>Automatic</b>"), null, null);
					
					foreach (var f in filtered) {
						var displayName = isDuplicate[f.Name]
							? string.Format ("{0} ({1})", f.Name, f.CreationDate)
							: f.Name;
						profileStore.AppendValues (GLib.Markup.EscapeText (displayName), f.Uuid, f);
					}
					provisioningCombo.Active = 0;
					provisioningCombo.Sensitive = true;
					return;
				}
			}
			
			profileStore.AppendValues (GettextCatalog.GetString ("No matching profiles found"), null, null);
			provisioningCombo.Active = 0;
			provisioningCombo.Sensitive = false;
		}
		
		bool SelectMatchingItem (ComboBox combo, int column, object value)
		{
			var m = combo.Model;
			TreeIter iter;
			int i = 0;
			if (m.GetIterFirst (out iter)) {
				do {
					if (value.Equals (m.GetValue (iter, column))) {
						combo.Active = i;
						return true;
					}
					i++;
				} while (m.IterNext (ref iter));
			}
			return false;
		}
		
		string NullIfEmpty (string s)
		{
			return (s == null || s.Length == 0)? null : s;
		}
	}
}
