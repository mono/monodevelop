//
// NewFolderDialog.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using Xwt;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	class NewFolderDialog : Dialog
	{
		readonly FilePath parentFolder;
		TextEntry folderNameTextEntry;
		DialogButton addButton;

		public NewFolderDialog (FilePath parentFolder)
		{
			this.parentFolder = parentFolder;

			Build ();

			folderNameTextEntry.Text = GetDefaultFolderName ();
			folderNameTextEntry.SetFocus ();
			folderNameTextEntry.Changed += FolderNameTextEntryChanged;
			addButton.Clicked += AddButtonClicked;
		}

		public FilePath NewFolderCreated { get; private set; }

		void Build ()
		{
			Padding = 0;
			Resizable = false;
			Title = GettextCatalog.GetString ("New Folder");

			var mainVBox = new VBox ();

			var folderNameHBox = new HBox ();
			folderNameHBox.Margin = 20;
			var folderNameLabel = new Label ();
			folderNameLabel.Text = GettextCatalog.GetString ("Folder Name:");
			folderNameHBox.PackStart (folderNameLabel);

			folderNameTextEntry = new TextEntry ();
			folderNameTextEntry.MinWidth = 200;
			folderNameHBox.PackStart (folderNameTextEntry, true, true);
			folderNameTextEntry.SetCommonAccessibilityAttributes (
				"NewFolderDialog.FolderNameTextEntry",
				folderNameLabel.Text,
				GettextCatalog.GetString ("Enter the name for the new folder"));

			mainVBox.PackStart (folderNameHBox);

			var cancelButton = new DialogButton (Command.Cancel);
			Buttons.Add (cancelButton);

			addButton = new DialogButton (Command.Add);
			Buttons.Add (addButton);

			DefaultCommand = addButton.Command;

			Content = mainVBox;
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (disposing) {
				folderNameTextEntry.Changed -= FolderNameTextEntryChanged;
				addButton.Clicked -= AddButtonClicked;
			}
		}

		internal static Task<FilePath> Open (FilePath parentFolder)
		{
			var result = new TaskCompletionSource<FilePath> ();
			Toolkit.NativeEngine.Invoke (delegate {
				using (var dialog = new NewFolderDialog (parentFolder)) {
					dialog.Run (MessageDialog.RootWindow);
					result.SetResult (dialog.NewFolderCreated);
				}
			});
			return result.Task;
		}

		string GetDefaultFolderName ()
		{
			string childFolderName = GettextCatalog.GetString ("New Folder");
			string directoryName = Path.Combine (parentFolder, childFolderName);
			int index = -1;

			if (Directory.Exists (directoryName)) {
				while (Directory.Exists (directoryName + (++index + 1))) {
				}
			}

			if (index >= 0) {
				return childFolderName += index + 1;
			}
			return childFolderName;
		}

		void FolderNameTextEntryChanged (object sender, EventArgs e)
		{
			addButton.Sensitive = folderNameTextEntry.Text.Length > 0;
		}

		void AddButtonClicked (object sender, EventArgs e)
		{
			try {
				bool canClose = AddNewFolder ();
				if (canClose) {
					Close ();
				}
			} catch (Exception ex) {
				MessageService.ShowError (
					TransientFor,
					GettextCatalog.GetString ("An error occurred creating the new folder"),
					null,
					ex,
					logError: true);
				Present ();
			}
		}

		FilePath GetNewFolderPath ()
		{
			return parentFolder.Combine (folderNameTextEntry.Text);
		}

		bool AddNewFolder ()
		{
			FilePath directoryPath = GetNewFolderPath ();

			if (!IsValidFolderName (directoryPath, folderNameTextEntry.Text)) {
				ShowWarning (GettextCatalog.GetString ("The name you have chosen contains illegal characters. Please choose a different name."));
				return false;
			}

			if (Directory.Exists (directoryPath)) {
				ShowWarning (GettextCatalog.GetString ("Folder name is already in use. Please choose a different name."));
				return false;
			}

			Directory.CreateDirectory (directoryPath);
			NewFolderCreated = directoryPath;

			return true;
		}

		void ShowWarning (string message)
		{
			MessageService.ShowWarning (TransientFor, message);

			// Give focus back to dialog.
			Present ();
		}

		bool IsValidFolderName (FilePath folderPath, string folderName)
		{
			return FileService.IsValidPath (folderPath) &&
				!ProjectFolderCommandHandler.ContainsDirectorySeparator (folderName);
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd == Command.Add) {
				// Prevent dialog closing after Add button is activated since an alert message dialog may have been shown.
				return;
			}
			base.OnCommandActivated (cmd);
		}
	}
}
