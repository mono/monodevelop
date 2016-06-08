// HighlightingPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using System.Text;
using Gtk;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components.Extensions;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class HighlightingPanel : Gtk.Bin, IOptionsPanel
	{
		string schemeName;
		ListStore styleStore = new ListStore (typeof (string), typeof (Mono.TextEditor.Highlighting.ColorScheme), typeof(bool));
		static Lazy<Gdk.Pixbuf> errorPixbuf = new Lazy<Gdk.Pixbuf> (() => ImageService.GetIcon (Stock.DialogError, IconSize.Menu).ToPixbuf ());

		public HighlightingPanel ()
		{
			this.Build ();
			var col = new TreeViewColumn ();
			var crpixbuf = new CellRendererPixbuf ();
			col.PackStart (crpixbuf, false);
			col.SetCellDataFunc (crpixbuf, ImageDataFunc);
			var crtext = new CellRendererText ();
			col.PackEnd (crtext, true);
			col.SetAttributes (crtext, "markup", 0);
			styleTreeview.AppendColumn (col);
			styleTreeview.Model = styleStore;
			styleTreeview.SearchColumn = -1; // disable the interactive search
			schemeName = DefaultSourceEditorOptions.Instance.ColorScheme;
			MonoDevelop.Ide.Gui.Styles.Changed += HandleThemeChanged;
		}

		static void ImageDataFunc (TreeViewColumn tree_column, CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{

			var isError = (bool)tree_model.GetValue (iter, 2);
			var crpixbuf = (CellRendererPixbuf)cell;
			crpixbuf.Visible = isError;
			crpixbuf.Pixbuf = isError ? errorPixbuf.Value : null;
		}

		void HandleThemeChanged (object sender, EventArgs e)
		{
			ShowStyles ();
		}
		
		protected override void OnDestroyed ()
		{
			DefaultSourceEditorOptions.Instance.ColorScheme = schemeName;

			if (styleStore != null) {
				styleStore.Dispose ();
				styleStore = null;
			}

			MonoDevelop.Ide.Gui.Styles.Changed -= HandleThemeChanged;
			base.OnDestroyed ();
		}

		string GetMarkup (string name, string description)
		{
			return String.Format ("<b>{0}</b> - {1}", GLib.Markup.EscapeText (name), GLib.Markup.EscapeText (description));
		}

		public virtual Control CreatePanelWidget ()
		{
			this.addButton.Clicked += AddColorScheme;
			this.removeButton.Clicked += RemoveColorScheme;
			this.buttonEdit.Clicked += HandleButtonEdithandleClicked;
			this.buttonNew.Clicked += HandleButtonNewClicked;
			this.buttonExport.Clicked += HandleButtonExportClicked;
			this.styleTreeview.Selection.Changed += HandleStyleTreeviewSelectionChanged;
			EnableHighlightingCheckbuttonToggled (this, EventArgs.Empty);
			ShowStyles ();
			HandleStyleTreeviewSelectionChanged (null, null);
			return this;
		}

		void HandleButtonNewClicked (object sender, EventArgs e)
		{
			using (var newShemeDialog = new NewColorShemeDialog ()) {
				MessageService.ShowCustomDialog (newShemeDialog, dialog);
			}
			ShowStyles ();
		}

		void HandleStyleTreeviewSelectionChanged (object sender, EventArgs e)
		{
			this.removeButton.Sensitive = false;
			this.buttonEdit.Sensitive = false;
			this.buttonExport.Sensitive = false;
			Gtk.TreeIter iter;
			if (!styleTreeview.Selection.GetSelected (out iter)) 
				return;
			var sheme = (Mono.TextEditor.Highlighting.ColorScheme)styleStore.GetValue (iter, 1);
			if (sheme == null)
				return;
			var isError = (bool)styleStore.GetValue (iter, 2);
			if (isError) {
				this.removeButton.Sensitive = true;
				return;
			}
			DefaultSourceEditorOptions.Instance.ColorScheme = sheme.Name;
			this.buttonExport.Sensitive = true;
			string fileName = sheme.FileName;
			if (fileName == null)
				return;
			this.removeButton.Sensitive = true;
			this.buttonEdit.Sensitive = true;
		}

		void HandleButtonEdithandleClicked (object sender, EventArgs e)
		{
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) {
				using (var editor = new ColorShemeEditor (this)) {
					var colorScheme = (Mono.TextEditor.Highlighting.ColorScheme)this.styleStore.GetValue (selectedIter, 1);
					editor.SetSheme (colorScheme);
					MessageService. ShowCustomDialog (editor, dialog);
				}
			}
		}
		
		Mono.TextEditor.Highlighting.ColorScheme LoadStyle (string styleName, out bool error)
		{
			try {
				error = false;
				return Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (styleName);
			} catch (StyleImportException e) {
				error = true;

				var style = Mono.TextEditor.Highlighting.SyntaxModeService.DefaultColorStyle.Clone ();
				style.Name = styleName;
				switch (e.Reason) {
				case StyleImportException.ImportFailReason.NoValidColorsFound:
					style.Description = GettextCatalog.GetString ("No valid colors found inside the settings. (Maybe only theme is defined - check <FontsAndColors> node?)");
					break;
				default:
					style.Description = GettextCatalog.GetString ("Loading error");
					break;
				}
				style.FileName = Mono.TextEditor.Highlighting.SyntaxModeService.GetFileName (styleName);
				return style;
			} catch (Exception e) {
				LoggingService.LogError ("Error while loading color style " + styleName, e);
				error = true;
				var style = Mono.TextEditor.Highlighting.SyntaxModeService.DefaultColorStyle.Clone ();
				style.Name = styleName;
				style.Description = GettextCatalog.GetString ("Loading error:" + e.Message);
				style.FileName = Mono.TextEditor.Highlighting.SyntaxModeService.GetFileName (styleName);
				return style;
			}
		
		}
		
		internal void ShowStyles ()
		{
			styleStore.Clear ();
			bool error;
			var defaultStyle = LoadStyle (MonoDevelop.Ide.Editor.Highlighting.ColorScheme.DefaultColorStyle, out error);
			TreeIter selectedIter = styleStore.AppendValues (GetMarkup (defaultStyle.Name, defaultStyle.Description), defaultStyle);
			foreach (string styleName in Mono.TextEditor.Highlighting.SyntaxModeService.Styles) {
				if (styleName == MonoDevelop.Ide.Editor.Highlighting.ColorScheme.DefaultColorStyle)
					continue;
				var style = LoadStyle (styleName, out error);
				string name = style.Name ?? "";
				string description = style.Description ?? "";
				// translate only build-in sheme names
				if (string.IsNullOrEmpty (style.FileName)) {
					try {
						name = GettextCatalog.GetString (name);
						if (!string.IsNullOrEmpty (description))
							description = GettextCatalog.GetString (description);
					} catch {
					}
				}
				TreeIter iter = styleStore.AppendValues (GetMarkup (name, description), style, error);
				if (style.Name == DefaultSourceEditorOptions.Instance.ColorScheme)
					selectedIter = iter;
			}
			if (styleTreeview.Selection != null)
				styleTreeview.Selection.SelectIter (selectedIter); 
		}
		
		void RemoveColorScheme (object sender, EventArgs args)
		{
			TreeIter selectedIter;
			if (!styleTreeview.Selection.GetSelected (out selectedIter)) 
				return;
			var sheme = (ColorScheme)this.styleStore.GetValue (selectedIter, 1);
			
			string fileName = sheme.FileName;

			if (fileName != null && fileName.StartsWith (MonoDevelop.Ide.Editor.TextEditorDisplayBinding.SyntaxModePath, StringComparison.Ordinal)) {
				Mono.TextEditor.Highlighting.SyntaxModeService.Remove (sheme);
				File.Delete (fileName);
				ShowStyles ();
			}
		}
		
		void HandleButtonExportClicked (object sender, EventArgs e)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Highlighting Scheme"), MonoDevelop.Components.FileChooserAction.Save) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (GettextCatalog.GetString ("Color schemes"), "*.json");
			if (!dialog.Run ())
				return;
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) {
				var sheme = (Mono.TextEditor.Highlighting.ColorScheme)this.styleStore.GetValue (selectedIter, 1);
				var selectedFile = dialog.SelectedFile.ToString ();
				if (!selectedFile.EndsWith (".json", StringComparison.Ordinal))
					selectedFile += ".json";
				sheme.Save (selectedFile);
			}

		}
		
		void AddColorScheme (object sender, EventArgs args)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Highlighting Scheme"), MonoDevelop.Components.FileChooserAction.Open) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (GettextCatalog.GetString ("Color schemes"), "*.json");
			dialog.AddFilter (GettextCatalog.GetString ("Visual Studio .NET settings"), "*.vssettings");
			if (!dialog.Run ())
				return;

			string newFileName = MonoDevelop.Ide.Editor.TextEditorDisplayBinding.SyntaxModePath.Combine (dialog.SelectedFile.FileName);

			bool success = true;
			try {
				if (File.Exists (newFileName)) {
					MessageService.ShowError (string.Format (GettextCatalog.GetString ("Highlighting with the same name already exists. Remove {0} first."), System.IO.Path.GetFileNameWithoutExtension (newFileName)));
					return;
				}

				File.Copy (dialog.SelectedFile.FullPath, newFileName);
			} catch (Exception e) {
				success = false;
				LoggingService.LogError ("Can't copy syntax mode file.", e);
			}
			if (success) {
				Mono.TextEditor.Highlighting.SyntaxModeService.LoadStylesAndModes (TextEditorDisplayBinding.SyntaxModePath);
				MonoDevelop.Ide.Editor.Highlighting.SyntaxModeService.LoadStylesAndModes (TextEditorDisplayBinding.SyntaxModePath);
				MonoDevelop.Ide.Editor.TextEditorDisplayBinding.LoadCustomStylesAndModes ();
				ShowStyles ();
			}
		}
		
		void EnableHighlightingCheckbuttonToggled (object sender, EventArgs e)
		{
		}

		internal static void UpdateActiveDocument ()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.UpdateParseDocument ();
//				var editor = IdeApp.Workbench.ActiveDocument.Editor;
//				if (editor != null) {
//					editor.Parent.TextViewMargin.PurgeLayoutCache ();
//					editor.Parent.QueueDraw ();
//				}
			}
		}
		
		public virtual void ApplyChanges ()
		{
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) {
				ColorScheme sheme = ((Mono.TextEditor.Highlighting.ColorScheme)this.styleStore.GetValue (selectedIter, 1));
				DefaultSourceEditorOptions.Instance.ColorScheme = schemeName = sheme != null ? sheme.Name : null;
			}
		}

		OptionsDialog dialog;
		
		public void Initialize (OptionsDialog dialog, object dataObject)
		{
			this.dialog = dialog;
		}

		public bool IsVisible ()
		{
			return true;
		}

		public bool ValidateChanges ()
		{
			return true;
		}
	}
}
