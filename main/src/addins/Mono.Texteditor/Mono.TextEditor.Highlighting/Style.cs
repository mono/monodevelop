// Style.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Xml;
using Gdk;

namespace Mono.TextEditor.Highlighting
{
	public class Style
	{
		string name;
		string description;
		
		Dictionary<string, IColorDefinition> colors = new Dictionary<string, IColorDefinition> ();
		Dictionary<string, ChunkStyle> styleLookupTable = new Dictionary<string, ChunkStyle> (); 
		Dictionary<string, string> customPalette = new Dictionary<string, string> (); 
		
		public Color GetColorFromDefinition (string colorName)
		{
			IColorDefinition definition;
			if (colors.TryGetValue (colorName, out definition))
				return definition.Color;
			
			int dotIndex = colorName.LastIndexOf ('.');
			string fallbackName = colorName;
			while (dotIndex > 1) {
				fallbackName = fallbackName.Substring (0, dotIndex);
				if (colors.TryGetValue (fallbackName, out definition)) {
					colors[colorName] = definition;
					Console.WriteLine ("Color {0} fell back to {1}", colorName, fallbackName);
					return definition.Color;
				}
				dotIndex = fallbackName.LastIndexOf ('.');
			}
			
			Console.WriteLine ("Color {0} fell back to default", colorName);
			if (colors.TryGetValue (DefaultString, out definition))
				return definition.Color;
			
			return new Gdk.Color (0, 0, 0);
		}
		
		#region Named colors
		
		public const string DefaultString = "text";
		public virtual Color Default {
			get {
				return GetColorFromDefinition (DefaultString);
			}
		}
		
		public const string CaretString = "caret";
		public virtual Color Caret {
			get {
				return GetColorFromDefinition ("caret");
			}
		}

		public const string LineNumberFgString = "linenumber";
		public virtual Color LineNumberFg {
			get {
				return GetColorFromDefinition (LineNumberFgString);
			}
		}

		public const string LineNumberBgString = "linenumber.background";
		public virtual Color LineNumberBg {
			get {
				return GetColorFromDefinition (LineNumberBgString);
			}
		}

		public const string LineNumberFgHighlightedString = "linenumber.highlight";
		public virtual Color LineNumberFgHighlighted {
			get {
				return GetColorFromDefinition (LineNumberFgHighlightedString);
			}
		}

		public const string IconBarBgString = "iconbar";
		public virtual Color IconBarBg {
			get {
				return GetColorFromDefinition (IconBarBgString);
			}
		}

		public const string IconBarSeperatorString = "iconbar.separator";
		public virtual Color IconBarSeperator {
			get {
				return GetColorFromDefinition (IconBarSeperatorString);
			}
		}

		public const string FoldLineString = "fold";
		public virtual Color FoldLine {
			get {
				return GetColorFromDefinition (FoldLineString);
			}
		}

		public const string FoldLineHighlightedString = "fold.highlight";
		public virtual Color FoldLineHighlighted {
			get {
				return GetColorFromDefinition (FoldLineHighlightedString);
			}
		}
		
		public const string FoldBgString = "fold.background";
		public virtual Color FoldBg {
			get {
				return GetColorFromDefinition (FoldBgString);
			}
		}
		
		public const string LineChangedBgString = "marker.line.changed";
		public virtual Color LineChangedBg {
			get {
				return GetColorFromDefinition (LineChangedBgString);
			}
		}
		
		public const string LineDirtyBgString = "marker.line.dirty";
		public virtual Color LineDirtyBg {
			get {
				return GetColorFromDefinition (LineDirtyBgString);
			}
		}

		public const string BackgroundString = "text.background";
		public virtual Color Background {
			get {
				return GetColorFromDefinition (BackgroundString);
			}
		}

		public const string SelectedBgString = "text.background.selection";
		public virtual Color SelectedBg {
			get {
				return GetColorFromDefinition (SelectedBgString);
			}
		}
		
		public const string SelectedFgString = "text.selection";
		public virtual Color SelectedFg {
			get {
				return GetColorFromDefinition (SelectedFgString);
			}
		}

