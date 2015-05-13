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
	public sealed class ColorScheme
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public string Originator { get; set; }
		public string BaseScheme { get; set; }
		public string FileName { get; set; }

		#region Ambient Colors

		[ColorDescription("Background(Read Only)",VSSetting="color=Plain Text/Background")]
		public AmbientColor BackgroundReadOnly { get; private set; }
		
		[ColorDescription("Search result background")]
		public AmbientColor SearchResult { get; private set; }
		
		[ColorDescription("Search result background (highlighted)")]
		public AmbientColor SearchResultMain { get; private set; }

		[ColorDescription("Fold Square", VSSetting="color=outlining.verticalrule/Foreground")]
		public AmbientColor FoldLineColor { get; private set; }
		
		[ColorDescription("Fold Cross", VSSetting="color=outlining.square/Foreground,secondcolor=outlining.square/Background")]
		public AmbientColor FoldCross { get; private set; }
		
		[ColorDescription("Indentation Guide")] // not defined
		public AmbientColor IndentationGuide { get; private set; }

		[ColorDescription("Indicator Margin", VSSetting="color=Indicator Margin/Background")]
		public AmbientColor IndicatorMargin { get; private set; }

		[ColorDescription("Indicator Margin(Separator)", VSSetting="color=Indicator Margin/Background")]
		public AmbientColor IndicatorMarginSeparator { get; private set; }

		[ColorDescription("Tooltip Border")]
		public AmbientColor TooltipBorder { get; private set; }

		[ColorDescription("Tooltip Pager Top")]
		public AmbientColor TooltipPagerTop { get; private set; }

		[ColorDescription("Tooltip Pager Bottom")]
		public AmbientColor TooltipPagerBottom { get; private set; }

		[ColorDescription("Tooltip Pager Triangle")]
		public AmbientColor TooltipPagerTriangle { get; private set; }
		
		[ColorDescription("Tooltip Pager Text")]
		public AmbientColor TooltipPagerText { get; private set; }

		[ColorDescription("Notification Border")]
		public AmbientColor NotificationBorder { get; private set; }

		[ColorDescription("Bookmarks")]
		public AmbientColor Bookmarks { get; private set; }

		[ColorDescription("Underline(Error)", VSSetting="color=Syntax Error/Foreground")]
		public AmbientColor UnderlineError { get; private set; }
		
		[ColorDescription("Underline(Warning)", VSSetting="color=Warning/Foreground")]
		public AmbientColor UnderlineWarning { get; private set; }

		[ColorDescription("Underline(Suggestion)", VSSetting="color=Other Error/Foreground")]
		public AmbientColor UnderlineSuggestion { get; private set; }

		[ColorDescription("Underline(Hint)", VSSetting="color=Other Error/Foreground")]
		public AmbientColor UnderlineHint { get; private set; }

		[ColorDescription("Quick Diff(Dirty)")]
		public AmbientColor QuickDiffDirty { get; private set; }

		[ColorDescription("Quick Diff(Changed)")]
		public AmbientColor QuickDiffChanged { get; private set; }

		[ColorDescription("Brace Matching(Rectangle)", VSSetting="color=Brace Matching (Rectangle)/Background,secondcolor=Brace Matching (Rectangle)/Foreground")]
		public AmbientColor BraceMatchingRectangle { get; private set; }
		
		[ColorDescription("Usages(Rectangle)", VSSetting="color=MarkerFormatDefinition/HighlightedReference/Background,secondcolor=MarkerFormatDefinition/HighlightedReference/Background,bordercolor=MarkerFormatDefinition/HighlightedReference/Background")]
		public AmbientColor UsagesRectangle { get; private set; }

		[ColorDescription("Changing usages(Rectangle)", VSSetting="color=MarkerFormatDefinition/HighlightedReference/Background,secondcolor=MarkerFormatDefinition/HighlightedReference/Background,bordercolor=MarkerFormatDefinition/HighlightedReference/Background")]
		public AmbientColor ChangingUsagesRectangle { get; private set; }

		[ColorDescription("Breakpoint Marker", VSSetting = "color=Breakpoint (Enabled)/Background")]
		public AmbientColor BreakpointMarker { get; private set; }

		[ColorDescription("Breakpoint Marker(Invalid)", VSSetting = "color=Breakpoint (Disabled)/Background")]
		public AmbientColor BreakpointMarkerInvalid { get; private set; }

		[ColorDescription("Breakpoint Marker(Disabled)")]
		public AmbientColor BreakpointMarkerDisabled { get; private set; }

		[ColorDescription("Debugger Current Line Marker", VSSetting = "color=Current Statement/Background")]
		public AmbientColor DebuggerCurrentLineMarker { get; private set; }

		[ColorDescription("Debugger Stack Line Marker")]
		public AmbientColor DebuggerStackLineMarker { get; private set; }
		
		[ColorDescription("Primary Link", VSSetting = "color=Refactoring Dependent Field/Background" )]
		public AmbientColor PrimaryTemplate { get; private set; }
		
		[ColorDescription("Primary Link(Highlighted)", VSSetting = "color=Refactoring Current Field/Background")]
		public AmbientColor PrimaryTemplateHighlighted { get; private set; }

		[ColorDescription("Secondary Link")] // not defined
		public AmbientColor SecondaryTemplate { get; private set; }
		
		[ColorDescription("Secondary Link(Highlighted)")] // not defined
		public AmbientColor SecondaryTemplateHighlighted { get; private set; }

		[ColorDescription("Current Line Marker", VSSetting = "color=CurrentLineActiveFormat/Background,secondcolor=CurrentLineActiveFormat/Foreground")]
		public AmbientColor LineMarker { get; private set; }

		[ColorDescription("Current Line Marker(Inactive)", VSSetting = "color=CurrentLineInactiveFormat/Background,secondcolor=CurrentLineInactiveFormat/Foreground")]
		public AmbientColor LineMarkerInactive { get; private set; }

		[ColorDescription("Column Ruler")] // not defined
		public AmbientColor Ruler { get; private set; }

		[ColorDescription("Completion Window", VSSetting = "color=Plain Text/Background")]
		public AmbientColor CompletionWindow { get; private set; }

		[ColorDescription("Completion Tooltip Window", VSSetting = "color=Plain Text/Background")]
		public AmbientColor CompletionTooltipWindow { get; private set; }

		[ColorDescription("Completion Selection Bar Border", VSSetting = "color=Selected Text/Background")]
		public AmbientColor CompletionSelectionBarBorder { get; private set; }

		[ColorDescription("Completion Selection Bar Background", VSSetting = "color=Selected Text/Background,secondcolor=Selected Text/Background")]
		public AmbientColor CompletionSelectionBarBackground { get; private set; }

		[ColorDescription("Completion Selection Bar Border(Inactive)", VSSetting = "color=Inactive Selected Text/Background")]
		public AmbientColor CompletionSelectionBarBorderInactive { get; private set; }

		[ColorDescription("Completion Selection Bar Background(Inactive)", VSSetting = "color=Inactive Selected Text/Background,secondcolor=Inactive Selected Text/Background")]
		public AmbientColor CompletionSelectionBarBackgroundInactive { get; private set; }

		[ColorDescription("Message Bubble Error Marker")]
		public AmbientColor MessageBubbleErrorMarker { get; private set; }

		[ColorDescription("Message Bubble Error Tag")]
		public AmbientColor MessageBubbleErrorTag { get; private set; }

		[ColorDescription("Message Bubble Error Tooltip")]
		public AmbientColor MessageBubbleErrorTooltip { get; private set; }

		[ColorDescription("Message Bubble Error Line")]
		public AmbientColor MessageBubbleErrorLine { get; private set; }

		[ColorDescription("Message Bubble Error Counter")]
		public AmbientColor MessageBubbleErrorCounter { get; private set; }

		[ColorDescription("Message Bubble Error IconMargin")]
		public AmbientColor MessageBubbleErrorIconMargin { get; private set; }

		[ColorDescription("Message Bubble Warning Marker")]
		public AmbientColor MessageBubbleWarningMarker { get; private set; }

		[ColorDescription("Message Bubble Warning Tag")]
		public AmbientColor MessageBubbleWarningTag { get; private set; }

		[ColorDescription("Message Bubble Warning Tooltip")]
		public AmbientColor MessageBubbleWarningTooltip { get; private set; }

		[ColorDescription("Message Bubble Warning Line")]
		public AmbientColor MessageBubbleWarningLine { get; private set; }

		[ColorDescription("Message Bubble Warning Counter")]
		public AmbientColor MessageBubbleWarningCounter { get; private set; }

		[ColorDescription("Message Bubble Warning IconMargin")]
		public AmbientColor MessageBubbleWarningIconMargin { get; private set; }

		#endregion

		#region Text Colors
		
		public const string PlainTextKey = "Plain Text";
		
		[ColorDescription(PlainTextKey, VSSetting = "Plain Text")]
		public ChunkStyle PlainText { get; private set; }

		public const string SelectedTextKey = "Selected Text";
		[ColorDescription(SelectedTextKey, VSSetting = "Selected Text")]
		public ChunkStyle SelectedText { get; private set; }

		public const string SelectedInactiveTextKey = "Selected Text(Inactive)";
		[ColorDescription(SelectedInactiveTextKey, VSSetting = "Inactive Selected Text")]
		public ChunkStyle SelectedInactiveText { get; private set; }

		public const string CollapsedTextKey = "Collapsed Text";
		[ColorDescription(CollapsedTextKey, VSSetting = "Collapsible Text")]
		public ChunkStyle CollapsedText { get; private set; }

		public const string LineNumbersKey = "Line Numbers";
		[ColorDescription(LineNumbersKey, VSSetting = "Line Numbers")]
		public ChunkStyle LineNumbers { get; private set; }

		public const string PunctuationKey = "Punctuation";
		[ColorDescription(PunctuationKey, VSSetting = "Operator")]
		public ChunkStyle Punctuation { get; private set; }

		public const string PunctuationForBracketsKey = "Punctuation(Brackets)";
		[ColorDescription(PunctuationForBracketsKey, VSSetting = "Plain Text")]
		public ChunkStyle PunctuationForBrackets { get; private set; }

		public const string CommentsSingleLineKey = "Comment(Line)";
		[ColorDescription(CommentsSingleLineKey, VSSetting = "Comment")]
		public ChunkStyle CommentsSingleLine { get; private set; }

		public const string CommentsBlockKey = "Comment(Block)";
		[ColorDescription(CommentsBlockKey, VSSetting = "Comment")]
		public ChunkStyle CommentsBlock { get; private set; }

		public const string CommentsForDocumentationKey = "Comment(Doc)";
		[ColorDescription(CommentsForDocumentationKey, VSSetting = "XML Doc Comment")]
		public ChunkStyle CommentsForDocumentation { get; private set; }
		
		public const string CommentsForDocumentationTagsKey = "Comment(DocTag)";
		[ColorDescription(CommentsForDocumentationTagsKey, VSSetting = "XML Doc Tag")]
		public ChunkStyle CommentsForDocumentationTags { get; private set; }
		
		public const string CommentTagsKey = "Comment Tag";
		[ColorDescription(CommentTagsKey, VSSetting = "Comment")]
		public ChunkStyle CommentTags { get; private set; }
		
		public const string ExcludedCodeKey = "Excluded Code";
		[ColorDescription(ExcludedCodeKey, VSSetting = "Excluded Code")]
		public ChunkStyle ExcludedCode { get; private set; }

		public const string StringKey = "String";
		[ColorDescription(StringKey, VSSetting = "String")]
		public ChunkStyle String { get; private set; }

		public const string StringEscapeSequenceKey = "String(Escape)";
		[ColorDescription(StringEscapeSequenceKey, VSSetting = "String")]
		public ChunkStyle StringEscapeSequence { get; private set; }

		public const string StringVerbatimKey = "String(C# @ Verbatim)";
		[ColorDescription(StringVerbatimKey, VSSetting = "String(C# @ Verbatim)")]
		public ChunkStyle StringVerbatim { get; private set; }

		public const string NumberKey = "Number";
		[ColorDescription(NumberKey, VSSetting = "Number")]
		public ChunkStyle Number { get; private set; }

		public const string PreprocessorKey = "Preprocessor";
		[ColorDescription(PreprocessorKey, VSSetting = "Preprocessor Keyword")]
		public ChunkStyle Preprocessor { get; private set; }

		public const string PreprocessorRegionNameKey = "Preprocessor(Region Name)";
		[ColorDescription(PreprocessorRegionNameKey, VSSetting = "Plain Text")]
		public ChunkStyle PreprocessorRegionName { get; private set; }

		public const string XmlTextKey = "Xml Text";
		[ColorDescription(XmlTextKey, VSSetting = "XML Text")]
		public ChunkStyle XmlText { get; private set; }

		public const string XmlDelimiterKey = "Xml Delimiter";
		[ColorDescription(XmlDelimiterKey, VSSetting = "XML Delimiter")]
		public ChunkStyle XmlDelimiter { get; private set; }

		public const string XmlNameKey = "Xml Name";
		[ColorDescription(XmlNameKey, VSSetting ="XML Name")]
		public ChunkStyle XmlName { get; private set; }

		public const string XmlAttributeKey = "Xml Attribute";
		[ColorDescription(XmlAttributeKey, VSSetting = "XML Attribute")]
		public ChunkStyle XmlAttribute { get; private set; }
		
		public const string XmlAttributeQuotesKey = "Xml Attribute Quotes";
		[ColorDescription(XmlAttributeQuotesKey, VSSetting = "XML Attribute Quotes")]
		public ChunkStyle XmlAttributeQuotes { get; private set; }
		
		public const string XmlAttributeValueKey = "Xml Attribute Value";
		[ColorDescription(XmlAttributeValueKey, VSSetting = "XML Attribute Value")]
		public ChunkStyle XmlAttributeValue { get; private set; }
		
		public const string XmlCommentKey = "Xml Comment";
		[ColorDescription(XmlCommentKey, VSSetting = "XML Comment")]
		public ChunkStyle XmlComment { get; private set; }

		public const string XmlCDataSectionKey = "Xml CData Section";
		[ColorDescription(XmlCDataSectionKey, VSSetting = "XML CData Section")]
		public ChunkStyle XmlCDataSection { get; private set; }

		public const string TooltipTextKey = "Tooltip Text";
		[ColorDescription(TooltipTextKey)] // not defined in vs.net
		public ChunkStyle TooltipText { get; private set; }

		public const string NotificationTextKey = "Notification Text";
		[ColorDescription(NotificationTextKey)] // not defined in vs.net
		public ChunkStyle NotificationText { get; private set; }

		public const string CompletionTextKey = "Completion Text";
		[ColorDescription(CompletionTextKey, VSSetting = "Plain Text")]
		public ChunkStyle CompletionText { get; private set; }

		public const string CompletionMatchingSubstringKey = "Completion Matching Substring";
		[ColorDescription(CompletionMatchingSubstringKey, VSSetting = "Keyword")]
		public ChunkStyle CompletionMatchingSubstring { get; private set; }

		public const string CompletionSelectedTextKey = "Completion Selected Text";
		[ColorDescription(CompletionSelectedTextKey, VSSetting = "Selected Text")]
		public ChunkStyle CompletionSelectedText { get; private set; }

		public const string CompletionSelectedMatchingSubstringKey = "Completion Selected Matching Substring";
		[ColorDescription(CompletionSelectedMatchingSubstringKey, VSSetting = "Keyword")]
		public ChunkStyle CompletionSelectedMatchingSubstring { get; private set; }

		public const string CompletionSelectedInactiveTextKey = "Completion Selected Text(Inactive)";
		[ColorDescription(CompletionSelectedInactiveTextKey, VSSetting = "Inactive Selected Text")]
		public ChunkStyle CompletionSelectedInactiveText { get; private set; }

		public const string CompletionSelectedInactiveMatchingSubstringKey = "Completion Selected Matching Substring(Inactive)";
		[ColorDescription(CompletionSelectedInactiveMatchingSubstringKey, VSSetting = "Keyword")]
		public ChunkStyle CompletionSelectedInactiveMatchingSubstring { get; private set; }

		public const string KeywordAccessorsKey = "Keyword(Access)";
		[ColorDescription(KeywordAccessorsKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordAccessors { get; private set; }

		public const string KeywordTypesKey = "Keyword(Type)";
		[ColorDescription(KeywordTypesKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordTypes { get; private set; }

		public const string KeywordOperatorsKey = "Keyword(Operator)";
		[ColorDescription(KeywordOperatorsKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordOperators { get; private set; }

		public const string KeywordSelectionKey = "Keyword(Selection)";
		[ColorDescription(KeywordSelectionKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordSelection { get; private set; }

		public const string KeywordIterationKey = "Keyword(Iteration)";
		[ColorDescription(KeywordIterationKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordIteration { get; private set; }

		public const string KeywordJumpKey = "Keyword(Jump)";
		[ColorDescription(KeywordJumpKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordJump { get; private set; }

		public const string KeywordContextKey = "Keyword(Context)";
		[ColorDescription(KeywordContextKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordContext { get; private set; }

		public const string KeywordExceptionKey = "Keyword(Exception)";
		[ColorDescription(KeywordExceptionKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordException { get; private set; }
		
		public const string KeywordModifiersKey = "Keyword(Modifiers)";
		[ColorDescription(KeywordModifiersKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordModifiers { get; private set; }
		
		public const string KeywordConstantsKey = "Keyword(Constants)";
		[ColorDescription(KeywordConstantsKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordConstants { get; private set; }
		
		public const string KeywordVoidKey = "Keyword(Void)";
		[ColorDescription(KeywordVoidKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordVoid { get; private set; }
		
		public const string KeywordNamespaceKey = "Keyword(Namespace)";
		[ColorDescription(KeywordNamespaceKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordNamespace { get; private set; }
		
		public const string KeywordPropertyKey = "Keyword(Property)";
		[ColorDescription(KeywordPropertyKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordProperty { get; private set; }
		
		public const string KeywordDeclarationKey = "Keyword(Declaration)";
		[ColorDescription(KeywordDeclarationKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordDeclaration { get; private set; }
		
		public const string KeywordParameterKey = "Keyword(Parameter)";
		[ColorDescription(KeywordParameterKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordParameter { get; private set; }
		
		public const string KeywordOperatorDeclarationKey = "Keyword(Operator Declaration)";
		[ColorDescription(KeywordOperatorDeclarationKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordOperatorDeclaration { get; private set; }
		
		public const string KeywordOtherKey = "Keyword(Other)";
		[ColorDescription(KeywordOtherKey, VSSetting = "Keyword")]
		public ChunkStyle KeywordOther { get; private set; }

		public const string UserTypesKey = "User Types";
		[ColorDescription(UserTypesKey, VSSetting = "User Types")]
		public ChunkStyle UserTypes { get; private set; }

		public const string UserTypesEnumsKey = "User Types(Enums)";
		[ColorDescription(UserTypesEnumsKey, VSSetting = "User Types(Enums)")]
		public ChunkStyle UserTypesEnums { get; private set; }
		
		public const string UserTypesInterfacesKey = "User Types(Interfaces)";
		[ColorDescription(UserTypesInterfacesKey, VSSetting = "User Types(Interfaces)")]
		public ChunkStyle UserTypesInterfaces { get; private set; }
		
		public const string UserTypesDelegatesKey = "User Types(Delegates)";
		[ColorDescription(UserTypesDelegatesKey, VSSetting = "User Types(Delegates)")]
		public ChunkStyle UserTypesDelegates { get; private set; }
		
		public const string UserTypesValueTypesKey = "User Types(Value types)";
		[ColorDescription(UserTypesValueTypesKey, VSSetting = "User Types(Value types)")]
		public ChunkStyle UserTypesValueTypes { get; private set; }

		public const string UserTypesTypeParametersKey = "User Types(Type parameters)";
		[ColorDescription(UserTypesTypeParametersKey, VSSetting = "User Types(Type parameters)")]
		public ChunkStyle UserTypesTypeParameters { get; private set; }

		public const string UserFieldUsageKey = "User Field Usage";
		[ColorDescription(UserFieldUsageKey, VSSetting = "Identifier")]
		public ChunkStyle UserFieldUsage { get; private set; }
		
		public const string UserFieldDeclarationKey = "User Field Declaration";
		[ColorDescription(UserFieldDeclarationKey, VSSetting = "Identifier")]
		public ChunkStyle UserFieldDeclaration { get; private set; }
		
		public const string UserPropertyUsageKey = "User Property Usage";
		[ColorDescription(UserPropertyUsageKey, VSSetting = "Identifier")]
		public ChunkStyle UserPropertyUsage { get; private set; }
		
		public const string UserPropertyDeclarationKey = "User Property Declaration";
		[ColorDescription(UserPropertyDeclarationKey, VSSetting = "Identifier")]
		public ChunkStyle UserPropertyDeclaration { get; private set; }
		
		public const string UserEventUsageKey = "User Event Usage";
		[ColorDescription(UserEventUsageKey, VSSetting = "Identifier")]
		public ChunkStyle UserEventUsage { get; private set; }
		
		public const string UserEventDeclarationKey = "User Event Declaration";
		[ColorDescription(UserEventDeclarationKey, VSSetting = "Identifier")]
		public ChunkStyle UserEventDeclaration { get; private set; }
		
		public const string UserMethodUsageKey = "User Method Usage";
		[ColorDescription(UserMethodUsageKey, VSSetting = "Identifier")]
		public ChunkStyle UserMethodUsage { get; private set; }

		public const string UserMethodDeclarationKey = "User Method Declaration";
		[ColorDescription(UserMethodDeclarationKey, VSSetting = "Identifier")]
		public ChunkStyle UserMethodDeclaration { get; private set; }

		public const string UserParameterUsageKey = "User Parameter Usage";
		[ColorDescription(UserParameterUsageKey, VSSetting = "Identifier")]
		public ChunkStyle UserParameterUsage { get; private set; }
		
		public const string UserParameterDeclarationKey = "User Parameter Declaration";
		[ColorDescription(UserParameterDeclarationKey, VSSetting = "Identifier")]
		public ChunkStyle UserParameterDeclaration { get; private set; }

		public const string UserVariableUsageKey = "User Variable Usage";
		[ColorDescription(UserVariableUsageKey, VSSetting = "Identifier")]
		public ChunkStyle UserVariableUsage { get; private set; }
		
		public const string UserVariableDeclarationKey = "User Variable Declaration";
		[ColorDescription(UserVariableDeclarationKey, VSSetting = "Identifier")]
		public ChunkStyle UserVariableDeclaration { get; private set; }

		public const string SyntaxErrorKey = "Syntax Error";
		[ColorDescription(SyntaxErrorKey, VSSetting = "Syntax Error")]
		public ChunkStyle SyntaxError { get; private set; }

		public const string StringFormatItemsKey = "String Format Items";
		[ColorDescription(StringFormatItemsKey, VSSetting = "String")]
		public ChunkStyle StringFormatItems { get; private set; }

		public const string BreakpointTextKey = "Breakpoint Text";
		[ColorDescription(BreakpointTextKey, VSSetting = "Breakpoint (Enabled)")]
		public ChunkStyle BreakpointText { get; private set; }

		public const string DebuggerCurrentLineKey = "Debugger Current Statement";
		[ColorDescription(DebuggerCurrentLineKey, VSSetting = "Current Statement")]
		public ChunkStyle DebuggerCurrentLine { get; private set; }

		public const string DebuggerStackLineKey = "Debugger Stack Line";
		[ColorDescription(DebuggerStackLineKey)] // not defined
		public ChunkStyle DebuggerStackLine { get; private set; }

		public const string DiffLineAddedKey = "Diff Line(Added)";
		[ColorDescription(DiffLineAddedKey)] //not defined
		public ChunkStyle DiffLineAdded { get; private set; }

		public const string DiffLineRemovedKey = "Diff Line(Removed)";
		[ColorDescription(DiffLineRemovedKey)] //not defined
		public ChunkStyle DiffLineRemoved { get; private set; }

		public const string DiffLineChangedKey = "Diff Line(Changed)";
		[ColorDescription(DiffLineChangedKey)] //not defined
		public ChunkStyle DiffLineChanged { get; private set; }

		public const string DiffHeaderKey = "Diff Header";
		[ColorDescription(DiffHeaderKey)] //not defined
		public ChunkStyle DiffHeader { get; private set; }
		
		public const string DiffHeaderSeparatorKey = "Diff Header(Separator)";
		[ColorDescription(DiffHeaderSeparatorKey)] //not defined
		public ChunkStyle DiffHeaderSeparator { get; private set; }
		
		public const string DiffHeaderOldKey = "Diff Header(Old)";
		[ColorDescription(DiffHeaderOldKey)] //not defined
		public ChunkStyle DiffHeaderOld { get; private set; }
		
		public const string DiffHeaderNewKey = "Diff Header(New)";
		[ColorDescription(DiffHeaderNewKey)] //not defined
		public ChunkStyle DiffHeaderNew { get; private set; }

		public const string DiffLocationKey = "Diff Location";
		[ColorDescription(DiffLocationKey)] //not defined
		public ChunkStyle DiffLocation { get; private set; }

		public const string HtmlAttributeNameKey = "Html Attribute Name";
		[ColorDescription(HtmlAttributeNameKey, VSSetting="HTML Attribute")]
		public ChunkStyle HtmlAttributeName { get; private set; }

		public const string HtmlAttributeValueKey = "Html Attribute Value";
		[ColorDescription(HtmlAttributeValueKey, VSSetting="HTML Attribute Value")]
		public ChunkStyle HtmlAttributeValue { get; private set; }
		
		public const string HtmlCommentKey = "Html Comment";
		[ColorDescription(HtmlCommentKey, VSSetting="HTML Comment")]
		public ChunkStyle HtmlComment { get; private set; }
		
		public const string HtmlElementNameKey = "Html Element Name";
		[ColorDescription(HtmlElementNameKey, VSSetting="HTML Element Name")]
		public ChunkStyle HtmlElementName { get; private set; }
		
		public const string HtmlEntityKey = "Html Entity";
		[ColorDescription(HtmlEntityKey, VSSetting="HTML Entity")]
		public ChunkStyle HtmlEntity { get; private set; }
		
		public const string HtmlOperatorKey = "Html Operator";
		[ColorDescription(HtmlOperatorKey, VSSetting="HTML Operator")]
		public ChunkStyle HtmlOperator { get; private set; }
		
		public const string HtmlServerSideScriptKey = "Html Server-Side Script";
		[ColorDescription(HtmlServerSideScriptKey, VSSetting="HTML Server-Side Script")]
		public ChunkStyle HtmlServerSideScript { get; private set; }
		
		public const string HtmlTagDelimiterKey = "Html Tag Delimiter";
		[ColorDescription(HtmlTagDelimiterKey, VSSetting="HTML Tag Delimiter")]
		public ChunkStyle HtmlTagDelimiter { get; private set; }
		
		public const string RazorCodeKey = "Razor Code";
		[ColorDescription(RazorCodeKey, VSSetting="Razor Code")]
		public ChunkStyle RazorCode { get; private set; }

		public const string CssCommentKey = "Css Comment";
		[ColorDescription(CssCommentKey, VSSetting="CSS Comment")]
		public ChunkStyle CssComment { get; private set; }

		public const string CssPropertyNameKey = "Css Property Name";
		[ColorDescription(CssPropertyNameKey, VSSetting="CSS Property Name")]
		public ChunkStyle CssPropertyName { get; private set; }
		
		public const string CssPropertyValueKey = "Css Property Value";
		[ColorDescription(CssPropertyValueKey, VSSetting="CSS Property Value")]
		public ChunkStyle CssPropertyValue { get; private set; }
		
		public const string CssSelectorKey = "Css Selector";
		[ColorDescription(CssSelectorKey, VSSetting="CSS Selector")]
		public ChunkStyle CssSelector { get; private set; }
		
		public const string CssStringValueKey = "Css String Value";
		[ColorDescription(CssStringValueKey, VSSetting="CSS String Value")]
		public ChunkStyle CssStringValue { get; private set; }
		
		public const string CssKeywordKey = "Css Keyword";
		[ColorDescription(CssKeywordKey, VSSetting="CSS Keyword")]
		public ChunkStyle CssKeyword { get; private set; }

		public const string ScriptCommentKey = "Script Comment";
		[ColorDescription(ScriptCommentKey, VSSetting="Script Comment")]
		public ChunkStyle ScriptComment { get; private set; }
		
		public const string ScriptIdentifierKey = "Script Identifier";
		[ColorDescription(ScriptIdentifierKey, VSSetting="Script Identifier")]
		public ChunkStyle ScriptIdentifier { get; private set; }
		
		public const string ScriptKeywordKey = "Script Keyword";
		[ColorDescription(ScriptKeywordKey, VSSetting="Script Keyword")]
		public ChunkStyle ScriptKeyword { get; private set; }
		
		public const string ScriptNumberKey = "Script Number";
		[ColorDescription(ScriptNumberKey, VSSetting="Script Number")]
		public ChunkStyle ScriptNumber { get; private set; }
		
		public const string ScriptOperatorKey = "Script Operator";
		[ColorDescription(ScriptOperatorKey, VSSetting="Script Operator")]
		public ChunkStyle ScriptOperator { get; private set; }

		public const string ScriptStringKey = "Script String";
		[ColorDescription(ScriptStringKey, VSSetting="Script String")]
		public ChunkStyle ScriptString { get; private set; }

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

		static ColorScheme ()
		{
			foreach (var property in typeof(ColorScheme).GetProperties ()) {
				var description = property.GetCustomAttributes (false).FirstOrDefault (p => p is ColorDescriptionAttribute) as ColorDescriptionAttribute;
				if (description == null)
					continue;
				if (property.PropertyType == typeof (ChunkStyle)) {
					textColors.Add (description.Name, new PropertyDescription (property, description));
				} else {
					ambientColors.Add (description.Name, new PropertyDescription (property, description));
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
				double r = ((double) int.Parse (value.Substring (1,2), System.Globalization.NumberStyles.HexNumber)) / 255;
				double g = ((double) int.Parse (value.Substring (3,2), System.Globalization.NumberStyles.HexNumber)) / 255;
				double b = ((double) int.Parse (value.Substring (5,2), System.Globalization.NumberStyles.HexNumber)) / 255;
				double a = ((double) int.Parse (value.Substring (7,2), System.Globalization.NumberStyles.HexNumber)) / 255;
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
			PropertyDescription val;
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

			var root = XElement.Load(reader);
			
			// The fields we'd like to extract
			result.Name = root.XPathSelectElement("name").Value;

			if (result.Name != TextEditorOptions.DefaultColorStyle)
				result.CopyValues (SyntaxModeService.DefaultColorStyle);

			var version = Version.Parse (root.XPathSelectElement("version").Value);
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
				PropertyDescription info;
				if (!ambientColors.TryGetValue (color.Name, out info)) {
					Console.WriteLine ("Ambient color:" + color.Name + " not found.");
					continue;
				}
				info.Info.SetValue (result, color, null);
			}

			foreach (var textColorElement in root.XPathSelectElements("//text/*")) {
				var color = ChunkStyle.Create (textColorElement, palette);
				PropertyDescription info;
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
						colors[color.Name] = color;
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
