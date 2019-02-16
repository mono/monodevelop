//
// XwtCredentialsDialog.cs
//
// Author:
//       Jose Medrano <josmed@nmicrosoft.com>
//
// Copyright (c) 2019 Microsoft Corp, Inc
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
using LibGit2Sharp;
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.VersionControl.Git
{
	public class XwtCredentialsDialog : Xwt.Dialog
	{
		const int DefaultlLabelWidth = 100;
		const int InputContainerContainerSpacing = 10;
		readonly SupportedCredentialTypes type;
		readonly TextEntry privateKeyLocationTextEntry;
		readonly TextEntry publicKeyLocationTextEntry;
		readonly PasswordEntry passwordEntry;
		readonly Credentials cred;
		readonly DialogButton okButton;
		readonly DialogButton cancelButton;
		readonly Button privateKeyLocationButton;
		readonly Button publicKeyLocationButton;

		TextEntry userTextEntry;
		const string credentialMarkupFormat = "<b>{0}</b>";

		readonly Components.InformationPopoverWidget warningPublicKey;
		readonly Components.InformationPopoverWidget warningPrivateKey;

		public XwtCredentialsDialog (string uri, SupportedCredentialTypes supportedCredential, Credentials credentials)
		{
			Title = GettextCatalog.GetString ("Git Credentials");
			Resizable = false;

			Width = 500;

			type = supportedCredential;
			cred = credentials;

			var mainContainer = new VBox ();
			Content = mainContainer;

			//Credentials
			var credentialsLabel = new Label (GettextCatalog.GetString ("Credentials required for the repository:")) {
				Wrap = WrapMode.Word
			};
			mainContainer.PackStart (credentialsLabel);

			var credentialValue = new Label {
				Markup = string.Format (credentialMarkupFormat, uri),
				Wrap = WrapMode.Word
			};
			mainContainer.PackStart (credentialValue);

			//Private key location
			var inputContainer = new Table () { DefaultRowSpacing = InputContainerContainerSpacing };
			mainContainer.PackStart (inputContainer, marginTop: InputContainerContainerSpacing);
			int inputContainerCurrentRow = 0;

			var privateKeyLocationLabel = new Label (GettextCatalog.GetString ("Private Key:")) {
				MinWidth = DefaultlLabelWidth,
				TextAlignment = Alignment.End
			};
			inputContainer.Add (privateKeyLocationLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);

			var privateKeyLocationContainer = new HBox ();
			inputContainer.Add (privateKeyLocationContainer, 1, inputContainerCurrentRow, hexpand: true);

			privateKeyLocationTextEntry = new TextEntry ();
			privateKeyLocationTextEntry.Accessible.LabelWidget = privateKeyLocationLabel;
			privateKeyLocationTextEntry.KeyPressed += PrivateKeyLocationTextEntry_Changed;
			privateKeyLocationContainer.PackStart (privateKeyLocationTextEntry, true, vpos: WidgetPlacement.Center);

			warningPrivateKey = new Components.InformationPopoverWidget { Severity = Ide.Tasks.TaskSeverity.Warning };
			privateKeyLocationContainer.PackStart (warningPrivateKey);
			privateKeyLocationButton = new Button ("…");
			privateKeyLocationButton.Accessible.LabelWidget = privateKeyLocationLabel;
			privateKeyLocationButton.Accessible.Title = GettextCatalog.GetString ("Select a key file");
			privateKeyLocationContainer.PackStart (privateKeyLocationButton);
			inputContainerCurrentRow++;

			//Public key location
			var publicKeyLocationLabel = new Label (GettextCatalog.GetString ("Public Key:")) {
				MinWidth = DefaultlLabelWidth,
				TextAlignment = Alignment.End
			};
			inputContainer.Add (publicKeyLocationLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);

			var publicKeyLocationContainer = new HBox ();
			inputContainer.Add (publicKeyLocationContainer, 1, inputContainerCurrentRow, hexpand: true);

			publicKeyLocationTextEntry = new TextEntry ();
			publicKeyLocationTextEntry.Accessible.LabelWidget = publicKeyLocationLabel;
			publicKeyLocationTextEntry.KeyPressed += PublicKeyLocationTextEntry_KeyPressed;
			publicKeyLocationContainer.PackStart (publicKeyLocationTextEntry, true, vpos: WidgetPlacement.Center);

			warningPublicKey = new Components.InformationPopoverWidget { Severity = Ide.Tasks.TaskSeverity.Warning };
			publicKeyLocationContainer.PackStart (warningPublicKey);
			publicKeyLocationButton = new Button ("…");
			publicKeyLocationButton.Accessible.LabelWidget = publicKeyLocationLabel;
			publicKeyLocationButton.Accessible.Title = GettextCatalog.GetString ("Select a key file");
			publicKeyLocationContainer.PackStart (publicKeyLocationButton);
			inputContainerCurrentRow++;

			//user container
			if (type == SupportedCredentialTypes.UsernamePassword) {
				var userLabel = new Label (GettextCatalog.GetString ("Username:")) {
					MinWidth = DefaultlLabelWidth
				};
				inputContainer.Add (userLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);
				userLabel.TextAlignment = Alignment.End;
				userTextEntry = new TextEntry ();
				inputContainer.Add (userTextEntry, 1, inputContainerCurrentRow, hexpand: true, vpos: WidgetPlacement.Center, marginRight: Toolkit.CurrentEngine.Type == ToolkitType.XamMac ? 1 : -1);

				userTextEntry.KeyPressed += UserTextEntry_KeyPressed;
				inputContainerCurrentRow++;
			}

			//password container
			var passwordLabel = new Label () {
				TextAlignment = Alignment.End,
				Text = type == SupportedCredentialTypes.Ssh ? GettextCatalog.GetString ("Passphrase:") : GettextCatalog.GetString ("Password:"),
				MinWidth = DefaultlLabelWidth
			};
			inputContainer.Add (passwordLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);

			passwordEntry = new PasswordEntry () { MarginTop = 5 };
			passwordEntry.Accessible.LabelWidget = passwordLabel;
			inputContainer.Add (passwordEntry, 1, inputContainerCurrentRow, hexpand: true, vpos: WidgetPlacement.Center, marginRight: Toolkit.CurrentEngine.Type == ToolkitType.XamMac ? 1 : -1);
			passwordEntry.Changed += PasswordEntry_Changed;

			//Buttons
			cancelButton = new Xwt.DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);
			cancelButton.Clicked += CancelButton_Clicked;

			okButton = new Xwt.DialogButton (Command.Ok);
			Buttons.Add (okButton);
			okButton.Clicked += OkButton_Clicked;

			privateKeyLocationButton.Clicked += PrivateKeyLocationButton_Clicked;
			publicKeyLocationButton.Clicked += PublicKeyLocationButton_Clicked;
			RefreshPasswordState ();
		}

		void PasswordEntry_Changed (object sender, EventArgs e)
		{
			if (cred is UsernamePasswordCredentials usernamePasswordCredentials) {
				usernamePasswordCredentials.Password = passwordEntry.Password ?? string.Empty;
			} else if (cred is SshUserKeyCredentials userKeyCredentials) {
				userKeyCredentials.Passphrase = passwordEntry.Password ?? string.Empty;
			}
			RefreshPasswordState ();
		}

		void PublicKeyLocationButton_Clicked (object sender, EventArgs e)
		{
			var dialog = new Components.SelectFileDialog (GettextCatalog.GetString ("Select a public SSH key to use.")) {
				ShowHidden = true,
				CurrentFolder = System.IO.File.Exists (privateKeyLocationTextEntry.Text) 
				? System.IO.Path.GetDirectoryName (privateKeyLocationTextEntry.Text) 
				: Environment.GetFolderPath (Environment.SpecialFolder.Personal)
			};
			dialog.AddFilter (GettextCatalog.GetString ("Public Key Files (.pub)"), "*.pub");
			dialog.AddAllFilesFilter ();

			if (dialog.Run ()) {
				publicKeyLocationTextEntry.Text = dialog.SelectedFile;
				RefreshPasswordState ();
				if (type == SupportedCredentialTypes.Ssh) {
					if (passwordEntry.Sensitive == true) {
						passwordEntry.SetFocus ();
					}
				} else {
					userTextEntry?.SetFocus ();
				}
			};
		}

		void OkButton_Clicked (object sender, EventArgs e)
		{
			if (cred is SshUserKeyCredentials ssh) {
				ssh.PrivateKey = privateKeyLocationTextEntry.Text;
				ssh.PublicKey = publicKeyLocationTextEntry.Text;
			}
			Close ();
		}

		void CancelButton_Clicked (object sender, EventArgs e) => Close ();

		void PrivateKeyLocationButton_Clicked (object sender, EventArgs e)
		{
			var dialog = new Components.SelectFileDialog (GettextCatalog.GetString ("Select a private SSH key to use.")) {
				ShowHidden = true,
				CurrentFolder = System.IO.File.Exists (privateKeyLocationTextEntry.Text) ?
				System.IO.Path.GetDirectoryName (privateKeyLocationTextEntry.Text) : Environment.GetFolderPath (Environment.SpecialFolder.Personal)
			};
			dialog.AddFilter (GettextCatalog.GetString ("Private Key Files"), "*");
			if (dialog.Run ()) {
				privateKeyLocationTextEntry.Text = dialog.SelectedFile;
				if (System.IO.File.Exists (privateKeyLocationTextEntry.Text + ".pub")) {
					publicKeyLocationTextEntry.Text = privateKeyLocationTextEntry.Text + ".pub";
				}
				RefreshPasswordState ();
				if (type == SupportedCredentialTypes.Ssh) {
					if (passwordEntry.Sensitive == true) {
						passwordEntry.SetFocus ();
					}
				} else {
					userTextEntry?.SetFocus ();
				}
			};
		}

		void UserTextEntry_KeyPressed (object sender, KeyEventArgs e)
		{
			if (cred is UsernamePasswordCredentials usernamePasswordCredentials) {
				usernamePasswordCredentials.Username = userTextEntry.Text;
			}
		}

		void PrivateKeyLocationTextEntry_Changed (object sender, EventArgs e) => RefreshPasswordState ();
		void PublicKeyLocationTextEntry_KeyPressed (object sender, KeyEventArgs e) => RefreshPasswordState ();

		static bool ValidatePrivateKey (FilePath privateKey)
		{
			if (privateKey.IsNullOrEmpty)
				return false;
			var finfo = new System.IO.FileInfo (privateKey);
			if (!finfo.Exists)
				return false;
			if (finfo.Length > 512 * 1024) // let's don't allow anything bigger than 512kb
				return false;
			return true;
		}

		void RefreshPasswordState ()
		{
			okButton.Sensitive = false;
			bool hasPassphrase = false;
			if (ValidatePrivateKey (privateKeyLocationTextEntry.Text)) {
				hasPassphrase = passwordEntry.Sensitive = GitCredentials.KeyHasPassphrase (privateKeyLocationTextEntry.Text);
				if (!hasPassphrase) {
					passwordEntry.Password = "";
					passwordEntry.PlaceholderText = passwordEntry.Accessible.Description = GettextCatalog.GetString ("Private Key is not encrypted");
					passwordEntry.Sensitive = false;
				}
				warningPrivateKey.Hide ();
			} else {
				warningPrivateKey.Message = GettextCatalog.GetString ("Please select a valid private key file");
				warningPrivateKey.Visible = true;
			}

			if (System.IO.File.Exists (publicKeyLocationTextEntry.Text)) {
				okButton.Sensitive = !hasPassphrase || passwordEntry.Password.Length > 0;
				warningPublicKey.Hide ();
			} else {
				okButton.Sensitive = false;
				warningPublicKey.Message = GettextCatalog.GetString ("Please select a valid public key (.pub) file");
				warningPublicKey.Show ();
			}
		}

		protected override void Dispose (bool disposing)
		{
			privateKeyLocationTextEntry.KeyPressed -= PrivateKeyLocationTextEntry_Changed;
			publicKeyLocationTextEntry.KeyPressed -= PublicKeyLocationTextEntry_KeyPressed;
			if (userTextEntry != null) {
				userTextEntry.KeyPressed -= UserTextEntry_KeyPressed;
			}
			passwordEntry.Changed -= PasswordEntry_Changed;
			okButton.Clicked -= OkButton_Clicked;
			cancelButton.Clicked -= CancelButton_Clicked;
			privateKeyLocationButton.Clicked -= PrivateKeyLocationButton_Clicked;
			publicKeyLocationButton.Clicked -= PublicKeyLocationButton_Clicked;

			base.Dispose (disposing);
		}
	}
}