		public const string LineMarkerString = "marker.line";
		public virtual Color LineMarker {
			get {
				return GetColorFromDefinition (LineMarkerString);
			}
		}

		public const string RulerString = "marker.ruler";
		public virtual Color Ruler {
			get {
				return GetColorFromDefinition (RulerString);
			}
		}

		public const string WhitespaceMarkerString = "marker.whitespace";
		public virtual Color WhitespaceMarker {
			get {
				return GetColorFromDefinition (WhitespaceMarkerString);
			}
		}

		public const string InvalidLineMarkerString = "marker.invalidline";
		public virtual Color InvalidLineMarker {
			get {
				return GetColorFromDefinition (InvalidLineMarkerString);
			}
		}
		
		public const string FoldToggleMarkerString = "fold.togglemarker";
		public virtual Color FoldToggleMarker {
			get {
				return GetColorFromDefinition (FoldToggleMarkerString);
			}
		}

		public const string BracketHighlightBgString = "marker.bracket.background";
		public virtual Color BracketHighlightBg {
			get {
				return GetColorFromDefinition (BracketHighlightBgString);
			}
		}
		
		public const string BracketHighlightRectangleString = "marker.bracket";
		public virtual Color BracketHighlightRectangle {
			get {
				return GetColorFromDefinition (BracketHighlightRectangleString);
			}
		}
		
		public const string BookmarkColor2String = "marker.bookmark.color2";
		public virtual Color BookmarkColor2 {
			get {
				return GetColorFromDefinition (BookmarkColor2String);
			}
		}
		
		public const string BookmarkColor1String = "marker.bookmark.color1";
		public virtual Color BookmarkColor1 {
			get {
				return GetColorFromDefinition (BookmarkColor1String);
			}
		}

		public const string CaretForegroundString = "caret.foreground";
		public virtual Color CaretForeground {
			get {
				return GetColorFromDefinition (CaretForegroundString);
			}
		}
		
		public const string SearchTextBgString = "text.background.searchresult";
		public Color SearchTextBg {
			get {
				return GetColorFromDefinition (SearchTextBgString);
			}
		}

		public const string BreakpointFgString = "marker.breakpoint.foreground";
		public Color BreakpointFg {
			get {
				return GetColorFromDefinition (BreakpointFgString);
			}
		}

		public const string BreakpointBgString = "marker.breakpoint.background";
		public Color BreakpointBg {
			get {
				return GetColorFromDefinition (BreakpointBgString);
			}
		}

