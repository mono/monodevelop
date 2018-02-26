//
// ColorScheme.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;
using System.Text;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Core.Text;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public class StyleImportException : Exception
	{
		public ImportFailReason Reason { get; private set; }

		public StyleImportException (ImportFailReason reason)
		{
			Reason = reason;
		}

		public enum ImportFailReason
		{
			Unknown,
			NoValidColorsFound
		}
	}

	static class OldFormat
	{
		public static EditorTheme ImportVsSetting (string url, Stream stream)
		{
			return ConvertToEditorTheme (ColorScheme.Import (url, stream));
		}


		public static EditorTheme ImportColorScheme (Stream stream)
		{
			return ConvertToEditorTheme (ColorScheme.LoadFrom (stream));
		}

		static Dictionary<string, string> ConvertChunkStyle (ChunkStyle style)
		{
			if (style.Foreground.Alpha == 0)
				return new Dictionary<string, string> ();
			var result = new Dictionary<string, string> {
				{ "foreground", style.Foreground.ToPangoString () }
			};
			if (style.FontStyle != Xwt.Drawing.FontStyle.Normal || 
			    style.FontWeight != Xwt.Drawing.FontWeight.Normal) {
				var fontStyle = StringBuilderCache.Allocate ();
				if (style.FontStyle != Xwt.Drawing.FontStyle.Normal) {
					fontStyle.Append (style.FontStyle.ToString ().ToLower ());
					fontStyle.Append (" ");
				}
				if (style.FontWeight != Xwt.Drawing.FontWeight.Normal) {
					fontStyle.Append (style.FontWeight.ToString ().ToLower ());
				}
				result ["fontStyle"] = StringBuilderCache.ReturnAndFree (fontStyle);
			}
			return result;
		}

		static EditorTheme ConvertToEditorTheme (ColorScheme colorScheme)
		{
			var settings = new List<ThemeSetting> ();

			var defaultSettings = new Dictionary<string, string> ();
			defaultSettings [EditorThemeColors.Background] = colorScheme.PlainText.Background.ToPangoString ();
			defaultSettings [EditorThemeColors.Caret] = colorScheme.PlainText.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.Foreground] = colorScheme.PlainText.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.Invisibles] = colorScheme.PlainText.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.LineHighlight] = colorScheme.LineMarker.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.InactiveLineHighlight] = colorScheme.LineMarkerInactive.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.Selection] = colorScheme.SelectedText.Background.ToPangoString ();
			defaultSettings [EditorThemeColors.InactiveSelection] = colorScheme.SelectedInactiveText.Background.ToPangoString ();
			defaultSettings [EditorThemeColors.FindHighlight] = colorScheme.SearchResult.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.FindHighlightForeground] = colorScheme.PlainText.Foreground.ToPangoString ();
			// selectionBorder ?, activeGuide?
			defaultSettings [EditorThemeColors.BracketsForeground] = colorScheme.BraceMatchingRectangle.Color.ToPangoString ();
			defaultSettings ["bracketsOptions"] = "underline";

			defaultSettings [EditorThemeColors.UsagesRectangle] = colorScheme.UsagesRectangle.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.ChangingUsagesRectangle] = colorScheme.ChangingUsagesRectangle.Color.ToPangoString ();

			defaultSettings [EditorThemeColors.TooltipPager] = colorScheme.TooltipPager.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.TooltipPagerTriangle] = colorScheme.TooltipPagerTriangle.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.TooltipPagerText] = colorScheme.TooltipPagerText.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.TooltipBackground] = colorScheme.TooltipText.Background.ToPangoString ();
			defaultSettings [EditorThemeColors.NotificationText] = colorScheme.NotificationText.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.NotificationTextBackground] = colorScheme.NotificationText.Background.ToPangoString ();
			defaultSettings [EditorThemeColors.NotificationBorder] = colorScheme.NotificationBorder.Color.ToPangoString ();

			defaultSettings [EditorThemeColors.MessageBubbleErrorMarker] = colorScheme.MessageBubbleErrorMarker.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorTag] = colorScheme.MessageBubbleErrorTag.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorTag2] = colorScheme.MessageBubbleErrorTag.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorTooltip] = colorScheme.MessageBubbleErrorTooltip.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorLine] = colorScheme.MessageBubbleErrorLine.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorLine2] = colorScheme.MessageBubbleErrorLine.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorBorderLine] = colorScheme.MessageBubbleErrorLine.BorderColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorCounter] = colorScheme.MessageBubbleErrorCounter.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorCounter2] = colorScheme.MessageBubbleErrorCounter.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorIconMargin] = colorScheme.MessageBubbleErrorIconMargin.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleErrorIconMarginBorder] = colorScheme.MessageBubbleErrorIconMargin.BorderColor.ToPangoString ();

			defaultSettings [EditorThemeColors.MessageBubbleWarningMarker] = colorScheme.MessageBubbleWarningMarker.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningTag] = colorScheme.MessageBubbleWarningTag.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningTag2] = colorScheme.MessageBubbleWarningTag.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningTooltip] = colorScheme.MessageBubbleWarningTooltip.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningLine] = colorScheme.MessageBubbleWarningLine.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningLine2] = colorScheme.MessageBubbleWarningLine.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningBorderLine] = colorScheme.MessageBubbleWarningLine.BorderColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningCounter] = colorScheme.MessageBubbleWarningCounter.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningCounter2] = colorScheme.MessageBubbleWarningCounter.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningIconMargin] = colorScheme.MessageBubbleWarningIconMargin.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.MessageBubbleWarningIconMarginBorder] = colorScheme.MessageBubbleWarningIconMargin.BorderColor.ToPangoString ();

			defaultSettings [EditorThemeColors.UnderlineError] = colorScheme.UnderlineError.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.UnderlineWarning] = colorScheme.UnderlineWarning.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.UnderlineSuggestion] = colorScheme.UnderlineSuggestion.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.Link] = colorScheme.LinkColor.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.LineNumbersBackground] = colorScheme.LineNumbers.Background.ToPangoString ();
			defaultSettings [EditorThemeColors.LineNumbers] = colorScheme.LineNumbers.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.CollapsedText] = colorScheme.CollapsedText.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.FoldLine] = colorScheme.FoldLineColor.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.FoldCross] = colorScheme.FoldCross.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.FoldCrossBackground] = colorScheme.FoldCross.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.QuickDiffChanged] = colorScheme.QuickDiffChanged.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.QuickDiffDirty] = colorScheme.QuickDiffDirty.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.IndicatorMargin] = colorScheme.IndicatorMargin.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.IndicatorMarginSeparator] = colorScheme.IndicatorMarginSeparator.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.BreakpointMarker] = colorScheme.BreakpointMarker.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.BreakpointText] = colorScheme.BreakpointText.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.BreakpointMarkerDisabled] = colorScheme.BreakpointMarkerDisabled.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.BreakpointMarkerInvalid] = colorScheme.BreakpointMarkerInvalid.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.DebuggerStackLineMarker] = colorScheme.DebuggerStackLineMarker.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.DebuggerCurrentLineMarker] = colorScheme.DebuggerCurrentLineMarker.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.DebuggerCurrentLine] = colorScheme.DebuggerCurrentLine.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.DebuggerStackLine] = colorScheme.DebuggerStackLine.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.IndentationGuide] = colorScheme.IndentationGuide.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.Ruler] = colorScheme.Ruler.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.PrimaryTemplate2] = colorScheme.PrimaryTemplate.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.PrimaryTemplateHighlighted2] = colorScheme.PrimaryTemplateHighlighted.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.SecondaryTemplate] = colorScheme.SecondaryTemplate.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.SecondaryTemplateHighlighted] = colorScheme.SecondaryTemplateHighlighted.Color.ToPangoString ();
			defaultSettings [EditorThemeColors.SecondaryTemplateHighlighted2] = colorScheme.SecondaryTemplateHighlighted.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.SecondaryTemplate2] = colorScheme.SecondaryTemplate.SecondColor.ToPangoString ();
			defaultSettings [EditorThemeColors.PreviewDiffRemoved] = colorScheme.PreviewDiffRemoved.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.PreviewDiffRemovedBackground] = colorScheme.PreviewDiffRemoved.Background.ToPangoString ();
			defaultSettings [EditorThemeColors.PreviewDiffAdded] = colorScheme.PreviewDiffAddedd.Foreground.ToPangoString ();
			defaultSettings [EditorThemeColors.PreviewDiffAddedBackground] = colorScheme.PreviewDiffAddedd.Background.ToPangoString ();
			
			settings.Add (new ThemeSetting (null, new List<string>(), defaultSettings));

			settings.Add (new ThemeSetting ("Comment", new List<string> { "comment" }, ConvertChunkStyle (colorScheme.CommentsSingleLine)));
			settings.Add (new ThemeSetting ("Comment Tags", new List<string> { "markup.other" }, ConvertChunkStyle (colorScheme.CommentTags)));
			settings.Add (new ThemeSetting ("Comment Block", new List<string> { "comment.block" }, ConvertChunkStyle (colorScheme.CommentsBlock)));
			settings.Add (new ThemeSetting ("Comment XML Doc Comment", new List<string> { "comment.line.documentation" }, ConvertChunkStyle (colorScheme.CommentsForDocumentation)));
			settings.Add (new ThemeSetting ("Comment XML Doc Tag", new List<string> { "punctuation.definition.tag" }, ConvertChunkStyle (colorScheme.CommentsForDocumentationTags)));

			settings.Add (new ThemeSetting ("String", new List<string> { "string" }, ConvertChunkStyle (colorScheme.String)));
			settings.Add (new ThemeSetting ("punctuation.definition.string", new List<string> { "punctuation.definition.string" }, ConvertChunkStyle (colorScheme.String)));

			settings.Add (new ThemeSetting ("Number", new List<string> { "constant.numeric" }, ConvertChunkStyle (colorScheme.Number)));
			settings.Add (new ThemeSetting ("Built-in constant", new List<string> { "constant.language" }, ConvertChunkStyle (colorScheme.KeywordConstants)));
			settings.Add (new ThemeSetting ("User-defined constant", new List<string> { "constant.character", "constant.other" }, ConvertChunkStyle (colorScheme.Number)));
			settings.Add (new ThemeSetting ("Variable", new List<string> { "variable" }, ConvertChunkStyle (colorScheme.UserVariableUsage)));
			settings.Add (new ThemeSetting ("variable.other", new List<string> { "variable.other" }, ConvertChunkStyle (colorScheme.ScriptKeyword)));
			settings.Add (new ThemeSetting ("Keyword", new List<string> { "keyword - (source.c keyword.operator | source.c++ keyword.operator | source.objc keyword.operator | source.objc++ keyword.operator), keyword.operator.word" }, ConvertChunkStyle (colorScheme.KeywordOther)));
			settings.Add (new ThemeSetting ("Keyword (Access)", new List<string> { "keyword.other.access" }, ConvertChunkStyle (colorScheme.KeywordAccessors)));
			settings.Add (new ThemeSetting ("Keyword (Types)", new List<string> { "keyword.other.type" }, ConvertChunkStyle (colorScheme.KeywordTypes)));
			settings.Add (new ThemeSetting ("Keyword (Operator)", new List<string> { "keyword.operator" }, ConvertChunkStyle (colorScheme.KeywordOperators)));
			settings.Add (new ThemeSetting ("Keyword (Selection)", new List<string> { "keyword.other.selection" }, ConvertChunkStyle (colorScheme.KeywordSelection)));
			settings.Add (new ThemeSetting ("Keyword (Itarator)", new List<string> { "keyword.other.iteration" }, ConvertChunkStyle (colorScheme.KeywordIteration)));
			settings.Add (new ThemeSetting ("Keyword (Jump)", new List<string> { "keyword.other.jump" }, ConvertChunkStyle (colorScheme.KeywordJump)));
			settings.Add (new ThemeSetting ("Keyword (Context)", new List<string> { "keyword.other.context" }, ConvertChunkStyle (colorScheme.KeywordContext)));
			settings.Add (new ThemeSetting ("Keyword (Exception)", new List<string> { "keyword.other.exception" }, ConvertChunkStyle (colorScheme.KeywordException)));
			settings.Add (new ThemeSetting ("Keyword (Modifier)", new List<string> { "keyword.other.modifiers" }, ConvertChunkStyle (colorScheme.KeywordModifiers)));
			settings.Add (new ThemeSetting ("Keyword (Void)", new List<string> { "keyword.other.void" }, ConvertChunkStyle (colorScheme.KeywordVoid)));
			settings.Add (new ThemeSetting ("Keyword (Namespace)", new List<string> { "keyword.other.namespace" }, ConvertChunkStyle (colorScheme.KeywordNamespace)));
			settings.Add (new ThemeSetting ("Keyword (Property)", new List<string> { "keyword.other.property" }, ConvertChunkStyle (colorScheme.KeywordProperty)));
			settings.Add (new ThemeSetting ("Keyword (Declaration)", new List<string> { "keyword.other.declaration" }, ConvertChunkStyle (colorScheme.KeywordDeclaration)));
			settings.Add (new ThemeSetting ("Keyword (Parameter)", new List<string> { "keyword.other.parameter" }, ConvertChunkStyle (colorScheme.KeywordParameter)));
			settings.Add (new ThemeSetting ("Keyword (Operator)", new List<string> { "keyword.other.access" }, ConvertChunkStyle (colorScheme.KeywordOperatorDeclaration)));

			settings.Add (new ThemeSetting ("storage", new List<string> { "storage" }, ConvertChunkStyle (colorScheme.KeywordOther)));
			settings.Add (new ThemeSetting ("storage.type", new List<string> { "storage.type" }, ConvertChunkStyle (colorScheme.KeywordOther)));
			settings.Add (new ThemeSetting ("punctuation", new List<string> { "punctuation" }, ConvertChunkStyle (colorScheme.Punctuation)));
			settings.Add (new ThemeSetting ("punctuation.definition.comment", new List<string> { "punctuation.definition.comment" }, ConvertChunkStyle (colorScheme.CommentsSingleLine)));


			settings.Add (new ThemeSetting ("Entity name", new List<string> { "entity.name - (entity.name.filename | entity.name.section | entity.name.tag | entity.name.label)" }, ConvertChunkStyle (colorScheme.PlainText)));
			settings.Add (new ThemeSetting ("Tag name", new List<string> { "entity.name.tag" }, ConvertChunkStyle (colorScheme.HtmlElementName)));
			settings.Add (new ThemeSetting ("Preprocessor", new List<string> { "meta.preprocessor" }, ConvertChunkStyle (colorScheme.Preprocessor)));
			settings.Add (new ThemeSetting ("Preprocessor region name", new List<string> { "meta.preprocessor.region.name" }, ConvertChunkStyle (colorScheme.PreprocessorRegionName)));

			settings.Add (new ThemeSetting ("Tag attribute", new List<string> { "entity.other.attribute-name" }, ConvertChunkStyle (colorScheme.XmlAttribute)));
			settings.Add (new ThemeSetting ("Function call", new List<string> { "variable.function" }, ConvertChunkStyle (colorScheme.UserMethodDeclaration)));
			settings.Add (new ThemeSetting ("Library function", new List<string> { "support.function" }, ConvertChunkStyle (colorScheme.UserMethodDeclaration)));
			settings.Add (new ThemeSetting ("Library constant", new List<string> { "support.constant" }, ConvertChunkStyle (colorScheme.KeywordConstants)));
			settings.Add (new ThemeSetting ("Library class/type", new List<string> { "support.type", "support.class" }, ConvertChunkStyle (colorScheme.UserTypes)));
			settings.Add (new ThemeSetting ("Library variable", new List<string> { "support.other.variable" }, ConvertChunkStyle (colorScheme.UserVariableUsage)));
			settings.Add (new ThemeSetting ("Invalid", new List<string> { "invalid" }, ConvertChunkStyle (colorScheme.SyntaxError)));
			settings.Add (new ThemeSetting ("Invalid deprecated", new List<string> { "invalid.deprecated" }, ConvertChunkStyle (colorScheme.SyntaxError)));
			settings.Add (new ThemeSetting ("JSON String", new List<string> { "meta.structure.dictionary.json string.quoted.double.json" }, ConvertChunkStyle (colorScheme.String)));
			settings.Add (new ThemeSetting ("YAML String", new List<string> { "string.unquoted.yaml" }, ConvertChunkStyle (colorScheme.String)));

			settings.Add (new ThemeSetting ("Entity Names", new List<string> { "entity.name.tag" }, ConvertChunkStyle (colorScheme.XmlName)));
			settings.Add (new ThemeSetting ("Entity Attributes", new List<string> { "entity.other.attribute" }, ConvertChunkStyle (colorScheme.XmlAttribute)));

			settings.Add (new ThemeSetting ("Diff Header", new List<string> { "meta.diff", "meta.diff.header" }, ConvertChunkStyle (colorScheme.DiffHeader)));
			settings.Add (new ThemeSetting ("Diff Line(Removed)", new List<string> { "markup.deleted" }, ConvertChunkStyle (colorScheme.DiffLineRemoved)));
			settings.Add (new ThemeSetting ("Diff Line(Added)", new List<string> { "markup.inserted" }, ConvertChunkStyle (colorScheme.DiffLineAdded)));
			settings.Add (new ThemeSetting ("Diff Line(Changed)", new List<string> { "markup.changed" }, ConvertChunkStyle (colorScheme.DiffLineChanged)));

			settings.Add (new ThemeSetting ("String(Regex Character Class)", new List<string> { "constant.character.regex.characterclass" }, ConvertChunkStyle (colorScheme.RegexCharacterClass)));
			settings.Add (new ThemeSetting ("String(Regex Grouping Constructs)", new List<string> { "constant.character.regex.grouping" }, ConvertChunkStyle (colorScheme.RegexGroupingConstructs)));
			settings.Add (new ThemeSetting ("String(Regex Set Constructs)", new List<string> { "constant.character.regex.set" }, ConvertChunkStyle (colorScheme.RegexSetConstructs)));
			settings.Add (new ThemeSetting ("String(Regex Errors)", new List<string> { "constant.character.regex.errors" }, ConvertChunkStyle (colorScheme.SyntaxError)));
			settings.Add (new ThemeSetting ("String(Regex Comments)", new List<string> { "constant.character.regex.comments" }, ConvertChunkStyle (colorScheme.CommentsSingleLine)));
			settings.Add (new ThemeSetting ("String(Regex Escape Character)", new List<string> { "constant.character.regex.escape" }, ConvertChunkStyle (colorScheme.RegexEscapeCharacter)));
			settings.Add (new ThemeSetting ("String(Regex Alternate Escape Character)", new List<string> { "constant.character.regex.altescape" }, ConvertChunkStyle (colorScheme.RegexAltEscapeCharacter)));

			settings.Add (new ThemeSetting ("User Types", new List<string> { EditorThemeColors.UserTypes }, ConvertChunkStyle (colorScheme.UserTypes)));
			settings.Add (new ThemeSetting ("User Types(Value types)", new List<string> { EditorThemeColors.UserTypesValueTypes }, ConvertChunkStyle (colorScheme.UserTypesValueTypes)));
			settings.Add (new ThemeSetting ("User Types(Interfaces)", new List<string> { EditorThemeColors.UserTypesInterfaces }, ConvertChunkStyle (colorScheme.UserTypesInterfaces)));
			settings.Add (new ThemeSetting ("User Types(Enums)", new List<string> { EditorThemeColors.UserTypesEnums }, ConvertChunkStyle (colorScheme.UserTypesEnums)));
			settings.Add (new ThemeSetting ("User Types(Type parameters)", new List<string> { EditorThemeColors.UserTypesTypeParameters }, ConvertChunkStyle (colorScheme.UserTypesTypeParameters)));
			settings.Add (new ThemeSetting ("User Types(Delegates)", new List<string> { EditorThemeColors.UserTypesDelegates }, ConvertChunkStyle (colorScheme.UserTypesDelegates)));
			settings.Add (new ThemeSetting ("User Types(Mutable)", new List<string> { EditorThemeColors.UserTypesMutable }, ConvertChunkStyle (colorScheme.UserTypesMutable)));

			settings.Add (new ThemeSetting ("User Field(Declaration)", new List<string> { EditorThemeColors.UserFieldDeclaration }, ConvertChunkStyle (colorScheme.UserFieldDeclaration)));
			settings.Add (new ThemeSetting ("User Field(Usage)", new List<string> { EditorThemeColors.UserFieldUsage }, ConvertChunkStyle (colorScheme.UserFieldUsage)));
			settings.Add (new ThemeSetting ("User Property(Declaration)", new List<string> { EditorThemeColors.UserPropertyDeclaration }, ConvertChunkStyle (colorScheme.UserPropertyDeclaration)));
			settings.Add (new ThemeSetting ("User Property(Usage)", new List<string> { EditorThemeColors.UserPropertyUsage }, ConvertChunkStyle (colorScheme.UserPropertyUsage)));
			settings.Add (new ThemeSetting ("User Event(Declaration)", new List<string> { EditorThemeColors.UserEventDeclaration }, ConvertChunkStyle (colorScheme.UserEventDeclaration)));
			settings.Add (new ThemeSetting ("User Event(Usage)", new List<string> { EditorThemeColors.UserEventUsage }, ConvertChunkStyle (colorScheme.UserEventUsage)));
			settings.Add (new ThemeSetting ("User Method(Declaration)", new List<string> { EditorThemeColors.UserMethodDeclaration }, ConvertChunkStyle (colorScheme.UserMethodDeclaration)));
			settings.Add (new ThemeSetting ("User Method(Usage)", new List<string> { EditorThemeColors.UserMethodUsage }, ConvertChunkStyle (colorScheme.UserMethodUsage)));
			settings.Add (new ThemeSetting ("User Parameter(Declaration)", new List<string> { EditorThemeColors.UserParameterDeclaration }, ConvertChunkStyle (colorScheme.UserParameterDeclaration)));
			settings.Add (new ThemeSetting ("User Parameter(Usage)", new List<string> { EditorThemeColors.UserParameterUsage }, ConvertChunkStyle (colorScheme.UserParameterUsage)));
			settings.Add (new ThemeSetting ("User Variable(Declaration)", new List<string> { EditorThemeColors.UserVariableDeclaration }, ConvertChunkStyle (colorScheme.UserVariableDeclaration)));
			settings.Add (new ThemeSetting ("User Variable(Usage)", new List<string> { EditorThemeColors.UserVariableUsage }, ConvertChunkStyle (colorScheme.UserVariableUsage)));

			settings.Add (new ThemeSetting ("CSS Comment", new List<string> { "comment.block.css" }, ConvertChunkStyle (colorScheme.CssComment)));
			settings.Add (new ThemeSetting ("CSS Keyword", new List<string> { "keyword.other.css" }, ConvertChunkStyle (colorScheme.CssKeyword)));
			settings.Add (new ThemeSetting ("CSS Selector", new List<string> { "entity.other.pseudo-class.css" }, ConvertChunkStyle (colorScheme.CssSelector)));
			settings.Add (new ThemeSetting ("CSS Property Name", new List<string> { "support.type.property-name.css" }, ConvertChunkStyle (colorScheme.CssPropertyName)));
			settings.Add (new ThemeSetting ("CSS Property Value", new List<string> { "support.constant.property-value.css" }, ConvertChunkStyle (colorScheme.CssPropertyValue)));
			settings.Add (new ThemeSetting ("CSS String Value", new List<string> { "string.quoted.double.css" }, ConvertChunkStyle (colorScheme.CssStringValue)));

			settings.Add (new ThemeSetting ("HTML Attribute Name", new List<string> { "entity.other.attribute-name.html" }, ConvertChunkStyle (colorScheme.HtmlAttributeName)));
			settings.Add (new ThemeSetting ("HTML Attribute Value", new List<string> { "string.unquoted.html" }, ConvertChunkStyle (colorScheme.HtmlAttributeValue)));
			settings.Add (new ThemeSetting ("HTML Comment", new List<string> { "comment.block.html" }, ConvertChunkStyle (colorScheme.HtmlComment)));
			settings.Add (new ThemeSetting ("HTML Element Name", new List<string> { "entity.name.tag.html" }, ConvertChunkStyle (colorScheme.HtmlElementName)));
			settings.Add (new ThemeSetting ("HTML Entity", new List<string> { "constant.character.entity.html" }, ConvertChunkStyle (colorScheme.HtmlEntity)));

			var style = ConvertChunkStyle (colorScheme.PlainText);
			style ["fontStyle"] = "bold";
			settings.Add (new ThemeSetting ("Bold Markup", new List<string> { "markup.bold" }, style));

			style = ConvertChunkStyle (colorScheme.PlainText);
			style ["fontStyle"] = "italic";
			settings.Add (new ThemeSetting ("Italic Markup", new List<string> { "markup.italic" }, style));

			settings.Add (new ThemeSetting ("markup.emoji", new List<string> { "markup.emoji" }, ConvertChunkStyle (colorScheme.Preprocessor)));
			settings.Add (new ThemeSetting ("markup.link", new List<string> { "markup.link" }, ConvertChunkStyle (colorScheme.UserTypesInterfaces)));

			return new EditorTheme (colorScheme.Name, settings);
		}

		sealed class AmbientColor
		{
			public string Name { get; private set; }
			public readonly List<Tuple<string, HslColor>> Colors = new List<Tuple<string, HslColor>> ();

			public HslColor Color {
				get {
					return GetColor ("color");
				}
				set {
					for (int i = 0; i < Colors.Count; i++) {
						var t = Colors [i];
						if (t.Item1 == "color") {
							Colors [i] = Tuple.Create ("color", value);
							return;
						}
					}
					Colors.Add (Tuple.Create ("color", value));
				}
			}

			public HslColor SecondColor {
				get {
					return GetColor ("secondcolor");
				}
				set {
					for (int i = 0; i < Colors.Count; i++) {
						var t = Colors [i];
						if (t.Item1 == "secondcolor") {
							Colors [i] = Tuple.Create ("secondcolor", value);
							return;
						}
					}
					Colors.Add (Tuple.Create ("secondcolor", value));
				}
			}

			public bool HasSecondColor {
				get {
					return Colors.Any (c => c.Item1 == "secondcolor");
				}
			}

			public HslColor BorderColor {
				get {
					return GetColor ("bordercolor");
				}
				set {
					for (int i = 0; i < Colors.Count; i++) {
						var t = Colors [i];
						if (t.Item1 == "bordercolor") {
							Colors [i] = Tuple.Create ("bordercolor", value);
							return;
						}
					}
					Colors.Add (Tuple.Create ("bordercolor", value));
				}
			}

			public bool HasBorderColor {
				get {
					return Colors.Any (c => c.Item1 == "bordercolor");
				}
			}

			public HslColor GetColor (string name)
			{
				foreach (var color in Colors) {
					if (color.Item1 == name)
						return color.Item2;
				}

				return new HslColor (0, 0, 0);
			}

			internal static AmbientColor Create (XElement element, Dictionary<string, HslColor> palette)
			{
				var result = new AmbientColor ();
				foreach (var node in element.DescendantNodes ()) {
					if (node.NodeType == System.Xml.XmlNodeType.Element) {
						var el = (XElement)node;
						switch (el.Name.LocalName) {
						case "name":
							result.Name = el.Value;
							break;
						default:
							result.Colors.Add (Tuple.Create (el.Name.LocalName, ColorScheme.ParsePaletteColor (palette, el.Value)));
							break;
						}
					}
				}

				return result;
			}

			public override bool Equals (object obj)
			{
				if (obj == null)
					return false;
				if (ReferenceEquals (this, obj))
					return true;
				if (obj.GetType () != typeof (AmbientColor))
					return false;
				AmbientColor other = (AmbientColor)obj;
				return Colors.Equals (other.Colors) && Name == other.Name;
			}

			public override int GetHashCode ()
			{
				unchecked {
					return (Colors != null ? Colors.GetHashCode () : 0) ^ (Name != null ? Name.GetHashCode () : 0);
				}
			}

			internal static AmbientColor Import (Dictionary<string, ColorScheme.VSSettingColor> colors, string vsSetting)
			{
				var result = new AmbientColor ();
				var attrs = vsSetting.Split (',');
				foreach (var attr in attrs) {
					var info = attr.Split ('=');
					if (info.Length != 2)
						continue;
					var idx = info [1].LastIndexOf ('/');
					var source = info [1].Substring (0, idx);
					var dest = info [1].Substring (idx + 1);

					ColorScheme.VSSettingColor color;
					if (!colors.TryGetValue (source, out color))
						continue;
					result.Name = color.Name;
					string colorString;
					switch (dest) {
					case "Foreground":
						colorString = color.Foreground;
						break;
					case "Background":
						colorString = color.Background;
						break;
					default:
						throw new InvalidDataException ("Invalid attribute source: " + dest);
					}
					result.Colors.Add (Tuple.Create (info [0], ColorScheme.ImportVsColor (colorString)));
				}
				if (result.Colors.Count == 0)
					return null;
				return result;
			}
		}

		sealed class ColorScheme
		{
			public string Name { get; set; }
			public string Description { get; set; }
			public string Originator { get; set; }
			public string BaseScheme { get; set; }
			public string FileName { get; set; }

			#region Ambient Colors

			[ColorDescription ("Background(Read Only)", VSSetting = "color=Plain Text/Background")]
			public AmbientColor BackgroundReadOnly { get; private set; }

			[ColorDescription ("Search result background")]
			public AmbientColor SearchResult { get; private set; }

			[ColorDescription ("Search result background (highlighted)")]
			public AmbientColor SearchResultMain { get; private set; }

			[ColorDescription ("Fold Square", VSSetting = "color=outlining.verticalrule/Foreground")]
			public AmbientColor FoldLineColor { get; private set; }

			[ColorDescription ("Fold Cross", VSSetting = "color=outlining.square/Foreground,secondcolor=outlining.square/Background")]
			public AmbientColor FoldCross { get; private set; }

			[ColorDescription ("Indentation Guide")] // not defined
			public AmbientColor IndentationGuide { get; private set; }

			[ColorDescription ("Indicator Margin", VSSetting = "color=Indicator Margin/Background")]
			public AmbientColor IndicatorMargin { get; private set; }

			[ColorDescription ("Indicator Margin(Separator)", VSSetting = "color=Indicator Margin/Background")]
			public AmbientColor IndicatorMarginSeparator { get; private set; }

			[ColorDescription ("Tooltip Pager Top")]
			public AmbientColor TooltipPager { get; private set; }

			[ColorDescription ("Tooltip Pager Triangle")]
			public AmbientColor TooltipPagerTriangle { get; private set; }

			[ColorDescription ("Tooltip Pager Text")]
			public AmbientColor TooltipPagerText { get; private set; }

			[ColorDescription ("Notification Border")]
			public AmbientColor NotificationBorder { get; private set; }

			[ColorDescription ("Bookmarks")]
			public AmbientColor Bookmarks { get; private set; }

			[ColorDescription ("Underline(Error)", VSSetting = "color=Syntax Error/Foreground")]
			public AmbientColor UnderlineError { get; private set; }

			[ColorDescription ("Underline(Warning)", VSSetting = "color=Warning/Foreground")]
			public AmbientColor UnderlineWarning { get; private set; }

			[ColorDescription ("Underline(Suggestion)", VSSetting = "color=Other Error/Foreground")]
			public AmbientColor UnderlineSuggestion { get; private set; }

			[ColorDescription ("Underline(Hint)", VSSetting = "color=Other Error/Foreground")]
			public AmbientColor UnderlineHint { get; private set; }

			[ColorDescription ("Quick Diff(Dirty)")]
			public AmbientColor QuickDiffDirty { get; private set; }

			[ColorDescription ("Quick Diff(Changed)")]
			public AmbientColor QuickDiffChanged { get; private set; }

			[ColorDescription ("Brace Matching(Rectangle)", VSSetting = "color=Brace Matching (Rectangle)/Background,secondcolor=Brace Matching (Rectangle)/Foreground")]
			public AmbientColor BraceMatchingRectangle { get; private set; }

			[ColorDescription ("Usages(Rectangle)", VSSetting = "color=MarkerFormatDefinition/HighlightedReference/Background,secondcolor=MarkerFormatDefinition/HighlightedReference/Background,bordercolor=MarkerFormatDefinition/HighlightedReference/Background")]
			public AmbientColor UsagesRectangle { get; private set; }

			[ColorDescription ("Changing usages(Rectangle)", VSSetting = "color=MarkerFormatDefinition/HighlightedReference/Foreground,secondcolor=MarkerFormatDefinition/HighlightedReference/Foreground,bordercolor=MarkerFormatDefinition/HighlightedReference/Foreground")]
			public AmbientColor ChangingUsagesRectangle { get; private set; }

			[ColorDescription ("Breakpoint Marker", VSSetting = "color=Breakpoint (Enabled)/Background")]
			public AmbientColor BreakpointMarker { get; private set; }

			[ColorDescription ("Breakpoint Marker(Invalid)", VSSetting = "color=Breakpoint (Disabled)/Background")]
			public AmbientColor BreakpointMarkerInvalid { get; private set; }

			[ColorDescription ("Breakpoint Marker(Disabled)")]
			public AmbientColor BreakpointMarkerDisabled { get; private set; }

			[ColorDescription ("Debugger Current Line Marker", VSSetting = "color=Current Statement/Background")]
			public AmbientColor DebuggerCurrentLineMarker { get; private set; }

			[ColorDescription ("Debugger Stack Line Marker")]
			public AmbientColor DebuggerStackLineMarker { get; private set; }

			[ColorDescription ("Primary Link", VSSetting = "color=Refactoring Dependent Field/Background")]
			public AmbientColor PrimaryTemplate { get; private set; }

			[ColorDescription ("Primary Link(Highlighted)", VSSetting = "color=Refactoring Current Field/Background")]
			public AmbientColor PrimaryTemplateHighlighted { get; private set; }

			[ColorDescription ("Secondary Link")] // not defined
			public AmbientColor SecondaryTemplate { get; private set; }

			[ColorDescription ("Secondary Link(Highlighted)")] // not defined
			public AmbientColor SecondaryTemplateHighlighted { get; private set; }

			[ColorDescription ("Current Line Marker", VSSetting = "color=CurrentLineActiveFormat/Background,secondcolor=CurrentLineActiveFormat/Foreground")]
			public AmbientColor LineMarker { get; private set; }

			[ColorDescription ("Current Line Marker(Inactive)", VSSetting = "color=CurrentLineInactiveFormat/Background,secondcolor=CurrentLineInactiveFormat/Foreground")]
			public AmbientColor LineMarkerInactive { get; private set; }

			[ColorDescription ("Column Ruler")] // not defined
			public AmbientColor Ruler { get; private set; }

			[ColorDescription ("Completion Window", VSSetting = "color=Plain Text/Background")]
			public AmbientColor CompletionWindow { get; private set; }

			[ColorDescription ("Completion Tooltip Window", VSSetting = "color=Plain Text/Background")]
			public AmbientColor CompletionTooltipWindow { get; private set; }

			[ColorDescription ("Completion Selection Bar Border", VSSetting = "color=Selected Text/Background")]
			public AmbientColor CompletionSelectionBarBorder { get; private set; }

			[ColorDescription ("Completion Selection Bar Background", VSSetting = "color=Selected Text/Background,secondcolor=Selected Text/Background")]
			public AmbientColor CompletionSelectionBarBackground { get; private set; }

			[ColorDescription ("Completion Selection Bar Border(Inactive)", VSSetting = "color=Inactive Selected Text/Background")]
			public AmbientColor CompletionSelectionBarBorderInactive { get; private set; }

			[ColorDescription ("Completion Selection Bar Background(Inactive)", VSSetting = "color=Inactive Selected Text/Background,secondcolor=Inactive Selected Text/Background")]
			public AmbientColor CompletionSelectionBarBackgroundInactive { get; private set; }

			[ColorDescription ("Message Bubble Error Marker")]
			public AmbientColor MessageBubbleErrorMarker { get; private set; }

			[ColorDescription ("Message Bubble Error Tag")]
			public AmbientColor MessageBubbleErrorTag { get; private set; }

			[ColorDescription ("Message Bubble Error Tooltip")]
			public AmbientColor MessageBubbleErrorTooltip { get; private set; }

			[ColorDescription ("Message Bubble Error Line")]
			public AmbientColor MessageBubbleErrorLine { get; private set; }

			[ColorDescription ("Message Bubble Error Counter")]
			public AmbientColor MessageBubbleErrorCounter { get; private set; }

			[ColorDescription ("Message Bubble Error IconMargin")]
			public AmbientColor MessageBubbleErrorIconMargin { get; private set; }

			[ColorDescription ("Message Bubble Warning Marker")]
			public AmbientColor MessageBubbleWarningMarker { get; private set; }

			[ColorDescription ("Message Bubble Warning Tag")]
			public AmbientColor MessageBubbleWarningTag { get; private set; }

			[ColorDescription ("Message Bubble Warning Tooltip")]
			public AmbientColor MessageBubbleWarningTooltip { get; private set; }

			[ColorDescription ("Message Bubble Warning Line")]
			public AmbientColor MessageBubbleWarningLine { get; private set; }

			[ColorDescription ("Message Bubble Warning Counter")]
			public AmbientColor MessageBubbleWarningCounter { get; private set; }

			[ColorDescription ("Message Bubble Warning IconMargin")]
			public AmbientColor MessageBubbleWarningIconMargin { get; private set; }

			[ColorDescription ("Link Color")]
			public AmbientColor LinkColor { get; private set; }

			[ColorDescription ("Link Color(Active)")]
			public AmbientColor ActiveLinkColor { get; private set; }

			#endregion

			#region Text Colors

			public const string PlainTextKey = "Plain Text";

			[ColorDescription (PlainTextKey, VSSetting = "Plain Text")]
			public ChunkStyle PlainText { get; private set; }

			public const string SelectedTextKey = "Selected Text";
			[ColorDescription (SelectedTextKey, VSSetting = "Selected Text")]
			public ChunkStyle SelectedText { get; private set; }

			public const string SelectedInactiveTextKey = "Selected Text(Inactive)";
			[ColorDescription (SelectedInactiveTextKey, VSSetting = "Inactive Selected Text")]
			public ChunkStyle SelectedInactiveText { get; private set; }

			public const string CollapsedTextKey = "Collapsed Text";
			[ColorDescription (CollapsedTextKey, VSSetting = "Collapsible Text")]
			public ChunkStyle CollapsedText { get; private set; }

			public const string LineNumbersKey = "Line Numbers";
			[ColorDescription (LineNumbersKey, VSSetting = "Line Numbers")]
			public ChunkStyle LineNumbers { get; private set; }

			public const string PunctuationKey = "Punctuation";
			[ColorDescription (PunctuationKey, VSSetting = "Operator")]
			public ChunkStyle Punctuation { get; private set; }

			public const string PunctuationForBracketsKey = "Punctuation(Brackets)";
			[ColorDescription (PunctuationForBracketsKey, VSSetting = "Plain Text")]
			public ChunkStyle PunctuationForBrackets { get; private set; }

			public const string CommentsSingleLineKey = "Comment(Line)";
			[ColorDescription (CommentsSingleLineKey, VSSetting = "Comment")]
			public ChunkStyle CommentsSingleLine { get; private set; }

			public const string CommentsBlockKey = "Comment(Block)";
			[ColorDescription (CommentsBlockKey, VSSetting = "Comment")]
			public ChunkStyle CommentsBlock { get; private set; }

			public const string CommentsForDocumentationKey = "Comment(Doc)";
			[ColorDescription (CommentsForDocumentationKey, VSSetting = "XML Doc Comment")]
			public ChunkStyle CommentsForDocumentation { get; private set; }

			public const string CommentsForDocumentationTagsKey = "Comment(DocTag)";
			[ColorDescription (CommentsForDocumentationTagsKey, VSSetting = "XML Doc Tag")]
			public ChunkStyle CommentsForDocumentationTags { get; private set; }

			public const string CommentTagsKey = "Comment Tag";
			[ColorDescription (CommentTagsKey, VSSetting = "Comment")]
			public ChunkStyle CommentTags { get; private set; }

			public const string ExcludedCodeKey = "Excluded Code";
			[ColorDescription (ExcludedCodeKey, VSSetting = "Excluded Code")]
			public ChunkStyle ExcludedCode { get; private set; }

			public const string StringKey = "String";
			[ColorDescription (StringKey, VSSetting = "String")]
			public ChunkStyle String { get; private set; }

			public const string StringEscapeSequenceKey = "String(Escape)";
			[ColorDescription (StringEscapeSequenceKey, VSSetting = "String")]
			public ChunkStyle StringEscapeSequence { get; private set; }

			public const string StringVerbatimKey = "String(C# @ Verbatim)";
			[ColorDescription (StringVerbatimKey, VSSetting = "String(C# @ Verbatim)")]
			public ChunkStyle StringVerbatim { get; private set; }

			public const string NumberKey = "Number";
			[ColorDescription (NumberKey, VSSetting = "Number")]
			public ChunkStyle Number { get; private set; }

			public const string PreprocessorKey = "Preprocessor";
			[ColorDescription (PreprocessorKey, VSSetting = "Preprocessor Keyword")]
			public ChunkStyle Preprocessor { get; private set; }

			public const string PreprocessorRegionNameKey = "Preprocessor(Region Name)";
			[ColorDescription (PreprocessorRegionNameKey, VSSetting = "Plain Text")]
			public ChunkStyle PreprocessorRegionName { get; private set; }

			public const string XmlTextKey = "Xml Text";
			[ColorDescription (XmlTextKey, VSSetting = "XML Text")]
			public ChunkStyle XmlText { get; private set; }

			public const string XmlDelimiterKey = "Xml Delimiter";
			[ColorDescription (XmlDelimiterKey, VSSetting = "XML Delimiter")]
			public ChunkStyle XmlDelimiter { get; private set; }

			public const string XmlNameKey = "Xml Name";
			[ColorDescription (XmlNameKey, VSSetting = "XML Name")]
			public ChunkStyle XmlName { get; private set; }

			public const string XmlAttributeKey = "Xml Attribute";
			[ColorDescription (XmlAttributeKey, VSSetting = "XML Attribute")]
			public ChunkStyle XmlAttribute { get; private set; }

			public const string XmlAttributeQuotesKey = "Xml Attribute Quotes";
			[ColorDescription (XmlAttributeQuotesKey, VSSetting = "XML Attribute Quotes")]
			public ChunkStyle XmlAttributeQuotes { get; private set; }

			public const string XmlAttributeValueKey = "Xml Attribute Value";
			[ColorDescription (XmlAttributeValueKey, VSSetting = "XML Attribute Value")]
			public ChunkStyle XmlAttributeValue { get; private set; }

			public const string XmlCommentKey = "Xml Comment";
			[ColorDescription (XmlCommentKey, VSSetting = "XML Comment")]
			public ChunkStyle XmlComment { get; private set; }

			public const string XmlCDataSectionKey = "Xml CData Section";
			[ColorDescription (XmlCDataSectionKey, VSSetting = "XML CData Section")]
			public ChunkStyle XmlCDataSection { get; private set; }

			public const string TooltipTextKey = "Tooltip Text";
			[ColorDescription (TooltipTextKey)] // not defined in vs.net
			public ChunkStyle TooltipText { get; private set; }

			public const string NotificationTextKey = "Notification Text";
			[ColorDescription (NotificationTextKey)] // not defined in vs.net
			public ChunkStyle NotificationText { get; private set; }

			public const string CompletionTextKey = "Completion Text";
			[ColorDescription (CompletionTextKey, VSSetting = "Plain Text")]
			public ChunkStyle CompletionText { get; private set; }

			public const string CompletionMatchingSubstringKey = "Completion Matching Substring";
			[ColorDescription (CompletionMatchingSubstringKey, VSSetting = "Keyword")]
			public ChunkStyle CompletionMatchingSubstring { get; private set; }

			public const string CompletionSelectedTextKey = "Completion Selected Text";
			[ColorDescription (CompletionSelectedTextKey, VSSetting = "Selected Text")]
			public ChunkStyle CompletionSelectedText { get; private set; }

			public const string CompletionSelectedMatchingSubstringKey = "Completion Selected Matching Substring";
			[ColorDescription (CompletionSelectedMatchingSubstringKey, VSSetting = "Keyword")]
			public ChunkStyle CompletionSelectedMatchingSubstring { get; private set; }

			public const string CompletionSelectedInactiveTextKey = "Completion Selected Text(Inactive)";
			[ColorDescription (CompletionSelectedInactiveTextKey, VSSetting = "Inactive Selected Text")]
			public ChunkStyle CompletionSelectedInactiveText { get; private set; }

			public const string CompletionSelectedInactiveMatchingSubstringKey = "Completion Selected Matching Substring(Inactive)";
			[ColorDescription (CompletionSelectedInactiveMatchingSubstringKey, VSSetting = "Keyword")]
			public ChunkStyle CompletionSelectedInactiveMatchingSubstring { get; private set; }

			public const string KeywordAccessorsKey = "Keyword(Access)";
			[ColorDescription (KeywordAccessorsKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordAccessors { get; private set; }

			public const string KeywordTypesKey = "Keyword(Type)";
			[ColorDescription (KeywordTypesKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordTypes { get; private set; }

			public const string KeywordOperatorsKey = "Keyword(Operator)";
			[ColorDescription (KeywordOperatorsKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordOperators { get; private set; }

			public const string KeywordSelectionKey = "Keyword(Selection)";
			[ColorDescription (KeywordSelectionKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordSelection { get; private set; }

			public const string KeywordIterationKey = "Keyword(Iteration)";
			[ColorDescription (KeywordIterationKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordIteration { get; private set; }

			public const string KeywordJumpKey = "Keyword(Jump)";
			[ColorDescription (KeywordJumpKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordJump { get; private set; }

			public const string KeywordContextKey = "Keyword(Context)";
			[ColorDescription (KeywordContextKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordContext { get; private set; }

			public const string KeywordExceptionKey = "Keyword(Exception)";
			[ColorDescription (KeywordExceptionKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordException { get; private set; }

			public const string KeywordModifiersKey = "Keyword(Modifiers)";
			[ColorDescription (KeywordModifiersKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordModifiers { get; private set; }

			public const string KeywordConstantsKey = "Keyword(Constants)";
			[ColorDescription (KeywordConstantsKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordConstants { get; private set; }

			public const string KeywordVoidKey = "Keyword(Void)";
			[ColorDescription (KeywordVoidKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordVoid { get; private set; }

			public const string KeywordNamespaceKey = "Keyword(Namespace)";
			[ColorDescription (KeywordNamespaceKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordNamespace { get; private set; }

			public const string KeywordPropertyKey = "Keyword(Property)";
			[ColorDescription (KeywordPropertyKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordProperty { get; private set; }

			public const string KeywordDeclarationKey = "Keyword(Declaration)";
			[ColorDescription (KeywordDeclarationKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordDeclaration { get; private set; }

			public const string KeywordParameterKey = "Keyword(Parameter)";
			[ColorDescription (KeywordParameterKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordParameter { get; private set; }

			public const string KeywordOperatorDeclarationKey = "Keyword(Operator Declaration)";
			[ColorDescription (KeywordOperatorDeclarationKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordOperatorDeclaration { get; private set; }

			public const string KeywordOtherKey = "Keyword(Other)";
			[ColorDescription (KeywordOtherKey, VSSetting = "Keyword")]
			public ChunkStyle KeywordOther { get; private set; }

			public const string UserTypesKey = "User Types";
			[ColorDescription (UserTypesKey, VSSetting = "User Types")]
			public ChunkStyle UserTypes { get; private set; }

			public const string UserTypesEnumsKey = "User Types(Enums)";
			[ColorDescription (UserTypesEnumsKey, VSSetting = "User Types(Enums)")]
			public ChunkStyle UserTypesEnums { get; private set; }

			public const string UserTypesInterfacesKey = "User Types(Interfaces)";
			[ColorDescription (UserTypesInterfacesKey, VSSetting = "User Types(Interfaces)")]
			public ChunkStyle UserTypesInterfaces { get; private set; }

			public const string UserTypesDelegatesKey = "User Types(Delegates)";
			[ColorDescription (UserTypesDelegatesKey, VSSetting = "User Types(Delegates)")]
			public ChunkStyle UserTypesDelegates { get; private set; }

			public const string UserTypesValueTypesKey = "User Types(Value types)";
			[ColorDescription (UserTypesValueTypesKey, VSSetting = "User Types(Value types)")]
			public ChunkStyle UserTypesValueTypes { get; private set; }

			public const string UserTypesTypeParametersKey = "User Types(Type parameters)";
			[ColorDescription (UserTypesTypeParametersKey, VSSetting = "User Types(Type parameters)")]
			public ChunkStyle UserTypesTypeParameters { get; private set; }

			public const string UserTypesMutableKey = "User Types(Mutable)";
			[ColorDescription (UserTypesMutableKey, VSSetting = "User Types(Mutable")]
			public ChunkStyle UserTypesMutable { get; private set; }

			public const string UserFieldUsageKey = "User Field Usage";
			[ColorDescription (UserFieldUsageKey, VSSetting = "Identifier")]
			public ChunkStyle UserFieldUsage { get; private set; }

			public const string UserFieldDeclarationKey = "User Field Declaration";
			[ColorDescription (UserFieldDeclarationKey, VSSetting = "Identifier")]
			public ChunkStyle UserFieldDeclaration { get; private set; }

			public const string UserPropertyUsageKey = "User Property Usage";
			[ColorDescription (UserPropertyUsageKey, VSSetting = "Identifier")]
			public ChunkStyle UserPropertyUsage { get; private set; }

			public const string UserPropertyDeclarationKey = "User Property Declaration";
			[ColorDescription (UserPropertyDeclarationKey, VSSetting = "Identifier")]
			public ChunkStyle UserPropertyDeclaration { get; private set; }

			public const string UserEventUsageKey = "User Event Usage";
			[ColorDescription (UserEventUsageKey, VSSetting = "Identifier")]
			public ChunkStyle UserEventUsage { get; private set; }

			public const string UserEventDeclarationKey = "User Event Declaration";
			[ColorDescription (UserEventDeclarationKey, VSSetting = "Identifier")]
			public ChunkStyle UserEventDeclaration { get; private set; }

			public const string UserMethodUsageKey = "User Method Usage";
			[ColorDescription (UserMethodUsageKey, VSSetting = "Identifier")]
			public ChunkStyle UserMethodUsage { get; private set; }

			public const string UserMethodDeclarationKey = "User Method Declaration";
			[ColorDescription (UserMethodDeclarationKey, VSSetting = "Identifier")]
			public ChunkStyle UserMethodDeclaration { get; private set; }

			public const string UserParameterUsageKey = "User Parameter Usage";
			[ColorDescription (UserParameterUsageKey, VSSetting = "Identifier")]
			public ChunkStyle UserParameterUsage { get; private set; }

			public const string UserParameterDeclarationKey = "User Parameter Declaration";
			[ColorDescription (UserParameterDeclarationKey, VSSetting = "Identifier")]
			public ChunkStyle UserParameterDeclaration { get; private set; }

			public const string UserVariableUsageKey = "User Variable Usage";
			[ColorDescription (UserVariableUsageKey, VSSetting = "Identifier")]
			public ChunkStyle UserVariableUsage { get; private set; }

			public const string UserVariableDeclarationKey = "User Variable Declaration";
			[ColorDescription (UserVariableDeclarationKey, VSSetting = "Identifier")]
			public ChunkStyle UserVariableDeclaration { get; private set; }

			public const string SyntaxErrorKey = "Syntax Error";
			[ColorDescription (SyntaxErrorKey, VSSetting = "Syntax Error")]
			public ChunkStyle SyntaxError { get; private set; }

			public const string StringFormatItemsKey = "String Format Items";
			[ColorDescription (StringFormatItemsKey, VSSetting = "String")]
			public ChunkStyle StringFormatItems { get; private set; }

			public const string BreakpointTextKey = "Breakpoint Text";
			[ColorDescription (BreakpointTextKey, VSSetting = "Breakpoint (Enabled)")]
			public ChunkStyle BreakpointText { get; private set; }

			public const string DebuggerCurrentLineKey = "Debugger Current Statement";
			[ColorDescription (DebuggerCurrentLineKey, VSSetting = "Current Statement")]
			public ChunkStyle DebuggerCurrentLine { get; private set; }

			public const string DebuggerStackLineKey = "Debugger Stack Line";
			[ColorDescription (DebuggerStackLineKey)] // not defined
			public ChunkStyle DebuggerStackLine { get; private set; }

			public const string DiffLineAddedKey = "Diff Line(Added)";
			[ColorDescription (DiffLineAddedKey)] //not defined
			public ChunkStyle DiffLineAdded { get; private set; }

			public const string DiffLineRemovedKey = "Diff Line(Removed)";
			[ColorDescription (DiffLineRemovedKey)] //not defined
			public ChunkStyle DiffLineRemoved { get; private set; }

			public const string DiffLineChangedKey = "Diff Line(Changed)";
			[ColorDescription (DiffLineChangedKey)] //not defined
			public ChunkStyle DiffLineChanged { get; private set; }

			public const string DiffHeaderKey = "Diff Header";
			[ColorDescription (DiffHeaderKey)] //not defined
			public ChunkStyle DiffHeader { get; private set; }

			public const string DiffHeaderSeparatorKey = "Diff Header(Separator)";
			[ColorDescription (DiffHeaderSeparatorKey)] //not defined
			public ChunkStyle DiffHeaderSeparator { get; private set; }

			public const string DiffHeaderOldKey = "Diff Header(Old)";
			[ColorDescription (DiffHeaderOldKey)] //not defined
			public ChunkStyle DiffHeaderOld { get; private set; }

			public const string DiffHeaderNewKey = "Diff Header(New)";
			[ColorDescription (DiffHeaderNewKey)] //not defined
			public ChunkStyle DiffHeaderNew { get; private set; }

			public const string DiffLocationKey = "Diff Location";
			[ColorDescription (DiffLocationKey)] //not defined
			public ChunkStyle DiffLocation { get; private set; }

			public const string PreviewDiffRemovedKey = "Preview Diff Removed Line";
			[ColorDescription (PreviewDiffRemovedKey)] //not defined
			public ChunkStyle PreviewDiffRemoved { get; private set; }

			public const string PreviewDiffAddeddKey = "Preview Diff Added Line";
			[ColorDescription (PreviewDiffAddeddKey)] //not defined
			public ChunkStyle PreviewDiffAddedd { get; private set; }

			public const string HtmlAttributeNameKey = "Html Attribute Name";
			[ColorDescription (HtmlAttributeNameKey, VSSetting = "HTML Attribute")]
			public ChunkStyle HtmlAttributeName { get; private set; }

			public const string HtmlAttributeValueKey = "Html Attribute Value";
			[ColorDescription (HtmlAttributeValueKey, VSSetting = "HTML Attribute Value")]
			public ChunkStyle HtmlAttributeValue { get; private set; }

			public const string HtmlCommentKey = "Html Comment";
			[ColorDescription (HtmlCommentKey, VSSetting = "HTML Comment")]
			public ChunkStyle HtmlComment { get; private set; }

			public const string HtmlElementNameKey = "Html Element Name";
			[ColorDescription (HtmlElementNameKey, VSSetting = "HTML Element Name")]
			public ChunkStyle HtmlElementName { get; private set; }

			public const string HtmlEntityKey = "Html Entity";
			[ColorDescription (HtmlEntityKey, VSSetting = "HTML Entity")]
			public ChunkStyle HtmlEntity { get; private set; }

			public const string HtmlOperatorKey = "Html Operator";
			[ColorDescription (HtmlOperatorKey, VSSetting = "HTML Operator")]
			public ChunkStyle HtmlOperator { get; private set; }

			public const string HtmlServerSideScriptKey = "Html Server-Side Script";
			[ColorDescription (HtmlServerSideScriptKey, VSSetting = "HTML Server-Side Script")]
			public ChunkStyle HtmlServerSideScript { get; private set; }

			public const string HtmlTagDelimiterKey = "Html Tag Delimiter";
			[ColorDescription (HtmlTagDelimiterKey, VSSetting = "HTML Tag Delimiter")]
			public ChunkStyle HtmlTagDelimiter { get; private set; }

			public const string RazorCodeKey = "Razor Code";
			[ColorDescription (RazorCodeKey, VSSetting = "Razor Code")]
			public ChunkStyle RazorCode { get; private set; }

			public const string CssCommentKey = "Css Comment";
			[ColorDescription (CssCommentKey, VSSetting = "CSS Comment")]
			public ChunkStyle CssComment { get; private set; }

			public const string CssPropertyNameKey = "Css Property Name";
			[ColorDescription (CssPropertyNameKey, VSSetting = "CSS Property Name")]
			public ChunkStyle CssPropertyName { get; private set; }

			public const string CssPropertyValueKey = "Css Property Value";
			[ColorDescription (CssPropertyValueKey, VSSetting = "CSS Property Value")]
			public ChunkStyle CssPropertyValue { get; private set; }

			public const string CssSelectorKey = "Css Selector";
			[ColorDescription (CssSelectorKey, VSSetting = "CSS Selector")]
			public ChunkStyle CssSelector { get; private set; }

			public const string CssStringValueKey = "Css String Value";
			[ColorDescription (CssStringValueKey, VSSetting = "CSS String Value")]
			public ChunkStyle CssStringValue { get; private set; }

			public const string CssKeywordKey = "Css Keyword";
			[ColorDescription (CssKeywordKey, VSSetting = "CSS Keyword")]
			public ChunkStyle CssKeyword { get; private set; }

			public const string ScriptCommentKey = "Script Comment";
			[ColorDescription (ScriptCommentKey, VSSetting = "Script Comment")]
			public ChunkStyle ScriptComment { get; private set; }

			public const string ScriptIdentifierKey = "Script Identifier";
			[ColorDescription (ScriptIdentifierKey, VSSetting = "Script Identifier")]
			public ChunkStyle ScriptIdentifier { get; private set; }

			public const string ScriptKeywordKey = "Script Keyword";
			[ColorDescription (ScriptKeywordKey, VSSetting = "Script Keyword")]
			public ChunkStyle ScriptKeyword { get; private set; }

			public const string ScriptNumberKey = "Script Number";
			[ColorDescription (ScriptNumberKey, VSSetting = "Script Number")]
			public ChunkStyle ScriptNumber { get; private set; }

			public const string ScriptOperatorKey = "Script Operator";
			[ColorDescription (ScriptOperatorKey, VSSetting = "Script Operator")]
			public ChunkStyle ScriptOperator { get; private set; }

			public const string ScriptStringKey = "Script String";
			[ColorDescription (ScriptStringKey, VSSetting = "Script String")]
			public ChunkStyle ScriptString { get; private set; }

			public const string RegexSetConstructsKey = "String(Regex Set Constructs)";
			[ColorDescription (RegexSetConstructsKey)]
			public ChunkStyle RegexSetConstructs { get; private set; }

			public const string RegexCharacterClassKey = "String(Regex Character Class)";
			[ColorDescription (RegexCharacterClassKey)]
			public ChunkStyle RegexCharacterClass { get; private set; }

			public const string RegexGroupingConstructsKey = "String(Regex Grouping Constructs)";
			[ColorDescription (RegexGroupingConstructsKey)]
			public ChunkStyle RegexGroupingConstructs { get; private set; }

			public const string RegexEscapeCharacterKey = "String(Regex Escape Character)";
			[ColorDescription (RegexEscapeCharacterKey)]
			public ChunkStyle RegexEscapeCharacter { get; private set; }

			public const string RegexAltEscapeCharacterKey = "String(Regex Alt Escape Character)";
			[ColorDescription (RegexAltEscapeCharacterKey)]
			public ChunkStyle RegexAltEscapeCharacter { get; private set; }
			#endregion

			public sealed class PropertyDescription
			{
				public readonly PropertyInfo Info;
				public readonly ColorDescriptionAttribute Attribute;

				public PropertyDescription (PropertyInfo info, ColorDescriptionAttribute attribute)
				{
					this.Info = info;
					this.Attribute = attribute;
				}
			}

			static Dictionary<string, PropertyDescription> textColors = new Dictionary<string, PropertyDescription> ();

			public static IEnumerable<PropertyDescription> TextColors {
				get {
					return textColors.Values;
				}
			}

			static Dictionary<string, PropertyDescription> ambientColors = new Dictionary<string, PropertyDescription> ();

			public static IEnumerable<PropertyDescription> AmbientColors {
				get {
					return ambientColors.Values;
				}
			}

			static readonly ColorScheme dark, light;
			static ColorScheme ()
			{
				foreach (var property in typeof (ColorScheme).GetProperties ()) {
					var description = property.GetCustomAttributes (false).FirstOrDefault (p => p is ColorDescriptionAttribute) as ColorDescriptionAttribute;
					if (description == null)
						continue;
					if (property.PropertyType == typeof (ChunkStyle)) {
						textColors.Add (description.Name, new PropertyDescription (property, description));
					} else {
						ambientColors.Add (description.Name, new PropertyDescription (property, description));
					}
				}
				using (var stream = typeof (ColorScheme).Assembly.GetManifestResourceStream ("DarkStyle.json"))
					dark = ColorScheme.LoadFrom (stream);
				using (var stream = typeof (ColorScheme).Assembly.GetManifestResourceStream ("FallbackStyle.json"))
					light = ColorScheme.LoadFrom (stream);
			}

			public ColorScheme Clone ()
			{
				var result = new ColorScheme () {
					Name = this.Name,
					BaseScheme = this.BaseScheme,
					Originator = this.Originator,
					Description = this.Description
				};
				result.CopyValues (this);
				return result;
			}

			static HslColor ParseColor (string value)
			{
				if (value.Length == 9 && value.StartsWith ("#", StringComparison.Ordinal)) {
					double r = ((double)int.Parse (value.Substring (1, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
					double g = ((double)int.Parse (value.Substring (3, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
					double b = ((double)int.Parse (value.Substring (5, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
					double a = ((double)int.Parse (value.Substring (7, 2), System.Globalization.NumberStyles.HexNumber)) / 255;
					return new Cairo.Color (r, g, b, a);
				}
				return HslColor.Parse (value);
			}

			public static HslColor ParsePaletteColor (Dictionary<string, HslColor> palette, string value)
			{
				HslColor result;
				if (palette.TryGetValue (value, out result))
					return result;
				return ParseColor (value);
			}

			void CopyValues (ColorScheme baseScheme)
			{
				foreach (var color in textColors.Values)
					color.Info.SetValue (this, color.Info.GetValue (baseScheme, null), null);
				foreach (var color in ambientColors.Values)
					color.Info.SetValue (this, color.Info.GetValue (baseScheme, null), null);
			}

			static ChunkStyle CreateChunkStyle (XElement element, Dictionary<string, HslColor> palette)
			{
				var result = new ChunkStyle ();

				foreach (var node in element.DescendantNodes ()) {
					if (node.NodeType == System.Xml.XmlNodeType.Element) {
						var el = (XElement)node;
						switch (el.Name.LocalName) {
						case "name":
							result.ScopeStack = new ScopeStack (el.Value);
							break;
						case "fore":
							result.Foreground = ColorScheme.ParsePaletteColor (palette, el.Value);
							break;
						case "back":
							result.Background = ColorScheme.ParsePaletteColor (palette, el.Value);
							break;
						case "weight":
							Xwt.Drawing.FontWeight weight;
							if (!Enum.TryParse<Xwt.Drawing.FontWeight> (el.Value, true, out weight))
								throw new InvalidDataException (el.Value + " is no valid text weight values are: " + string.Join (",", Enum.GetNames (typeof (Xwt.Drawing.FontWeight))));
							result.FontWeight = weight;
							break;
						case "style":
							Xwt.Drawing.FontStyle style;
							if (!Enum.TryParse<Xwt.Drawing.FontStyle> (el.Value, true, out style))
								throw new InvalidDataException (el.Value + " is no valid text weight values are: " + string.Join (",", Enum.GetNames (typeof (Xwt.Drawing.FontStyle))));
							result.FontStyle = style;
							break;
						default:
							throw new InvalidDataException ("Invalid element in text color:" + el.Name);
						}
					}
				}

				return result;
			}


			public static ColorScheme LoadFrom (Stream stream)
			{
				var result = new ColorScheme ();
				byte [] bytes;
				using (var sr = TextFileUtility.OpenStream (stream)) {
					bytes = System.Text.Encoding.UTF8.GetBytes (sr.ReadToEnd ());
				}

				var reader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader (bytes, new System.Xml.XmlDictionaryReaderQuotas ());

				var root = XElement.Load (reader);

				// The fields we'd like to extract
				result.Name = root.XPathSelectElement ("name").Value;

				if (result.Name != "")
					result.CopyValues (LoadFrom (Assembly.GetCallingAssembly ().GetManifestResourceStream ("FallbackStyle.json")));

				var version = Version.Parse (root.XPathSelectElement ("version").Value);
				if (version.Major != 1) {
					Console.WriteLine ("Can't load scheme : " + result.Name + " unsupported version:" + version);
					return null;
				}
				var el = root.XPathSelectElement ("description");
				if (el != null)
					result.Description = el.Value;
				el = root.XPathSelectElement ("originator");
				if (el != null)
					result.Originator = el.Value;
				el = root.XPathSelectElement ("baseScheme");
				if (el != null)
					result.BaseScheme = el.Value;


				var palette = new Dictionary<string, HslColor> ();
				foreach (var color in root.XPathSelectElements ("palette/*")) {
					var name = color.XPathSelectElement ("name").Value;
					if (palette.ContainsKey (name))
						throw new InvalidDataException ("Duplicate palette color definition for: " + name);
					palette.Add (
						name,
						ParseColor (color.XPathSelectElement ("value").Value)
					);
				}

				foreach (var colorElement in root.XPathSelectElements ("//colors/*")) {
					var color = AmbientColor.Create (colorElement, palette);
					PropertyDescription info;
					if (!ambientColors.TryGetValue (color.Name, out info)) {
						Console.WriteLine ("Ambient color:" + color.Name + " not found.");
						continue;
					}
					info.Info.SetValue (result, color, null);
				}

				foreach (var textColorElement in root.XPathSelectElements ("//text/*")) {
					var color = CreateChunkStyle (textColorElement, palette);
					PropertyDescription info;
					if (!textColors.TryGetValue (color.ScopeStack.Peek (), out info)) {
						Console.WriteLine ("Text color:" + color.ScopeStack + " not found.");
						continue;
					}
					info.Info.SetValue (result, color, null);
				}

				if (result.BaseScheme != null) {
					var defaultStyle = HslColor.Brightness (result.PlainText.Background) < 0.5 ? dark : light;
					foreach (var color in textColors.Values) {
						if (color.Info.GetValue (result, null) == null)
							color.Info.SetValue (result, color.Info.GetValue (defaultStyle, null), null);
					}
					foreach (var color in ambientColors.Values) {
						if (color.Info.GetValue (result, null) == null)
							color.Info.SetValue (result, color.Info.GetValue (defaultStyle, null), null);
					}
				}

				// Check scheme
				bool valid = true;
				foreach (var color in textColors.Values) {
					if (color.Info.GetValue (result, null) == null) {
						Console.WriteLine (color.Attribute.Name + " == null");
						valid = false;
					}
				}
				foreach (var color in ambientColors.Values) {
					if (color.Info.GetValue (result, null) == null) {
						Console.WriteLine (color.Attribute.Name + " == null");
						valid = false;
					}
				}
				if (!valid)
					throw new InvalidDataException ("Scheme " + result.Name + " is not valid.");
				return result;
			}

			public static string ColorToMarkup (Cairo.Color color)
			{
				var r = (byte)(color.R * byte.MaxValue);
				var g = (byte)(color.G * byte.MaxValue);
				var b = (byte)(color.B * byte.MaxValue);
				var a = (byte)(color.A * byte.MaxValue);

				if (a == 255)
					return string.Format ("#{0:X2}{1:X2}{2:X2}", r, g, b);
				return string.Format ("#{0:X2}{1:X2}{2:X2}{3:X2}", r, g, b, a);
			}

			internal static HslColor ImportVsColor (string colorString)
			{
				if (colorString == "0x02000000")
					return new Cairo.Color (0, 0, 0, 0);
				string color = "#" + colorString.Substring (8, 2) + colorString.Substring (6, 2) + colorString.Substring (4, 2);
				return HslColor.Parse (color);
			}

			public sealed class VSSettingColor
			{
				public string Name { get; private set; }
				public string Foreground { get; private set; }
				public string Background { get; private set; }
				public bool BoldFont { get; private set; }

				public static VSSettingColor Create (XmlReader reader)
				{
					return new VSSettingColor {
						Name = reader.GetAttribute ("Name"),
						Foreground = reader.GetAttribute ("Foreground"),
						Background = reader.GetAttribute ("Background"),
						BoldFont = reader.GetAttribute ("BoldFont") == "Yes"
					};
				}
			}

			public static Cairo.Color AlphaBlend (Cairo.Color fore, Cairo.Color back, double alpha)
			{
				return new Cairo.Color (
					(1.0 - alpha) * back.R + alpha * fore.R,
					(1.0 - alpha) * back.G + alpha * fore.G,
					(1.0 - alpha) * back.B + alpha * fore.B);
			}

			public static ColorScheme Import (string fileName, Stream stream)
			{
				var result = new ColorScheme ();
				result.Name = Path.GetFileNameWithoutExtension (fileName);
				result.Description = "Imported color scheme";
				result.Originator = "Imported from " + fileName;

				var colors = new Dictionary<string, VSSettingColor> ();
				using (var reader = XmlReader.Create (stream)) {
					while (reader.Read ()) {
						if (reader.LocalName == "Item") {
							var color = VSSettingColor.Create (reader);
							if (colors.ContainsKey (color.Name)) {
								Console.WriteLine ("Warning: {0} is defined twice in vssettings.", color.Name);
								continue;
							}
							colors [color.Name] = color;
						}
					}
				}


				HashSet<string> importedAmbientColors = new HashSet<string> ();
				// convert ambient colors
				foreach (var ambient in ambientColors.Values) {
					if (!string.IsNullOrEmpty (ambient.Attribute.VSSetting)) {
						var import = AmbientColor.Import (colors, ambient.Attribute.VSSetting);
						if (import != null) {
							importedAmbientColors.Add (import.Name);
							ambient.Info.SetValue (result, import, null);
							continue;
						}
					}
				}

				// convert text colors
				foreach (var vsc in colors.Values) {
					bool found = false;
					foreach (var color in textColors) {
						if (color.Value.Attribute.VSSetting == null)
							continue;
						var split = color.Value.Attribute.VSSetting.Split ('?');
						foreach (var s in split) {
							if (s == vsc.Name) {
								/*	if (vsc.Foreground == "0x02000000" && vsc.Background == "0x02000000") {
										color.Value.Info.SetValue (result, result.PlainText, null);
										found = true;
										continue;
									}*/
								var textColor = ImportChunkStyle (color.Value.Attribute.Name, vsc);
								if (textColor != null) {
									color.Value.Info.SetValue (result, textColor, null);
									found = true;
								}
							}
						}
					}
					if (!found && !importedAmbientColors.Contains (vsc.Name))
						Console.WriteLine (vsc.Name + " not imported!");
				}

				if (result.PlainText == null)
					throw new StyleImportException (StyleImportException.ImportFailReason.NoValidColorsFound);

				result.IndentationGuide = new AmbientColor ();
				result.IndentationGuide.Colors.Add (Tuple.Create ("color", (HslColor)AlphaBlend (result.PlainText.Foreground, result.PlainText.Background, 0.3)));

				result.TooltipText = result.PlainText.Clone ();
				var h = (HslColor)result.TooltipText.Background;
				h.L += 0.01;
				result.TooltipText.Background = h;

				result.TooltipPager = new AmbientColor ();
				result.TooltipPager.Colors.Add (Tuple.Create ("color", result.TooltipText.Background));

				result.TooltipPagerTriangle = new AmbientColor ();
				result.TooltipPagerTriangle.Colors.Add (Tuple.Create ("color", (HslColor)AlphaBlend (result.PlainText.Foreground, result.PlainText.Background, 0.8)));

				var defaultStyle = HslColor.Brightness (result.PlainText.Background) < 0.5 ? dark : light;
				foreach (var color in textColors.Values) {
					if (color.Info.GetValue (result, null) == null)
						color.Info.SetValue (result, color.Info.GetValue (defaultStyle, null), null);
				}
				foreach (var color in ambientColors.Values) {
					if (color.Info.GetValue (result, null) == null)
						color.Info.SetValue (result, color.Info.GetValue (defaultStyle, null), null);
				}
				if (result.PlainText.TransparentForeground)
					result.PlainText.Foreground = new Cairo.Color (0, 0, 0);
				return result;
			}

			internal static ChunkStyle ImportChunkStyle (string name, VSSettingColor vsc)
			{
				var textColor = new ChunkStyle ();
				textColor.ScopeStack = new ScopeStack (name);
				if (!string.IsNullOrEmpty (vsc.Foreground) && vsc.Foreground != "0x02000000") {
					textColor.Foreground = ColorScheme.ImportVsColor (vsc.Foreground);
					if (textColor.TransparentForeground && name != "Selected Text" && name != "Selected Text(Inactive)")
						textColor.Foreground = new HslColor (0, 0, 0);
				}
				if (!string.IsNullOrEmpty (vsc.Background) && vsc.Background != "0x02000000")
					textColor.Background = ColorScheme.ImportVsColor (vsc.Background);
				if (vsc.BoldFont)
					textColor.FontWeight = Xwt.Drawing.FontWeight.Bold;
				return textColor;
			}

			public Cairo.Color GetForeground (ChunkStyle chunkStyle)
			{
				if (chunkStyle.TransparentForeground)
					return PlainText.Foreground;
				return chunkStyle.Foreground;
			}
		}
	}
}
