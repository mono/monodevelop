//
// ConfigureRepositoryDialog.cs
//
// Author:
//       Javier Suárez Ruiz <jsuarez@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using Xwt;

namespace MonoDevelop.VersionControl.Dialogs
{
	public class ConfigureRepositoryDialog : Dialog
	{
		readonly bool hasChanges;

		TextEntry entryName;
		TextEntry entryMessage;
		Button cancelButton;
		Button okButton;

		IRepositoryEditor currentEditor;

		public ConfigureRepositoryDialog (Repository repo, bool hasChanges = true)
		{
			Repository = repo;
			this.hasChanges = hasChanges;

			Initialize ();
			UpdateCheckoutButton ();
		}

        public Repository Repository { get; }

        public string ModuleName {
			get { return entryName.Text; }
			set { entryName.Text = value; }
		}

		public string Message {
			get { return entryMessage.Text; }
			set { entryMessage.Text = value; }
		}

		void Initialize ()
		{
			Title = GettextCatalog.GetString ("Configure Repository");
			Width = 500;
			Resizable = false;

			var repoContainer = new VBox ();

			var contentContainer = new VBox {
				BackgroundColor = Ide.Gui.Styles.BackgroundColor
			};

			var separator = new HSeparator ();

			var repository = new Label (GettextCatalog.GetString ("Repository") + ":") {
				Margin = new WidgetSpacing (0, 6, 0, 6)
			};

			var moduleContainer = new HBox ();

			var moduleName = new Label (GettextCatalog.GetString ("Module name") + ":") {
				WidthRequest = 80
			};

			entryName = new TextEntry ();
			var messageContainer = new HBox ();

			var message = new Label (GettextCatalog.GetString ("Message") + ":") {
				WidthRequest = 80
			};

			entryMessage = new TextEntry ();

			var buttonContainer = new HBox {
				Margin = new WidgetSpacing (0, 6, 0, 0)
			};

			cancelButton = new Button (GettextCatalog.GetString ("Cancel"));
			okButton = new Button (GettextCatalog.GetString ("Configure"));
			currentEditor = Repository.VersionControlSystem.CreateRepositoryEditor (Repository);

			contentContainer.PackStart (currentEditor.Widget, true, true);
			currentEditor.Show ();

			repoContainer.PackStart (contentContainer, true);
			repoContainer.PackStart (separator);
			repoContainer.PackStart (repository);

			moduleContainer.PackStart (moduleName);
			moduleContainer.PackStart (entryName, true);

			messageContainer.PackStart (message);
			messageContainer.PackStart (entryMessage, true);

			repoContainer.PackStart (moduleContainer);
			repoContainer.PackStart (messageContainer);

			entryName.Sensitive = hasChanges;
			entryMessage.Sensitive = hasChanges;

			buttonContainer.PackEnd (okButton);
			buttonContainer.PackEnd (cancelButton);

			repoContainer.PackEnd (buttonContainer);

			Content = repoContainer;

			if (currentEditor is UrlBasedRepositoryEditor edit) {
				edit.UrlChanged += OnEditUrlChanged;
				edit.PathChanged += OnEditUrlChanged;
			}

			if (hasChanges) {
				entryMessage.Changed += OnMessageChanged;
			}

			cancelButton.Clicked += OnCancel;
			okButton.Clicked += OnOkClicked;
		}

		protected override void Dispose (bool disposing)
		{
			if (currentEditor is UrlBasedRepositoryEditor edit) {
				edit.UrlChanged -= OnEditUrlChanged;
				edit.PathChanged -= OnEditUrlChanged;
			}

			if (hasChanges) {
				entryMessage.Changed -= OnMessageChanged;
			}

			cancelButton.Clicked -= OnCancel;
			okButton.Clicked -= OnOkClicked;

			currentEditor = null;
			entryName = null;
			entryMessage = null;
			cancelButton = null;
			okButton = null;

			base.Dispose (disposing);
		}

		void OnMessageChanged (object sender, EventArgs e)
		{
			okButton.Sensitive = !string.IsNullOrEmpty (entryMessage.Text);
		}

		void OnEditUrlChanged (object sender, EventArgs e)
		{
			UpdateCheckoutButton ();
		}

		void OnCancel (object sender, EventArgs e)
		{
			Respond (Command.Cancel);
		}

		void OnOkClicked (object sender, EventArgs e)
		{
			if (Repository != null) {
				if (!currentEditor.Validate ())
					return;
			}

			Respond (Command.Ok);
		}

		void UpdateCheckoutButton ()
		{
			if (!(currentEditor is UrlBasedRepositoryEditor edit))
				return;
			okButton.Sensitive = !string.IsNullOrWhiteSpace (edit.RepositoryServer);
		}
	}
}