		public const string BreakpointMarkerColor2String = "marker.breakpoint.color2";
		public Color BreakpointMarkerColor2 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor2String);
			}
		}

		public const string BreakpointMarkerColor1String = "marker.breakpoint.color1";
		public Color BreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor1String);
			}
		}

		public const string CurrentDebugLineFgString = "marker.debug.currentline.foreground";
		public Color CurrentDebugLineFg {
			get {
				return GetColorFromDefinition (CurrentDebugLineFgString);
			}
		}
		
		public const string CurrentDebugLineBgString = "marker.debug.currentline.background";
		public Color CurrentDebugLineBg {
			get {
				return GetColorFromDefinition (CurrentDebugLineBgString);
			}
		}

		public const string CurrentDebugLineMarkerColor2String = "marker.debug.currentline.color2";
		public Color CurrentDebugLineMarkerColor2 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor2String);
			}
		}

		public const string CurrentDebugLineMarkerColor1String = "marker.debug.currentline.color1";
		public Color CurrentDebugLineMarkerColor1 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor1String);
			}
		}

		public const string CurrentDebugLineMarkerBorderString = "marker.debug.currentline.border";
		public Color CurrentDebugLineMarkerBorder {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerBorderString);
			}
		}

		public const string InvalidBreakpointBgString = "marker.breakpoint.invalid.background";
		public Color InvalidBreakpointBg {
			get {
				return GetColorFromDefinition (InvalidBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerColor1String = "marker.breakpoint.invalid.color1";
		public Color InvalidBreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerColor1String);
			}
		}

		public const string DisabledBreakpointBgString = "marker.breakpoint.disabled.background";
		public Color DisabledBreakpointBg {
			get {
				return GetColorFromDefinition (DisabledBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerBorderString = "marker.breakpoint.invalid.border";
		public Color InvalidBreakpointMarkerBorder {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerBorderString);
			}
		}
		
		public const string ErrorUnderlineString = "marker.underline.error";
		public Color ErrorUnderline {
			get {
				return GetColorFromDefinition (ErrorUnderlineString);
			}
		}
		
		public const string WarningUnderlineString = "marker.underline.warning";
		public Color WarningUnderline {
			get {
				return GetColorFromDefinition (WarningUnderlineString);
			}
		}
		
		#endregion
		
		public string Name {
			get {
				return name;
			}
		}
		
		public string Description {
			get {
				return description;
			}
		}
		
		public static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue);
		}
		
		protected Style ()
		{
			colors[DefaultString] = new ColorDefinition (new Gdk.Color (0, 0, 0));
			colors[BackgroundString] = new ColorDefinition (new Gdk.Color (255, 255, 255));
			
			colors[CaretString]           = new ReferencedColorDefinition (this, DefaultString);
			colors[CaretForegroundString] = new ReferencedColorDefinition (this, BackgroundString);

			colors[LineNumberBgString]            = new ReferencedColorDefinition (this, BackgroundString);
			colors[LineNumberFgString]            = new ColorDefinition (new Gdk.Color (172, 168, 153));
			colors[LineNumberFgHighlightedString] = new ColorDefinition (new Gdk.Color (122, 118, 103));
			
			colors[IconBarBgString]           = new ReferencedColorDefinition (this, BackgroundString);
			colors[IconBarSeperatorString]    = new ReferencedColorDefinition (this, LineNumberFgString);
			
			colors[FoldBgString]              = new ReferencedColorDefinition (this, BackgroundString);
			colors[FoldLineString]            = new ReferencedColorDefinition (this, LineNumberFgString);
			colors[FoldLineHighlightedString] = new ReferencedColorDefinition (this, LineNumberFgHighlightedString);
			colors[FoldToggleMarkerString]    = new ReferencedColorDefinition (this, DefaultString);
			
			colors[LineDirtyBgString]         = new ColorDefinition (new Gdk.Color (255, 238, 98));
			colors[LineChangedBgString]       = new ColorDefinition (new Gdk.Color (108, 226, 108));
			
			colors[SelectedBgString] = new ColorDefinition (new Gdk.Color (96, 87, 210));
			colors[SelectedFgString] = new ReferencedColorDefinition (this, BackgroundString);
			
			
			colors[LineMarkerString] = new ColorDefinition (new Gdk.Color (200, 255, 255));
			colors[RulerString] = new ColorDefinition (new Gdk.Color (187, 187, 187));
			colors[WhitespaceMarkerString] = new ReferencedColorDefinition (this, RulerString);
			
			colors[InvalidLineMarkerString] = new ColorDefinition (new Gdk.Color (210, 0, 0));
			
			colors[BreakpointBgString] = new ColorDefinition (new Gdk.Color (125, 0, 0));
			colors[BreakpointFgString] = new ReferencedColorDefinition (this, BackgroundString);
			
			colors[BreakpointMarkerColor1String] = new ReferencedColorDefinition (this, BackgroundString);
			colors[BreakpointMarkerColor2String] = new ColorDefinition (new Gdk.Color (125, 0, 0));

			colors[DisabledBreakpointBgString] = new ColorDefinition (new Gdk.Color (237, 220, 220));
			
			colors[CurrentDebugLineBgString] = new ColorDefinition (new Gdk.Color (255, 255, 0));
			colors[CurrentDebugLineFgString] = new ColorDefinition (new Gdk.Color (0, 0, 0));
			
			colors[CurrentDebugLineMarkerColor1String] = new ColorDefinition (new Gdk.Color (255, 255, 0));
			colors[CurrentDebugLineMarkerColor2String] = new ColorDefinition (new Gdk.Color (255, 255, 204));
			colors[CurrentDebugLineMarkerBorderString] = new ColorDefinition (new Gdk.Color (102, 102, 0));
			colors[InvalidBreakpointBgString] = new ColorDefinition (new Gdk.Color (237, 220, 220));
			colors[InvalidBreakpointMarkerColor1String] = new ColorDefinition (new Gdk.Color (237, 220, 220));
			colors[InvalidBreakpointMarkerBorderString] = new ColorDefinition (new Gdk.Color (125, 0, 0));
			colors[SearchTextBgString] = new ColorDefinition (new Gdk.Color (250, 250, 0));
			colors[BracketHighlightBgString] = new ColorDefinition (new Gdk.Color (196, 196, 196));
			colors[BracketHighlightRectangleString] = new ColorDefinition (new Gdk.Color (128, 128, 128));
			
			colors[BookmarkColor1String] = new ColorDefinition (new Gdk.Color (255, 255, 255));
			colors[BookmarkColor2String] = new ColorDefinition (new Gdk.Color (105, 156, 235));
			
			colors[ErrorUnderlineString] = new ColorDefinition (new Gdk.Color (255, 0, 0));
			colors[WarningUnderlineString] = new ColorDefinition (new Gdk.Color (30, 30, 255));
		}
		
		protected void PopulateDefaults ()
		{
			SetStyle ("text", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), false, false));
			SetStyle ("text.punctuation", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), false, false));
			SetStyle ("text.link", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("text.preprocessor", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 128 , 0), false, false));
			SetStyle ("text.preprocessor.keyword", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 128, 0), true, false));
			SetStyle ("text.markup", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0x8A , 0x8C), false, false));
			SetStyle ("text.markup.tag", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0x6A, 0x5A, 0xCD), false, false));
			
			SetStyle ("comment", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("comment.line", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("comment.block", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("comment.doc", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("comment.tag", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), false, true));
			SetStyle ("comment.tag.line", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), false, true));
			SetStyle ("comment.tag.block", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), false, true));
			SetStyle ("comment.tag.doc", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), false, true));
			SetStyle ("comment.keyword", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0,255), false, true));
			SetStyle ("comment.keyword.todo", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), true, false));
			
			SetStyle ("constant", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			SetStyle ("constant.digit", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			SetStyle ("constant.language", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("constant.language.void", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			
			SetStyle ("string", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			SetStyle ("string.single", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			SetStyle ("string.double", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			SetStyle ("string.other", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			
			SetStyle ("keyword", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), true, false));
			
			SetStyle ("keyword.access", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.operator", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.operator.declaration", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.selection", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.iteration", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.jump", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.context", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.exceptions", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.modifier", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.type", new Mono.TextEditor.ChunkStyle (new Gdk.Color ( 46, 139,  87), true, false));
			SetStyle ("keyword.namespace", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.property", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.declaration", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.parameter", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("keyword.misc", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			
		}
		
		void SetStyle (string name, ChunkStyle style)
		{
			styleLookupTable[name] = style;
		}
		
		public ChunkStyle GetDefaultChunkStyle ()
		{
			ChunkStyle style;
			if (!styleLookupTable.TryGetValue (DefaultString, out style)) {
				style = new ChunkStyle (GetColorFromDefinition (DefaultString));
				styleLookupTable[DefaultString] = style;
			}
			return style;
		}
		
		public ChunkStyle GetChunkStyle (string name)
		{
			ChunkStyle style;
			if (styleLookupTable.TryGetValue (name, out style))
				return style;
			
			int dotIndex = name.LastIndexOf ('.');
			string fallbackName = name;
			while (dotIndex > 1) {
				fallbackName = fallbackName.Substring (0, dotIndex);
				if (styleLookupTable.TryGetValue (fallbackName, out style)) {
					styleLookupTable[name] = style;
					Console.WriteLine ("Chunk style {0} fell back to {1}", name, fallbackName);
					return style;
				}
				dotIndex = fallbackName.LastIndexOf ('.');
			}
			
			Console.WriteLine ("Chunk style {0} fell back to default", name);
			return GetDefaultChunkStyle ();
		}
		
		public void SetChunkStyle (string name, string weight, string value)
		{
			Gdk.Color color = this.GetColorFromString (value);
			SetStyle (name, new Mono.TextEditor.ChunkStyle (color, 
			                                                weight == null ? false : weight.ToUpper ().IndexOf ("BOLD") >= 0,
			                                                weight == null ? false : weight.ToUpper ().IndexOf ("ITALIC") >= 0));
		}
		
		public static bool IsChunkStyle (string name)
		{
			
			switch (name) {
			case DefaultString:
			case CaretString:
			case LineNumberFgString:
			case LineNumberBgString:
			case LineNumberFgHighlightedString:
			case IconBarBgString:
			case IconBarSeperatorString:
			case FoldLineString:
			case FoldLineHighlightedString:
			case FoldBgString:
			case LineDirtyBgString:
			case LineChangedBgString:
			case BackgroundString:
			case SelectedBgString:
			case SelectedFgString:
			case LineMarkerString:
			case RulerString:
			case WhitespaceMarkerString:
			case InvalidLineMarkerString:
			case FoldToggleMarkerString:
			case BracketHighlightBgString:
			case BracketHighlightRectangleString:
			case BookmarkColor2String:
			case BookmarkColor1String:
			case CaretForegroundString:
			case SearchTextBgString:
			case BreakpointFgString:
			case BreakpointBgString:
			case BreakpointMarkerColor2String:
			case BreakpointMarkerColor1String:
			case CurrentDebugLineFgString:
			case CurrentDebugLineBgString:
			case CurrentDebugLineMarkerColor2String:
			case CurrentDebugLineMarkerColor1String:
			case CurrentDebugLineMarkerBorderString:
			case InvalidBreakpointBgString:
			case InvalidBreakpointMarkerColor1String:
			case DisabledBreakpointBgString:
			case InvalidBreakpointMarkerBorderString:
				return false;
			}
			return true;
		}
		
		public Gdk.Color GetColorFromString (string colorString)
		{
			if (customPalette.ContainsKey (colorString))
				return this.GetColorFromString (customPalette[colorString]);
			
			if (colors.ContainsKey (colorString)) 
				return ((IColorDefinition)colors[colorString]).Color;
			if (styleLookupTable.ContainsKey (colorString)) 
				return styleLookupTable[colorString].Color;
			
			Gdk.Color result = new Color ();
			if (!Gdk.Color.Parse (colorString, ref result)) 
				throw new Exception ("Can't parse color: " + colorString);
			return result;
		}
		
		public void SetColor (string name, string value)
		{
			if (!IsChunkStyle (value)) {
				colors[name] = new ReferencedColorDefinition (this, value);
			} else {
				Gdk.Color color = GetColorFromString (value);
				colors[name] = new ColorDefinition (color);
				if (name == DefaultString)
					this.SetStyle (DefaultString, new Mono.TextEditor.ChunkStyle (color, false, false));
			}
		}
		
		public const string NameAttribute = "name";
		
		static void ReadStyleTree (XmlReader reader, Style result, string curName, string curWeight, string curColor)
		{
			string name   = reader.GetAttribute ("name"); 
			string weight = reader.GetAttribute ("weight") ?? curWeight;
			string color  = reader.GetAttribute ("color") ?? curColor;
			string fullName;
			if (String.IsNullOrEmpty (curName)) {
				fullName = name;
			} else {
				fullName = curName + "." + name;
			}
			if (!String.IsNullOrEmpty (color)) {
				if (IsChunkStyle (fullName)) {
					result.SetChunkStyle (fullName, weight, color);
				} else {
					result.SetColor (fullName, color);
				}
			}
			XmlReadHelper.ReadList (reader, "Style", delegate () {
				switch (reader.LocalName) {
				case "Style":
					ReadStyleTree (reader, result, fullName, weight, color);
					return true;
				}
				return false;
			});
		}
		
		public static Style LoadFrom (XmlReader reader)
		{
			Style result = new Style ();
			XmlReadHelper.ReadList (reader, "EditorStyle", delegate () {
				switch (reader.LocalName) {
				case "EditorStyle":
					result.name        = reader.GetAttribute (NameAttribute);
					result.description = reader.GetAttribute ("_description");
					return true;
				case "Color":
					result.customPalette[reader.GetAttribute ("name")] = reader.GetAttribute ("value");
					return true;
				case "Style":
					ReadStyleTree (reader, result, null, null, null);
					return true;
				}
				return false;
			});
			
			return result;
		}
	}
}
