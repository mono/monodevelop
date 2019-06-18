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
using Xwt.Drawing;

namespace MonoDevelop.VersionControl.Git
{
	internal static class Styles
	{
		public static Color ErrorForegroundColor { get; internal set; }
		public static int FontSize { get; internal set; }
		public static int ErrorFontSize { get; internal set; }
		public static int DefaultlLabelWidth { get; internal set; }
		public static int InputContainerMargin { get; internal set; }
		public static int InputContainerSpacing { get; internal set; }

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				ErrorForegroundColor = Color.FromName ("#FF3B30");
			} else {
				ErrorForegroundColor = Color.FromName ("#FF453A");
			}

			FontSize = 12;
			ErrorFontSize = 10;
			DefaultlLabelWidth = 60;
			InputContainerMargin = 120;
			InputContainerSpacing = 8;
		}
	}

	public class XwtCredentialsDialog : Xwt.Dialog
	{
		readonly Label credentialsLabel;
		readonly Label errorLabel;
		readonly DialogButton okButton;

		readonly ICredentialsWidget credentialsWidget;

		const string credentialMarkupFormat = "<b>{0}</b>";

		public XwtCredentialsDialog (string uri, SupportedCredentialTypes supportedCredential, Credentials credentials, bool hasError)
		{
			Title = GettextCatalog.GetString ("Sign In to Repository");
			Resizable = false;

			Width = 600;
			Padding = new WidgetSpacing (20, 24, 20, 20);

			var mainContainer = new VBox ();

			Content = mainContainer;

			// Credentials
			credentialsLabel = new Label (GettextCatalog.GetString ("Enter credentials for")) {
				Font = Font.FromName (Ide.Gui.Styles.DefaultFontName).WithSize (Styles.FontSize),
				Wrap = WrapMode.Word
			};
			mainContainer.PackStart (credentialsLabel);

			var credentialValue = new Label {
				Font = Font.FromName (Ide.Gui.Styles.DefaultFontName).WithSize (Styles.FontSize),
				Markup = string.Format (credentialMarkupFormat, uri),
				TooltipText = uri,
				WidthRequest = 560,
				Ellipsize = EllipsizeMode.End
			};
			mainContainer.PackStart (credentialValue, marginTop: -6);

			if (supportedCredential.HasFlag (SupportedCredentialTypes.UsernamePassword))
				credentialsWidget = new UserPasswordCredentialsWidget (credentials as UsernamePasswordCredentials);
			else
				credentialsWidget = new SshCredentialsWidget (credentials as SshUserKeyCredentials);

			credentialsWidget.CredentialsChanged += OnCredentialsChanged;

			mainContainer.PackStart (credentialsWidget.Widget, marginLeft: Styles.InputContainerMargin, marginTop: 20, marginRight: Styles.InputContainerMargin - 20);

			// Error
			errorLabel = new Label () {
				Font = Font.FromName (Ide.Gui.Styles.DefaultFontName).WithSize (Styles.ErrorFontSize),
				Margin = new WidgetSpacing (192, 0, 100, 0),
				Visible = false,
				Wrap = WrapMode.Word,
				TextAlignment = Alignment.Start
			};

			if (supportedCredential.HasFlag (SupportedCredentialTypes.Ssh))
				errorLabel.MarginLeft += 6;

			mainContainer.PackStart (errorLabel);

			//Buttons
			Buttons.Add (new DialogButton (Command.Cancel));
			okButton = new DialogButton (Command.Ok) {
				Label = GettextCatalog.GetString ("Sign In")
			};
			Buttons.Add (okButton);
			DefaultCommand = Command.Ok;

			okButton.Sensitive = credentialsWidget.CredentialsAreValid;

			UpdateStatus (supportedCredential, hasError);
		}

		void UpdateStatus (SupportedCredentialTypes supportedCredential, bool hasError)
		{
			if (hasError) {
				string errorMessage = GettextCatalog.GetString ("The credentials you entered weren't recognized. Please enter a valid username and password.");
				if (supportedCredential.HasFlag (SupportedCredentialTypes.Ssh))
					errorMessage = GettextCatalog.GetString ("The credentials you entered weren't recognized. Please enter a valid key.");

				errorLabel.Markup = "<span color='" + Styles.ErrorForegroundColor.ToHexString (false) + "'>" + errorMessage + "</span>";
				errorLabel.Show ();
				okButton.Label = GettextCatalog.GetString ("Retry");
				okButton.Sensitive = false;
			} else
				errorLabel.Hide ();
		}

		void OnCredentialsChanged (object sender, EventArgs e)
		{
			okButton.Sensitive = credentialsWidget.CredentialsAreValid;
		}

		public static Task<bool> Run (string url, SupportedCredentialTypes types, Credentials cred, Components.Window parentWindow = null, bool hasError = false)
		{
			return Runtime.RunInMainThread (() => {
				var engine = Platform.IsMac ? Toolkit.NativeEngine : Toolkit.CurrentEngine;
				var response = false;
				engine.Invoke (() => {
					using (var xwtDialog = new XwtCredentialsDialog (url, types, cred, hasError)) {
						response = xwtDialog.Run (parentWindow ?? IdeServices.DesktopService.GetFocusedTopLevelWindow ()) == Command.Ok;
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

			DefaultRowSpacing = Styles.InputContainerSpacing;

			int inputContainerCurrentRow = 0;
			//user container
			var userLabel = new Label (GettextCatalog.GetString ("Username:")) {
				MinWidth = Styles.DefaultlLabelWidth
			};
			Add (userLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center, marginRight: 0);
			userLabel.TextAlignment = Alignment.End;
			userTextEntry = new TextEntry { HeightRequest = 21, PlaceholderText = GettextCatalog.GetString ("Required"), Text = Credentials.Username ?? string.Empty };
			Add (userTextEntry, 1, inputContainerCurrentRow, hexpand: true, vpos: WidgetPlacement.Center, marginRight: Toolkit.CurrentEngine.Type == ToolkitType.XamMac ? 0 : -1);

			userTextEntry.Changed += UserTextEntry_Changed;
			inputContainerCurrentRow++;

			//password container
			var passwordLabel = new Label () {
				TextAlignment = Alignment.End,
				Text = GettextCatalog.GetString ("Password:"),
				MinWidth = Styles.DefaultlLabelWidth
			};
			Add (passwordLabel, 0, inputContainerCurrentRow, hexpand: false, vpos: WidgetPlacement.Center, marginRight: 0);

			passwordEntry = new PasswordEntry () { HeightRequest = 21, PlaceholderText = GettextCatalog.GetString("Required"), Password = Credentials.Password ?? string.Empty};
			passwordEntry.Accessible.LabelWidget = passwordLabel;
			Add (passwordEntry, 1, inputContainerCurrentRow, hexpand: true, vpos: WidgetPlacement.Center, marginRight: Toolkit.CurrentEngine.Type == ToolkitType.XamMac ? 0 : -1, marginLeft: 0);
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

			DefaultRowSpacing = Styles.InputContainerSpacing;

			int inputContainerCurrentRow = 0;

			var privateKeyLocationLabel = new Label (GettextCatalog.GetString ("Private Key:")) {
				MinWidth = Styles.DefaultlLabelWidth,
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
				MinWidth = Styles.DefaultlLabelWidth,
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
				MinWidth = Styles.DefaultlLabelWidth
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
