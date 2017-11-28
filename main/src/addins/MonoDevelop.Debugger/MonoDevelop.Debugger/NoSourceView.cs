//
// NoSourceView.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using Xwt;

namespace MonoDevelop.Debugger
{
	public class NoSourceView : ViewContent
	{
		XwtControl xwtControl;
		ScrollView scrollView = new ScrollView ();
		public NoSourceView ()
		{
			xwtControl = new XwtControl (scrollView);
		}

		public void Update (bool disassemblyNotSupported)
		{
			scrollView.Content = CreateContent (disassemblyNotSupported);
		}

		Widget CreateContent (bool disassemblyNotSupported)
		{
			var fileName = GetFilename (DebuggingService.CurrentFrame?.SourceLocation?.FileName);
			var box = new VBox ();
			box.Margin = 30;
			box.Spacing = 10;
			if (!string.IsNullOrEmpty (fileName)) {
				ContentName = GettextCatalog.GetString ("Source Not Found");
				var headerLabel = new Label ();
				headerLabel.Markup = GettextCatalog.GetString ("{0} file not found", $"<b>{fileName}</b>");
				box.PackStart (headerLabel);
				var actionsBox = new HBox ();
				var buttonBrowseAndFind = new Button (GettextCatalog.GetString ("Browse and find {0}", fileName));
				buttonBrowseAndFind.Clicked += OpenFindSourceFileDialog;
				actionsBox.PackStart (buttonBrowseAndFind);
				box.PackStart (actionsBox);
				if (IdeApp.ProjectOperations.CurrentSelectedSolution != null) {
					var manageLookupsLabel = new Label ();
					manageLookupsLabel.Markup = GettextCatalog.GetString ("Manage the locations used to find source files in the {0}", "<a href=\"clicked\">" + GettextCatalog.GetString ("Solution Options") + "</a>");
					manageLookupsLabel.LinkClicked += (sender, e) => {
						if (IdeApp.ProjectOperations.CurrentSelectedSolution == null)
							return;
						IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedSolution, "DebugSourceFiles");
					};
					box.PackStart (manageLookupsLabel);
				}
				headerLabel.Font = headerLabel.Font.WithScaledSize (2);
			} else {
				ContentName = GettextCatalog.GetString ("Source Not Available");
				var headerLabel = new Label (GettextCatalog.GetString ("Source Not Available"));
				box.PackStart (headerLabel);
				var label = new Label (GettextCatalog.GetString ("Source information is missing from the debug information for this module"));
				box.PackStart (label);
				headerLabel.Font = label.Font.WithScaledSize (2);
			}
			if (!disassemblyNotSupported) {
				var labelDisassembly = new Label ();
				labelDisassembly.Markup = GettextCatalog.GetString ("View disassembly in the {0}", "<a href=\"clicked\">" + GettextCatalog.GetString ("Disassembly Tab") + "</a>");
				labelDisassembly.LinkClicked += (sender, e) => {
					DebuggingService.ShowDisassembly ();
					this.WorkbenchWindow.CloseWindow (false);
				};
				box.PackStart (labelDisassembly);
			}
			return box;
		}

		string GetFilename (string fileName)
		{
			if (fileName == null)
				return null;
			var index = fileName.LastIndexOfAny (new char [] { '/', '\\' });
			if (index != -1)
				return fileName.Substring (index + 1);
			return fileName;
		}

		private void OpenFindSourceFileDialog (object sender, EventArgs e)
		{
			var sf = DebuggingService.CurrentFrame;
			if (sf == null) {
				LoggingService.LogWarning ($"CurrentFrame was null in {nameof (OpenFindSourceFileDialog)}");
				return;
			}
			var dlg = new Ide.Gui.Dialogs.OpenFileDialog (GettextCatalog.GetString ("File to Open") + " " + sf.SourceLocation.FileName, FileChooserAction.Open) {
				TransientFor = IdeApp.Workbench.RootWindow,
				ShowEncodingSelector = true,
				ShowViewerSelector = true
			};
			dlg.DirectoryChangedHandler = (s, path) => {
				return SourceCodeLookup.TryDebugSourceFolders (sf.SourceLocation.FileName, sf.SourceLocation.FileHash, new string [] { path });
			};
			if (!dlg.Run ())
				return;
			var newFilePath = dlg.SelectedFile;
			try {
				if (File.Exists (newFilePath)) {
					var ignoreButton = new AlertButton (GettextCatalog.GetString ("Ignore"));
					if (SourceCodeLookup.CheckFileHash (newFilePath, sf.SourceLocation.FileHash) ||
						MessageService.AskQuestion (GettextCatalog.GetString ("File checksum doesn't match."), 1, ignoreButton, new AlertButton (GettextCatalog.GetString ("Cancel"))) == ignoreButton) {
						SourceCodeLookup.AddLoadedFile (newFilePath, sf.SourceLocation.FileName);
						sf.UpdateSourceFile (newFilePath);
						if (IdeApp.Workbench.OpenDocument (newFilePath, null, sf.SourceLocation.Line, 1, OpenDocumentOptions.Debugger) != null) {
							this.WorkbenchWindow.CloseWindow (false);
						}
					}
				} else {
					MessageService.ShowWarning (GettextCatalog.GetString ("File not found."));
				}
			} catch (Exception) {
				MessageService.ShowWarning (GettextCatalog.GetString ("Error opening file."));
			}
		}

		public override Control Control => xwtControl;
	}
}
