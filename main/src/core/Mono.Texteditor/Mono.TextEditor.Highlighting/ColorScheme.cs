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
using Xwt.Drawing;

namespace Mono.TextEditor.Highlighting
{
	public class ColorScheme
	{
		public string Name { get; set; }

		public string Description { get; set; }

		public string Originator { get; set; }

		public string BaseScheme { get; set; }

		public string FileName { get; set; }

		#region Ambient Colors

		[ColorDescription ("Background(Read Only)", GroupName = GroupNames.Other, VSSetting = "color=Plain Text/Background")]
		public AmbientColor BackgroundReadOnly { get; private set; }

		[ColorDescription ("Search result background", GroupName = GroupNames.Other)]
		public AmbientColor SearchResult { get; private set; }

		[ColorDescription ("Search result background (highlighted)", GroupName = GroupNames.Other)]
		public AmbientColor SearchResultMain { get; private set; }

		[ColorDescription ("Fold Square", GroupName = GroupNames.Other, VSSetting = "color=outlining.verticalrule/Foreground")]
		public AmbientColor FoldLineColor { get; private set; }

		[ColorDescription ("Fold Cross", GroupName = GroupNames.Other, VSSetting = "color=outlining.square/Foreground,secondcolor=outlining.square/Background")]
		public AmbientColor FoldCross { get; private set; }

		[ColorDescription ("Indentation Guide", GroupName = GroupNames.Other)] // not defined
		public AmbientColor IndentationGuide { get; private set; }

		[ColorDescription ("Indicator Margin", GroupName = GroupNames.Other, VSSetting = "color=Indicator Margin/Background")]
		public AmbientColor IndicatorMargin { get; private set; }

		[ColorDescription ("Indicator Margin(Separator)", GroupName = GroupNames.Other, VSSetting = "color=Indicator Margin/Background")]
		public AmbientColor IndicatorMarginSeparator { get; private set; }

		[ColorDescription ("Tooltip Border", GroupName = GroupNames.Tooltip)]
		public AmbientColor TooltipBorder { get; private set; }

		[ColorDescription ("Tooltip Pager Top", GroupName = GroupNames.Tooltip)]
		public AmbientColor TooltipPagerTop { get; private set; }

		[ColorDescription ("Tooltip Pager Bottom", GroupName = GroupNames.Tooltip)]
		public AmbientColor TooltipPagerBottom { get; private set; }

		[ColorDescription ("Tooltip Pager Triangle", GroupName = GroupNames.Tooltip)]
		public AmbientColor TooltipPagerTriangle { get; private set; }

		[ColorDescription ("Tooltip Pager Text", GroupName = GroupNames.Tooltip)]
		public AmbientColor TooltipPagerText { get; private set; }

		[ColorDescription ("Notification Border", GroupName = GroupNames.Other)]
		public AmbientColor NotificationBorder { get; private set; }

		[ColorDescription ("Bookmarks", GroupName = GroupNames.Other)]
		public AmbientColor Bookmarks { get; private set; }

		[ColorDescription ("Underline(Error)", GroupName = GroupNames.ErrorWarning, VSSetting = "color=Syntax Error/Foreground")]
		public AmbientColor UnderlineError { get; private set; }

		[ColorDescription ("Underline(Warning)", GroupName = GroupNames.ErrorWarning, VSSetting = "color=Warning/Foreground")]
		public AmbientColor UnderlineWarning { get; private set; }

		[ColorDescription ("Underline(Suggestion)", GroupName = GroupNames.ErrorWarning, VSSetting = "color=Other Error/Foreground")]
		public AmbientColor UnderlineSuggestion { get; private set; }

		[ColorDescription ("Underline(Hint)", GroupName = GroupNames.ErrorWarning, VSSetting = "color=Other Error/Foreground")]
		public AmbientColor UnderlineHint { get; private set; }

		[ColorDescription ("Quick Diff(Dirty)", GroupName = GroupNames.Diffs)]
		public AmbientColor QuickDiffDirty { get; private set; }

		[ColorDescription ("Quick Diff(Changed)", GroupName = GroupNames.Diffs)]
		public AmbientColor QuickDiffChanged { get; private set; }

		[ColorDescription ("Brace Matching(Rectangle)", GroupName = GroupNames.Refactoring, VSSetting = "color=Brace Matching (Rectangle)/Background,secondcolor=Brace Matching (Rectangle)/Foreground")]
		public AmbientColor BraceMatchingRectangle { get; private set; }

