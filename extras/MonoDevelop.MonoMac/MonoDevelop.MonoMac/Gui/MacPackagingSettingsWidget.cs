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
using MonoDevelop.MacInterop;

namespace MonoDevelop.MonoMac.Gui
{
	public partial class MonoMacPackagingSettingsWidget : Gtk.Bin
	{
		IList<X509Certificate2> certs;
		
		public static string INSTALLER_PREFIX = "3rd Party Mac Developer Installer";
		public static string APPLICATION_PREFIX = "3rd Party Mac Developer Application";
		
		bool hasBundleCerts, hasPackageCerts, hasProductBuild;
		
		public MonoMacPackagingSettingsWidget ()
		{
			this.Build ();
			
			certs = Keychain.GetAllSigningCertificates ()
				.Where (k => {
					var cn = Keychain.GetCertificateCommonName (k);
					return !cn.StartsWith ("iPhone") && !cn.StartsWith ("com.apple");
				}) .ToList ();
			
			productDefinitionFileEntry.BrowserTitle = GettextCatalog.GetString ("Select Product Definition...");
			
			includeMonoCheck.Toggled += CheckToggled;
			signBundleCheck.Toggled += CheckToggled;
			createPackageCheck.Toggled += CheckToggled;
			signPackageCheck.Toggled += CheckToggled;
			
			string noSignMsg = GettextCatalog.GetString ("Signing is disabled, as no signing keys were found");
			signBundleImage.TooltipText = noSignMsg;
			signInstallerImage.TooltipText = noSignMsg;
			
			//disable packaging and show message if productbuild not found
			hasProductBuild = !string.IsNullOrEmpty (Which ("productbuild"));
			if (!hasProductBuild) {
				installerImage.TooltipText = GettextCatalog.GetString (
					"Packaging is disabled, as the productbuild utility from\n" +
					 "the Apple Application Tools is not installed");
				createPackageCheck.Sensitive = false;
				if (signInstallerImage != null) {
					((Gtk.Container)signInstallerImage.Parent).Remove (signInstallerImage);
					signInstallerImage.Destroy ();
					signInstallerImage = null;
				}
			} else {
				((Gtk.Container)installerImage.Parent).Remove (installerImage);
				installerImage.Destroy ();
				installerImage = null;
			}
			
			UpdateSensitivity ();
		}
		
		static string Which (string programName)
		{
			var env = System.Environment.GetEnvironmentVariable ("PATH");
			if (string.IsNullOrEmpty (env))
				return null;
			return env.Split (System.IO.Path.PathSeparator)
				.Select (d => System.IO.Path.Combine (d, programName))
				.FirstOrDefault (System.IO.File.Exists);
		}

		void CheckToggled (object sender, EventArgs e)
		{
			UpdateSensitivity ();
		}
		
		public void LoadSettings (MonoMacPackagingSettings settings)
		{
			//refill every time in case the value is an unknown - don't want those to be kept in the list
			hasBundleCerts = FillIdentities (bundleIdentityCombo, APPLICATION_PREFIX, INSTALLER_PREFIX, signBundleCheck, signBundleImage);
			hasPackageCerts = FillIdentities (packageIdentityCombo, INSTALLER_PREFIX, APPLICATION_PREFIX, signPackageCheck, signInstallerImage);
			
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
			settings.SignBundle        = signBundleCheck.Sensitive && signBundleCheck.Active;
			settings.BundleSigningKey  = bundleIdentityCombo.SelectedName;
			settings.LinkerMode        = (MonoMacLinkerMode)linkerCombo.Active;
			settings.CreatePackage     = createPackageCheck.Sensitive && createPackageCheck.Active;
			settings.SignPackage       = signPackageCheck.Sensitive && signPackageCheck.Active;
			settings.PackageSigningKey = packageIdentityCombo.SelectedName;
			settings.ProductDefinition = productDefinitionFileEntry.Path;
			if (settings.ProductDefinition.IsEmpty)
				settings.ProductDefinition = FilePath.Null;
		}
		
		bool FillIdentities (SigningIdentityCombo combo, string preferredPrefix, string excludePrefix,
			Gtk.CheckButton enabledCheck, Gtk.Image errorImage)
		{
			combo.ClearList ();
			
			var preferred = new List<string> ();
			var other = new List<string> ();
			
			foreach (var cert in certs) {
				var name = Keychain.GetCertificateCommonName (cert);
				if (excludePrefix != null && name.StartsWith (excludePrefix))
					continue;
				if (name.StartsWith (preferredPrefix)) {
					preferred.Add (name);
				} else {
					other.Add (name);
				}
			}
			
			//disable signing options and show message if no keys found
			if (preferred.Count == 0 && other.Count == 0) {
				enabledCheck.Sensitive = false;
				if (errorImage != null)
					errorImage.Visible = true;
				return false;
			} else {
				enabledCheck.Sensitive = true;
				if (errorImage != null)
					errorImage.Visible = false;
			}
			
			if (preferred.Count > 0) {
				combo.AddItemWithMarkup (GettextCatalog.GetString ("<b>Default App Store Identity</b>"), "", null);
				combo.AddSeparator ();
				foreach (var name in preferred)
					combo.AddItem (name, name, null);
			}
			
			if (other.Count > 0) {
				if (preferred.Any ())
					combo.AddSeparator ();
				foreach (var name in other)
					combo.AddItem (name, name, null);
			}
			return true;
		}
		
		void UpdateSensitivity ()
		{
			linkerLabel.Sensitive = linkerCombo.Sensitive = includeMonoCheck.Active;
			
			signBundleCheck.Sensitive = hasBundleCerts;
			bool signBundle = hasBundleCerts && signBundleCheck.Active;
			bundleSigningLabel.Sensitive = signBundle;
			bundleIdentityCombo.Sensitive = signBundle;
			
			createPackageCheck.Sensitive = hasProductBuild;
			bool createPackage = hasProductBuild && createPackageCheck.Active;
			productDefinitionLabel.Sensitive = createPackage;
			productDefinitionFileEntry.Sensitive = createPackage;
			if (signInstallerImage != null)
				signInstallerImage.Sensitive = createPackage;
			
			signPackageCheck.Sensitive = createPackage && hasPackageCerts;
			bool signPackage = createPackage && hasPackageCerts && signPackageCheck.Active;
			packageSigningLabel.Sensitive = signPackage;
			packageIdentityCombo.Sensitive = signPackage;
		}
	}
}

