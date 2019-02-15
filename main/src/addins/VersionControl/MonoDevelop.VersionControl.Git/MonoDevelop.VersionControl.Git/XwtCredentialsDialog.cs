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
		const int DefaultTextWidth = 350;
		const int DefaultlLabelWidth = 100;
		const int DefaultlErrorLabelWidth = DefaultlLabelWidth + 30;

		readonly SupportedCredentialTypes type;
		readonly TextEntry keyLocationTextEntry;
		readonly PasswordEntry passwordEntry;
		readonly Label errorLabel;
		readonly Credentials cred;
		readonly DialogButton okButton;
		readonly DialogButton cancelButton;
		readonly Button keyLocationButton;

		TextEntry userTextEntry;

		public XwtCredentialsDialog (string uri, SupportedCredentialTypes supportedCredential, Credentials credentials)
		{
			Title = GettextCatalog.GetString ("Git Credential");
			Width = 500;
			Resizable = false;

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
				Markup = string.Format ("<b>{0}</b>", uri),
				Wrap = WrapMode.Word
			};
			mainContainer.PackStart (credentialValue);

			//Key location
			var keyLocationContainer = new HBox ();
			mainContainer.PackStart (keyLocationContainer);

			keyLocationContainer.MarginTop = 10;

			var keyLocationLabel = new Label (GettextCatalog.GetString ("Key Location:")) {
				WidthRequest = DefaultlLabelWidth,
				TextAlignment = Alignment.End
			};
			keyLocationContainer.PackStart (keyLocationLabel);

			keyLocationTextEntry = new TextEntry ();
			keyLocationTextEntry.KeyPressed += KeyLocationTextEntry_Changed;

			keyLocationContainer.PackStart (keyLocationTextEntry, true);

			keyLocationButton = new Button ("…");
			keyLocationContainer.PackStart (keyLocationButton);

			errorLabel = new Label {
				TextColor = Xwt.Drawing.Colors.Red,
				Visible = false
			};
			mainContainer.PackStart (errorLabel);
			errorLabel.MarginLeft = DefaultlLabelWidth;
			//user container
			var userContainer = new HBox ();
			mainContainer.PackStart (userContainer);

			if (type == SupportedCredentialTypes.UsernamePassword) {
				var userLabel = new Label (GettextCatalog.GetString ("Username:"));
				userLabel.WidthRequest = DefaultlLabelWidth;
				userContainer.PackStart (userLabel);
				userLabel.TextAlignment = Alignment.End;
				userTextEntry = new TextEntry ();
				userContainer.PackStart (userTextEntry, true);

				userTextEntry.KeyPressed += UserTextEntry_KeyPressed;
			}

			//password container
			var passwordContainer = new HBox ();
			mainContainer.PackStart (passwordContainer);

			var passwordLabel = new Label () {
				TextAlignment = Alignment.End,
				Text = GettextCatalog.GetString (type == SupportedCredentialTypes.Ssh ? "Passphrase:" : "Password:"),
				WidthRequest = DefaultlLabelWidth
			};
			passwordContainer.PackStart (passwordLabel);

			passwordEntry = new PasswordEntry {
				WidthRequest = DefaultTextWidth
			};
			passwordContainer.PackStart (passwordEntry, true);
			passwordEntry.KeyPressed += PasswordEntry_KeyPressed;

			//Buttons
			cancelButton = new Xwt.DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);
			cancelButton.Clicked += CancelButton_Clicked;

			okButton = new Xwt.DialogButton (Command.Ok);
			Buttons.Add (okButton);
			okButton.Clicked += OkButton_Clicked;

			keyLocationButton.Clicked += KeyLocationButton_Clicked;
			RefreshPasswordState ();
		}

		void OkButton_Clicked (object sender, EventArgs e)
		{
			if (cred is SshUserKeyCredentials ssh) {
				ssh.PrivateKey = keyLocationTextEntry.Text;
				ssh.PublicKey = ssh.PrivateKey + ".pub";
			}
			Close ();
		}

		void CancelButton_Clicked (object sender, EventArgs e) => Close ();

		void KeyLocationButton_Clicked (object sender, EventArgs e)
		{
			var dialog = new Components.SelectFileDialog (GettextCatalog.GetString ("Select a private SSH key to use.")) {
				ShowHidden = true,
				CurrentFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal)
			};
			if (dialog.Run ()) {
				keyLocationTextEntry.Text = dialog.SelectedFile;
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

		void PasswordEntry_KeyPressed (object sender, KeyEventArgs e)
		{
			if (cred is UsernamePasswordCredentials usernamePasswordCredentials) {
				usernamePasswordCredentials.Password = passwordEntry.Password ?? "";
			} else if (cred is SshUserKeyCredentials userKeyCredentials) {
				userKeyCredentials.Passphrase = passwordEntry.Password ?? "";
			}
			RefreshPasswordState ();
		}

		void UserTextEntry_KeyPressed (object sender, KeyEventArgs e)
		{
			if (cred is UsernamePasswordCredentials usernamePasswordCredentials) {
				usernamePasswordCredentials.Username = userTextEntry.Text;
			}
		}

		void KeyLocationTextEntry_Changed (object sender, EventArgs e) => RefreshPasswordState ();

		void RefreshPasswordState ()
		{
			if (System.IO.File.Exists (keyLocationTextEntry.Text)) {
				var hasPassphrase = passwordEntry.Sensitive = GitCredentials.KeyHasPassphrase (keyLocationTextEntry.Text);

				if (System.IO.File.Exists (keyLocationTextEntry.Text + ".pub")) {
					errorLabel.Visible = false;
					okButton.Sensitive = !hasPassphrase || passwordEntry.Password.Length > 0;
				} else {
					errorLabel.Text = GettextCatalog.GetString ("The public key (.pub) is missing in the selected location");
					errorLabel.Visible = true;
					okButton.Sensitive = false;
				}
				return;
			}

			errorLabel.Text = GettextCatalog.GetString ("No key file in the selected location");
			errorLabel.Visible = true;
			okButton.Sensitive = false;
			passwordEntry.Sensitive = false;
		}

		protected override void Dispose (bool disposing)
		{
			keyLocationTextEntry.KeyPressed -= KeyLocationTextEntry_Changed;
			if (userTextEntry != null) {
				userTextEntry.KeyPressed -= UserTextEntry_KeyPressed;
			}

			passwordEntry.KeyPressed -= PasswordEntry_KeyPressed;
			okButton.Clicked -= OkButton_Clicked;
			cancelButton.Clicked -= CancelButton_Clicked;
			keyLocationButton.Clicked -= KeyLocationButton_Clicked;
			base.Dispose (disposing);
		}
	}
}
