//
// ColorSheme2.cs
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

namespace Mono.TextEditor.Highlighting
{
	public class ColorDescriptionAttribute : Attribute
	{
		public string Name { get; private set; }
		public string Description { get; set; }
		public string VSSetting { get; set; }

		public ColorDescriptionAttribute (string name)
		{
			this.Name = name;
		}
	}

	public class AmbientColor
	{
		public List<Tuple<string, Cairo.Color>> Colors;
		
		public Cairo.Color GetColor (string name)
		{
			return Colors.First (t => t.Item1 == name).Item2;
		}
	}

	public enum TextWeight {
		Normal,
		Bold,
		Italic
	}

	public class TextColor
	{
		public Cairo.Color Foreground { get; set; }
		public Cairo.Color Background { get; set; }
		public TextWeight Weight { get; set; }
	}

	public class ColorSheme2
	{
		public string Name { get; set; }
		public Version Version { get; set; }
		public string Description { get; set; }
		public string Originator { get; set; }


		#region Ambient Colors
		[ColorDescription("Background(Read Only)",VSSetting="color=Plain Text/Background")]
		public AmbientColor BackgroundReadOnly { get; private set; }
		
		[ColorDescription("Search result background")]
		public AmbientColor SearchResult { get; private set; }
		
		[ColorDescription("Search result background (highlighted)")]
		public AmbientColor SearchResultMain { get; private set; }

		[ColorDescription("Fold Margin")]
		public AmbientColor FoldMargin { get; private set; }

		[ColorDescription("Indicator Margin")]
		public AmbientColor IndicatorMargin { get; private set; }

		[ColorDescription("Indicator Margin(Separator)")]
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

		[ColorDescription("Bookmarks")]
		public AmbientColor Bookmarks { get; private set; }

		[ColorDescription("Underline(Error)")]
		public AmbientColor UnderlineError { get; private set; }
		
		[ColorDescription("Underline(Warning)")]
		public AmbientColor UnderlineWarning { get; private set; }

		[ColorDescription("Underline(Suggestion)")]
		public AmbientColor UnderlineSuggestion { get; private set; }

		[ColorDescription("Underline(Hint)")]
		public AmbientColor UnderlineHint { get; private set; }

		[ColorDescription("Quick Diff(Dirty)")]
		public AmbientColor QuickDiffDirty { get; private set; }

		[ColorDescription("Quick Diff(Changed)")]
		public AmbientColor QuickDiffChanged { get; private set; }

		[ColorDescription("Brace Matching(Rectangle)")]
		public AmbientColor BraceMatchingRectangle { get; private set; }
		
		[ColorDescription("Usages(Rectangle)")]
		public AmbientColor UsagesRectangle { get; private set; }

		[ColorDescription("Breakpoint Marker")]
		public AmbientColor BreakpointMarker { get; private set; }

		[ColorDescription("Breakpoint Marker(Disabled)")]
		public AmbientColor BreakpointMarkerDisabled { get; private set; }

		[ColorDescription("Debugger Current Line Marker")]
		public AmbientColor DebuggerCurrentLineMarker { get; private set; }

		[ColorDescription("Debugger Stack Line Marker")]
		public AmbientColor DebuggerStackLineMarker { get; private set; }
		#endregion

		#region Text Colors

		[ColorDescription("Plain Text", VSSetting = "Plain Text")]
		public TextColor PlainText { get; private set; }

		[ColorDescription("Selected Text", VSSetting = "Selected Text")]
		public TextColor SelectedText { get; private set; }

		[ColorDescription("Selected Text(Inactive)", VSSetting = "Inactive Selected Text")]
		public TextColor SelectedInactiveText { get; private set; }

		[ColorDescription("Collapsed Text", VSSetting = "Collapsible Text")]
		public TextColor CollapsedText { get; private set; }

		[ColorDescription("Line Numbers", VSSetting = "Line Numbers")]
		public TextColor LineNumbers { get; private set; }

		[ColorDescription("Punctuation", VSSetting = "Operator")]
		public TextColor Punctuation { get; private set; }

		[ColorDescription("Punctuation(Brackets)", VSSetting = "Plain Text")]
		public TextColor PunctuationForBrackets { get; private set; }

		[ColorDescription("Comment(Line)", VSSetting = "Comment")]
		public TextColor CommentsSingleLine { get; private set; }

		[ColorDescription("Comment(Block)", VSSetting = "Comment")]
		public TextColor CommentsMultiLine { get; private set; }

		[ColorDescription("Comment(Doc)", VSSetting = "Comment")]
		public TextColor CommentsForDocumentation { get; private set; }

		[ColorDescription("Comment Tag", VSSetting = "Comment")]
		public TextColor CommentTags { get; private set; }

		[ColorDescription("String", VSSetting = "String")]
		public TextColor String { get; private set; }

		[ColorDescription("String(Escape)", VSSetting = "String")]
		public TextColor StringEscapeSequence { get; private set; }

		[ColorDescription("String(C# @ Verbatim)", VSSetting = "String(C# @ Verbatim)")]
		public TextColor StringVerbatim { get; private set; }

		[ColorDescription("Number", VSSetting = "Number")]
		public TextColor Number { get; private set; }

		[ColorDescription("Preprocessor", VSSetting = "Preprocessor Keyword")]
		public TextColor Preprocessor { get; private set; }

		[ColorDescription("Preprocessor Keyword", VSSetting = "Preprocessor Keyword")]
		public TextColor PreprocessorKeyword { get; private set; }

		[ColorDescription("Xml Text", VSSetting = "XML Text")]
		public TextColor XmlText { get; private set; }

		[ColorDescription("Xml Delimiter", VSSetting = "XML Delimiter")]
		public TextColor XmlDelimiter { get; private set; }

