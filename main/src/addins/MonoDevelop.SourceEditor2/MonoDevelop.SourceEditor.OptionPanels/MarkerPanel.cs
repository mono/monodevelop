// MarkerPanel.cs
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	
	partial class MarkerPanel : Gtk.Bin, IOptionsPanel
	{

		bool showLineNumbers;

		bool highlightMatchingBracket;

		bool highlightCurrentLine;

		bool showRuler;

		bool enableAnimation;

		bool enableHighlightUsages;

		bool drawIndentMarkers;

		ShowWhitespaces showWhitespaces;

		bool enableQuickDiff;

		bool showProcedureLineSeparators;

		IncludeWhitespaces includeWhitespaces;

		public MarkerPanel()
		{
			this.Build();
			showLineNumbers = DefaultSourceEditorOptions.Instance.ShowLineNumberMargin;
			highlightMatchingBracket = DefaultSourceEditorOptions.Instance.HighlightMatchingBracket;
			highlightCurrentLine = DefaultSourceEditorOptions.Instance.HighlightCaretLine;
			showRuler = DefaultSourceEditorOptions.Instance.ShowRuler;
			enableAnimation = DefaultSourceEditorOptions.Instance.EnableAnimations;
			enableHighlightUsages = DefaultSourceEditorOptions.Instance.EnableHighlightUsages;
			drawIndentMarkers = DefaultSourceEditorOptions.Instance.DrawIndentationMarkers;
			showWhitespaces = DefaultSourceEditorOptions.Instance.ShowWhitespaces;
			includeWhitespaces = DefaultSourceEditorOptions.Instance.IncludeWhitespaces;
			enableQuickDiff = DefaultSourceEditorOptions.Instance.EnableQuickDiff;
			showProcedureLineSeparators = DefaultSourceEditorOptions.Instance.ShowProcedureLineSeparators;
			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			checkbuttonTabs.SetCommonAccessibilityAttributes ("MarkerPanel.tabs", "",
			                                                  GettextCatalog.GetString ("Check to show tabs when showing invisible characters"));
			checkbuttonSpaces.SetCommonAccessibilityAttributes ("MarkerPanel.spaces", "",
			                                                    GettextCatalog.GetString ("Check to show spaces when showing invisible characters"));
			checkbuttonLineEndings.SetCommonAccessibilityAttributes ("MarkerPanel.lineEndings", "",
			                                                         GettextCatalog.GetString ("Check to show line endings when showing invisible characters"));
			showRulerCheckbutton.SetCommonAccessibilityAttributes ("MarkerPanel.showRuler", "",
			                                                       GettextCatalog.GetString ("Check to show the column ruler"));
			showLineNumbersCheckbutton.SetCommonAccessibilityAttributes ("MarkerPanel.showLineNumbers", "",
			                                                             GettextCatalog.GetString ("Check to show line numbers"));
			highlightCurrentLineCheckbutton.SetCommonAccessibilityAttributes ("MarkerPanel.highlightCurrentLine", "",
			                                                                  GettextCatalog.GetString ("Check to highlight to current line"));
			highlightMatchingBracketCheckbutton.SetCommonAccessibilityAttributes ("MarkerPanel.highlightMatching", "",
			                                                                      GettextCatalog.GetString ("Check to highlight the matching bracket"));
			enableHighlightUsagesCheckbutton.SetCommonAccessibilityAttributes ("MarkerPanel.highlightUsages", "",
			                                                                   GettextCatalog.GetString ("Check to highlight identifier references"));
			enableAnimationCheckbutton1.SetCommonAccessibilityAttributes ("MarkerPanel.enableAnim", "",
			                                                              GettextCatalog.GetString ("Check to enable animations in the text editor"));
			enableQuickDiffCheckbutton.SetCommonAccessibilityAttributes ("MarkerPanel.quickDiff", "",
			                                                             GettextCatalog.GetString ("Check to highlight changed line"));
			drawIndentMarkersCheckbutton.SetCommonAccessibilityAttributes ("MarkerPanel.drawIndent", "",
			                                                               GettextCatalog.GetString ("Check to draw indentation markers"));

			showWhitespacesCombobox.SetCommonAccessibilityAttributes ("MarkerPanel.showWhitespace", label1,
			                                                          GettextCatalog.GetString ("Select when to show invisible characters"));
		}
		
		public virtual Control CreatePanelWidget ()
		{
			this.showLineNumbersCheckbutton.Active = showLineNumbers = DefaultSourceEditorOptions.Instance.ShowLineNumberMargin;
			this.showLineNumbersCheckbutton.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.ShowLineNumberMargin = this.showLineNumbersCheckbutton.Active;
			};

			this.highlightMatchingBracketCheckbutton.Active = highlightMatchingBracket = DefaultSourceEditorOptions.Instance.HighlightMatchingBracket;
			this.highlightMatchingBracketCheckbutton.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.HighlightMatchingBracket = this.highlightMatchingBracketCheckbutton.Active;
			};

			this.highlightCurrentLineCheckbutton.Active = highlightCurrentLine = DefaultSourceEditorOptions.Instance.HighlightCaretLine;
			this.highlightCurrentLineCheckbutton.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.HighlightCaretLine = this.highlightCurrentLineCheckbutton.Active;
			};

			this.showRulerCheckbutton.Active = showRuler = DefaultSourceEditorOptions.Instance.ShowRuler;
			this.showRulerCheckbutton.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.ShowRuler = this.showRulerCheckbutton.Active;
			};

			this.enableAnimationCheckbutton1.Active = enableAnimation = DefaultSourceEditorOptions.Instance.EnableAnimations;
			this.enableAnimationCheckbutton1.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.EnableAnimations = this.enableAnimationCheckbutton1.Active;
			};

			this.enableHighlightUsagesCheckbutton.Active = enableHighlightUsages = DefaultSourceEditorOptions.Instance.EnableHighlightUsages;
			this.enableHighlightUsagesCheckbutton.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.EnableHighlightUsages = this.enableHighlightUsagesCheckbutton.Active;
			};

			this.drawIndentMarkersCheckbutton.Active = drawIndentMarkers = DefaultSourceEditorOptions.Instance.DrawIndentationMarkers;
			this.drawIndentMarkersCheckbutton.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.DrawIndentationMarkers = this.drawIndentMarkersCheckbutton.Active;
			};
			this.showWhitespacesCombobox.AppendText (GettextCatalog.GetString ("Never"));
			this.showWhitespacesCombobox.AppendText (GettextCatalog.GetString ("Selection"));
			this.showWhitespacesCombobox.AppendText (GettextCatalog.GetString ("Always"));
			this.showWhitespacesCombobox.Active = (int)(showWhitespaces = DefaultSourceEditorOptions.Instance.ShowWhitespaces);
			this.showWhitespacesCombobox.Changed += delegate {
				DefaultSourceEditorOptions.Instance.ShowWhitespaces = (ShowWhitespaces) this.showWhitespacesCombobox.Active;
			};
			this.checkbuttonSpaces.Active = DefaultSourceEditorOptions.Instance.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Space);
			this.checkbuttonSpaces.Toggled += CheckbuttonSpaces_Toggled;

			this.checkbuttonTabs.Active = DefaultSourceEditorOptions.Instance.IncludeWhitespaces.HasFlag (IncludeWhitespaces.Tab);
			this.checkbuttonTabs.Toggled += CheckbuttonSpaces_Toggled;

			this.checkbuttonLineEndings.Active = DefaultSourceEditorOptions.Instance.IncludeWhitespaces.HasFlag (IncludeWhitespaces.LineEndings);
			this.checkbuttonLineEndings.Toggled += CheckbuttonSpaces_Toggled;

			includeWhitespaces = DefaultSourceEditorOptions.Instance.IncludeWhitespaces;
			this.enableQuickDiffCheckbutton.Active = enableQuickDiff = DefaultSourceEditorOptions.Instance.EnableQuickDiff;
			this.enableQuickDiffCheckbutton.Toggled += delegate {
				DefaultSourceEditorOptions.Instance.EnableQuickDiff = this.enableQuickDiffCheckbutton.Active;
			};

			this.showProcedureLineSeparatorsCheckbutton.Active = showProcedureLineSeparators = DefaultSourceEditorOptions.Instance.ShowProcedureLineSeparators;

			return this;
		}

		void CheckbuttonSpaces_Toggled (object sender, EventArgs e)
		{
			var include = IncludeWhitespaces.None;
			if (checkbuttonSpaces.Active)
				include |= IncludeWhitespaces.Space;
			if (checkbuttonTabs.Active)
				include |= IncludeWhitespaces.Tab;
			if (checkbuttonLineEndings.Active)
				include |= IncludeWhitespaces.LineEndings;
			DefaultSourceEditorOptions.Instance.IncludeWhitespaces = include;
		}

		public virtual void ApplyChanges ()
		{
			showLineNumbers = this.showLineNumbersCheckbutton.Active;
			highlightMatchingBracket = this.highlightMatchingBracketCheckbutton.Active;
			highlightCurrentLine = this.highlightCurrentLineCheckbutton.Active;
			showRuler = this.showRulerCheckbutton.Active;
			enableAnimation = this.enableAnimationCheckbutton1.Active;
			enableHighlightUsages = this.enableHighlightUsagesCheckbutton.Active;
			drawIndentMarkers = this.drawIndentMarkersCheckbutton.Active;
			showWhitespaces = (ShowWhitespaces) this.showWhitespacesCombobox.Active;
			enableQuickDiff = this.enableQuickDiffCheckbutton.Active;
			showProcedureLineSeparators = this.showProcedureLineSeparatorsCheckbutton.Active;

			var include = IncludeWhitespaces.None;
			if (checkbuttonSpaces.Active)
				include |= IncludeWhitespaces.Space;
			if (checkbuttonTabs.Active)
				include |= IncludeWhitespaces.Tab;
			if (checkbuttonLineEndings.Active)
				include |= IncludeWhitespaces.LineEndings;
			includeWhitespaces = include;
		}

		protected override void OnDestroyed ()
		{
			DefaultSourceEditorOptions.Instance.ShowLineNumberMargin = showLineNumbers;
			DefaultSourceEditorOptions.Instance.HighlightMatchingBracket = highlightMatchingBracket;
			DefaultSourceEditorOptions.Instance.HighlightCaretLine = highlightCurrentLine;
			DefaultSourceEditorOptions.Instance.ShowRuler = showRuler;
			DefaultSourceEditorOptions.Instance.EnableAnimations = enableAnimation;
			DefaultSourceEditorOptions.Instance.EnableHighlightUsages = enableHighlightUsages;
			DefaultSourceEditorOptions.Instance.DrawIndentationMarkers = drawIndentMarkers;
			DefaultSourceEditorOptions.Instance.ShowWhitespaces = showWhitespaces;
			DefaultSourceEditorOptions.Instance.EnableQuickDiff = enableQuickDiff;
			DefaultSourceEditorOptions.Instance.IncludeWhitespaces = includeWhitespaces;
			DefaultSourceEditorOptions.Instance.ShowProcedureLineSeparators = showProcedureLineSeparators;
			base.OnDestroyed ();
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
