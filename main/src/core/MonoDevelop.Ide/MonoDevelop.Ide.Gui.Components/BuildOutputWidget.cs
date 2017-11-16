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
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Components
{
	class BuildOutputWidget : VBox
	{
		FilePath filename;
		TextEditor editor;
		CompactScrolledWindow scrolledWindow;

		public BuildOutputWidget (FilePath filename)
		{
			this.filename = filename;

			var showDiagnosticsButton = new CheckButton (GettextCatalog.GetString ("Show Diagnostics")) {
				BorderWidth = 0
			};
			showDiagnosticsButton.Clicked += (sender, e) => ReadFile (showDiagnosticsButton.Active);

			var toolbar = new DocumentToolbar ();
			toolbar.Add (showDiagnosticsButton); 
			PackStart (toolbar.Container, expand: false, fill: true, padding: 0);

			editor = TextEditorFactory.CreateNewEditor ();
			editor.IsReadOnly = true;
			editor.FileName = filename;
			editor.Options = new CustomEditorOptions (editor.Options) {
				ShowFoldMargin = true,
				TabsToSpaces = false
			};

			scrolledWindow = new CompactScrolledWindow ();
			scrolledWindow.Add (editor);

			PackStart (scrolledWindow, expand: true, fill: true, padding: 0);
			ShowAll ();

			ReadFile (false);
		}

		protected override void OnDestroyed ()
		{
			editor.Dispose ();
			base.OnDestroyed ();
		}

		void ReadFile (bool showDiagnostics)
		{
			var processor = new BuildOutputProcessor (filename.FullPath, showDiagnostics);
			processor.Process ();

			var (text, segments) = processor.ToTextEditor (editor);

			editor.SetFoldings (new List<IFoldSegment> ());
			editor.Text = text;
			if (segments != null) {
				editor.SetFoldings (segments);
			}
		}
	}
}