		[ColorDescription ("Usages(Rectangle)", GroupName = GroupNames.Refactoring, VSSetting = "color=MarkerFormatDefinition/HighlightedReference/Background,secondcolor=MarkerFormatDefinition/HighlightedReference/Background,bordercolor=MarkerFormatDefinition/HighlightedReference/Background")]
		public AmbientColor UsagesRectangle { get; private set; }

		[ColorDescription ("Changing usages(Rectangle)", GroupName = GroupNames.Refactoring, VSSetting = "color=MarkerFormatDefinition/HighlightedReference/Background,secondcolor=MarkerFormatDefinition/HighlightedReference/Background,bordercolor=MarkerFormatDefinition/HighlightedReference/Background")]
		public AmbientColor ChangingUsagesRectangle { get; private set; }

		[ColorDescription ("Breakpoint Marker", VSSetting = "color=Breakpoint (Enabled)/Background", GroupName = GroupNames.Debugging)]
		public AmbientColor BreakpointMarker { get; private set; }

		[ColorDescription ("Breakpoint Marker(Invalid)", VSSetting = "color=Breakpoint (Disabled)/Background", GroupName = GroupNames.Debugging)]
		public AmbientColor BreakpointMarkerInvalid { get; private set; }

		[ColorDescription ("Breakpoint Marker(Disabled)", GroupName = GroupNames.Debugging)]
		public AmbientColor BreakpointMarkerDisabled { get; private set; }

		[ColorDescription ("Debugger Current Line Marker", VSSetting = "color=Current Statement/Background", GroupName = GroupNames.Debugging)]
		public AmbientColor DebuggerCurrentLineMarker { get; private set; }

		[ColorDescription ("Debugger Stack Line Marker", GroupName = GroupNames.Debugging)]
		public AmbientColor DebuggerStackLineMarker { get; private set; }

		[ColorDescription ("Primary Link", GroupName = GroupNames.Refactoring, VSSetting = "color=Refactoring Dependent Field/Background")]
		public AmbientColor PrimaryTemplate { get; private set; }

		[ColorDescription ("Primary Link(Highlighted)", GroupName = GroupNames.Refactoring, VSSetting = "color=Refactoring Current Field/Background")]
		public AmbientColor PrimaryTemplateHighlighted { get; private set; }

		[ColorDescription ("Secondary Link", GroupName = GroupNames.Refactoring)] // not defined
		public AmbientColor SecondaryTemplate { get; private set; }

		[ColorDescription ("Secondary Link(Highlighted)", GroupName = GroupNames.Refactoring)] // not defined
		public AmbientColor SecondaryTemplateHighlighted { get; private set; }

		[ColorDescription ("Current Line Marker", GroupName = GroupNames.Other, VSSetting = "color=CurrentLineActiveFormat/Background,secondcolor=CurrentLineActiveFormat/Foreground")]
		public AmbientColor LineMarker { get; private set; }

		[ColorDescription ("Current Line Marker(Inactive)", GroupName = GroupNames.Other, VSSetting = "color=CurrentLineInactiveFormat/Background,secondcolor=CurrentLineInactiveFormat/Foreground")]
		public AmbientColor LineMarkerInactive { get; private set; }

		[ColorDescription ("Column Ruler", GroupName = GroupNames.Other)] // not defined
		public AmbientColor Ruler { get; private set; }

		[ColorDescription ("Completion Matching Substring", GroupName = GroupNames.Completion)]
		public AmbientColor CompletionHighlight { get; private set; }

		[ColorDescription ("Completion Border", GroupName = GroupNames.Completion)]
		public AmbientColor CompletionBorder { get; private set; }

		[ColorDescription ("Completion Border(Inactive)", GroupName = GroupNames.Completion)]
		public AmbientColor CompletionInactiveBorder { get; private set; }

