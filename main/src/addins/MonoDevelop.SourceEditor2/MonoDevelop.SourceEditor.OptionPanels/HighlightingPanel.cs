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
		ListStore styleStore = new ListStore (typeof (string), typeof (string));
		
		public HighlightingPanel()
		{
			this.Build();
			styleTreeview.AppendColumn ("", new CellRendererText (), "markup", 0);
			styleTreeview.Model = styleStore;
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
			return String.Format ("<b>{0}</b> - {1}", name, description);
		}

		public virtual Gtk.Widget CreatePanelWidget ()
		{
			this.addButton.Clicked    += AddColorScheme;
			this.removeButton.Clicked += RemoveColorScheme;
			this.enableHighlightingCheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting;
			this.enableSemanticHighlightingCheckbutton.Active = DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting;
			this.enableHighlightingCheckbutton.Toggled += EnableHighlightingCheckbuttonToggled;
			EnableHighlightingCheckbuttonToggled (this, EventArgs.Empty);
			ShowStyles ();
			return this;
		}
		
		void ShowStyles ()
		{
			styleStore.Clear ();
			TreeIter selectedIter = styleStore.AppendValues (GetMarkup (GettextCatalog.GetString ("Default"), GettextCatalog.GetString ("The default color scheme.")), "Default");
			foreach (string styleName in Mono.TextEditor.Highlighting.SyntaxModeService.Styles) {
				Mono.TextEditor.Highlighting.Style style = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (null, styleName);
				TreeIter iter = styleStore.AppendValues (GetMarkup (GettextCatalog.GetString (style.Name), GettextCatalog.GetString (style.Description)), style.Name);
				if (style.Name == DefaultSourceEditorOptions.Instance.ColorScheme)
					selectedIter = iter;
			}
			styleTreeview.Selection.SelectIter (selectedIter); 
		}
		
		void RemoveColorScheme (object sender, EventArgs args)
		{
			string styleName = null;
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) 
				styleName = (string)this.styleStore.GetValue (selectedIter, 1);
			var style = Mono.TextEditor.Highlighting.SyntaxModeService.GetColorStyle (this.Style, styleName);
			UrlXmlProvider provider = Mono.TextEditor.Highlighting.SyntaxModeService.GetProvider (style) as UrlXmlProvider;
			if (provider != null) {
				if (provider.Url.StartsWith (SourceEditorDisplayBinding.SyntaxModePath)) {
					Mono.TextEditor.Highlighting.SyntaxModeService.Remove (style);
					File.Delete (provider.Url);
					SourceEditorDisplayBinding.LoadCustomStylesAndModes ();
					ShowStyles ();
				}
			}
			
		}
		
		void AddColorScheme (object sender, EventArgs args)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Application to Debug"), Gtk.FileChooserAction.Open) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (null, "*.xml");
			if (!dialog.Run ())
				return;
			
			System.Collections.Generic.List<System.Xml.Schema.ValidationEventArgs> validationResult;
			try {
				validationResult = Mono.TextEditor.Highlighting.SyntaxModeService.ValidateStyleFile (dialog.SelectedFile);
			} catch (Exception) {
				MessageService.ShowError (GettextCatalog.GetString ("Validation of style file failed."));
				return;
			}
			if (validationResult.Count == 0) {
				string newFileName = SourceEditorDisplayBinding.SyntaxModePath.Combine (dialog.SelectedFile.FileName);
				if (!newFileName.EndsWith ("Style.xml"))
					newFileName = SourceEditorDisplayBinding.SyntaxModePath.Combine (dialog.SelectedFile.FileNameWithoutExtension + "Style.xml");
				bool success = true;
				try {
					File.Copy (dialog.SelectedFile, newFileName);
				} catch (Exception e) {
					success = false;
					LoggingService.LogError ("Can't copy syntax mode file.", e);
				}
				if (success) {
					SourceEditorDisplayBinding.LoadCustomStylesAndModes ();
					ShowStyles ();
				}
			} else {
				StringBuilder errorMessage = new StringBuilder ();
				errorMessage.AppendLine (GettextCatalog.GetString ("Validation of style file failed."));
				int count = 0;
				foreach (System.Xml.Schema.ValidationEventArgs vArg in validationResult) {
					errorMessage.AppendLine (vArg.Message);
					if (count++ > 5) {
						errorMessage.AppendLine ("...");
						break;
					}
				}
				MessageService.ShowError (errorMessage.ToString ());
			}
		}
		
		void EnableHighlightingCheckbuttonToggled (object sender, EventArgs e)
		{
			this.enableSemanticHighlightingCheckbutton.Sensitive = this.enableHighlightingCheckbutton.Active;
		}
		
		public virtual void ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting = this.enableHighlightingCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.EnableSemanticHighlighting = this.enableSemanticHighlightingCheckbutton.Active;
			TreeIter selectedIter;
			if (styleTreeview.Selection.GetSelected (out selectedIter)) {
				DefaultSourceEditorOptions.Instance.ColorScheme = (string)this.styleStore.GetValue (selectedIter, 1);
			}
		}

		public void Initialize (OptionsDialog dialog, object dataObject)
		{
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
