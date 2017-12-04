//
// TextMateBundleOptionsPanelWidget.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using Gtk;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Editor.TextMate
{
	[System.ComponentModel.ToolboxItem (true)]
	partial class TextMateBundleOptionsPanelWidget : Gtk.Bin
	{
		ListStore styleStore = new ListStore (typeof (string), typeof (LanguageBundle));
		
		public TextMateBundleOptionsPanelWidget ()
		{
			this.Build ();
			textview1.SetMarkup (textview1.Buffer.Text);

			this.addButton.Clicked += AddLanguageBundle;
			this.removeButton.Clicked += RemoveLanguageBundle;

			var col = new TreeViewColumn ();
			var crtext = new CellRendererText ();
			col.PackEnd (crtext, true);
			col.SetAttributes (crtext, "markup", 0);
			bundleTreeview.AppendColumn (col);
			bundleTreeview.Model = styleStore;

			FillBundles ();
		}

		protected override void OnDestroyed ()
		{
			addButton.Clicked -= AddLanguageBundle;
			removeButton.Clicked -= RemoveLanguageBundle;

			if (styleStore != null) {
				styleStore.Dispose ();
				styleStore = null;
			}
			base.OnDestroyed ();
		}

		void AddLanguageBundle (object sender, EventArgs e)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Language Bundles"), MonoDevelop.Components.FileChooserAction.Open) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (GettextCatalog.GetString ("Bundles"), "*.tmBundle", "*.sublime-package", "*.tmbundle");
			if (!dialog.Run ())
				return;
			string newFileName = SyntaxHighlightingService.LanguageBundlePath.Combine (dialog.SelectedFile.FileName);
			bool success = true;
			try {
				if (File.Exists (newFileName)) {
					MessageService.ShowError (string.Format (GettextCatalog.GetString ("Bundle with the same name already exists. Remove {0} first."), System.IO.Path.GetFileNameWithoutExtension (newFileName)));
					return;
				}
				File.Copy (dialog.SelectedFile.FullPath, newFileName);
			} catch (Exception ex) {
				success = false;
				LoggingService.LogError ("Can't copy syntax mode file.", ex);
			}
			if (success) {
				var bundle = SyntaxHighlightingService.LoadStyleOrMode (newFileName) as LanguageBundle;
				if (bundle != null) {
					foreach (var h in bundle.Highlightings)
						h.PrepareMatches ();
					FillBundles ();
				} else {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid bundle: " + dialog.SelectedFile.FileName));
					try {
						File.Delete (newFileName);
					} catch (Exception) {}
 				}
			}
		}

		void RemoveLanguageBundle (object sender, EventArgs e)
		{
			TreeIter selectedIter;
			if (!bundleTreeview.Selection.GetSelected (out selectedIter))
				return;
			var bundle = (LanguageBundle)this.styleStore.GetValue (selectedIter, 1);
			SyntaxHighlightingService.Remove (bundle);
			if (File.Exists (bundle.FileName))
				FileService.DeleteFile (bundle.FileName);
			FillBundles ();
		}

		void FillBundles ()
		{
			styleStore.Clear ();
			foreach (var bundle in SyntaxHighlightingService.LanguageBundles) {
				styleStore.AppendValues (bundle.Name, bundle);
			}
		}
	}
}