		[ColorDescription ("Message Bubble Error Marker", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleErrorMarker { get; private set; }

		[ColorDescription ("Message Bubble Error Tag", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleErrorTag { get; private set; }

		[ColorDescription ("Message Bubble Error Tooltip", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleErrorTooltip { get; private set; }

		[ColorDescription ("Message Bubble Error Line", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleErrorLine { get; private set; }

		[ColorDescription ("Message Bubble Error Counter", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleErrorCounter { get; private set; }

		[ColorDescription ("Message Bubble Error IconMargin", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleErrorIconMargin { get; private set; }

		[ColorDescription ("Message Bubble Warning Marker", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleWarningMarker { get; private set; }

		[ColorDescription ("Message Bubble Warning Tag", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleWarningTag { get; private set; }

		[ColorDescription ("Message Bubble Warning Tooltip", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleWarningTooltip { get; private set; }

		[ColorDescription ("Message Bubble Warning Line", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleWarningLine { get; private set; }

		[ColorDescription ("Message Bubble Warning Counter", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleWarningCounter { get; private set; }

		[ColorDescription ("Message Bubble Warning IconMargin", GroupName = GroupNames.ErrorWarning)]
		public AmbientColor MessageBubbleWarningIconMargin { get; private set; }

		#endregion

		#region Text Colors

		[ColorDescription ("Plain Text", VSSetting = "Plain Text", GroupName = GroupNames.CSharp)]
		public ChunkStyle PlainText { get; private set; }

		[ColorDescription ("Selected Text", VSSetting = "Selected Text", GroupName = GroupNames.CSharp)]
		public ChunkStyle SelectedText { get; private set; }

		[ColorDescription ("Selected Text(Inactive)", VSSetting = "Inactive Selected Text", GroupName = GroupNames.CSharp)]
		public ChunkStyle SelectedInactiveText { get; private set; }

		[ColorDescription ("Collapsed Text", VSSetting = "Collapsible Text", GroupName = GroupNames.CSharp)]
		public ChunkStyle CollapsedText { get; private set; }

		[ColorDescription ("Line Numbers", VSSetting = "Line Numbers", GroupName = GroupNames.CSharp)]
		public ChunkStyle LineNumbers { get; private set; }

		[ColorDescription ("Punctuation", VSSetting = "Operator", GroupName = GroupNames.CSharp)]
		public ChunkStyle Punctuation { get; private set; }

		[ColorDescription ("Punctuation(Brackets)", VSSetting = "Plain Text", GroupName = GroupNames.CSharp)]
		public ChunkStyle PunctuationForBrackets { get; private set; }

		[ColorDescription ("Comment(Line)", VSSetting = "Comment", GroupName = GroupNames.CSharp)]
		public ChunkStyle CommentsSingleLine { get; private set; }

		[ColorDescription ("Comment(Block)", VSSetting = "Comment", GroupName = GroupNames.CSharp)]
		public ChunkStyle CommentsMultiLine { get; private set; }

		[ColorDescription ("Comment(Doc)", VSSetting = "XML Doc Comment", GroupName = GroupNames.CSharp)]
		public ChunkStyle CommentsForDocumentation { get; private set; }

		[ColorDescription ("Comment(DocTag)", VSSetting = "XML Doc Tag", GroupName = GroupNames.CSharp)]
		public ChunkStyle CommentsForDocumentationTags { get; private set; }

		[ColorDescription ("Comment Tag", VSSetting = "Comment", GroupName = GroupNames.CSharp)]
		public ChunkStyle CommentTags { get; private set; }

		[ColorDescription ("Excluded Code", VSSetting = "Excluded Code", GroupName = GroupNames.CSharp)]
		public ChunkStyle ExcludedCode { get; private set; }

		[ColorDescription ("String", VSSetting = "String", GroupName = GroupNames.CSharp)]
		public ChunkStyle String { get; private set; }

		[ColorDescription ("String(Escape)", VSSetting = "String", GroupName = GroupNames.CSharp)]
		public ChunkStyle StringEscapeSequence { get; private set; }

		[ColorDescription ("String(C# @ Verbatim)", VSSetting = "String(C# @ Verbatim)", GroupName = GroupNames.CSharp)]
		public ChunkStyle StringVerbatim { get; private set; }

		[ColorDescription ("Number", VSSetting = "Number", GroupName = GroupNames.CSharp)]
		public ChunkStyle Number { get; private set; }

		[ColorDescription ("Preprocessor", VSSetting = "Preprocessor Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle Preprocessor { get; private set; }

		[ColorDescription ("Preprocessor(Region Name)", VSSetting = "Plain Text", GroupName = GroupNames.CSharp)]
		public ChunkStyle PreprocessorRegionName { get; private set; }

		[ColorDescription ("Xml Text", VSSetting = "XML Text", GroupName = GroupNames.XML)]
		public ChunkStyle XmlText { get; private set; }

		[ColorDescription ("Xml Delimiter", VSSetting = "XML Delimiter", GroupName = GroupNames.XML)]
		public ChunkStyle XmlDelimiter { get; private set; }

		[ColorDescription ("Xml Name", VSSetting = "XML Name", GroupName = GroupNames.XML)]
		public ChunkStyle XmlName { get; private set; }

		[ColorDescription ("Xml Attribute", VSSetting = "XML Attribute", GroupName = GroupNames.XML)]
		public ChunkStyle XmlAttribute { get; private set; }

		[ColorDescription ("Xml Attribute Quotes", VSSetting = "XML Attribute Quotes", GroupName = GroupNames.XML)]
		public ChunkStyle XmlAttributeQuotes { get; private set; }

		[ColorDescription ("Xml Attribute Value", VSSetting = "XML Attribute Value", GroupName = GroupNames.XML)]
		public ChunkStyle XmlAttributeValue { get; private set; }

		[ColorDescription ("Xml Comment", VSSetting = "XML Comment", GroupName = GroupNames.XML)]
		public ChunkStyle XmlComment { get; private set; }

		[ColorDescription ("Xml CData Section", VSSetting = "XML CData Section", GroupName = GroupNames.XML)]
		public ChunkStyle XmlCDataSection { get; private set; }

		[ColorDescription ("Tooltip Text", GroupName = GroupNames.Tooltip)] // not defined in vs.net
		public ChunkStyle TooltipText { get; private set; }

		[ColorDescription ("Notification Text", GroupName = GroupNames.Other)] // not defined in vs.net
		public ChunkStyle NotificationText { get; private set; }

		[ColorDescription ("Completion Text", GroupName = GroupNames.Completion)] //not defined in vs.net
		public ChunkStyle CompletionText { get; private set; }

		[ColorDescription ("Completion Selected Text", GroupName = GroupNames.Completion)] //not defined in vs.net
		public ChunkStyle CompletionSelectedText { get; private set; }

		[ColorDescription ("Completion Selected Text(Inactive)", GroupName = GroupNames.Completion)] //not defined in vs.net
		public ChunkStyle CompletionSelectedInactiveText { get; private set; }

		[ColorDescription ("Keyword(Access)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordAccessors { get; private set; }

		[ColorDescription ("Keyword(Type)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordTypes { get; private set; }

		[ColorDescription ("Keyword(Operator)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordOperators { get; private set; }

		[ColorDescription ("Keyword(Selection)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordSelection { get; private set; }

		[ColorDescription ("Keyword(Iteration)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordIteration { get; private set; }

		[ColorDescription ("Keyword(Jump)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordJump { get; private set; }

		[ColorDescription ("Keyword(Context)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordContext { get; private set; }

		[ColorDescription ("Keyword(Exception)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordException { get; private set; }

		[ColorDescription ("Keyword(Modifiers)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordModifiers { get; private set; }

		[ColorDescription ("Keyword(Constants)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordConstants { get; private set; }

		[ColorDescription ("Keyword(Void)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordVoid { get; private set; }

		[ColorDescription ("Keyword(Namespace)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordNamespace { get; private set; }

		[ColorDescription ("Keyword(Property)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordProperty { get; private set; }

		[ColorDescription ("Keyword(Declaration)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordDeclaration { get; private set; }

		[ColorDescription ("Keyword(Parameter)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordParameter { get; private set; }

		[ColorDescription ("Keyword(Operator Declaration)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordOperatorDeclaration { get; private set; }

		[ColorDescription ("Keyword(Other)", VSSetting = "Keyword", GroupName = GroupNames.CSharp)]
		public ChunkStyle KeywordOther { get; private set; }

		[ColorDescription ("User Types", VSSetting = "User Types", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserTypes { get; private set; }

		[ColorDescription ("User Types(Enums)", VSSetting = "User Types(Enums)", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserTypesEnums { get; private set; }

		[ColorDescription ("User Types(Interfaces)", VSSetting = "User Types(Interfaces)", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserTypesInterfaces { get; private set; }

		[ColorDescription ("User Types(Delegates)", VSSetting = "User Types(Delegates)", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserTypesDelegatess { get; private set; }

		[ColorDescription ("User Types(Value types)", VSSetting = "User Types(Value types)", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserTypesValueTypes { get; private set; }

		[ColorDescription ("User Types(Type parameters)", VSSetting = "User Types(Type parameters)", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserTypesTypeParameters { get; private set; }

		[ColorDescription ("User Field Usage", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserFieldUsage { get; private set; }

		[ColorDescription ("User Field Declaration", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserFieldDeclaration { get; private set; }

		[ColorDescription ("User Property Usage", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserPropertyUsage { get; private set; }

		[ColorDescription ("User Property Declaration", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserPropertyDeclaration { get; private set; }

		[ColorDescription ("User Event Usage", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserEventUsage { get; private set; }

		[ColorDescription ("User Event Declaration", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserEventDeclaration { get; private set; }

		[ColorDescription ("User Method Usage", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserMethodUsage { get; private set; }

		[ColorDescription ("User Method Declaration", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserMethodDeclaration { get; private set; }

		[ColorDescription ("User Parameter Usage", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserParameterUsage { get; private set; }

		[ColorDescription ("User Parameter Declaration", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserParameterDeclaration { get; private set; }

		[ColorDescription ("User Variable Usage", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserVariableUsage { get; private set; }

		[ColorDescription ("User Variable Declaration", VSSetting = "Identifier", GroupName = GroupNames.CSharp)]
		public ChunkStyle UserVariableDeclaration { get; private set; }

		[ColorDescription ("Syntax Error", VSSetting = "Syntax Error", GroupName = GroupNames.CSharp)]
		public ChunkStyle SyntaxError { get; private set; }

		[ColorDescription ("String Format Items", VSSetting = "String", GroupName = GroupNames.CSharp)]
		public ChunkStyle StringFormatItems { get; private set; }

		[ColorDescription ("Breakpoint Text", VSSetting = "Breakpoint (Enabled)", GroupName = GroupNames.Debugging)]
		public ChunkStyle BreakpointText { get; private set; }

		[ColorDescription ("Debugger Current Statement", VSSetting = "Current Statement", GroupName = GroupNames.Debugging)]
		public ChunkStyle DebuggerCurrentLine { get; private set; }

		[ColorDescription ("Debugger Stack Line", GroupName = GroupNames.Debugging)] // not defined
		public ChunkStyle DebuggerStackLine { get; private set; }

		[ColorDescription ("Diff Line(Added)", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffLineAdded { get; private set; }

		[ColorDescription ("Diff Line(Removed)", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffLineRemoved { get; private set; }

		[ColorDescription ("Diff Line(Changed)", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffLineChanged { get; private set; }

		[ColorDescription ("Diff Header", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffHeader { get; private set; }

		[ColorDescription ("Diff Header(Separator)", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffHeaderSeparator { get; private set; }

		[ColorDescription ("Diff Header(Old)", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffHeaderOld { get; private set; }

		[ColorDescription ("Diff Header(New)", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffHeaderNew { get; private set; }

		[ColorDescription ("Diff Location", GroupName = GroupNames.Diffs)] //not defined
		public ChunkStyle DiffLocation { get; private set; }

		[ColorDescription ("Html Attribute Name", VSSetting = "HTML Attribute", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlAttributeName { get; private set; }

		[ColorDescription ("Html Attribute Value", VSSetting = "HTML Attribute Value", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlAttributeValue { get; private set; }

		[ColorDescription ("Html Comment", VSSetting = "HTML Comment", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlComment { get; private set; }

		[ColorDescription ("Html Element Name", VSSetting = "HTML Element Name", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlElementName { get; private set; }

		[ColorDescription ("Html Entity", VSSetting = "HTML Entity", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlEntity { get; private set; }

		[ColorDescription ("Html Operator", VSSetting = "HTML Operator", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlOperator { get; private set; }

		[ColorDescription ("Html Server-Side Script", VSSetting = "HTML Server-Side Script", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlServerSideScript { get; private set; }

		[ColorDescription ("Html Tag Delimiter", VSSetting = "HTML Tag Delimiter", GroupName = GroupNames.HTML)]
		public ChunkStyle HtmlTagDelimiter { get; private set; }

		[ColorDescription ("Razor Code", VSSetting = "Razor Code", GroupName = GroupNames.HTML)]
		public ChunkStyle RazorCode { get; private set; }

		[ColorDescription ("Css Comment", VSSetting = "CSS Comment", GroupName = GroupNames.CSS)]
		public ChunkStyle CssComment { get; private set; }

		[ColorDescription ("Css Property Name", VSSetting = "CSS Property Name", GroupName = GroupNames.CSS)]
		public ChunkStyle CssPropertyName { get; private set; }

		[ColorDescription ("Css Property Value", VSSetting = "CSS Property Value", GroupName = GroupNames.CSS)]
		public ChunkStyle CssPropertyValue { get; private set; }

		[ColorDescription ("Css Selector", VSSetting = "CSS Selector", GroupName = GroupNames.CSS)]
		public ChunkStyle CssSelector { get; private set; }

		[ColorDescription ("Css String Value", VSSetting = "CSS String Value", GroupName = GroupNames.CSS)]
		public ChunkStyle CssStringValue { get; private set; }

		[ColorDescription ("Css Keyword", VSSetting = "CSS Keyword", GroupName = GroupNames.CSS)]
		public ChunkStyle CssKeyword { get; private set; }

		[ColorDescription ("Script Comment", VSSetting = "Script Comment", GroupName = GroupNames.Script)]
		public ChunkStyle ScriptComment { get; private set; }

		[ColorDescription ("Script Identifier", VSSetting = "Script Identifier", GroupName = GroupNames.Script)]
		public ChunkStyle ScriptIdentifier { get; private set; }

		[ColorDescription ("Script Keyword", VSSetting = "Script Keyword", GroupName = GroupNames.Script)]
		public ChunkStyle ScriptKeyword { get; private set; }

		[ColorDescription ("Script Number", VSSetting = "Script Number", GroupName = GroupNames.Script)]
		public ChunkStyle ScriptNumber { get; private set; }

		[ColorDescription ("Script Operator", VSSetting = "Script Operator", GroupName = GroupNames.Script)]
		public ChunkStyle ScriptOperator { get; private set; }

		[ColorDescription ("Script String", VSSetting = "Script String", GroupName = GroupNames.Script)]
		public ChunkStyle ScriptString { get; private set; }

		#endregion

		public class PropertyDecsription
		{
			public readonly PropertyInfo Info;
			public readonly ColorDescriptionAttribute Attribute;

			public PropertyDecsription (PropertyInfo info, ColorDescriptionAttribute attribute)
			{
				this.Info = info;
				this.Attribute = attribute;
			}
		}

		static Dictionary<string, PropertyDecsription> textColors = new Dictionary<string, PropertyDecsription> ();

		public static IEnumerable<PropertyDecsription> TextColors {
			get {
				return textColors.Values;
			}
		}

		static Dictionary<string, PropertyDecsription> ambientColors = new Dictionary<string, PropertyDecsription> ();

		public static IEnumerable<PropertyDecsription> AmbientColors {
			get {
				return ambientColors.Values;
			}
		}

		static ColorScheme ()
		{
			foreach (var property in typeof(ColorScheme).GetProperties ()) {
				var description = property.GetCustomAttributes (false).FirstOrDefault (p => p is ColorDescriptionAttribute) as ColorDescriptionAttribute;
				if (description == null)
					continue;
				if (property.PropertyType == typeof(ChunkStyle)) {
					textColors.Add (description.Name, new PropertyDecsription (property, description));
				} else {
					ambientColors.Add (description.Name, new PropertyDecsription (property, description));
				}
			}
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

		static Cairo.Color ParseColor (string value)
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

		public static Cairo.Color ParsePaletteColor (Dictionary<string, Cairo.Color> palette, string value)
		{
			Cairo.Color result;
			if (palette.TryGetValue (value, out result))
				return result;
			return ParseColor (value);
		}

		public ChunkStyle GetChunkStyle (Chunk chunk)
		{
			return GetChunkStyle (chunk.Style);
		}

		public ChunkStyle GetChunkStyle (string color)
		{
			if (color == null)
				return GetChunkStyle ("Plain Text");
			PropertyDecsription val;
			if (!textColors.TryGetValue (color, out val)) {
				Console.WriteLine ("Chunk style : " + color + " is undefined.");
				return GetChunkStyle ("Plain Text");
			}
			return val.Info.GetValue (this, null) as ChunkStyle;
		}

		void CopyValues (ColorScheme baseScheme)
		{
			foreach (var color in textColors.Values)
				color.Info.SetValue (this, color.Info.GetValue (baseScheme, null), null);
			foreach (var color in ambientColors.Values)
				color.Info.SetValue (this, color.Info.GetValue (baseScheme, null), null);
		}

		public static ColorScheme LoadFrom (Stream stream)
		{
			var result = new ColorScheme ();
			var reader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader (stream, new System.Xml.XmlDictionaryReaderQuotas ());

			var root = XElement.Load (reader);
			
			// The fields we'd like to extract
			result.Name = root.XPathSelectElement ("name").Value;

			if (result.Name != TextEditorOptions.DefaultColorStyle)
				result.CopyValues (SyntaxModeService.DefaultColorStyle);

			var version = Version.Parse (root.XPathSelectElement ("version").Value);
			if (version.Major != 1)
				return null;
			var el = root.XPathSelectElement ("description");
			if (el != null)
				result.Description = el.Value;
			el = root.XPathSelectElement ("originator");
			if (el != null)
				result.Originator = el.Value;
			el = root.XPathSelectElement ("baseScheme");
			if (el != null)
				result.BaseScheme = el.Value;

			if (result.BaseScheme != null) {
				var baseScheme = SyntaxModeService.GetColorStyle (result.BaseScheme);
				if (baseScheme != null)
					result.CopyValues (baseScheme);
			}

			var palette = new Dictionary<string, Cairo.Color> ();
			foreach (var color in root.XPathSelectElements("palette/*")) {
				var name = color.XPathSelectElement ("name").Value;
				if (palette.ContainsKey (name))
					throw new InvalidDataException ("Duplicate palette color definition for: " + name);
				palette.Add (
					name,
					ParseColor (color.XPathSelectElement ("value").Value)
				);
			}

			foreach (var colorElement in root.XPathSelectElements("//colors/*")) {
				var color = AmbientColor.Create (colorElement, palette);
				PropertyDecsription info;
				if (!ambientColors.TryGetValue (color.Name, out info)) {
					Console.WriteLine ("Ambient color:" + color.Name + " not found.");
					continue;
				}
				info.Info.SetValue (result, color, null);
			}

			foreach (var textColorElement in root.XPathSelectElements("//text/*")) {
				var color = ChunkStyle.Create (textColorElement, palette);
				PropertyDecsription info;
				if (!textColors.TryGetValue (color.Name, out info)) {
					Console.WriteLine ("Text color:" + color.Name + " not found.");
					continue;
				}
				info.Info.SetValue (result, color, null);
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


		public void Save (string fileName)
		{
			using (var writer = new StreamWriter (fileName)) {
				writer.WriteLine ("{");
				writer.WriteLine ("\t\"name\":\"{0}\",", Name);
				writer.WriteLine ("\t\"version\":\"1.0\",");
				if (!string.IsNullOrEmpty (Description))
					writer.WriteLine ("\t\"description\":\"{0}\",", Description);
				if (!string.IsNullOrEmpty (Originator))
					writer.WriteLine ("\t\"originator\":\"{0}\",", Originator);
				if (!string.IsNullOrEmpty (BaseScheme))
					writer.WriteLine ("\t\"baseScheme\":\"{0}\",", BaseScheme);

				var baseStyle = SyntaxModeService.GetColorStyle (BaseScheme ?? TextEditorOptions.DefaultColorStyle);

				writer.WriteLine ("\t\"colors\":[");
				bool first = true;
				foreach (var ambient in ambientColors) {
					var thisValue = ambient.Value.Info.GetValue (this, null) as AmbientColor;
					if (thisValue == null)
						continue;
					var baseValue = ambient.Value.Info.GetValue (baseStyle, null) as AmbientColor;
					if (thisValue.Equals (baseValue)) {
						continue;
					}

					var colorString = new StringBuilder ();
					foreach (var color in thisValue.Colors) {
						if (colorString.Length > 0)
							colorString.Append (", ");
						colorString.Append (string.Format ("\"{0}\":\"{1}\"", color.Item1, ColorToMarkup (color.Item2)));
					}
					if (colorString.Length == 0) {
						Console.WriteLine ("Invalid ambient color :" + thisValue);
						continue;
					}
					if (!first) {
						writer.WriteLine (",");
					} else {
						first = false;
					}
					writer.Write ("\t\t{");
					writer.Write ("\"name\": \"{0}\", {1}", ambient.Value.Attribute.Name, colorString);
					writer.Write (" }");
				}

				writer.WriteLine ("\t],");
				first = true;
				writer.WriteLine ("\t\"text\":[");
				foreach (var textColor in textColors) {
					var thisValue = textColor.Value.Info.GetValue (this, null) as ChunkStyle;
					if (thisValue == null)
						continue;
					var baseValue = textColor.Value.Info.GetValue (baseStyle, null) as ChunkStyle;
					if (thisValue.Equals (baseValue)) {
						continue;
					}
					var colorString = new StringBuilder ();
					if (!thisValue.TransparentForeground)
						colorString.Append (string.Format ("\"fore\":\"{0}\"", ColorToMarkup (thisValue.Foreground)));
					if (!thisValue.TransparentBackground) {
						if (colorString.Length > 0)
							colorString.Append (", ");
						colorString.Append (string.Format ("\"back\":\"{0}\"", ColorToMarkup (thisValue.Background)));
					}
					if (thisValue.FontWeight != FontWeight.Normal) {
						if (colorString.Length > 0)
							colorString.Append (", ");
						colorString.Append (string.Format ("\"weight\":\"{0}\"", thisValue.FontWeight));
					}
					if (thisValue.FontStyle != FontStyle.Normal) {
						if (colorString.Length > 0)
							colorString.Append (", ");
						colorString.Append (string.Format ("\"style\":\"{0}\"", thisValue.FontStyle));
					}
					if (!first) {
						writer.WriteLine (",");
					} else {
						first = false;
					}
					writer.Write ("\t\t{");
					if (colorString.Length == 0) {
						writer.Write ("\"name\": \"{0}\"", textColor.Value.Attribute.Name);
					} else {
						writer.Write ("\"name\": \"{0}\", {1}", textColor.Value.Attribute.Name, colorString);
					}
					writer.Write (" }");
				}
				writer.WriteLine ();
				writer.WriteLine ("\t]");

				writer.WriteLine ("}");
			}
		}

		internal static Cairo.Color ImportVsColor (string colorString)
		{
			if (colorString == "0x02000000")
				return new Cairo.Color (0, 0, 0, 0);
			string color = "#" + colorString.Substring (8, 2) + colorString.Substring (6, 2) + colorString.Substring (4, 2);
			return HslColor.Parse (color);
		}

		public class VSSettingColor
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
							var textColor = ChunkStyle.Import (color.Value.Attribute.Name, vsc);
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

			result.IndentationGuide = new AmbientColor ();
			result.IndentationGuide.Colors.Add (Tuple.Create ("color", AlphaBlend (result.PlainText.Foreground, result.PlainText.Background, 0.3)));

			result.TooltipText = result.PlainText.Clone ();
			var h = (HslColor)result.TooltipText.Background;
			h.L += 0.01;
			result.TooltipText.Background = h;

			result.TooltipPagerTop = new AmbientColor ();
			result.TooltipPagerTop.Colors.Add (Tuple.Create ("color", result.TooltipText.Background));

			result.TooltipPagerBottom = new AmbientColor ();
			result.TooltipPagerBottom.Colors.Add (Tuple.Create ("color", result.TooltipText.Background));
			
			result.TooltipPagerTriangle = new AmbientColor ();
			result.TooltipPagerTriangle.Colors.Add (Tuple.Create ("color", AlphaBlend (result.PlainText.Foreground, result.PlainText.Background, 0.8)));

			result.TooltipBorder = new AmbientColor ();
			result.TooltipBorder.Colors.Add (Tuple.Create ("color", AlphaBlend (result.PlainText.Foreground, result.PlainText.Background, 0.5)));

			var defaultStyle = SyntaxModeService.GetColorStyle (HslColor.Brightness (result.PlainText.Background) < 0.5 ? "Monokai" : TextEditorOptions.DefaultColorStyle);

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

		public Cairo.Color GetForeground (ChunkStyle chunkStyle)
		{
			if (chunkStyle.TransparentForeground)
				return PlainText.Foreground;
			return chunkStyle.Foreground;
		}

	}
}
