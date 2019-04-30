// GeneralOptionsPanel.cs
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

using Microsoft.VisualStudio.Text.Editor;

using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide;
using MonoDevelop.Components.Extensions;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	partial class GeneralOptionsPanel : Gtk.Bin, IOptionsPanel
	{
		readonly Xwt.CheckBox newEditorCheckBox;
		readonly Xwt.CheckBox wordWrapCheckBox;
		readonly Xwt.CheckBox wordWrapVisualGlyphsCheckBox;

		public GeneralOptionsPanel()
		{
			this.Build();

			this.comboboxLineEndings.AppendText (GettextCatalog.GetString ("Always ask for conversion"));
			this.comboboxLineEndings.AppendText (GettextCatalog.GetString ("Leave line endings as is"));
			this.comboboxLineEndings.AppendText (GettextCatalog.GetString ("Always convert line endings"));
			this.comboboxLineEndings.Active = (int)DefaultSourceEditorOptions.Instance.LineEndingConversion;

			var newEditorOptionsBox = new Xwt.VBox ();

			var newEditorLearnMoreLink = new Xwt.LinkLabel {
				MarginBottom = 6,
				MarginTop = 6,
				Text = GettextCatalog.GetString ("Learn more about the New Editor"),
				Uri = new Uri ("https://aka.ms/vs/mac/editor/learn-more")
			};
			newEditorOptionsBox.PackStart (newEditorLearnMoreLink);

			newEditorCheckBox = new Xwt.CheckBox (GettextCatalog.GetString ("Open C# files in the New Editor"));
			newEditorCheckBox.Active = DefaultSourceEditorOptions.Instance.EnableNewEditor;
			newEditorCheckBox.Toggled += HandleNewEditorOptionToggled;
			newEditorOptionsBox.PackStart (newEditorCheckBox);

			wordWrapCheckBox = new Xwt.CheckBox (GettextCatalog.GetString ("_Word wrap"));
			wordWrapCheckBox.MarginLeft = 18;
			wordWrapCheckBox.Active = DefaultSourceEditorOptions.Instance.WordWrapStyle.HasFlag (WordWrapStyles.WordWrap);
			wordWrapCheckBox.Toggled += HandleNewEditorOptionToggled;
			newEditorOptionsBox.PackStart (wordWrapCheckBox);

			wordWrapVisualGlyphsCheckBox = new Xwt.CheckBox (GettextCatalog.GetString ("Show visible glyphs for word wrap"));
			wordWrapVisualGlyphsCheckBox.MarginLeft = 36;
			wordWrapVisualGlyphsCheckBox.Active = DefaultSourceEditorOptions.Instance.WordWrapStyle.HasFlag (WordWrapStyles.VisibleGlyphs);
			wordWrapVisualGlyphsCheckBox.Toggled += HandleNewEditorOptionToggled;
			newEditorOptionsBox.PackStart (wordWrapVisualGlyphsCheckBox);

			if (Xwt.Toolkit.CurrentEngine.Type == Xwt.ToolkitType.Gtk)
				experimentalSection.PackStart ((Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget (newEditorOptionsBox), false, false, 0);
			else
				LoggingService.LogError ("GeneralOptionsPanel: Xwt.Toolkit.CurrentEngine.Type != Xwt.ToolkitType.Gtk - currently unsupported");

			HandleNewEditorOptionToggled (this, EventArgs.Empty);

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			comboboxLineEndings.SetCommonAccessibilityAttributes ("SourceEditorGeneral.lineEndings", label1,
			                                                      GettextCatalog.GetString ("Select how to handle line ending conversions"));
			foldingCheckbutton.SetCommonAccessibilityAttributes ("SourceEditorGeneral.folding", "",
			                                                     GettextCatalog.GetString ("Check to enable line folding"));
			foldregionsCheckbutton.SetCommonAccessibilityAttributes ("SourceEditorGeneral.regions", "",
			                                                         GettextCatalog.GetString ("Check to fold regions by default"));
			foldCommentsCheckbutton.SetCommonAccessibilityAttributes ("SourceEditorGeneral.commens", "",
			                                                          GettextCatalog.GetString ("Check to fold comments by default"));
			newEditorCheckBox.SetCommonAccessibilityAttributes ("SourceEditorGeneral.newEditor", "",
			                                                    GettextCatalog.GetString ("Check to enable experimental new editor"));
			wordWrapCheckBox.SetCommonAccessibilityAttributes ("SourceEditorGeneral.newEditor.wordWrap", "",
			                                                   GettextCatalog.GetString ("Check to enable word wrap in the experimental new editor"));
			wordWrapVisualGlyphsCheckBox.SetCommonAccessibilityAttributes ("SourceEditorGeneral.newEditor.wordWrap.enableVisualGlyphs", "",
			                                                               GettextCatalog.GetString ("Check to enable visual word wrap glyphs in the experimental new editor"));
		}

		public virtual Control CreatePanelWidget ()
		{
			this.foldingCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowFoldMargin;
			this.foldregionsCheckbutton.Active = DefaultSourceEditorOptions.Instance.DefaultRegionsFolding;
			this.foldCommentsCheckbutton.Active = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;

			antiAliasingCheckbutton.Visible = false;
			GtkLabel15.Visible = false;

			return this;
		}
		
		public virtual void ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.DefaultRegionsFolding = this.foldregionsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.DefaultCommentFolding = this.foldCommentsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.LineEndingConversion = (LineEndingConversion)this.comboboxLineEndings.Active;

			if (DefaultSourceEditorOptions.Instance.ShowFoldMargin != this.foldingCheckbutton.Active) {
				DefaultSourceEditorOptions.Instance.ShowFoldMargin = this.foldingCheckbutton.Active;
				HighlightingPanel.UpdateActiveDocument ();
			}

			DefaultSourceEditorOptions.Instance.EnableNewEditor = this.newEditorCheckBox.Active;
		}

		void HandleNewEditorOptionToggled (object sender, EventArgs e)
		{
			wordWrapCheckBox.Sensitive = newEditorCheckBox.Active;
			wordWrapVisualGlyphsCheckBox.Sensitive = newEditorCheckBox.Active && wordWrapCheckBox.Active;

			var wrap = DefaultSourceEditorOptions.Instance.WordWrapStyle;

			if (wordWrapCheckBox.Active)
				wrap |= WordWrapStyles.WordWrap;
			else
				wrap &= ~WordWrapStyles.WordWrap;

			if (wordWrapVisualGlyphsCheckBox.Active)
				wrap |= WordWrapStyles.VisibleGlyphs;
			else
				wrap &= ~WordWrapStyles.VisibleGlyphs;

			DefaultSourceEditorOptions.Instance.WordWrapStyle = wrap;
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
