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

namespace Mono.TextEditor.Highlighting
{
	public class ColorScheme
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
		
		[ColorDescription("Primary Link")] // not defined
		public AmbientColor PrimaryTemplate { get; private set; }
		
		[ColorDescription("Primary Link(Highlighted)")] // not defined
		public AmbientColor PrimaryTemplateHighlighted { get; private set; }

		[ColorDescription("Secondary Link")] // not defined
		public AmbientColor SecondaryTemplate { get; private set; }
		
		[ColorDescription("Secondary Link(Highlighted)")] // not defined
		public AmbientColor SecondaryTemplateHighlighted { get; private set; }

		[ColorDescription("Current Line Marker")] // not defined
		public AmbientColor LineMarker { get; private set; }
		
		[ColorDescription("Column Ruler")] // not defined
		public AmbientColor Ruler { get; private set; }
		
		#endregion

		#region Text Colors

		[ColorDescription("Plain Text", VSSetting = "Plain Text")]
		public ChunkStyle Default { get; private set; }

		[ColorDescription("Selected Text", VSSetting = "Selected Text")]
		public ChunkStyle SelectedText { get; private set; }

		[ColorDescription("Selected Text(Inactive)", VSSetting = "Inactive Selected Text")]
		public ChunkStyle SelectedInactiveText { get; private set; }

		[ColorDescription("Collapsed Text", VSSetting = "Collapsible Text")]
		public ChunkStyle CollapsedText { get; private set; }

		[ColorDescription("Line Numbers", VSSetting = "Line Numbers")]
		public ChunkStyle LineNumbers { get; private set; }

		[ColorDescription("Punctuation", VSSetting = "Operator")]
		public ChunkStyle Punctuation { get; private set; }

		[ColorDescription("Punctuation(Brackets)", VSSetting = "Plain Text")]
		public ChunkStyle PunctuationForBrackets { get; private set; }

		[ColorDescription("Comment(Line)", VSSetting = "Comment")]
		public ChunkStyle CommentsSingleLine { get; private set; }

		[ColorDescription("Comment(Block)", VSSetting = "Comment")]
		public ChunkStyle CommentsMultiLine { get; private set; }

		[ColorDescription("Comment(Doc)", VSSetting = "Comment")]
		public ChunkStyle CommentsForDocumentation { get; private set; }

		[ColorDescription("Comment Tag", VSSetting = "Comment")]
		public ChunkStyle CommentTags { get; private set; }

		[ColorDescription("String", VSSetting = "String")]
		public ChunkStyle String { get; private set; }

		[ColorDescription("String(Escape)", VSSetting = "String")]
		public ChunkStyle StringEscapeSequence { get; private set; }

		[ColorDescription("String(C# @ Verbatim)", VSSetting = "String(C# @ Verbatim)")]
		public ChunkStyle StringVerbatim { get; private set; }

		[ColorDescription("Number", VSSetting = "Number")]
		public ChunkStyle Number { get; private set; }

		[ColorDescription("Preprocessor", VSSetting = "Preprocessor Keyword")]
		public ChunkStyle Preprocessor { get; private set; }

		[ColorDescription("Preprocessor Keyword", VSSetting = "Preprocessor Keyword")]
		public ChunkStyle PreprocessorKeyword { get; private set; }

		[ColorDescription("Xml Text", VSSetting = "XML Text")]
		public ChunkStyle XmlText { get; private set; }

		[ColorDescription("Xml Delimiter", VSSetting = "XML Delimiter")]
		public ChunkStyle XmlDelimiter { get; private set; }

		[ColorDescription("Xml Name", VSSetting ="XML Name")]
		public ChunkStyle XmlName { get; private set; }

		[ColorDescription("Xml Attribute", VSSetting = "XML Attribute")]
		public ChunkStyle XmlAttribute { get; private set; }

		[ColorDescription("Xml CData Section", VSSetting = "XML CData Section")]
		public ChunkStyle XmlCDataSection { get; private set; }

		[ColorDescription("Tooltip Text")] // not defined in vs.net
		public ChunkStyle TooltipText { get; private set; }

