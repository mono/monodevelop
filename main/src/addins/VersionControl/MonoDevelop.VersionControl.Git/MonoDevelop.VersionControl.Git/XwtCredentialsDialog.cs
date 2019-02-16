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
		const int DefaultlErrorLabelWidth = DefaultlLabelWidth + 30;
		const int KeyLocationContainerHeight = 24;
		const int KeyLocationContainerTopMargin = 10;
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

		readonly Components.InformationPopoverWidget warningPublicKey = new Components.InformationPopoverWidget () { Severity = Ide.Tasks.TaskSeverity.Warning };
		readonly Components.InformationPopoverWidget warningPrivateKey = new Components.InformationPopoverWidget () { Severity = Ide.Tasks.TaskSeverity.Warning };

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
			var privateKeyLocationContainer = new HBox () { HeightRequest = KeyLocationContainerHeight, MarginTop = KeyLocationContainerTopMargin, VerticalPlacement = WidgetPlacement.Center };
			mainContainer.PackStart (privateKeyLocationContainer);

			var privateKeyLocationLabel = new Label (GettextCatalog.GetString ("Private Key:")) {
				WidthRequest = DefaultlLabelWidth,
				TextAlignment = Alignment.End
			};
			privateKeyLocationContainer.PackStart (privateKeyLocationLabel);

			privateKeyLocationTextEntry = new TextEntry ();
			privateKeyLocationTextEntry.KeyPressed += PrivateKeyLocationTextEntry_Changed;
			privateKeyLocationContainer.PackStart (privateKeyLocationTextEntry, true);

			privateKeyLocationContainer.PackStart (warningPrivateKey);
			privateKeyLocationButton = new Button ("…");
			privateKeyLocationContainer.PackStart (privateKeyLocationButton);

			//Public key location
			var publicKeyLocationContainer = new HBox () { HeightRequest = KeyLocationContainerHeight, MarginTop = KeyLocationContainerTopMargin, VerticalPlacement = WidgetPlacement.Center };
			mainContainer.PackStart (publicKeyLocationContainer);

			var publicKeyLocationLabel = new Label (GettextCatalog.GetString ("Public Key:")) {
				WidthRequest = DefaultlLabelWidth,
				TextAlignment = Alignment.End
			};
			publicKeyLocationContainer.PackStart (publicKeyLocationLabel);

			publicKeyLocationTextEntry = new TextEntry ();
			publicKeyLocationTextEntry.KeyPressed += PublicKeyLocationTextEntry_KeyPressed;
			publicKeyLocationContainer.PackStart (publicKeyLocationTextEntry, true);

			publicKeyLocationContainer.PackStart (warningPublicKey);
			publicKeyLocationButton = new Button ("…");
			publicKeyLocationContainer.PackStart (publicKeyLocationButton);

			//user container
			var userContainer = new HBox () { VerticalPlacement = WidgetPlacement.Center };
			mainContainer.PackStart (userContainer);

			if (type == SupportedCredentialTypes.UsernamePassword) {
				var userLabel = new Label (GettextCatalog.GetString ("Username:")) {
					WidthRequest = DefaultlLabelWidth
				};
				userContainer.PackStart (userLabel);
				userLabel.TextAlignment = Alignment.End;
				userTextEntry = new TextEntry ();
				userContainer.PackStart (userTextEntry, true);

				userTextEntry.KeyPressed += UserTextEntry_KeyPressed;
			}

			//password container
			var passwordContainer = new HBox () { VerticalPlacement = WidgetPlacement.Center };
			mainContainer.PackStart (passwordContainer);

			var passwordLabel = new Label () {
				TextAlignment = Alignment.End,
				Text = type == SupportedCredentialTypes.Ssh ? GettextCatalog.GetString ("Passphrase:") : GettextCatalog.GetString ("Password:"),
				WidthRequest = DefaultlLabelWidth
			};
			passwordContainer.PackStart (passwordLabel);

			passwordEntry = new PasswordEntry () { MarginTop = 5 };
			passwordContainer.PackStart (passwordEntry, true);
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

		void RefreshPasswordState ()
		{
			okButton.Sensitive = false;
			bool hasPassphrase = false;
			if (System.IO.File.Exists (privateKeyLocationTextEntry.Text)) {
				hasPassphrase = passwordEntry.Sensitive = GitCredentials.KeyHasPassphrase (privateKeyLocationTextEntry.Text);
				if (!hasPassphrase) {
					passwordEntry.Password = "";
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
