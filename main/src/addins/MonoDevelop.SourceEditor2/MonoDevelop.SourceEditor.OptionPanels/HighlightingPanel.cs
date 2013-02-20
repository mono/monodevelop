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

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public partial class HighlightingPanel : Gtk.Bin, IOptionsPanel
	{
		ListStore styleStore = new ListStore (typeof (string), typeof (Mono.TextEditor.Highlighting.ColorScheme));
		
		public HighlightingPanel ()
		{
			this.Build ();
			styleTreeview.AppendColumn ("", new CellRendererText (), "markup", 0);
			styleTreeview.Model = styleStore;
			// ensure that custom styles are loaded.
			new SourceEditorDisplayBinding ();
		}
		
		protected override void OnDestroyed ()
		{
			if (styleStore != null) {
				styleStore.Dispose ();
				styleStore = null;
			}
			base.OnDestroyed ();
		}

		string GetMarkup (string name, string description)
		{
			return String.Format ("<b>{0}</b> - {1}", GLib.Markup.EscapeText (name), GLib.Markup.EscapeText (description));
		}

		public virtual Gtk.Widget CreatePanelWidget ()
		{
			this.addButton.Clicked += AddColorScheme;
			this.removeButton.Clicked += RemoveColorScheme;
			this.buttonEdit.Clicked += HandleButtonEdithandleClicked;
			this.buttonNew.Clicked += HandleButtonNewClicked;
			this.buttonExport.Clicked += HandleButtonExportClicked;
			this.enableHighlightingCheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting;
			this.enableSemanticHighlightingCheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting;
			this.enableHighlightingCheckbutton.Toggled += EnableHighlightingCheckbuttonToggled;
			this.styleTreeview.Selection.Changed += HandleStyleTreeviewSelectionChanged;
			EnableHighlightingCheckbuttonToggled (this, EventArgs.Empty);
			ShowStyles ();
			HandleStyleTreeviewSelectionChanged (null, null);
			return this;
		}

		void HandleButtonNewClicked (object sender, EventArgs e)
		{
			var newShemeDialog = new NewColorShemeDialog ();
			MessageService.RunCustomDialog (newShemeDialog, dialog);
			newShemeDialog.Destroy ();
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
			this.buttonExport.Sensitive = true;
			string fileName = Mono.TextEditor.Highlighting.SyntaxModeService.GetFileNameForStyle (sheme);
			if (fileName == null)
				return;
			this.removeButton.Sensitive = true;
			this.buttonEdit.Sensitive = true;
		}

		void HandleButtonEdithandleClicked (object sender, EventArgs e)
		{
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) {
				var editor = new ColorShemeEditor (this);
				editor.SetSheme ((Mono.TextEditor.Highlighting.ColorScheme)this.styleStore.GetValue (selectedIter, 1));
				MessageService.RunCustomDialog (editor, dialog);
				editor.Destroy ();
			}
		}
		
		Mono.TextEditor.Highlighting.ColorScheme LoadStyle (string styleName, bool showException = true)
		{
			try {
				return Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (styleName);
			} catch (Exception e) {
				if (showException)
					MessageService.ShowError ("Error while importing color style " + styleName, (e.InnerException ?? e).Message);
				return Mono.TextEditor.Highlighting.SyntaxModeService.DefaultColorStyle;
			}
		
		}
		
		internal void ShowStyles ()
		{
			styleStore.Clear ();
			TreeIter selectedIter = styleStore.AppendValues (GetMarkup (GettextCatalog.GetString ("Default"), GettextCatalog.GetString ("The default color scheme.")), LoadStyle ("Default"));
			foreach (string styleName in Mono.TextEditor.Highlighting.SyntaxModeService.Styles) {
				if (styleName == "Default")
					continue;
				var style = LoadStyle (styleName);
				string name = style.Name ?? "";
				string description = style.Description ?? "";
				// translate only build-in sheme names
				if (string.IsNullOrEmpty (Mono.TextEditor.Highlighting.SyntaxModeService.GetFileNameForStyle (style))) {
					try {
						name = GettextCatalog.GetString (name);
						if (!string.IsNullOrEmpty (description))
							description = GettextCatalog.GetString (description);
					} catch {
					}
				}
				TreeIter iter = styleStore.AppendValues (GetMarkup (name, description), style);
				if (style.Name == DefaultSourceEditorOptions.Instance.ColorScheme)
					selectedIter = iter;
			}
			styleTreeview.Selection.SelectIter (selectedIter); 
		}
		
		void RemoveColorScheme (object sender, EventArgs args)
		{
			TreeIter selectedIter;
			if (!styleTreeview.Selection.GetSelected (out selectedIter)) 
				return;
			var sheme = (Mono.TextEditor.Highlighting.ColorScheme)this.styleStore.GetValue (selectedIter, 1);
			
			string fileName = Mono.TextEditor.Highlighting.SyntaxModeService.GetFileNameForStyle (sheme);
			
			if (fileName != null && fileName.StartsWith (SourceEditorDisplayBinding.SyntaxModePath)) {
				Mono.TextEditor.Highlighting.SyntaxModeService.Remove (sheme);
				File.Delete (fileName);
				ShowStyles ();
			}
		}
		
		void HandleButtonExportClicked (object sender, EventArgs e)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Highlighting Scheme"), Gtk.FileChooserAction.Save) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (null, "*.xml");
			if (!dialog.Run ())
				return;
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) {
				var sheme = (Mono.TextEditor.Highlighting.ColorScheme)this.styleStore.GetValue (selectedIter, 1);
				sheme.Save (dialog.SelectedFile);
			}

		}
		
		void AddColorScheme (object sender, EventArgs args)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Highlighting Scheme"), Gtk.FileChooserAction.Open) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (null, "*.*");
			if (!dialog.Run ())
				return;

			string newFileName = SourceEditorDisplayBinding.SyntaxModePath.Combine (dialog.SelectedFile.FileName);

			bool success = true;
			try {
				File.Copy (dialog.SelectedFile.FullPath, newFileName);
			} catch (Exception e) {
				success = false;
				LoggingService.LogError ("Can't copy syntax mode file.", e);
			}
			if (success) {
				SourceEditorDisplayBinding.LoadCustomStylesAndModes ();
				ShowStyles ();
			}
		}
		
		void EnableHighlightingCheckbuttonToggled (object sender, EventArgs e)
		{
			this.enableSemanticHighlightingCheckbutton.Sensitive = this.enableHighlightingCheckbutton.Active;
		}

		internal static void UpdateActiveDocument ()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				IdeApp.Workbench.ActiveDocument.UpdateParseDocument ();
				IdeApp.Workbench.ActiveDocument.Editor.Parent.TextViewMargin.PurgeLayoutCache ();
				IdeApp.Workbench.ActiveDocument.Editor.Parent.QueueDraw ();
			}
		}
		
		public virtual void ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting = this.enableHighlightingCheckbutton.Active;
			if (DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting != this.enableSemanticHighlightingCheckbutton.Active) {
				DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting = this.enableSemanticHighlightingCheckbutton.Active;
				UpdateActiveDocument ();
			}
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) {
				ColorScheme sheme = ((Mono.TextEditor.Highlighting.ColorScheme)this.styleStore.GetValue (selectedIter, 1));
				DefaultSourceEditorOptions.Instance.ColorScheme = sheme != null ? sheme.Name : null;
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