		[ColorDescription("Completion Text")] //not defined in vs.net
		public ChunkStyle CompletionText { get; private set; }

		[ColorDescription("Keyword(Access)", VSSetting = "Keyword")]
		public ChunkStyle KeywordAccessors { get; private set; }

		[ColorDescription("Keyword(Type)", VSSetting = "Keyword")]
		public ChunkStyle KeywordTypes { get; private set; }

		[ColorDescription("Keyword(Operator)", VSSetting = "Keyword")]
		public ChunkStyle KeywordOperators { get; private set; }

		[ColorDescription("Keyword(Selection)", VSSetting = "Keyword")]
		public ChunkStyle KeywordSelection { get; private set; }

		[ColorDescription("Keyword(Iteration)", VSSetting = "Keyword")]
		public ChunkStyle KeywordIteration { get; private set; }

		[ColorDescription("Keyword(Jump)", VSSetting = "Keyword")]
		public ChunkStyle KeywordJump { get; private set; }

		[ColorDescription("Keyword(Context)", VSSetting = "Keyword")]
		public ChunkStyle KeywordContext { get; private set; }

		[ColorDescription("Keyword(Exception)", VSSetting = "Keyword")]
		public ChunkStyle KeywordException { get; private set; }
		
		[ColorDescription("Keyword(Modifiers)", VSSetting = "Keyword")]
		public ChunkStyle KeywordModifiers { get; private set; }
		
		[ColorDescription("Keyword(Constants)", VSSetting = "Keyword")]
		public ChunkStyle KeywordConstants { get; private set; }
		
		[ColorDescription("Keyword(Void)", VSSetting = "Keyword")]
		public ChunkStyle KeywordVoid { get; private set; }
		
		[ColorDescription("Keyword(Namespace)", VSSetting = "Keyword")]
		public ChunkStyle KeywordNamespace { get; private set; }
		
		[ColorDescription("Keyword(Property)", VSSetting = "Keyword")]
		public ChunkStyle KeywordProperty { get; private set; }
		
		[ColorDescription("Keyword(Declaration)", VSSetting = "Keyword")]
		public ChunkStyle KeywordDeclaration { get; private set; }
		
		[ColorDescription("Keyword(Parameter)", VSSetting = "Keyword")]
		public ChunkStyle KeywordParameter { get; private set; }
		
		[ColorDescription("Keyword(Operator Declaration)", VSSetting = "Keyword")]
		public ChunkStyle KeywordOperatorDeclaration { get; private set; }
		
		[ColorDescription("Keyword(Other)", VSSetting = "Keyword")]
		public ChunkStyle KeywordOther { get; private set; }

		[ColorDescription("User Types", VSSetting = "User Types")]
		public ChunkStyle UserTypes { get; private set; }

		[ColorDescription("User Types(Enums)", VSSetting = "User Types(Enums)")]
		public ChunkStyle UserTypesEnums { get; private set; }
		
		[ColorDescription("User Types(Interfaces)", VSSetting = "User Types(Interfaces)")]
		public ChunkStyle UserTypesInterfaces { get; private set; }
		
		[ColorDescription("User Types(Delegates)", VSSetting = "User Types(Delegates)")]
		public ChunkStyle UserTypesDelegatess { get; private set; }
		
		[ColorDescription("User Types(Value types)", VSSetting = "User Types(Value types)")]
		public ChunkStyle UserTypesValuetypes { get; private set; }
		
		[ColorDescription("User Field Usage", VSSetting = "Plain Text")]
		public ChunkStyle UserFieldUsage { get; private set; }
		
		[ColorDescription("User Field Declaration", VSSetting = "Plain Text")]
		public ChunkStyle UserFieldDeclaration { get; private set; }
		
		[ColorDescription("User Property Usage", VSSetting = "Plain Text")]
		public ChunkStyle UserPropertyUsage { get; private set; }
		
		[ColorDescription("User Property Declaration", VSSetting = "Plain Text")]
		public ChunkStyle UserPropertyDeclaration { get; private set; }
		
		[ColorDescription("UserEventUsage", VSSetting = "Plain Text")]
		public ChunkStyle UserEventUsage { get; private set; }
		
