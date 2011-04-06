// 
// MonoDroidPublishDialog.cs
//  
// Author:
//       Carlos Alberto Cortez <ccortes@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.IO;
using System.Linq;
using System.Text;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;

namespace MonoDevelop.MonoDroid.Gui
{
	public partial class MonoDroidPublishDialog : Gtk.Dialog
	{
		// Support for automatic alias detection is in place,
		// but due to the inconvenience of parsing java's tools
		// with localization and relatively a lot of garbage, we may
		// find a way to process the keystores directly from C#

		AndroidSigningOptions signingOptions = new AndroidSigningOptions ();
		string [] dNameEntries; // DName entries
		int keyValidity;
		bool usingNewKey;
		
		enum PublishPage
		{
			KeyLocation = 0,
			KeyAlias = 1,
			KeyCreation = 2,
			Publish = 3
		}
		
		public MonoDroidPublishDialog ()
		{
			this.Build ();
			
			notebook1.ShowTabs = false;
			
			existingKeyStoreRadioButton.Toggled += delegate {
				if (existingKeyStoreRadioButton.Active)
					ValidateKeyStore ();
			};
			newKeyStoreRadioButton.Toggled += delegate {
				if (newKeyStoreRadioButton.Active)
					ValidateKeyStore ();
			};
			
			keyStoreLocEntry.PathChanged += delegate { ValidateKeyStore (); };
			keyStorePasswordEntry.Changed += delegate { ValidateKeyStore (); };
			keyStorePassword2Entry.Changed += delegate { ValidateKeyStore (); };
			keyStoreAliasEntry.Changed += delegate { ValidateKeyStore (); };
			keyStoreKeyPasswordEntry.Changed += delegate { ValidateKeyStore ();};
			
			keyAliasEntry.Changed += delegate { ValidateNewKey (); };
			keyPasswordEntry.Changed += delegate { ValidateNewKey (); };
			keyPassword2Entry.Changed += delegate { ValidateNewKey (); };
			keyValidityEntry.Changed += delegate { ValidateNewKey (); };
			keyNameEntry.Changed += delegate { ValidateNewKey (); };
			keyOrgUnitEntry.Changed += delegate { ValidateNewKey (); };
			keyOrganizationEntry.Changed += delegate { ValidateNewKey (); };
			keyCityEntry.Changed += delegate { ValidateNewKey (); };
			keyStateEntry.Changed += delegate { ValidateNewKey (); };
			keyCountryEntry.Changed += delegate { ValidateNewKey (); };
			
			keyAliasPasswordEntry.Changed += delegate { ValidateKeyAlias (); };
			keyAliasNewButton.Toggled += delegate {
				if (keyAliasNewButton.Active)
					ValidateKeyAlias ();
			};
			keyAliasExistingButton.Toggled += delegate {
				if (keyAliasExistingButton.Active)
					ValidateKeyAlias ();
			};
			
			apkDestionationLocEntry.PathChanged += delegate { ValidateDestination (); };
			
			buttonBack.Clicked += delegate { GoBackward (); };
			buttonForward.Clicked += delegate { GoForward (); };
			
			ValidateKeyStore ();
		}

		public string ApkPath { get; set; }
		public string BaseDirectory { get; set; }

		// Output properties
		public AndroidSigningOptions SigningOptions {
			get { return signingOptions; }
		}
		public int KeyValidity {
			get { return keyValidity; }
		}
		public bool CreateNewKey { 
			get { return usingNewKey; }
		}
		public string DName {
			get { return usingNewKey ? GetDNameFromValues (dNameEntries) : String.Empty; }
		}
		public string DestinationApkPath {
			get { return apkDestionationLocEntry.Path; }
		}

		bool VerifyKeyInfo ()
		{
			bool success = false;
			var output = new StringWriter ();
			IProcessAsyncOperation proc = null;
			try {
				proc = MonoDroidFramework.Toolbox.VerifyKeypair (signingOptions, output, output);
				proc.WaitForCompleted ();
				success = proc.Success;
			} catch {
			} finally {
				if (proc != null)
					proc.Dispose ();
			}

			if (success)
				return true;

			Ide.MessageService.ShowError ("Verification failed", "Keystore verification failed:\n" + output);
			return false;
		}

