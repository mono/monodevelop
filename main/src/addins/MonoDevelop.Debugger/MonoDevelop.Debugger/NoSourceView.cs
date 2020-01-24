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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xwt;

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Web;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.Debugger
{
	public class NoSourceView : DocumentController
	{
		readonly ScrollView scrollView = new ScrollView ();
		readonly XwtControl control;
		DebuggerSessionOptions options;
		Label manageOptionsLabel;
		CheckBox sourceLinkCheckbox;
		Button sourceLinkButton;
		Label sourceLinkLabel;
		Button disassemblyButton;
		Label disassemblyLabel;
		Button browseButton;
		StackFrame frame;
		bool downloading;

		public NoSourceView ()
		{
			control = new XwtControl (scrollView);
		}

		public void Update (DebuggerSessionOptions options, StackFrame frame, bool disassemblySupported)
		{
			scrollView.Content = CreateContent (options, frame, disassemblySupported);
		}

		Widget CreateContent (DebuggerSessionOptions options, StackFrame frame, bool disassemblySupported)
		{
			var fileName = GetFilename (frame.SourceLocation?.FileName);
			this.options = options;
			this.frame = frame;

			var vbox = new VBox {
				Spacing = 10,
				Margin = 30
			};

			if (!string.IsNullOrEmpty (fileName)) {
				DocumentTitle = GettextCatalog.GetString ("Source Not Found");
				var headerLabel = new Label {
					Markup = GettextCatalog.GetString ("{0} file not found", $"<b>{fileName}</b>")
				};
				headerLabel.Font = headerLabel.Font.WithScaledSize (2);
				vbox.PackStart (headerLabel);

				if (frame.SourceLocation?.SourceLink != null && options.AutomaticSourceLinkDownload == AutomaticSourceDownload.Ask) {
					var sourceLinkVbox = new VBox {
						MarginBottom = 20,
						MarginTop = 10,
						Spacing = 10,
					};

					sourceLinkLabel = new Label {
						Markup = GettextCatalog.GetString ("External source code is available. Would you like to download {0} and view it?", $"<a href=\"clicked\">{fileName}</a>"),
						Name = "SourceLinkLabel"
					};
					sourceLinkLabel.LinkClicked += OnDownloadSourceClicked;

					sourceLinkVbox.PackStart (sourceLinkLabel);

					var sourceLinkHbox = new HBox {
						Spacing = 10
					};

					sourceLinkButton = new Button (GettextCatalog.GetString ("Download {0}", fileName));
					sourceLinkButton.Clicked += OnDownloadSourceClicked;
					sourceLinkHbox.PackStart (sourceLinkButton);

					sourceLinkCheckbox = new CheckBox (GettextCatalog.GetString ("Always download source code automatically"));
					sourceLinkHbox.PackStart (sourceLinkCheckbox);

					sourceLinkVbox.PackStart (sourceLinkHbox);

					vbox.PackStart (sourceLinkVbox);

					var separator = new HSeparator ();
					vbox.PackStart (separator);
				}

				var buttonBox = new HBox ();

				browseButton = new Button (GettextCatalog.GetString ("Browse…"));
				browseButton.Clicked += OnBrowseClicked;
				buttonBox.PackStart (browseButton);

				if (disassemblySupported) {
					disassemblyButton = new Button (GettextCatalog.GetString ("Go to Disassembly"));
					disassemblyButton.Clicked += OnGoToDisassemblyClicked;
					buttonBox.PackStart (disassemblyButton);
				}

				var hbox = new HBox {
					MarginTop = 20,
					Spacing = 10
				};

				hbox.PackStart (buttonBox);

				if (IdeApp.ProjectOperations.CurrentSelectedSolution != null) {
					manageOptionsLabel = new Label {
						Markup = GettextCatalog.GetString ("Manage the locations used to find source files in the {0}.", "<a href=\"clicked\">" + GettextCatalog.GetString ("Solution Options") + "</a>"),
						MarginLeft = 10
					};
					manageOptionsLabel.LinkClicked += OnManageSolutionOptionsClicked;
					hbox.PackStart (manageOptionsLabel);
				}

				vbox.PackStart (hbox);
			} else {
				DocumentTitle = GettextCatalog.GetString ("Source Not Available");
				var headerLabel = new Label (GettextCatalog.GetString ("Source Not Available"));
				vbox.PackStart (headerLabel);
				var label = new Label (GettextCatalog.GetString ("Source information is missing from the debug information for this module"));
				headerLabel.Font = label.Font.WithScaledSize (2);
				vbox.PackStart (label);

				if (disassemblySupported) {
					disassemblyLabel = new Label {
						Markup = GettextCatalog.GetString ("View disassembly in the {0}", "<a href=\"clicked\">" + GettextCatalog.GetString ("Disassembly Tab") + "</a>")
					};
					disassemblyLabel.LinkClicked += OnGoToDisassemblyClicked;
					vbox.PackStart (disassemblyLabel);
				}
			}

			return vbox;
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

		void OnGoToDisassemblyClicked (object sender, EventArgs e)
		{
			DebuggingService.ShowDisassembly ();
			Document.Close (false).Ignore ();
		}

		void OnManageSolutionOptionsClicked (object sender, EventArgs e)
		{
			if (IdeApp.ProjectOperations.CurrentSelectedSolution == null)
				return;

			IdeApp.ProjectOperations.ShowOptions (IdeApp.ProjectOperations.CurrentSelectedSolution, "DebugSourceFiles");
		}

		async void OnBrowseClicked (object sender, EventArgs e)
		{
			var sf = DebuggingService.CurrentFrame;
			if (sf == null) {
				LoggingService.LogWarning ($"CurrentFrame was null in {nameof (OnBrowseClicked)}");
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

						var doc = await IdeApp.Workbench.OpenDocument (newFilePath, null, sf.SourceLocation.Line, 1, OpenDocumentOptions.Debugger);
						if (doc != null) {
							// close the NoSourceView document tab
							await Document.Close (false);
						}
					}
				} else {
					MessageService.ShowWarning (GettextCatalog.GetString ("File not found."));
				}
			} catch (Exception) {
				MessageService.ShowWarning (GettextCatalog.GetString ("Error opening file."));
			}
		}

		public static async Task<Document> DownloadAndOpenAsync (StackFrame frame)
		{
			var symbolCachePath = UserProfile.Current.CacheDir.Combine ("Symbols");
			var sourceLink = frame.SourceLocation.SourceLink;

			var pm = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (
				GettextCatalog.GetString ("Downloading {0}", sourceLink.Uri),
				Stock.StatusDownload,
				true
			);

			Document doc = null;
			try {
				var downloadLocation = sourceLink.GetDownloadLocation (symbolCachePath);
				Directory.CreateDirectory (Path.GetDirectoryName (downloadLocation));
				DocumentRegistry.SkipNextChange (downloadLocation);

				var client = HttpClientProvider.CreateHttpClient (sourceLink.Uri);

				using (var stream = await client.GetStreamAsync (sourceLink.Uri).ConfigureAwait (false)) {
					using (var fs = new FileStream (downloadLocation, FileMode.Create))
						await stream.CopyToAsync (fs).ConfigureAwait (false);
				}

				frame.UpdateSourceFile (downloadLocation);

				int line = frame.SourceLocation.Line;

				doc = await Runtime.RunInMainThread (() => IdeApp.Workbench.OpenDocument (downloadLocation, null, line, 1, OpenDocumentOptions.Debugger));
			} catch (Exception ex) {
				LoggingService.LogInternalError ("Error downloading SourceLink file", ex);
			} finally {
				pm.Dispose ();
			}

			return doc;
		}

		async void OnDownloadSourceClicked (object sender, EventArgs e)
		{
			if (downloading)
				return;

			downloading = true;

			try {
				if (sourceLinkCheckbox != null && sourceLinkCheckbox.Active) {
					options.AutomaticSourceLinkDownload = AutomaticSourceDownload.Always;
					DebuggingService.SetUserOptions (options);
				}

				var doc = await DownloadAndOpenAsync (frame);

				if (doc != null) {
					// close the NoSourceView document tab
					await Document.Close (false);
				}
			} finally {
				downloading = false;
			}
		}

		protected override Task<Control> OnGetViewControlAsync (CancellationToken token, DocumentViewContent view)
		{
			return Task.FromResult<Control> (control);
		}

		protected override void OnDispose ()
		{
			if (sourceLinkButton != null) {
				sourceLinkLabel.LinkClicked -= OnDownloadSourceClicked;
				sourceLinkButton.Clicked -= OnDownloadSourceClicked;
			}

			if (browseButton != null)
				browseButton.Clicked -= OnBrowseClicked;

			if (disassemblyButton != null)
				disassemblyButton.Clicked -= OnGoToDisassemblyClicked;

			if (disassemblyLabel != null)
				disassemblyLabel.LinkClicked -= OnGoToDisassemblyClicked;

			if (manageOptionsLabel != null)
				manageOptionsLabel.LinkClicked -= OnManageSolutionOptionsClicked;

			base.OnDispose ();
		}
	}
}
