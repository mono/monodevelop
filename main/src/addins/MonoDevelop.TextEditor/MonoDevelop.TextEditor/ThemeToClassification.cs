//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Core;

namespace MonoDevelop.TextEditor
{
	abstract class ThemeToClassification : IDisposable
	{
		readonly IEditorFormatMapService editorFormatMapService;

		protected ThemeToClassification (IEditorFormatMapService editorFormatMapService)
		{
			this.editorFormatMapService = editorFormatMapService;
			Ide.Editor.DefaultSourceEditorOptions.Instance.Changed += UpdateEditorFormatMap;
			UpdateEditorFormatMap (null, null);
		}

		public void Dispose ()
		{
			Ide.Editor.DefaultSourceEditorOptions.Instance.Changed -= UpdateEditorFormatMap;
		}

		static readonly List<(string EditorFormatName, string MDThemeSettingName)> mappings = new List<(string, string)> {
			("preprocessor text", "Preprocessor region name"),
			("punctuation", "punctuation"),
			("string - verbatim", "String Verbatim"),
			("property name","User Property"),
			("field name","User Field"),
			("event name","User Event"),
			("enum member name","User Enum Member"),
			("class name", "User Types"),
			("method name", "User Method"),
			("constant name", "User Constant"),
			("parameter name", "User Parameter"),
			("delegate name", "User Types(Delegates)"),
			("enum name", "User Types(Enums)"),
			("interface name", "User Types(Interfaces)"),
			("module name", ""),
			("local name", "User Variable"),
			("struct name", "User Types(Value types)"),
			("type parameter name", "User Types(Type parameters)"),
			("xml doc comment - attribute name", "Comment XML Doc Comment"),
			("xml doc comment - attribute quotes", "Comment XML Doc Comment"),
			("xml doc comment - attribute value", "Comment XML Doc Comment"),
			("xml doc comment - cdata section", "Comment XML Doc Comment"),
			("xml doc comment - comment", "Comment XML Doc Comment"),
			("xml doc comment - delimiter", "Comment XML Doc Comment"),
			("xml doc comment - entity reference", "Comment XML Doc Comment"),
			("xml doc comment - name", "Comment XML Doc Comment"),
			("xml doc comment - processing instruction", "Comment XML Doc Comment"),
			("xml doc comment - text", "Comment XML Doc Comment"),
			("xml literal - attribute name", "Xml Attribute"),
			("xml literal - attribute quotes", "Xml Attribute Quotes"),
			("xml literal - attribute value", "Xml Attribute Value"),
			("xml literal - cdata section", "Xml CData Section"),
			("xml literal - comment", "Xml Comment"),
			("xml literal - delimiter", "Xml Delimiter"),
			("xml literal - embedded expression", ""),
			("xml literal - entity reference", ""),
			("xml literal - name", "Xml Name"),
			("xml literal - processing instruction", ""),
			("xml literal - text", "Xml Text"),
			("axml - attribute name", "Xml Attribute"),
			("axml - attribute quotes", "Xml Attribute Quotes"),
			("axml - attribute value", "Xml Attribute Value"),
			("axml - cdata section", "Xml CData Section"),
			("axml - comment", "Xml Comment"),
			("axml - delimiter", "Xml Delimiter"),
			("axml - embedded expression", ""),
			("axml - entity reference", ""),
			("axml - name", "Xml Name"),
			("axml - processing instruction", ""),
			("axml - text", "Xml Text"),
			("axml - resource url", "Xml Name"),
			("XAML Attribute", "Xml Attribute"),
			("XAML Attribute Quotes", "Xml Attribute Quotes"),
			("XAML Attribute Value", "Xml Attribute Value"),
			("XAML CData Section", "Xml CData Section"),
			("XAML Comment", "Xml Comment"),
			("XAML Delimiter", "Xml Delimiter"),
			("XAML Keyword", "Xml Name"),
			("XAML Markup Extension Class", "Xml Name"),
			("XAML Markup Extension Parameter Name", "Xml Name"),
			("XAML Markup Extension Parameter Value", "Xml Name"),
			("XAML Name", "Xml Name"),
			("XAML Processing Instruction", ""),
			("XAML Text", "Xml Text"),
			("Peek Background", ""),
			("Peek Background Unfocused", ""),
			("Peek History Selected", ""),
			("Peek History Hovered", ""),
			("Peek Focused Border", ""),
			("Peek Label Text", ""),
			("Peek Highlighted Text", ""),
			("Peek Highlighted Text Unfocused", ""),
			("Comment", "Comment"),
			("Excluded Code", "Excluded Code"),
			("Keyword", "Keyword"),
			("Preprocessor Keyword", "Preprocessor"),
			("Operator", "Keyword(Operator)"),
			("Literal", "Number"),
			("Markup Attribute", ""),
			("Markup Attribute Value", ""),
			("Markup Node", ""),
			("String", "String"),
			("Type", "User Types"),
			("Number", "Number"),
			("SymbolDefinitionClassificationFormat", ""),
			("SymbolReferenceClassificationFormat", ""),
			("Natural Language Priority", ""),
			("Formal Language Priority", ""),
			("outlining.collapsehintadornment", ""),
			("outlining.verticalrule", ""),
			("SigHelpDocumentationFormat", ""),
			("CurrentParameterFormat", ""),
			("bookmark", ""),
			("breakpoint", ""),
			("currentstatement", ""),
			("returnstatement", ""),
			("stepbackcurrentstatement", ""),
			("vivid", ""),
			("blue", ""),
			("remove line", ""),
			("add line", ""),
			("remove word", ""),
			("add word", ""),
			("bracehighlight", ""),
			("BraceCompletionClosingBrace", ""),
			("outlining.collapsehintadornment.background", "" )
		};

