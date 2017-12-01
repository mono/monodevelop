//
// BuildOuputWidget.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutputWidget : VBox
	{
		TextEditor editor;
		CompactScrolledWindow scrolledWindow;
		CheckButton showDiagnosticsButton;
		Button saveButton;

		public BuildOutput BuildOutput { get; private set; }

		public BuildOutputWidget (BuildOutput output)
		{
			Initialize ();

			SetupBuildOutput (output);
		}

		public BuildOutputWidget (FilePath filePath)
		{
			Initialize ();

			editor.FileName = filePath;

			var output = new BuildOutput ();
			output.Load (filePath.FullPath, false);
			SetupBuildOutput (output);
		}

		void SetupBuildOutput (BuildOutput output)
		{
			BuildOutput = output;
			ProcessLogs (false);

			BuildOutput.OutputChanged += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);
		}

		void Initialize ()
		{
			showDiagnosticsButton = new CheckButton (GettextCatalog.GetString ("Show Diagnostics")) {
				BorderWidth = 0
			};
			showDiagnosticsButton.Clicked += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);

			saveButton = Button.NewWithLabel (GettextCatalog.GetString ("Save"));
			saveButton.Clicked += async (sender, e) => {
				var dlg = new OpenFileDialog (GettextCatalog.GetString ("Save as..."), MonoDevelop.Components.FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow
				};
				if (dlg.Run ()) {
					await BuildOutput.Save (dlg.SelectedFile);
				}
			};

			var toolbar = new DocumentToolbar ();
			toolbar.Add (showDiagnosticsButton);
			toolbar.Add (saveButton); 
			PackStart (toolbar.Container, expand: false, fill: true, padding: 0);

			editor = TextEditorFactory.CreateNewEditor ();
			editor.IsReadOnly = true;
			editor.Options = new CustomEditorOptions (editor.Options) {
				ShowFoldMargin = true,
				TabsToSpaces = false
			};

			scrolledWindow = new CompactScrolledWindow ();
			scrolledWindow.AddWithViewport (editor);

			PackStart (scrolledWindow, expand: true, fill: true, padding: 0);
			ShowAll ();
		}

		protected override void OnDestroyed ()
		{
			editor.Dispose ();
			base.OnDestroyed ();
		}

		void SetupTextEditor (string text, IList<IFoldSegment> segments)
		{
			editor.Text = text;
			if (segments != null) {
				editor.SetFoldings (segments);
			}
		}

		CancellationTokenSource cts;

		void ProcessLogs (bool showDiagnostics)
		{
			cts?.Cancel ();
			cts = new CancellationTokenSource ();

			Task.Run (async () => {
				var (text, segments) = await BuildOutput.ToTextEditor (editor, showDiagnostics);

				if (Runtime.IsMainThread) {
					SetupTextEditor (text, segments);
				} else {
					await Runtime.RunInMainThread (() => SetupTextEditor (text, segments));
				}
			}, cts.Token);
		}
	}
}
