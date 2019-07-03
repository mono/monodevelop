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
		readonly Repository repo;
		readonly bool hasChanges;

		IRepositoryEditor currentEditor;

		VBox repoContainer;
		VBox contentContainer;
		HSeparator separator;
		Label repository;
		HBox moduleContainer;
		Label moduleName;
		TextEntry entryName;
		HBox messageContainer;
		Label message;
		TextEntry entryMessage;
		HBox buttonContainer;
		Button cancelButton;
		Button okButton;

		public ConfigureRepositoryDialog (Repository repo, bool hasChanges = true)
		{
			this.repo = repo;
			this.hasChanges = hasChanges;

			Init ();
			BuildGui ();
			AttachEvents ();
			UpdateCheckoutButton ();
		}

		public Repository Repository {
			get {
				return repo;
			}
		}

		public string ModuleName {
			get { return entryName.Text; }
			set { entryName.Text = value; }
		}

		public string Message {
			get { return entryMessage.Text; }
			set { entryMessage.Text = value; }
		}

		void Init ()
		{
			repoContainer = new VBox ();

			contentContainer = new VBox {
				BackgroundColor = Ide.Gui.Styles.BackgroundColor
			};

			separator = new HSeparator ();

			repository = new Label (GettextCatalog.GetString ("Repository") + ":") {
				Margin = new WidgetSpacing (0, 6, 0, 6)
			};

			moduleContainer = new HBox ();

			moduleName = new Label (GettextCatalog.GetString ("Module name") + ":") {
				WidthRequest = 80
			};

			entryName = new TextEntry ();
			messageContainer = new HBox ();

			message = new Label (GettextCatalog.GetString ("Message") + ":") {
				WidthRequest = 80
			};

			entryMessage = new TextEntry ();

			buttonContainer = new HBox {
				Margin = new WidgetSpacing (0, 6, 0, 0)
			};

			cancelButton = new Button (GettextCatalog.GetString ("Cancel"));
			okButton = new Button (GettextCatalog.GetString ("Configure"));
			currentEditor = repo.VersionControlSystem.CreateRepositoryEditor (repo);

		}

		void BuildGui ()
		{
			Title = GettextCatalog.GetString ("Configure Repository");

			Width = 500;

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
			Resizable = false;
		}

		void AttachEvents ()
		{
			UrlBasedRepositoryEditor edit = currentEditor as UrlBasedRepositoryEditor;
			if (edit != null) {
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
			UrlBasedRepositoryEditor edit = currentEditor as UrlBasedRepositoryEditor;
			if (edit != null) {
				edit.UrlChanged -= OnEditUrlChanged;
				edit.PathChanged -= OnEditUrlChanged;
			}

			if (hasChanges) {
				entryMessage.Changed -= OnMessageChanged;
			}

			cancelButton.Clicked -= OnCancel;
			okButton.Clicked -= OnOkClicked;

			if (repoContainer != null) {
				repoContainer = null;
			}

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
			UrlBasedRepositoryEditor edit = currentEditor as UrlBasedRepositoryEditor;
			if (edit == null)
				return;
			okButton.Sensitive = !string.IsNullOrWhiteSpace (edit.RepositoryServer);
		}
	}
}