		void GoBackward ()
		{			
			var page = (PublishPage) notebook1.Page;
			switch (page) {
			case PublishPage.KeyLocation: // Nothing
				break;
			case PublishPage.KeyAlias:
				ValidateKeyStore ();
				notebook1.Page = (int)PublishPage.KeyLocation;
				break;
			case PublishPage.KeyCreation:
				/*if (keyAliasNewButton.Active) {
					// We came from the alias passwd page
					ValidateKeyAlias ();
					notebook1.Page = (int)PublishPage.KeyAlias;
				} else {
					ValidateKeyStore ();
					notebook1.Page = (int)PublishPage.KeyLocation;
				}*/
				ValidateKeyStore ();
				notebook1.Page = (int)PublishPage.KeyLocation;
				break;
			default: // Publish page
				buttonForward.UseStock = true;
				buttonForward.Label = Stock.GoForward;
				
				if (usingNewKey) {
					ValidateNewKey ();
					notebook1.Page = (int)PublishPage.KeyCreation;
				} else {
					/*ValidateKeyAlias ();
					notebook1.Page = (int)PublishPage.KeyAlias;*/
					ValidateKeyStore ();
					notebook1.Page = (int)PublishPage.KeyLocation;
				}
				
				break;
			}
		}
		
		void GoForward ()
		{
			var page = (PublishPage) notebook1.Page;
			switch (page) {
			case PublishPage.KeyLocation:
				if (newKeyStoreRadioButton.Active) {
					ValidateNewKey ();
					notebook1.Page = (int) PublishPage.KeyCreation;
				} else {
					if (!VerifyKeyInfo ())
						return;

					//ValidateKeyAlias ();
					//notebook1.Page = (int)PublishPage.KeyAlias;
					goto case PublishPage.KeyCreation;
				}
				break;
			case PublishPage.KeyAlias:
				if (keyAliasExistingButton.Active)
					goto case PublishPage.KeyCreation;
					
				ValidateNewKey ();
				notebook1.Page = (int)PublishPage.KeyCreation;
				break;
			case PublishPage.KeyCreation:
				usingNewKey = newKeyStoreRadioButton.Active;

				buttonForward.Label = GettextCatalog.GetString ("Create");
				buttonForward.UseStock = true;

				if (apkDestionationLocEntry.Path.Length == 0)
					apkDestionationLocEntry.Path = System.IO.Path.GetFileName (ApkPath);

				ValidateDestination ();
				destinationSummaryStatus.Text = GetSummary ();
				notebook1.Page = (int)PublishPage.Publish;
				break;
			default: // Publish
				Respond (ResponseType.Ok);
				break;
			}
		}

		string GetSummary ()
		{
			string summary = String.Empty;
			if (usingNewKey)
				summary = "Certificate expires in " + keyValidity + " years.\n\n";
			if (keyValidity < 25 || !usingNewKey)
				summary += "It is recommended that the certificate is valid for the planned lifetime of the product.\n\n" +
					"If the certificate expires, a new certificate will be needed, and applications will not" +
					"be able to updgrade, forcing an uninstall/install cycle.\n\n" +
					"Android Market requires certificares to be valid until 2033.";

			return summary;
		}

		void ValidateKeyStore ()
		{	
			bool newKeyStore = newKeyStoreRadioButton.Active;
			
			buttonBack.Sensitive = false;
			buttonForward.Sensitive = false;
			keyStorePassword2Entry.Sensitive = newKeyStore;
			keyStoreAliasEntry.Sensitive = !newKeyStore;
			keyStoreKeyPasswordEntry.Sensitive = !newKeyStore;
			
			string keyStoreLoc = keyStoreLocEntry.Path;
			if (keyStoreLoc.Length == 0) {
				SetKeyStoreStatus (false, "Enter a path to the keystore.");
				return;
			}	
			if (newKeyStore && File.Exists (keyStoreLoc)) {
				SetKeyStoreStatus (false, "Keystore file already exists.");
				return;
			}
			if (!newKeyStore && !File.Exists (keyStoreLoc)) {
				SetKeyStoreStatus (false, "Keystore does not exist.");
				return;
			}
			
			string keyStorePass = keyStorePasswordEntry.Text;
			string keyStorePass2 = keyStorePassword2Entry.Text;
			if (keyStorePass.Length == 0) {
				SetKeyStoreStatus (false, "Enter a password for the keystore.");
				return;
			}
			if (keyStorePass.Length < 6) {			
				SetKeyStoreStatus (false, "Keystore password too short. It must be at least 6 characters.");
				return;
			}
			if (newKeyStore && keyStorePass != keyStorePass2) {
				SetKeyStoreStatus (false, "Passwords do not match.");
				return;
			}

			string keyAlias = keyStoreAliasEntry.Text;
			string keyPass = keyStoreKeyPasswordEntry.Text;
			if (!newKeyStore) {
				if (keyAlias.Length == 0) {
					SetKeyStoreStatus (false, "Enter an alias identifier.");
					return;
				}
				if (keyPass.Length == 0) {
					SetKeyStoreStatus (false, "Enter key password.");
					return;
				}
			}

			SetKeyStoreStatus (true, String.Empty);
			buttonForward.Sensitive = true;
			signingOptions.KeyStore = keyStoreLoc;
			signingOptions.StorePass = keyStorePass;
			if (!newKeyStore) {
				signingOptions.KeyAlias = keyAlias;
				signingOptions.KeyPass = keyPass;
			}
		}
		
