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
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.IPhone.Gui
{
	
	class IPhoneSigningKeyPanel : MultiConfigItemOptionsPanel
	{
		IPhoneSigningKeyPanelWidget widget;
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is IPhoneProject;
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
	}
	
	partial class IPhoneSigningKeyPanelWidget : Gtk.Bin
	{
		//IList<string> signingCerts;
		
		public IPhoneSigningKeyPanelWidget (IPhoneProject project)
		{
			this.Build ();
			
			resourceRulesEntry.DefaultFilter = "*.plist";
			resourceRulesEntry.Project = project;
			resourceRulesEntry.EntryIsEditable = true;
			
			entitlementsEntry.DefaultFilter = "*.plist";
			entitlementsEntry.Project = project;
			entitlementsEntry.EntryIsEditable = true;
			
			additionalArgsEntry.AddOptions (IPhoneBuildOptionsPanelWidget.menuOptions);
			
			enableSigningCheck.Toggled += delegate {
				vbox3.Sensitive = enableSigningCheck.Active;
			};
			
			/*
			var txtRenderer = new CellRendererText ();
			txtRenderer.Ellipsize = Pango.EllipsizeMode.End;
			
			provisioningCombo.Model = developerStore = new ListStore (typeof (String), typeof (String));
			provisioningCombo.PackStart (txtRenderer, true);
			provisioningCombo.AddAttribute (txtRenderer, "markup", 1);
			
			distributionCombo.Model = distributionStore = new ListStore (typeof (String), typeof (String));
			distributionCombo.PackStart (txtRenderer, true);
			distributionCombo.AddAttribute (txtRenderer, "markup", 1);
			
			signingCerts = Keychain.GetAllSigningIdentities ();
			
			string auto = GettextCatalog.GetString ("<b>Automatic</b>");
			developerStore.AppendValues (null, auto);
			distributionStore.AppendValues (null, auto);
			
			foreach (var cert in signingCerts) {
				if (cert.StartsWith (Keychain.DEV_CERT_PREFIX)) {
					developerStore.AppendValues (cert,
						GLib.Markup.EscapeText (cert.Substring (Keychain.DEV_CERT_PREFIX.Length).Trim ()));
				} else if (cert.StartsWith (Keychain.DIST_CERT_PREFIX)) {
					distributionStore.AppendValues (cert,
						GLib.Markup.EscapeText (cert.Substring (Keychain.DIST_CERT_PREFIX.Length).Trim ()));
				}
			}
			
			useSpecificCertCheck.Toggled += delegate {
				certBox.Sensitive = useSpecificCertCheck.Active;
			};*/
			
			this.ShowAll ();
		}
		
		public void LoadPanelContents (IPhoneProjectConfiguration cfg)
		{
			enableSigningCheck.Active = !string.IsNullOrEmpty (cfg.CodesignKey);
			SigningKey = cfg.CodesignKey;
			ProvisionFingerprint = cfg.CodesignProvision;
			entitlementsEntry.SelectedFile = cfg.CodesignEntitlements;
			resourceRulesEntry.SelectedFile = cfg.CodesignResourceRules;
			additionalArgsEntry.Entry.Text = cfg.CodesignExtraArgs;
		}
		
		public void StorePanelContents (IPhoneProjectConfiguration cfg)
		{
			cfg.CodesignKey = enableSigningCheck.Active? SigningKey : null;
			cfg.CodesignProvision = enableSigningCheck.Active? ProvisionFingerprint : null;
			cfg.CodesignEntitlements = entitlementsEntry.SelectedFile;
			cfg.CodesignResourceRules = resourceRulesEntry.SelectedFile;
			cfg.CodesignExtraArgs = NullIfEmpty (additionalArgsEntry.Entry.Text);
		}
		
		string SigningKey {
			get {
				//throw new NotImplementedException ();
				return null;
			}
			set {
				//throw new NotImplementedException ();
			}
		}
		
		string ProvisionFingerprint {
			get {
				//throw new NotImplementedException ();
				return null;
			}
			set {
				//throw new NotImplementedException ();
			}
		}
		
		/*
		internal SigningKeyInformation GetValue ()
		{
			if (!useSpecificCertCheck.Active)
				return null;
			
			string devKey = null, distKey = null;
			TreeIter iter;
			if (developerStore.GetIter (out iter, new TreePath (new int[] { provisioningCombo.Active })))
				devKey = (string) developerStore.GetValue (iter, 0); 
			if (distributionStore.GetIter (out iter, new TreePath (new int[] { distributionCombo.Active })))
				distKey = (string) distributionStore.GetValue (iter, 0);
			return new SigningKeyInformation (devKey, distKey);
		}
		
		void SetValue (SigningKeyInformation value)
		{
			distributionCombo.Active = provisioningCombo.Active = 0;
			useSpecificCertCheck.Active = certBox.Sensitive = value != null;
			if (value == null)
				return;
			
			TreeIter iter;
			if (distributionStore.GetIterFirst (out iter)) {
				int index = 0;
				do {
					if ((string)distributionStore.GetValue (iter, 0) == value.Distribution) {
						distributionCombo.Active = index;
						break;
					}
					index++;
				} while (distributionStore.IterNext (ref iter));
			}
			
			if (developerStore.GetIterFirst (out iter)) {
				int index = 0;
				do {
					if ((string)developerStore.GetValue (iter, 0) == value.Developer) {
						provisioningCombo.Active = index;
						break;
					}
					index++;
				} while (developerStore.IterNext (ref iter));
			}
		}*/
		
		string NullIfEmpty (string s)
		{
			return (s == null || s.Length == 0)? null : s;
		}
	}
}