		void UpdateEditorFormatMap (object sender, EventArgs args)
		{
			UpdateEditorFormatMap ("text");
			UpdateEditorFormatMap ("tooltip");
		}

		void UpdateEditorFormatMap (string appearanceCategory)
		{
			var editorFormat = editorFormatMapService.GetEditorFormatMap (appearanceCategory);
			editorFormat.BeginBatchUpdate ();
			var theme = SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme.Value);
			var settingsMap = new Dictionary<string, ThemeSetting> ();
			var defaultSettings = theme.Settings[0];
			for (var i = 1; i < theme.Settings.Count; i++) {
				var setting = theme.Settings[i];
				settingsMap[setting.Name] = setting;
			}
			CreatePlainText (editorFormat, defaultSettings, appearanceCategory);
			CreateLineNumberAndSuggestion (editorFormat, defaultSettings);
			CreateOutlining (editorFormat, defaultSettings);
			CreateCaret (editorFormat, defaultSettings);
			CreateSelection (editorFormat, defaultSettings);
			CreateResourceDictionary (editorFormat, defaultSettings, "text", EditorThemeColors.Foreground, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "Identifier", EditorThemeColors.Foreground, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "TextView Background", EditorThemeColors.Background);
			CreateResourceDictionary (editorFormat, defaultSettings, "MarkerFormatDefinition/FindHighlight", EditorThemeColors.FindHighlight);
			CreateResourceDictionary (editorFormat, defaultSettings, "MarkerFormatDefinition/HighlightedReference", EditorThemeColors.UsagesRectangle);
			CreateResourceDictionary (editorFormat, defaultSettings, "MarkerFormatDefinition/HighlightedDefinition", EditorThemeColors.ChangingUsagesRectangle);
			CreateResourceDictionary (editorFormat, defaultSettings, "MarkerFormatDefinition/HighlightedWrittenReference", EditorThemeColors.ChangingUsagesRectangle);
			CreateResourceDictionary (editorFormat, defaultSettings, "brace matching", EditorThemeColors.BracketsForeground);
			CreateResourceDictionary (editorFormat, defaultSettings, "syntax error", EditorThemeColors.UnderlineError, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "compiler error", EditorThemeColors.UnderlineError, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "other error", EditorThemeColors.UnderlineError, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "compiler warning", EditorThemeColors.UnderlineWarning, EditorFormatDefinition.ForegroundColorId);
			// This is commented out because VS Windows also doesn't have color set for suggestion underline
			// which causes squiglly underlinings to be drawn where users don't expect it.
			//CreateResourceDictionary (editorFormat, defaultSettings, "suggestion", EditorThemeColors.UnderlineSuggestion, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "hinted suggestion", EditorThemeColors.UnderlineSuggestion, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "breakpoint", EditorThemeColors.BreakpointMarker);
			CreateResourceDictionary (editorFormat, defaultSettings, "breakpoint-disabled", EditorThemeColors.BreakpointMarkerDisabled);
			CreateResourceDictionary (editorFormat, defaultSettings, "breakpoint-invalid", EditorThemeColors.BreakpointMarkerInvalid);
			CreateResourceDictionary (editorFormat, defaultSettings, "currentstatement", EditorThemeColors.DebuggerCurrentLineMarker);
			CreateResourceDictionary (editorFormat, defaultSettings, "returnstatement", EditorThemeColors.DebuggerStackLineMarker);
			CreateResourceDictionary (editorFormat, defaultSettings, "Indicator Margin", EditorThemeColors.IndicatorMargin);
			CreateResourceDictionary (editorFormat, defaultSettings, "CurrentLineActiveFormat", EditorThemeColors.LineHighlight, EditorFormatDefinition.ForegroundColorId);
			// We need theme support for foreground (border) vs background color for the current line highlighter.
			// Until we have that, set the background brush explicitly since that's what the old editor did, and
			// all the themes expect that. Fixes https://devdiv.visualstudio.com/DevDiv/_workitems/edit/804158
			CreateResourceDictionary (editorFormat, defaultSettings, "CurrentLineActiveFormat", EditorThemeColors.LineHighlight, EditorFormatDefinition.BackgroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "Block Structure Adornments", EditorThemeColors.IndentationGuide);
			CreateRename (editorFormat, defaultSettings);
			foreach (var mapping in mappings) {
				if (settingsMap.TryGetValue (mapping.MDThemeSettingName, out var setting))
					CreateResourceDictionary (editorFormat, mapping.EditorFormatName, setting);
			}
			editorFormat.EndBatchUpdate ();
		}

