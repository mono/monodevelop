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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Editor; 

namespace MonoDevelop.SourceEditor.OptionPanels
{
	partial class GeneralOptionsPanel : Gtk.Bin, IOptionsPanel
	{
		public GeneralOptionsPanel()
		{
			this.Build();

			this.comboboxLineEndings.AppendText (GettextCatalog.GetString ("Always ask for conversion"));
			this.comboboxLineEndings.AppendText (GettextCatalog.GetString ("Leave line endings as is"));
			this.comboboxLineEndings.AppendText (GettextCatalog.GetString ("Always convert line endings"));
			this.comboboxLineEndings.Active = (int)DefaultSourceEditorOptions.Instance.LineEndingConversion;

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
		}

		public virtual Control CreatePanelWidget ()
		{
			this.foldingCheckbutton.Active = DefaultSourceEditorOptions.Instance.ShowFoldMargin;
			this.foldregionsCheckbutton.Active = DefaultSourceEditorOptions.Instance.DefaultRegionsFolding;
			this.foldCommentsCheckbutton.Active = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
			//			wordWrapCheckbutton.Active = DefaultSourceEditorOptions.Instance.WrapLines;
			wordWrapCheckbutton.Visible = false;
			antiAliasingCheckbutton.Visible = false;
			GtkLabel15.Visible = false;
			return this;
		}
		
		public virtual void ApplyChanges ()
		{
			DefaultSourceEditorOptions.Instance.DefaultRegionsFolding = this.foldregionsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.DefaultCommentFolding = this.foldCommentsCheckbutton.Active;
			DefaultSourceEditorOptions.Instance.LineEndingConversion = (MonoDevelop.Ide.Editor.LineEndingConversion)this.comboboxLineEndings.Active;
			if (DefaultSourceEditorOptions.Instance.ShowFoldMargin != this.foldingCheckbutton.Active) {
				DefaultSourceEditorOptions.Instance.ShowFoldMargin = this.foldingCheckbutton.Active;
				HighlightingPanel.UpdateActiveDocument ();
			}
//			DefaultSourceEditorOptions.Instance.WrapLines = wordWrapCheckbutton.Active;
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
