// 
// MacPackagingSettings.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using MonoDevelop.MacDev;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

namespace MonoDevelop.MonoMac.Gui
{
	public partial class MonoMacPackagingSettingsWidget : Gtk.Bin
	{
		IList<X509Certificate2> certs;
		
		public static string INSTALLER_PREFIX = "3rd Party Mac Developer Installer";
		public static string APPLICATION_PREFIX = "3rd Party Mac Developer Application";
		
		public MonoMacPackagingSettingsWidget ()
		{
			this.Build ();
			certs = Keychain.GetAllSigningCertificates ();
			productDefinitionFileEntry.BrowserTitle = GettextCatalog.GetString ("Select Product Definition...");
			
			includeMonoCheck.Toggled += CheckToggled;
			signBundleCheck.Toggled += CheckToggled;
			createPackageCheck.Toggled += CheckToggled;
			signPackageCheck.Toggled += CheckToggled;
		}

		void CheckToggled (object sender, EventArgs e)
		{
			UpdateSensitivity ();
		}
		
		public void LoadSettings (MonoMacPackagingSettings settings)
		{
			//refill every time in case the value is an unknown - don't want those to be kept in the list
			FillIdentities (bundleIdentityCombo, APPLICATION_PREFIX, INSTALLER_PREFIX);
			FillIdentities (packageIdentityCombo, INSTALLER_PREFIX, APPLICATION_PREFIX);
			
			includeMonoCheck.Active            = settings.IncludeMono;
			signBundleCheck.Active             = settings.SignBundle;
			bundleIdentityCombo.SelectedName   = settings.BundleSigningKey;
			linkerCombo.Active                 = (int) settings.LinkerMode;
			createPackageCheck.Active          = settings.CreatePackage;
			signPackageCheck.Active            = settings.SignPackage;
			packageIdentityCombo.SelectedName  = settings.PackageSigningKey;
			productDefinitionFileEntry.Path = settings.ProductDefinition.ToString () ?? "";
			
			UpdateSensitivity ();
		}
		
		public void SaveSettings (MonoMacPackagingSettings settings)
		{
			settings.IncludeMono       = includeMonoCheck.Active;
			settings.SignBundle        = signBundleCheck.Active;
			settings.BundleSigningKey  = bundleIdentityCombo.SelectedName;
			settings.LinkerMode        = (MonoMacLinkerMode)linkerCombo.Active;
			settings.CreatePackage     = createPackageCheck.Active;
			settings.SignPackage       = signPackageCheck.Active;
			settings.PackageSigningKey = packageIdentityCombo.SelectedName;
			settings.ProductDefinition = productDefinitionFileEntry.Path;
			if (settings.ProductDefinition.IsEmpty)
				settings.ProductDefinition = FilePath.Null;
		}
		
		void FillIdentities (SigningIdentityCombo combo, string preferredPrefix, string excludePrefix)
		{
			combo.ClearList ();
			
			combo.AddItemWithMarkup (GettextCatalog.GetString ("<b>Default App Store Identity</b>"), "", null);
			if (certs.Count == 0)
				return;
			
			var preferred = new List<string> ();
			var other = new List<string> ();
			
			foreach (var cert in certs) {
				var name = Keychain.GetCertificateCommonName (cert);
				if (excludePrefix != null && name.StartsWith (excludePrefix))
					continue;
				if (name.StartsWith ("iPhone"))
					continue;
				if (name.StartsWith (preferredPrefix)) {
					preferred.Add (name);
				} else {
					other.Add (name);
				}
			}
			
			if (preferred.Any ()) {
				combo.AddSeparator ();
				foreach (var name in preferred)
					combo.AddItem (name, name, null);
			}
			
			if (other.Any ()) {
				combo.AddSeparator ();
				foreach (var name in other)
					combo.AddItem (name, name, null);
			}
		}
		
		void UpdateSensitivity ()
		{
			linkerLabel.Sensitive = linkerCombo.Sensitive = includeMonoCheck.Active;
			bundleSigningLabel.Sensitive = bundleIdentityCombo.Sensitive = signBundleCheck.Active;
			signPackageCheck.Sensitive = productDefinitionLabel.Sensitive = productDefinitionFileEntry.Sensitive
				= createPackageCheck.Active;
			packageSigningLabel.Sensitive = packageIdentityCombo.Sensitive
				= signPackageCheck.Active && signPackageCheck.Sensitive;
		}
	}
}