		[ColorDescription("User Event Declaration", VSSetting = "Plain Text")]
		public ChunkStyle UserEventDeclaration { get; private set; }
		
		[ColorDescription("User Method Usage", VSSetting = "Plain Text")]
		public ChunkStyle UserMethodUsage { get; private set; }
		
		[ColorDescription("User Method Declaration", VSSetting = "Plain Text")]
		public ChunkStyle UserMethodDeclaration { get; private set; }
		
		[ColorDescription("Syntax Error", VSSetting = "Syntax Error")]
		public ChunkStyle SyntaxError { get; private set; }

		[ColorDescription("Breakpoint Text")] // not defined
		public ChunkStyle BreakpointText { get; private set; }

		[ColorDescription("Breakpoint Text(Invalid)")] // not defined
		public ChunkStyle BreakpointTextInvalid { get; private set; }

		
		[ColorDescription("Debugger Current Line")] // not defined
		public ChunkStyle DebuggerCurrentLine { get; private set; }
		
		[ColorDescription("Debugger Stack Line")] // not defined
		public ChunkStyle DebuggerStackLine { get; private set; }
		#endregion

		static Dictionary<string, PropertyInfo> textColors    = new Dictionary<string, PropertyInfo> ();
		static Dictionary<string, PropertyInfo> ambientColors = new Dictionary<string, PropertyInfo> ();

		static ColorScheme ()
		{
			foreach (var property in typeof(ColorScheme).GetProperties ()) {
				var description = property.GetCustomAttributes (false).FirstOrDefault (p => p is ColorDescriptionAttribute) as ColorDescriptionAttribute;
				if (description == null)
					continue;
				if (property.PropertyType.Name == "TextColor") {
					textColors.Add (description.Name, property);
				} else {
					ambientColors.Add (description.Name, property);
				}
			}
		}

		static Cairo.Color ParseColor (string value)
		{
			return HslColor.Parse (value);
		}

		public static Cairo.Color ParsePaletteColor (Dictionary<string, Cairo.Color> palette, string value)
		{
			Cairo.Color result;
			if (palette.TryGetValue (value, out result))
				return result;
			return ParseColor (value);
		}

		HashSet<string> textColorsSet;

		public ChunkStyle GetChunkStyle (Chunk chunk)
		{
			return GetChunkStyle (chunk.Style);
		}
	
		public ChunkStyle GetChunkStyle (string color)
		{
			PropertyInfo val;
			if (!textColors.TryGetValue (color, out val))
				return null;
			return val.GetValue (this, null) as ChunkStyle;
		}

		public static ColorScheme LoadFrom (Stream stream)
		{
			var result = new ColorScheme ();
			var reader = System.Runtime.Serialization.Json.JsonReaderWriterFactory.CreateJsonReader (stream, new System.Xml.XmlDictionaryReaderQuotas ());

			var root = XElement.Load(reader);
			
			// The fields we'd like to extract
			result.Name = root.XPathSelectElement("name").Value;
			result.Version = Version.Parse (root.XPathSelectElement("version").Value);
			result.Description = root.XPathSelectElement("description").Value;
			result.Originator = root.XPathSelectElement("originator").Value;
			var palette = new Dictionary<string, Cairo.Color> ();
			Console.WriteLine ("name:"+result.Name);
			Console.WriteLine ("descr:"+result.Description);
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
				PropertyInfo info;
				if (!ambientColors.TryGetValue (color.Name, out info)) {
					Console.WriteLine ("Ambient color:" + color.Name + " not found.");
					continue;
				}
				info.SetValue (result, color, null);
			}

			var textColorsSet = new HashSet<string> ();
			foreach (var textColorElement in root.XPathSelectElements("//text/*")) {
				var color = ChunkStyle.Create (textColorElement, palette);
				PropertyInfo info;
				if (!textColors.TryGetValue (color.Name, out info)) {
					Console.WriteLine ("Text color:" + color.Name + " not found.");
					continue;
				}
				textColorsSet.Add (color.Name);
				info.SetValue (result, color, null);
			}
			result.textColorsSet = textColorsSet;

			return result;
		}
	}
}

