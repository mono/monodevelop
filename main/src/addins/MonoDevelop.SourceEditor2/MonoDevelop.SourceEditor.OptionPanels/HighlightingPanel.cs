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

using Gtk;

using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Core;
using MonoDevelop.Components;

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
		
		public override void Destroy ()
		{
			if (styleStore != null) {
				styleStore.Dispose ();
				styleStore = null;
			}
			base.Destroy ();
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
			foreach (string styleName in SyntaxModeService.Styles) {
				Mono.TextEditor.Highlighting.Style style = SyntaxModeService.GetColorStyle (null, styleName);
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
			Mono.TextEditor.Highlighting.Style style = SyntaxModeService.GetColorStyle (this, styleName);
			UrlXmlProvider provider = SyntaxModeService.GetProvider (style) as UrlXmlProvider;
			if (provider != null) {
				if (provider.Url.StartsWith (SourceEditorDisplayBinding.SyntaxModePath)) {
					SyntaxModeService.Remove (style);
					File.Delete (provider.Url);
					SourceEditorDisplayBinding.LoadCustomStylesAndModes ();
					ShowStyles ();
				}
			}
			
		}
		
		void AddColorScheme (object sender, EventArgs args)
		{
			FileSelector fd = new FileSelector ();
			int response = fd.Run ();
			if (response == (int)ResponseType.Ok) {
				if (SyntaxModeService.IsValidStyle (fd.Filename)) {
					string newFileName = System.IO.Path.Combine (SourceEditorDisplayBinding.SyntaxModePath, System.IO.Path.GetFileName (fd.Filename));
					bool success = true;
					try {
						File.Copy (fd.Filename, newFileName);
					} catch (Exception e) {
						success = false;
						LoggingService.LogError ("Can't copy syntax mode file.", e);
					}
					if (success) {
						SourceEditorDisplayBinding.LoadCustomStylesAndModes ();
						ShowStyles ();
					}
				}
			}
			fd.Destroy ();
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
