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
		public ThemeToClassification (IEditorFormatMapService editorFormatMapService)
		{
			this.editorFormatMapService = editorFormatMapService;
			Ide.Editor.DefaultSourceEditorOptions.Instance.Changed += UpdateEditorFormatMap;
			UpdateEditorFormatMap (null, null);
		}

		public void Dispose ()
		{
			Ide.Editor.DefaultSourceEditorOptions.Instance.Changed -= UpdateEditorFormatMap;
		}

		List<(string EditorFormatName, string MDThemeSettingName)> mappings = new List<(string, string)> {
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
			("Operator", "Keyword (Operator)"),
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
		};

		void UpdateEditorFormatMap (object sender, EventArgs args)
		{
			var editorFormat = editorFormatMapService.GetEditorFormatMap ("text");
			editorFormat.BeginBatchUpdate ();
			var theme = SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme.Value);
			var settingsMap = new Dictionary<string, ThemeSetting> ();
			var defaultSettings = theme.Settings[0];
			for (var i = 1; i < theme.Settings.Count; i++) {
				var setting = theme.Settings[i];
				settingsMap[setting.Name] = setting;
			}
			CreatePlainText (editorFormat, defaultSettings);
			CreateLineNumber (editorFormat, defaultSettings);
			CreateOutlining (editorFormat, defaultSettings);
			CreateCaret (editorFormat, defaultSettings);
			CreateSelection (editorFormat, defaultSettings);
			CreateResourceDictionary (editorFormat, defaultSettings, "text", EditorThemeColors.Foreground, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "Identifier", EditorThemeColors.Foreground, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "TextView Background", EditorThemeColors.Background);
			CreateResourceDictionary (editorFormat, defaultSettings, "MarkerFormatDefinition/HighlightedReference", EditorThemeColors.UsagesRectangle);
			CreateResourceDictionary (editorFormat, defaultSettings, "MarkerFormatDefinition/HighlightedDefinition", EditorThemeColors.ChangingUsagesRectangle);
			CreateResourceDictionary (editorFormat, defaultSettings, "MarkerFormatDefinition/HighlightedWrittenReference", EditorThemeColors.ChangingUsagesRectangle);
			CreateResourceDictionary (editorFormat, defaultSettings, "brace matching", EditorThemeColors.BracketsForeground);
			CreateResourceDictionary (editorFormat, defaultSettings, "syntax error", EditorThemeColors.UnderlineError, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "compiler error", EditorThemeColors.UnderlineError, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "other error", EditorThemeColors.UnderlineError, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "compiler warning", EditorThemeColors.UnderlineWarning, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "suggestion", EditorThemeColors.UnderlineSuggestion, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "hinted suggestion", EditorThemeColors.UnderlineSuggestion, EditorFormatDefinition.ForegroundColorId);
			CreateResourceDictionary (editorFormat, defaultSettings, "breakpoint", EditorThemeColors.BreakpointMarker);
			CreateResourceDictionary (editorFormat, defaultSettings, "breakpoint-disabled", EditorThemeColors.BreakpointMarkerDisabled);
			CreateResourceDictionary (editorFormat, defaultSettings, "breakpoint-invalid", EditorThemeColors.BreakpointMarkerInvalid);
			CreateResourceDictionary (editorFormat, defaultSettings, "currentstatement", EditorThemeColors.DebuggerCurrentLineMarker);
			CreateResourceDictionary (editorFormat, defaultSettings, "returnstatement", EditorThemeColors.DebuggerStackLineMarker);
			CreateResourceDictionary (editorFormat, defaultSettings, "Indicator Margin", EditorThemeColors.IndicatorMargin);
			foreach (var mapping in mappings) {
				if (settingsMap.TryGetValue (mapping.MDThemeSettingName, out var setting))
					CreateResourceDictionary (editorFormat, mapping.EditorFormatName, setting);
			}
			editorFormat.EndBatchUpdate ();
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

		void CreateOutlining (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			// There is EditorThemeColors.FoldCross and EditorThemeColors.FoldCrossBackground
			// but old editor is using ForeGround and FoldLine colors...
			var resourceDictionary = editorFormat.GetProperties ("outlining.square");
			if (defaultSettings.TryGetColor (EditorThemeColors.Foreground, out var foregroundColor)) {
				var (r, g, b, a) = foregroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
			}
			if (defaultSettings.TryGetColor (EditorThemeColors.FoldLine, out var backgroundColor)) {
				var (r, g, b, a) = backgroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.BackgroundBrushId] = new SolidColorBrush (c);
			}
			editorFormat.SetProperties ("outlining.square", resourceDictionary);
			if (defaultSettings.TryGetColor (EditorThemeColors.CollapsedText, out var collapsedColor)) {
				var (r, g, b, a) = collapsedColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				var collapsedResourceDictionary = editorFormat.GetProperties ("Collapsible Text (Collapsed)");

				collapsedResourceDictionary [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush (c);
				editorFormat.SetProperties ("Collapsible Text (Collapsed)", collapsedResourceDictionary);
			}
		}

		void CreateResourceDictionary (IEditorFormatMap editorFormat, string formatName, ThemeSetting setting)
		{
			var resourceDictionary = editorFormat.GetProperties (formatName);
			HslColor color;
			if (setting.TryGetColor (EditorThemeColors.Foreground, out color)) {
				var (r, g, b, a) = color.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.ForegroundColorId] = c;
			}
			if (setting.TryGetColor (EditorThemeColors.Background, out color)) {
				var (r, g, b, a) = color.ToRgba ();
				resourceDictionary [EditorFormatDefinition.BackgroundColorId] = Color.FromArgb (a, r, g, b);
			}

			if (setting.TryGetSetting ("fontStyle", out var style)) {
				resourceDictionary [ClassificationFormatDefinition.IsBoldId] = style.Contains ("bold");
			}
			editorFormat.SetProperties (formatName, resourceDictionary);
		}

		static void CreateResourceDictionary (IEditorFormatMap editorFormat, ThemeSetting defaultSettings, string vsName, string settingName, string key = EditorFormatDefinition.BackgroundColorId)
		{
			ResourceDictionary resourceDictionary = editorFormat.GetProperties (vsName);
			if (defaultSettings.TryGetColor (settingName, out var backgroundColor)) {
				var (r, g, b, a) = backgroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [key] = c;
			}
			editorFormat.SetProperties (vsName, resourceDictionary);
		}

		static void CreateLineNumber (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			var resourceDictionary = editorFormat.GetProperties ("Line Number");
			if (defaultSettings.TryGetColor ("gutterForeground", out var foregroundColor)) {
				var (r, g, b, a) = foregroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.ForegroundColorId] = c;
			}
			if (defaultSettings.TryGetColor ("gutter", out var backgroundColor)) {
				var (r, g, b, a) = backgroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.BackgroundColorId] = c;
			}
			editorFormat.SetProperties ("Line Number", resourceDictionary);
		}

		void CreatePlainText (IEditorFormatMap editorFormat, ThemeSetting defaultSettings)
		{
			var resourceDictionary = editorFormat.GetProperties ("Plain Text");
			if (defaultSettings.TryGetColor ("foreground", out var foregroundColor)) {
				var (r, g, b, a) = foregroundColor.ToRgba ();
				var c = Color.FromArgb (a, r, g, b);
				resourceDictionary [EditorFormatDefinition.ForegroundColorId] = c;
			}
			var fontName = Ide.Editor.DefaultSourceEditorOptions.Instance.FontName;
			if (!int.TryParse (fontName.Substring (fontName.LastIndexOf (' ') + 1), out var fontSize)) {
				fontSize = 12;
				LoggingService.LogError ($"Failed to parse font size from font name {fontName}");
			}
			fontName = fontName.Remove (fontName.LastIndexOf (' '));

			AddFontToDictionary (resourceDictionary, fontName, fontSize);

			editorFormat.SetProperties ("Plain Text", resourceDictionary);
		}

		protected abstract void AddFontToDictionary (ResourceDictionary resourceDictionary, string fontName, int fontSize);
	}
}