		private void CreateRename (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			if (defaultSettings.TryGetColor (EditorThemeColors.PrimaryTemplateHighlighted2, out var selectionColor)) {
				var resourceDictionary = editorFormat.GetProperties ("RoslynRenameFieldBackgroundAndBorderTag");
				var (r, g, b, a) = selectionColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.BackgroundColorId] = c;
				resourceDictionary [MarkerFormatDefinition.BorderId] = new Pen (new SolidColorBrush (c), 2);
				editorFormat.SetProperties ("RoslynRenameFieldBackgroundAndBorderTag", resourceDictionary);
			}
		}

		void CreateSelection (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			if (defaultSettings.TryGetColor (EditorThemeColors.Selection, out var selectionColor)) {
				var (r, g, b, a) = selectionColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				var resourceDictionary = editorFormat.GetProperties ("Selected Text");
				resourceDictionary [EditorFormatDefinition.BackgroundBrushId] = new SolidColorBrush (c);
				editorFormat.SetProperties ("Selected Text", resourceDictionary);
			}
			if (defaultSettings.TryGetColor (EditorThemeColors.InactiveSelection, out var inactiveSelectionColor)) {
				var (r, g, b, a) = inactiveSelectionColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				var resourceDictionary = editorFormat.GetProperties ("Inactive Selected Text");
				resourceDictionary [EditorFormatDefinition.BackgroundBrushId] = new SolidColorBrush (c);
				editorFormat.SetProperties ("Inactive Selected Text", resourceDictionary);
			}
		}

		void CreateCaret (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			if (defaultSettings.TryGetColor (EditorThemeColors.Caret, out var primaryColor)) {
				var (r, g, b, a) = primaryColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				var resourceDictionary = editorFormat.GetProperties ("Caret (Primary)");
				resourceDictionary [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
				editorFormat.SetProperties ("Caret (Primary)", resourceDictionary);
			}
			//TODO: Use different color for secondary caret
			if (defaultSettings.TryGetColor (EditorThemeColors.Caret, out var secondaryColor)) {
				var (r, g, b, a) = secondaryColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				var resourceDictionary = editorFormat.GetProperties ("Caret (Secondary)");
				resourceDictionary [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
				editorFormat.SetProperties ("Caret (Secondary)", resourceDictionary);
			}
		}

		void SetBrushes (IEditorFormatMap editorFormat, ThemeSetting defaultSettings, string name, string foregroundKey, string backgroundKey = null)
		{
			var squareResources = editorFormat.GetProperties (name);
			if (defaultSettings.TryGetColor (foregroundKey, out var squareForeground)) {
				var (r, g, b, a) = squareForeground.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				squareResources[EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
			}
			if (backgroundKey != null && defaultSettings.TryGetColor (backgroundKey, out var squareBackground)) {
				var (r, g, b, a) = squareBackground.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				squareResources[EditorFormatDefinition.BackgroundBrushId] = new SolidColorBrush (c);
			}
			editorFormat.SetProperties (name, squareResources);
		}

		void CreateOutlining (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			// There is EditorThemeColors.FoldCross and EditorThemeColors.FoldCrossBackground
			// but old editor is using ForeGround and FoldLine colors...
			SetBrushes (editorFormat, defaultSettings, "outlining.square", EditorThemeColors.Foreground, EditorThemeColors.FoldLine);
			SetBrushes (editorFormat, defaultSettings, "outlining.collapsehintadornment", EditorThemeColors.LineHighlight, EditorThemeColors.LineHighlight);
			SetBrushes (editorFormat, defaultSettings, "outlining.verticalrule", EditorThemeColors.FoldLine);
			SetBrushes (editorFormat, defaultSettings, "Collapsible Text (Collapsed)", EditorThemeColors.CollapsedText);
		}

		void CreateResourceDictionary (IEditorFormatMap editorFormat, string formatName, ThemeSetting setting)
		{
			var resourceDictionary = editorFormat.GetProperties (formatName);
			HslColor color;
			if (setting.TryGetColor (EditorThemeColors.Foreground, out color)) {
				var (r, g, b, a) = color.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.ForegroundColorId] = c;
				resourceDictionary [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
			}
			if (setting.TryGetColor (EditorThemeColors.Background, out color)) {
				var (r, g, b, a) = color.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.BackgroundColorId] = c;
				resourceDictionary [EditorFormatDefinition.BackgroundBrushId] = new SolidColorBrush (c);
			}

			if (setting.TryGetSetting ("fontStyle", out var style)) {
				resourceDictionary [ClassificationFormatDefinition.IsBoldId] = style.Contains ("bold");
			}
			editorFormat.SetProperties (formatName, resourceDictionary);
		}

		static void CreateResourceDictionary (IEditorFormatMap editorFormat, ThemeSetting defaultSettings, string vsName, string settingName, string key = EditorFormatDefinition.BackgroundColorId, string brushKey = null)
		{
			ResourceDictionary resourceDictionary = editorFormat.GetProperties (vsName);
			if (defaultSettings.TryGetColor (settingName, out var backgroundColor)) {
				var (r, g, b, a) = backgroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [key] = c;

				if (brushKey == null) {
					if (key == EditorFormatDefinition.ForegroundColorId) {
						brushKey = EditorFormatDefinition.ForegroundBrushId;
					} else if (key == EditorFormatDefinition.BackgroundColorId) {
						brushKey = EditorFormatDefinition.BackgroundBrushId;
					}
				}
				if (brushKey != null) {
					resourceDictionary [brushKey] = new SolidColorBrush (c);
				}
			}
			editorFormat.SetProperties (vsName, resourceDictionary);
		}

		static readonly string [] editorFormatDefinitionNamesForLineNumberMarginTheme = { "Line Number", "Suggestion Margin" };

		static void CreateLineNumberAndSuggestion (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			foreach (var definitionName in editorFormatDefinitionNamesForLineNumberMarginTheme) {
				var resourceDictionary = editorFormat.GetProperties (definitionName);
				if (defaultSettings.TryGetColor ("gutterForeground", out var foregroundColor)) {
					var (r, g, b, a) = foregroundColor.ToRgba ();
					var c = Color.FromArgb (a, r, g, b);
					resourceDictionary [EditorFormatDefinition.ForegroundColorId] = c;
					resourceDictionary [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
				}
				if (defaultSettings.TryGetColor ("gutter", out var backgroundColor)) {
					var (r, g, b, a) = backgroundColor.ToRgba ();
					var c = Color.FromArgb (a, r, g, b);
					resourceDictionary [EditorFormatDefinition.BackgroundColorId] = c;
					resourceDictionary [EditorFormatDefinition.BackgroundBrushId] = new SolidColorBrush (c);
				}
				editorFormat.SetProperties (definitionName, resourceDictionary);
			}
		}

		void CreatePlainText (IEditorFormatMap editorFormat, ThemeSetting defaultSettings, string appearanceCategory)
		{
			var resourceDictionary = editorFormat.GetProperties ("Plain Text");
			if (defaultSettings.TryGetColor ("foreground", out var foregroundColor)) {
				var (r, g, b, a) = foregroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.ForegroundColorId] = c;
				resourceDictionary [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
			}
			var fontName = Ide.Editor.DefaultSourceEditorOptions.Instance.FontName;
			if (!double.TryParse (fontName.Substring (fontName.LastIndexOf (' ') + 1), out var fontSize)) {
				fontSize = 12;
				LoggingService.LogError ($"Failed to parse font size from font name {fontName}");
			}
			fontName = fontName.Remove (fontName.LastIndexOf (' '));

			AddFontToDictionary (resourceDictionary, appearanceCategory, fontName, fontSize);

			editorFormat.SetProperties ("Plain Text", resourceDictionary);
		}

		protected abstract void AddFontToDictionary (ResourceDictionary resourceDictionary, string appearanceCategory, string fontName, double fontSize);
	}
}