		[ColorDescription("Xml Name", VSSetting ="XML Name")]
		public TextColor XmlName { get; private set; }

		[ColorDescription("Xml Attribute", VSSetting = "XML Attribute")]
		public TextColor XmlAttribute { get; private set; }

		[ColorDescription("Xml CData Section", VSSetting = "XML CData Section")]
		public TextColor XmlCDataSection { get; private set; }

		[ColorDescription("Tooltip Text")] // not defined in vs.net
		public TextColor TooltipText { get; private set; }

		[ColorDescription("Completion Text")] //not defined in vs.net
		public TextColor CompletionText { get; private set; }

		[ColorDescription("Keyword(Access)", VSSetting = "Keyword")]
		public TextColor KeywordAccessors { get; private set; }

		[ColorDescription("Keyword(Type)", VSSetting = "Keyword")]
		public TextColor KeywordTypes { get; private set; }

		[ColorDescription("Keyword(Operator)", VSSetting = "Keyword")]
		public TextColor KeywordOperators { get; private set; }

		[ColorDescription("Keyword(Selection)", VSSetting = "Keyword")]
		public TextColor KeywordSelection { get; private set; }

		[ColorDescription("Keyword(Iteration)", VSSetting = "Keyword")]
		public TextColor KeywordIteration { get; private set; }

		[ColorDescription("Keyword(Jump)", VSSetting = "Keyword")]
		public TextColor KeywordJump { get; private set; }

		[ColorDescription("Keyword(Context)", VSSetting = "Keyword")]
		public TextColor KeywordContext { get; private set; }

		[ColorDescription("Keyword(Exception)", VSSetting = "Keyword")]
		public TextColor KeywordException { get; private set; }
		
		[ColorDescription("Keyword(Modifiers)", VSSetting = "Keyword")]
		public TextColor KeywordModifiers { get; private set; }
		
		[ColorDescription("Keyword(Constants)", VSSetting = "Keyword")]
		public TextColor KeywordConstants { get; private set; }
		
		[ColorDescription("Keyword(Void)", VSSetting = "Keyword")]
		public TextColor KeywordVoid { get; private set; }
		
		[ColorDescription("Keyword(Namespace)", VSSetting = "Keyword")]
		public TextColor KeywordNamespace { get; private set; }
		
		[ColorDescription("Keyword(Property)", VSSetting = "Keyword")]
		public TextColor KeywordProperty { get; private set; }
		
		[ColorDescription("Keyword(Declaration)", VSSetting = "Keyword")]
		public TextColor KeywordDeclaration { get; private set; }
		
		[ColorDescription("Keyword(Parameter)", VSSetting = "Keyword")]
		public TextColor KeywordParameter { get; private set; }
		
		[ColorDescription("Keyword(Operator Declaration)", VSSetting = "Keyword")]
		public TextColor KeywordOperatorDeclaration { get; private set; }
		
		[ColorDescription("Keyword(Other)", VSSetting = "Keyword")]
		public TextColor KeywordOther { get; private set; }

		[ColorDescription("User Types", VSSetting = "User Types")]
		public TextColor UserTypes { get; private set; }

		[ColorDescription("User Types(Enums)", VSSetting = "User Types(Enums)")]
		public TextColor UserTypesEnums { get; private set; }
		
		[ColorDescription("User Types(Interfaces)", VSSetting = "User Types(Interfaces)")]
		public TextColor UserTypesInterfaces { get; private set; }
		
		[ColorDescription("User Types(Delegates)", VSSetting = "User Types(Delegates)")]
		public TextColor UserTypesDelegatess { get; private set; }
		
		[ColorDescription("User Types(Value types)", VSSetting = "User Types(Value types)")]
		public TextColor UserTypesValuetypes { get; private set; }
		
		[ColorDescription("User Field Usage", VSSetting = "Plain Text")]
		public TextColor UserFieldUsage { get; private set; }
		
		[ColorDescription("User Field Declaration", VSSetting = "Plain Text")]
		public TextColor UserFieldDeclaration { get; private set; }
		
		[ColorDescription("User Property Usage", VSSetting = "Plain Text")]
		public TextColor UserPropertyUsage { get; private set; }
		
		[ColorDescription("User Property Declaration", VSSetting = "Plain Text")]
		public TextColor UserPropertyDeclaration { get; private set; }
		
		[ColorDescription("UserEventUsage", VSSetting = "Plain Text")]
		public TextColor UserEventUsage { get; private set; }
		
		[ColorDescription("User Event Declaration", VSSetting = "Plain Text")]
		public TextColor UserEventDeclaration { get; private set; }
		
		[ColorDescription("User Method Usage", VSSetting = "Plain Text")]
		public TextColor UserMethodUsage { get; private set; }
		
		[ColorDescription("User Method Declaration", VSSetting = "Plain Text")]
		public TextColor UserMethodDeclaration { get; private set; }
		
		[ColorDescription("Syntax Error", VSSetting = "Syntax Error")]
		public TextColor SyntaxError { get; private set; }

		[ColorDescription("Breakpoint Text")] // not defined
		public TextColor BreakpointText { get; private set; }

		[ColorDescription("Breakpoint Text(Invalid)")] // not defined
		public TextColor BreakpointTextInvalid { get; private set; }

		
		[ColorDescription("Debugger Current Line")] // not defined
		public TextColor DebuggerCurrentLine { get; private set; }
		
		[ColorDescription("Debugger Stack Line")] // not defined
		public TextColor DebuggerStackLine { get; private set; }

		#endregion

		public static ColorSheme2 LoadFrom (Stream stream)
		{
			var result = new ColorSheme2 ();
			// var obj = JsonValue.Parse (File.ReadAllText (fileName));
			// TODO: Loading
			return result;
		}
	}
}

