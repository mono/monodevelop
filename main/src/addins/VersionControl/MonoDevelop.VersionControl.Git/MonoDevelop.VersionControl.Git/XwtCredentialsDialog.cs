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
using System.Threading.Tasks;
using LibGit2Sharp;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Xwt;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Git
{
	public class XwtCredentialsDialog : Xwt.Dialog
	{
		internal const int DefaultlLabelWidth = 100;
		internal const int InputContainerContainerSpacing = 10;
		readonly DialogButton okButton;

		readonly ICredentialsWidget credentialsWidget;

		const string credentialMarkupFormat = "<b>{0}</b>";

		public XwtCredentialsDialog (string uri, SupportedCredentialTypes supportedCredential, Credentials credentials)
		{
			Title = GettextCatalog.GetString ("Git Credentials");
			Resizable = false;

			Width = 500;

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

			if (supportedCredential.HasFlag (SupportedCredentialTypes.UsernamePassword))
				credentialsWidget = new UserPasswordCredentialsWidget (credentials as UsernamePasswordCredentials);
			else
				credentialsWidget = new SshCredentialsWidget (credentials as SshUserKeyCredentials);

			credentialsWidget.CredentialsChanged += OnCredentialsChanged;
			mainContainer.PackStart (credentialsWidget.Widget, marginTop: InputContainerContainerSpacing);

			//Buttons
			Buttons.Add (new DialogButton (Command.Cancel));
			Buttons.Add (okButton = new DialogButton (Command.Ok));
			DefaultCommand = Command.Ok;

			okButton.Sensitive = credentialsWidget.CredentialsAreValid;
		}

		void OnCredentialsChanged (object sender, EventArgs e)
		{
			okButton.Sensitive = credentialsWidget.CredentialsAreValid;
		}

		public static Task<bool> Run (string url, SupportedCredentialTypes types, Credentials cred, Components.Window parentWindow = null)
		{
			return Runtime.RunInMainThread (() => {
				var engine = Platform.IsMac ? Toolkit.NativeEngine : Toolkit.CurrentEngine;
				var response = false;
				engine.Invoke (() => {
					using (var xwtDialog = new XwtCredentialsDialog (url, types, cred)) {
						response = xwtDialog.Run (parentWindow ?? DesktopService.GetFocusedTopLevelWindow ()) == Command.Ok;
					}
				});
				return response;
			});
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !IsDisposed) {
				credentialsWidget.CredentialsChanged -= OnCredentialsChanged;
				credentialsWidget.Dispose ();
			}
			base.Dispose (disposing);
		}
	}

	interface ICredentialsWidget : IDisposable
	{
		Widget Widget { get; }
		Credentials Credentials { get; }
		bool CredentialsAreValid { get; }
		event EventHandler CredentialsChanged;
	}

	class UserPasswordCredentialsWidget : Table, ICredentialsWidget
	{
		readonly PasswordEntry passwordEntry;
		readonly TextEntry userTextEntry;

		public Widget Widget => this;

		public UsernamePasswordCredentials Credentials { get; private set; }

		Credentials ICredentialsWidget.Credentials => Credentials;

		public bool CredentialsAreValid {
			get {
				return true;
				// TODO: should we check the strings?
				//return !string.IsNullOrEmpty (Credentials?.Username) && !string.IsNullOrEmpty (Credentials?.Password);
			}
		}

		public event EventHandler CredentialsChanged;

		public UserPasswordCredentialsWidget (UsernamePasswordCredentials creds)
		{
			Credentials = creds ?? new UsernamePasswordCredentials ();

			DefaultRowSpacing = XwtCredentialsDialog.InputContainerContainerSpacing;

			int inputContainerCurrentRow = 0;
			//user container
			var userLabel = new Label (GettextCatalog.GetString ("Username:")) {
				MinWidth = XwtCredentialsDialog.DefaultlLabelWidth
			};
			Add (userLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);
			userLabel.TextAlignment = Alignment.End;
			userTextEntry = new TextEntry { Text = Credentials.Username ?? string.Empty };
			Add (userTextEntry, 1, inputContainerCurrentRow, hexpand: true, vpos: WidgetPlacement.Center, marginRight: Toolkit.CurrentEngine.Type == ToolkitType.XamMac ? 10 : -1);

			userTextEntry.Changed += UserTextEntry_Changed;
			inputContainerCurrentRow++;

			//password container
			var passwordLabel = new Label () {
				TextAlignment = Alignment.End,
				Text = GettextCatalog.GetString ("Password:"),
				MinWidth = XwtCredentialsDialog.DefaultlLabelWidth
			};
			Add (passwordLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);

			passwordEntry = new PasswordEntry () { Password = Credentials.Password ?? string.Empty, MarginTop = 5 };
			passwordEntry.Accessible.LabelWidget = passwordLabel;
			Add (passwordEntry, 1, inputContainerCurrentRow, hexpand: true, vpos: WidgetPlacement.Center, marginRight: Toolkit.CurrentEngine.Type == ToolkitType.XamMac ? 10 : -1);
			passwordEntry.Changed += PasswordEntry_Changed;
		}

		void UserTextEntry_Changed (object sender, EventArgs e) => OnCredentialsChanged ();
		void PasswordEntry_Changed (object sender, EventArgs e) => OnCredentialsChanged ();

		void OnCredentialsChanged ()
		{
			Credentials.Username = userTextEntry.Text;
			Credentials.Password = passwordEntry.Password;
			CredentialsChanged?.Invoke (this, EventArgs.Empty);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !IsDisposed) {
				userTextEntry.KeyPressed -= UserTextEntry_Changed;
				passwordEntry.Changed -= PasswordEntry_Changed;
			}
			base.Dispose (disposing);
		}
	}

	class SshCredentialsWidget : Table, ICredentialsWidget
	{
		readonly TextEntry privateKeyLocationTextEntry;
		readonly TextEntry publicKeyLocationTextEntry;
		readonly PasswordEntry passphraseEntry;
		readonly Button privateKeyLocationButton;
		readonly Button publicKeyLocationButton;

		readonly InformationPopoverWidget warningPublicKey;
		readonly InformationPopoverWidget warningPrivateKey;

		public Widget Widget => this;

		public SshUserKeyCredentials Credentials { get; private set; }

		Credentials ICredentialsWidget.Credentials => Credentials;

		public bool CredentialsAreValid { get; private set; }

		public event EventHandler CredentialsChanged;

		public SshCredentialsWidget (SshUserKeyCredentials creds)
		{
			Credentials = creds ?? new SshUserKeyCredentials ();

			DefaultRowSpacing = XwtCredentialsDialog.InputContainerContainerSpacing;

			int inputContainerCurrentRow = 0;

			var privateKeyLocationLabel = new Label (GettextCatalog.GetString ("Private Key:")) {
				MinWidth = XwtCredentialsDialog.DefaultlLabelWidth,
				TextAlignment = Alignment.End
			};
			Add (privateKeyLocationLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);

			var privateKeyLocationContainer = new HBox ();
			Add (privateKeyLocationContainer, 1, inputContainerCurrentRow, hexpand: true);

			privateKeyLocationTextEntry = new TextEntry { Text = Credentials.PrivateKey ?? string.Empty };
			privateKeyLocationTextEntry.Accessible.LabelWidget = privateKeyLocationLabel;
			privateKeyLocationTextEntry.Changed += PrivateKeyLocationTextEntry_Changed;
			privateKeyLocationContainer.PackStart (privateKeyLocationTextEntry, true, vpos: WidgetPlacement.Center);

			warningPrivateKey = new InformationPopoverWidget { Severity = Ide.Tasks.TaskSeverity.Warning };
			privateKeyLocationContainer.PackStart (warningPrivateKey);
			privateKeyLocationButton = new Button ("…");
			privateKeyLocationButton.Accessible.LabelWidget = privateKeyLocationLabel;
			privateKeyLocationButton.Accessible.Title = GettextCatalog.GetString ("Select a key file");
			privateKeyLocationContainer.PackStart (privateKeyLocationButton);
			inputContainerCurrentRow++;

			//Public key location
			var publicKeyLocationLabel = new Label (GettextCatalog.GetString ("Public Key:")) {
				MinWidth = XwtCredentialsDialog.DefaultlLabelWidth,
				TextAlignment = Alignment.End
			};
			Add (publicKeyLocationLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);

			var publicKeyLocationContainer = new HBox ();
			Add (publicKeyLocationContainer, 1, inputContainerCurrentRow, hexpand: true);

			publicKeyLocationTextEntry = new TextEntry { Text = Credentials.PublicKey ?? string.Empty };
			publicKeyLocationTextEntry.Accessible.LabelWidget = publicKeyLocationLabel;
			publicKeyLocationTextEntry.Changed += PublicKeyLocationTextEntry_Changed;
			publicKeyLocationContainer.PackStart (publicKeyLocationTextEntry, true, vpos: WidgetPlacement.Center);

			warningPublicKey = new InformationPopoverWidget { Severity = Ide.Tasks.TaskSeverity.Warning };
			publicKeyLocationContainer.PackStart (warningPublicKey);
			publicKeyLocationButton = new Button ("…");
			publicKeyLocationButton.Accessible.LabelWidget = publicKeyLocationLabel;
			publicKeyLocationButton.Accessible.Title = GettextCatalog.GetString ("Select a key file");
			publicKeyLocationContainer.PackStart (publicKeyLocationButton);
			inputContainerCurrentRow++;

			//password container
			var passwordLabel = new Label () {
				TextAlignment = Alignment.End,
				Text = GettextCatalog.GetString ("Passphrase:"),
				MinWidth = XwtCredentialsDialog.DefaultlLabelWidth
			};
			Add (passwordLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center);

			passphraseEntry = new PasswordEntry () { MarginTop = 5 };
			passphraseEntry.Accessible.LabelWidget = passwordLabel;
			Add (passphraseEntry, 1, inputContainerCurrentRow, hexpand: true, vpos: WidgetPlacement.Center, marginRight: Toolkit.CurrentEngine.Type == ToolkitType.XamMac ? 1 : -1);
			passphraseEntry.Changed += PasswordEntry_Changed;

			privateKeyLocationButton.Clicked += PrivateKeyLocationButton_Clicked;
			publicKeyLocationButton.Clicked += PublicKeyLocationButton_Clicked;

			OnCredentialsChanged ();
		}

		void PrivateKeyLocationTextEntry_Changed (object sender, EventArgs e) => OnCredentialsChanged ();
		void PublicKeyLocationTextEntry_Changed (object sender, EventArgs e) => OnCredentialsChanged ();
		void PasswordEntry_Changed (object sender, EventArgs e) => OnCredentialsChanged ();

		void PrivateKeyLocationButton_Clicked (object sender, EventArgs e)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Select a private SSH key to use.")) {
				ShowHidden = true,
				CurrentFolder = System.IO.File.Exists (privateKeyLocationTextEntry.Text) ?
				System.IO.Path.GetDirectoryName (privateKeyLocationTextEntry.Text) : Environment.GetFolderPath (Environment.SpecialFolder.Personal)
			};
			dialog.AddFilter (GettextCatalog.GetString ("Private Key Files"), "*");
			if (dialog.Run ()) {
				privateKeyLocationTextEntry.Text = dialog.SelectedFile;
				if (System.IO.File.Exists (privateKeyLocationTextEntry.Text + ".pub"))
					publicKeyLocationTextEntry.Text = privateKeyLocationTextEntry.Text + ".pub";
				
				OnCredentialsChanged ();

				if (string.IsNullOrEmpty (Credentials.PublicKey))
					publicKeyLocationTextEntry.SetFocus ();
				else if (passphraseEntry.Sensitive)
					passphraseEntry.SetFocus ();
			};
		}

		void PublicKeyLocationButton_Clicked (object sender, EventArgs e)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Select a public SSH key to use.")) {
				ShowHidden = true,
				CurrentFolder = System.IO.File.Exists (privateKeyLocationTextEntry.Text)
				? System.IO.Path.GetDirectoryName (privateKeyLocationTextEntry.Text)
				: Environment.GetFolderPath (Environment.SpecialFolder.Personal)
			};
			dialog.AddFilter (GettextCatalog.GetString ("Public Key Files (.pub)"), "*.pub");
			dialog.AddAllFilesFilter ();

			if (dialog.Run ()) {
				publicKeyLocationTextEntry.Text = dialog.SelectedFile;
				OnCredentialsChanged ();
				if (passphraseEntry.Sensitive == true)
					passphraseEntry.SetFocus ();
			};
		}

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

		void OnCredentialsChanged ()
		{
			Credentials.PrivateKey = privateKeyLocationTextEntry.Text ?? string.Empty;
			Credentials.PublicKey = publicKeyLocationTextEntry.Text ?? string.Empty;
			Credentials.Passphrase = passphraseEntry.Password ?? string.Empty;

			bool privateKeyIsValid = ValidatePrivateKey (Credentials.PrivateKey);
			bool publicKeyIsValid = System.IO.File.Exists (Credentials.PublicKey);
			bool hasPassphrase = false;

			if (privateKeyIsValid) {
				hasPassphrase = passphraseEntry.Sensitive = GitCredentials.KeyHasPassphrase (Credentials.PrivateKey);
				if (!hasPassphrase) {
					passphraseEntry.Password = "";
					passphraseEntry.PlaceholderText = passphraseEntry.Accessible.Description = GettextCatalog.GetString ("Private Key is not encrypted");
				}
				warningPrivateKey.Hide ();
			} else {
				warningPrivateKey.Message = GettextCatalog.GetString ("Please select a valid private key file");
				warningPrivateKey.Visible = true;
			}

			if (publicKeyIsValid)
				warningPublicKey.Hide ();
			else {
				warningPublicKey.Message = GettextCatalog.GetString ("Please select a valid public key (.pub) file");
				warningPublicKey.Show ();
			}

			CredentialsAreValid = privateKeyIsValid && publicKeyIsValid && (!hasPassphrase || Credentials.Passphrase.Length > 0);

			CredentialsChanged?.Invoke (this, EventArgs.Empty);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !IsDisposed) {
				privateKeyLocationTextEntry.Changed -= PrivateKeyLocationTextEntry_Changed;
				publicKeyLocationTextEntry.Changed -= PublicKeyLocationTextEntry_Changed;
				passphraseEntry.Changed -= PasswordEntry_Changed;
				privateKeyLocationButton.Clicked -= PrivateKeyLocationButton_Clicked;
				publicKeyLocationButton.Clicked -= PublicKeyLocationButton_Clicked;
			}
			base.Dispose (disposing);
		}
	}
}
