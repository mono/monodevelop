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
		
		[ColorDescription("Usages(Rectangle)", VSSetting="color=MarkerFormatDefinition/HighlightedReference/Background,secondcolor=MarkerFormatDefinition/HighlightedReference/Background")]
		public AmbientColor UsagesRectangle { get; private set; }

		[ColorDescription("Breakpoint Marker")]
		public AmbientColor BreakpointMarker { get; private set; }

		[ColorDescription("Breakpoint Marker(Invalid)")]
		public AmbientColor InvalidBreakpointMarker { get; private set; }

		[ColorDescription("Breakpoint Marker(Disabled)")]
		public AmbientColor BreakpointMarkerDisabled { get; private set; }

		[ColorDescription("Debugger Current Line Marker")]
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

		[ColorDescription("Completion Matching Substring")]
		public AmbientColor CompletionHighlight { get; private set; }

		[ColorDescription("Completion Border")]
		public AmbientColor CompletionBorder { get; private set; }

		[ColorDescription("Completion Border(Inactive)")]
		public AmbientColor CompletionInactiveBorder { get; private set; }
		
		[ColorDescription("Message Bubble Error")]
		public AmbientColor MessageBubbleError { get; private set; }
		
		[ColorDescription("Message Bubble Warning")]
		public AmbientColor MessageBubbleWarning { get; private set; }
		#endregion

		#region Text Colors

		[ColorDescription("Plain Text", VSSetting = "Plain Text")]
		public ChunkStyle PlainText { get; private set; }

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

		[ColorDescription("Comment(Doc)", VSSetting = "XML Doc Comment")]
		public ChunkStyle CommentsForDocumentation { get; private set; }
		
		[ColorDescription("Comment(DocTag)", VSSetting = "XML Doc Tag")]
		public ChunkStyle CommentsForDocumentationTags { get; private set; }
		
		[ColorDescription("Comment Tag", VSSetting = "Comment")]
		public ChunkStyle CommentTags { get; private set; }
		
		[ColorDescription("Excluded Code", VSSetting = "Excluded Code")]
		public ChunkStyle ExcludedCode { get; private set; }

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

		[ColorDescription("Xml Text", VSSetting = "XML Text")]
		public ChunkStyle XmlText { get; private set; }

		[ColorDescription("Xml Delimiter", VSSetting = "XML Delimiter")]
		public ChunkStyle XmlDelimiter { get; private set; }

		[ColorDescription("Xml Name", VSSetting ="XML Name")]
		public ChunkStyle XmlName { get; private set; }

		[ColorDescription("Xml Attribute", VSSetting = "XML Attribute")]
		public ChunkStyle XmlAttribute { get; private set; }
		
		[ColorDescription("Xml Attribute Quotes", VSSetting = "XML Attribute Quotes")]
		public ChunkStyle XmlAttributeQuotes { get; private set; }
		
		[ColorDescription("Xml Attribute Value", VSSetting = "XML Attribute Value")]
		public ChunkStyle XmlAttributeValue { get; private set; }
		
		[ColorDescription("Xml Comment", VSSetting = "XML Comment")]
		public ChunkStyle XmlComment { get; private set; }

		[ColorDescription("Xml CData Section", VSSetting = "XML CData Section")]
		public ChunkStyle XmlCDataSection { get; private set; }

		[ColorDescription("Tooltip Text")] // not defined in vs.net
		public ChunkStyle TooltipText { get; private set; }

		[ColorDescription("Completion Text")] //not defined in vs.net
		public ChunkStyle CompletionText { get; private set; }

		[ColorDescription("Completion Selected Text")] //not defined in vs.net
		public ChunkStyle CompletionSelectedText { get; private set; }

		[ColorDescription("Completion Selected Text(Inactive)")] //not defined in vs.net
		public ChunkStyle CompletionSelectedInactiveText { get; private set; }

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
		public ChunkStyle UserTypesValueTypes { get; private set; }

		[ColorDescription("User Types(Type parameters)", VSSetting = "User Types(Type parameters)")]
		public ChunkStyle UserTypesTypeParameters { get; private set; }

		[ColorDescription("User Field Usage", VSSetting = "Identifier")]
		public ChunkStyle UserFieldUsage { get; private set; }
		
		[ColorDescription("User Field Declaration", VSSetting = "Identifier")]
		public ChunkStyle UserFieldDeclaration { get; private set; }
		
		[ColorDescription("User Property Usage", VSSetting = "Identifier")]
		public ChunkStyle UserPropertyUsage { get; private set; }
		
		[ColorDescription("User Property Declaration", VSSetting = "Identifier")]
		public ChunkStyle UserPropertyDeclaration { get; private set; }
		
		[ColorDescription("User Event Usage", VSSetting = "Identifier")]
		public ChunkStyle UserEventUsage { get; private set; }
		
		[ColorDescription("User Event Declaration", VSSetting = "Identifier")]
		public ChunkStyle UserEventDeclaration { get; private set; }
		
		[ColorDescription("User Method Usage", VSSetting = "Identifier")]
		public ChunkStyle UserMethodUsage { get; private set; }

		[ColorDescription("User Method Declaration", VSSetting = "Identifier")]
		public ChunkStyle UserMethodDeclaration { get; private set; }

		[ColorDescription("User Parameter Usage", VSSetting = "Identifier")]
		public ChunkStyle UserParameterUsage { get; private set; }
		
		[ColorDescription("User Parameter Declaration", VSSetting = "Identifier")]
		public ChunkStyle UserParameterDeclaration { get; private set; }

		[ColorDescription("User Variable Usage", VSSetting = "Identifier")]
		public ChunkStyle UserVariableUsage { get; private set; }
		
		[ColorDescription("User Variable Declaration", VSSetting = "Identifier")]
		public ChunkStyle UserVariableDeclaration { get; private set; }

		[ColorDescription("Syntax Error", VSSetting = "Syntax Error")]
		public ChunkStyle SyntaxError { get; private set; }

		[ColorDescription("Breakpoint Text", VSSetting = "Breakpoint (Enabled)")]
		public ChunkStyle BreakpointText { get; private set; }

		[ColorDescription("Breakpoint Text(Invalid)", VSSetting = "Breakpoint (Disabled)")]
		public ChunkStyle BreakpointTextInvalid { get; private set; }

		[ColorDescription("Debugger Current Statement", VSSetting = "Current Statement")]
		public ChunkStyle DebuggerCurrentLine { get; private set; }

		[ColorDescription("Debugger Stack Line")] // not defined
		public ChunkStyle DebuggerStackLine { get; private set; }


		[ColorDescription("Diff Line(Added)")] //not defined
		public ChunkStyle DiffLineAdded { get; private set; }

		[ColorDescription("Diff Line(Removed)")] //not defined
		public ChunkStyle DiffLineRemoved { get; private set; }

		[ColorDescription("Diff Line(Changed)")] //not defined
		public ChunkStyle DiffLineChanged { get; private set; }

		[ColorDescription("Diff Header")] //not defined
		public ChunkStyle DiffHeader { get; private set; }
		
		[ColorDescription("Diff Header(Separator)")] //not defined
		public ChunkStyle DiffHeaderSeparator { get; private set; }
		
		[ColorDescription("Diff Header(Old)")] //not defined
		public ChunkStyle DiffHeaderOld { get; private set; }
		
		[ColorDescription("Diff Header(New)")] //not defined
		public ChunkStyle DiffHeaderNew { get; private set; }

		[ColorDescription("Diff Location")] //not defined
		public ChunkStyle DiffLocation { get; private set; }

		[ColorDescription("Html Attribute Name", VSSetting="HTML Attribute")]
		public ChunkStyle HtmlAttributeName { get; private set; }
		
		[ColorDescription("Html Attribute Value", VSSetting="HTML Attribute Value")]
		public ChunkStyle HtmlAttributeValue { get; private set; }
		
		[ColorDescription("Html Comment", VSSetting="HTML Comment")]
		public ChunkStyle HtmlComment { get; private set; }
		
		[ColorDescription("Html Element Name", VSSetting="HTML Element Name")]
		public ChunkStyle HtmlElementName { get; private set; }
		
		[ColorDescription("Html Entity", VSSetting="HTML Entity")]
		public ChunkStyle HtmlEntity { get; private set; }
		
		[ColorDescription("Html Operator", VSSetting="HTML Operator")]
		public ChunkStyle HtmlOperator { get; private set; }
		
		[ColorDescription("Html Server-Side Script", VSSetting="HTML Server-Side Script")]
		public ChunkStyle HtmlServerSideScript { get; private set; }
		
		[ColorDescription("Html Tag Delimiter", VSSetting="HTML Tag Delimiter")]
		public ChunkStyle HtmlTagDelimiter { get; private set; }
		
		[ColorDescription("Razor Code", VSSetting="Razor Code")]
		public ChunkStyle RazorCode { get; private set; }


		[ColorDescription("Css Comment", VSSetting="CSS Comment")]
		public ChunkStyle CssComment { get; private set; }

		[ColorDescription("Css Property Name", VSSetting="CSS Property Name")]
		public ChunkStyle CssPropertyName { get; private set; }
		
		[ColorDescription("Css Property Value", VSSetting="CSS Property Value")]
		public ChunkStyle CssPropertyValue { get; private set; }
		
		[ColorDescription("Css Selector", VSSetting="CSS Selector")]
		public ChunkStyle CssSelector { get; private set; }
		
		[ColorDescription("Css String Value", VSSetting="CSS String Value")]
		public ChunkStyle CssStringValue { get; private set; }
		
		[ColorDescription("Css Keyword", VSSetting="CSS Keyword")]
		public ChunkStyle CssKeyword { get; private set; }

		[ColorDescription("Script Comment", VSSetting="Script Comment")]
		public ChunkStyle ScriptComment { get; private set; }
		
		[ColorDescription("Script Identifier", VSSetting="Script Identifier")]
		public ChunkStyle ScriptIdentifier { get; private set; }
		
		[ColorDescription("Script Keyword", VSSetting="Script Keyword")]
		public ChunkStyle ScriptKeyword { get; private set; }
		
		[ColorDescription("Script Number", VSSetting="Script Number")]
		public ChunkStyle ScriptNumber { get; private set; }
		
		[ColorDescription("Script Operator", VSSetting="Script Operator")]
		public ChunkStyle ScriptOperator { get; private set; }

		[ColorDescription("Script String", VSSetting="Script String")]
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
				if (property.PropertyType == typeof (ChunkStyle)) {
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
				return null;
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

			if (result.Name != "Default")
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

				var baseStyle = SyntaxModeService.GetColorStyle (BaseScheme ?? "Default");

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

			var defaultStyle = SyntaxModeService.GetColorStyle (HslColor.Brightness (result.PlainText.Background) < 0.5 ? "Monokai" : "Default");

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
