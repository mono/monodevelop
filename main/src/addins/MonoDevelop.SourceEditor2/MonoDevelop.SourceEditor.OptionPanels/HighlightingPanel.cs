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
using MonoDevelop.Ide.Editor.Highlighting;
using System.Diagnostics;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	partial class HighlightingPanel : Gtk.Bin, IOptionsPanel
	{
		string schemeName;
		ListStore styleStore = new ListStore (typeof (string), typeof (MonoDevelop.Ide.Editor.Highlighting.EditorTheme), typeof (bool));
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
			schemeName = DefaultSourceEditorOptions.Instance.EditorTheme;
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
			DefaultSourceEditorOptions.Instance.EditorTheme = schemeName;

			MonoDevelop.Ide.Gui.Styles.Changed -= HandleThemeChanged;
			base.OnDestroyed ();
		}

		string GetMarkup (string name, string description)
		{
			if (string.IsNullOrEmpty (description)) 
				return String.Format ("<b>{0}</b>", GLib.Markup.EscapeText (name));
			return String.Format ("<b>{0}</b> - {1}", GLib.Markup.EscapeText (name), GLib.Markup.EscapeText (description));
		}

		public virtual Control CreatePanelWidget ()
		{
			this.addButton.Clicked += AddColorScheme;
			this.removeButton.Clicked += RemoveColorScheme;
			this.buttonOpenFolder.Clicked += ButtonOpenFolder_Clicked;;
			this.styleTreeview.Selection.Changed += HandleStyleTreeviewSelectionChanged;
			EnableHighlightingCheckbuttonToggled (this, EventArgs.Empty);
			ShowStyles ();
			HandleStyleTreeviewSelectionChanged (null, null);
			return this;
		}

		void HandleStyleTreeviewSelectionChanged (object sender, EventArgs e)
		{
			this.removeButton.Sensitive = false;
			Gtk.TreeIter iter;
			if (!styleTreeview.Selection.GetSelected (out iter)) 
				return;
			var sheme = (MonoDevelop.Ide.Editor.Highlighting.EditorTheme)styleStore.GetValue (iter, 1);
			if (sheme == null)
				return;
			var isError = (bool)styleStore.GetValue (iter, 2);
			if (isError) {
				this.removeButton.Sensitive = true;
				return;
			}
			DefaultSourceEditorOptions.Instance.EditorTheme = sheme.Name;
			string fileName = sheme.FileName;
			if (fileName == null)
				return;
			this.removeButton.Sensitive = true;
		}

		EditorTheme LoadStyle (string styleName, out bool error)
		{
			try {
				error = false;
				return SyntaxHighlightingService.GetEditorTheme (styleName);
			} catch (StyleImportException) {
				error = true;
				return new EditorTheme (styleName, new System.Collections.Generic.List<ThemeSetting> (SyntaxHighlightingService.DefaultColorStyle.Settings));
			} catch (Exception e) {
				LoggingService.LogError ("Error while loading color theme " + styleName, e);
				error = true;
				return new EditorTheme (styleName, new System.Collections.Generic.List<ThemeSetting> (SyntaxHighlightingService.DefaultColorStyle.Settings));
			}
		
		}
		
		internal void ShowStyles ()
		{
			// This GetString calls are dummy just to make sure they are inserted into .po file so it can be translated
			// They are actually used few lines lower in this method
			// Only this 4 are translated because rest are names, but this 4 are exceptions because names have actual meaning
			// so it makes sense to translate them 
			GettextCatalog.GetString ("Light");
			GettextCatalog.GetString ("Dark");
			GettextCatalog.GetString ("High Contrast Dark");
			GettextCatalog.GetString ("High Contrast Light");
			styleStore.Clear ();
			bool error;
			var defaultStyle = LoadStyle (MonoDevelop.Ide.Editor.Highlighting.EditorTheme.DefaultThemeName, out error);
			TreeIter selectedIter = styleStore.AppendValues (GetMarkup (defaultStyle.Name, ""), defaultStyle);
			foreach (string styleName in SyntaxHighlightingService.Styles) {
				if (styleName == MonoDevelop.Ide.Editor.Highlighting.EditorTheme.DefaultThemeName)
					continue;
				var style = LoadStyle (styleName, out error);
				string name = style.Name ?? "";
				if (string.IsNullOrEmpty (name))
					continue;
				string description = "";
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
				if (style.Name == DefaultSourceEditorOptions.Instance.EditorTheme)
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
			var sheme = (Ide.Editor.Highlighting.EditorTheme)this.styleStore.GetValue (selectedIter, 1);
			
			string fileName = sheme.FileName;

			if (fileName != null && fileName.StartsWith (MonoDevelop.Ide.Editor.TextEditorDisplayBinding.SyntaxModePath, StringComparison.Ordinal)) {
				SyntaxHighlightingService.Remove (sheme);
				File.Delete (fileName);
				ShowStyles ();
			}
		}

		void AddColorScheme (object sender, EventArgs args)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Import Color Theme"), MonoDevelop.Components.FileChooserAction.Open) {
				TransientFor = this.Toplevel as Gtk.Window,
			};

			dialog.AddFilter (GettextCatalog.GetString ("Color themes (Visual Studio, Xamarin Studio, TextMate) "), "*.json", "*.vssettings", "*.tmTheme");
			if (!dialog.Run ())
				return;

			var fileName = dialog.SelectedFile.FileName;
			var filePath = dialog.SelectedFile.FullPath;
			string newFilePath = TextEditorDisplayBinding.SyntaxModePath.Combine (fileName);

			if (!SyntaxHighlightingService.IsValidTheme (filePath)) {
				MessageService.ShowError (GettextCatalog.GetString ("Could not import color theme."));
				return;
			}

			bool success = true;
			try {
				if (File.Exists (newFilePath)) {
					var answer = MessageService.AskQuestion (
						GettextCatalog.GetString (
							"A color theme with the name '{0}' already exists in your theme folder. Would you like to replace it?",
							fileName
						),
						AlertButton.Cancel,
						AlertButton.Replace
					);
					if (answer != AlertButton.Replace)
						return;
					File.Delete (newFilePath);
				}
				File.Copy (filePath, newFilePath);
			} catch (Exception e) {
				success = false;
				LoggingService.LogError ("Can't copy color theme file.", e);
			}
			if (success) {
				SyntaxHighlightingService.LoadStylesAndModesInPath (TextEditorDisplayBinding.SyntaxModePath);
				TextEditorDisplayBinding.LoadCustomStylesAndModes ();
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
				var sheme = ((EditorTheme)this.styleStore.GetValue (selectedIter, 1));
				DefaultSourceEditorOptions.Instance.EditorTheme = schemeName = sheme != null ? sheme.Name : null;
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

		void ButtonOpenFolder_Clicked (object sender, EventArgs e)
		{
			DesktopService.OpenFolder (MonoDevelop.Ide.Editor.TextEditorDisplayBinding.SyntaxModePath);
		}
	}
}