		void ValidateKeyAlias ()
		{
			buttonBack.Sensitive = true;
			buttonForward.Sensitive = false;
			
			string keyAliasPass = keyAliasPasswordEntry.Text;
			bool useExistingKey = keyAliasExistingButton.Active;
			if (useExistingKey && keyAliasPass.Length == 0) {
				SetKeyAliasStatus (false, "Enter key password.");
				return;
			}
			
			SetKeyAliasStatus (true, String.Empty);
			buttonForward.Sensitive = true;
			signingOptions.KeyPass = keyAliasPass;
		}
		
		void ValidateNewKey ()
		{
			buttonBack.Sensitive = true;
			buttonForward.Sensitive = false;
			
			string keyAlias = keyAliasEntry.Text.Trim ();
			
			if (keyAlias.Length == 0) {
				SetKeyStatus (false, "Enter a key alias.");
				return;
			}
			
			string keyPass = keyPasswordEntry.Text;
			string keyPass2 = keyPassword2Entry.Text;
			if (keyPass.Length == 0) {
				SetKeyStatus (false, "Enter key password.");
				return;
			}			
			if (keyPass.Length < 6) {			
				SetKeyStoreStatus (false, "Key password too short. It must be at least 6 characters.");
				return;
			}			
			if (keyPass != keyPass2) {			
				SetKeyStatus (false, "Passwords do not match.");
				return;
			}
			
			int validity = (int)keyValidityEntry.Value;
			if (validity <= 0) {
				SetKeyStatus (false, "Key certificate validity is required.");
				return;
			}
			
			string [] dName = GetDNameEntries ();
			if (!Enumerable.Any (dName, entry => entry.Length > 0)) {
				SetKeyStatus (false, "At least one certificate issuer field is required.");
				return;
			}
			
			SetKeyStatus (true, String.Empty);
			buttonForward.Sensitive = true;
			
			signingOptions.KeyAlias = keyAlias;
			signingOptions.KeyPass = keyPass;
			keyValidity = validity;
			dNameEntries = dName;
		}

		void ValidateDestination ()
		{
			buttonBack.Sensitive = true;

			if (apkDestionationLocEntry.Path.Length == 0) {
				apkDestionationStatusImage.Stock = Stock.Cancel;
				apkDestinationStatusLabel.Text = "Enter destination for the APK file.";
				buttonForward.Sensitive = false;
			} else if (File.Exists (apkDestionationLocEntry.Path)) {
				apkDestionationStatusImage.Stock = Stock.DialogWarning;
				apkDestinationStatusLabel.Text = "File already exists.";
				buttonForward.Sensitive = true;
			} else {
				apkDestionationStatusImage.Pixbuf = null;
				apkDestinationStatusLabel.Text = String.Empty;
				buttonForward.Sensitive = true;
			}
		}

		string [] GetDNameEntries ()
		{
			return new string [] {
				keyNameEntry.Text,
				keyOrgUnitEntry.Text,
				keyOrganizationEntry.Text,
				keyCityEntry.Text,
				keyStateEntry.Text,
				keyCountryEntry.Text
			};
		}

		static string GetDNameFromValues (string [] values)
		{
			var sb = new StringBuilder ();

			for (int i = 0; i < values.Length; i++) {
				string value = values [i];
				if (value.Length == 0)
					continue;

				if (sb.Length > 0)
					sb.Append (", ");

				switch (i) {
				case 0: sb.Append ("CN=");
					break;
				case 1: sb.Append ("OU=");
					break;
				case 2: sb.Append ("O=");
					break;
				case 3: sb.Append ("L=");
					break;
				case 4: sb.Append ("S=");
					break;
				case 5: sb.Append ("C=");
					break;
				}
				sb.Append (value);
			}

			return sb.ToString ();
		}
		
		void SetKeyStatus (bool success, string text)
		{
			if (success)
				keyStatusImage.Pixbuf = null;
			else
				keyStatusImage.Stock = Stock.Cancel;
			
			keyStatusLabel.Text = text;
		}
		
		void SetKeyAliasStatus (bool success, string text)
		{			
			if (success)
				keyAliasStatusImage.Pixbuf = null;
			else
				keyAliasStatusImage.Stock = Stock.Cancel;
			
			keyAliasStatusLabel.Text = text;
		}
		
		void SetKeyStoreStatus (bool success, string text)
		{
			if (success)
				keyStoreStatusImage.Pixbuf = null;
			else
				keyStoreStatusImage.Stock = Stock.Cancel;
			
			keyStoreStatusLabel.Text = text;
		}
	}
}

