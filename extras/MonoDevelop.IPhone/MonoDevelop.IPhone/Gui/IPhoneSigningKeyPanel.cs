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
using MonoDevelop.MacDev;

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
		Dictionary<string,string> profileSelections = new Dictionary<string, string> ();
		bool suppressSelectionSnapshot;
		
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
			
			FillIdentities ();
			
			identityCombo.Changed += delegate {
				UpdateProfiles ();
			};
			
			provisioningCombo.Changed += delegate {
				if (!suppressSelectionSnapshot)
					profileSelections[identityCombo.SelectedName] = provisioningCombo.SelectedName;
			};
			
			this.ShowAll ();
		}
		
		public void LoadPanelContents (IPhoneProjectConfiguration cfg)
		{
			profileSelections.Clear ();
			provisioningCombo.ClearList ();
			
			signingTable.Sensitive = cfg.Platform == IPhoneProject.PLAT_IPHONE;
			
			identityCombo.SelectedName = cfg.CodesignKey;
			provisioningCombo.SelectedName = cfg.CodesignProvision;
			entitlementsEntry.SelectedFile = cfg.CodesignEntitlements;
			resourceRulesEntry.SelectedFile = cfg.CodesignResourceRules;
			additionalArgsEntry.Text = cfg.CodesignExtraArgs ?? "";
		}
		
		public void StorePanelContents (IPhoneProjectConfiguration cfg)
		{
			cfg.CodesignKey = identityCombo.SelectedName;
			cfg.CodesignProvision = provisioningCombo.SelectedName;
			cfg.CodesignEntitlements = entitlementsEntry.SelectedFile;
			cfg.CodesignResourceRules = resourceRulesEntry.SelectedFile;
			cfg.CodesignExtraArgs = NullIfEmpty (additionalArgsEntry.Entry.Text);
		}
		
		void FillIdentities ()
		{
			var signingCerts = Keychain.FindNamedSigningCertificates (x => x.StartsWith ("iPhone")).ToList ();
			signingCerts.Sort ((x , y) => Keychain.GetCertificateCommonName (x).CompareTo (Keychain.GetCertificateCommonName (x)));
			
			identityCombo.AddItemWithMarkup ("<b>Developer (Automatic)</b>", IPhoneProject.DEV_CERT_PREFIX, null);
			identityCombo.AddItemWithMarkup ("<b>Distribution (Automatic)</b>", IPhoneProject.DIST_CERT_PREFIX, null);
			
			int trimStart = "iPhone ".Length;
			
			identityCombo.AddSeparator ();
			foreach (var cert in signingCerts) {
				string cn = Keychain.GetCertificateCommonName (cert);
				if (cn.StartsWith (IPhoneProject.DEV_CERT_PREFIX))
					identityCombo.AddItem (cn.Substring (trimStart, cn.Length - trimStart), cn, cert);
			}
			
			identityCombo.AddSeparator ();
			foreach (var cert in signingCerts) {
				string cn = Keychain.GetCertificateCommonName (cert);
				if (cn.StartsWith (IPhoneProject.DIST_CERT_PREFIX))
					identityCombo.AddItem (cn.Substring (trimStart, cn.Length - trimStart), cn, cert);
			}
		}
		
		void UpdateProfiles ()
		{
			suppressSelectionSnapshot = true;
			provisioningCombo.ClearList ();
			
			var identityName = identityCombo.SelectedName;
			string previousSelection = null;
			if (identityName != null)
				profileSelections.TryGetValue (identityName, out previousSelection);
			
			suppressSelectionSnapshot = false;
			
			if (identityName != null) {
				var identityObj = identityCombo.SelectedItem;
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
					string autoPrefix = identityName.StartsWith (IPhoneProject.DIST_CERT_PREFIX)?
						IPhoneProject.DIST_CERT_PREFIX : IPhoneProject.DEV_CERT_PREFIX;
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
					provisioningCombo.AddItemWithMarkup (GettextCatalog.GetString ("<b>Automatic</b>"), null, null);
					
					foreach (var f in filtered) {
						var displayName = isDuplicate[f.Name]
							? string.Format ("{0} ({1})", f.Name, f.CreationDate)
							: f.Name;
						provisioningCombo.AddItem (displayName, f.Uuid, f);
					}
					provisioningCombo.SelectedName = previousSelection;
					return;
				}
			}
			
			if (previousSelection != null) {
				provisioningCombo.SelectedName = previousSelection;
			} else {
				provisioningCombo.AddItem (GettextCatalog.GetString ("No matching profiles found"), null, null);
				provisioningCombo.Active = 0;
			}
		}
		
		static string NullIfEmpty (string s)
		{
			return s == null || s.Length == 0? null : s;
		}
	}
}
