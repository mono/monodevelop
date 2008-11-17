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
		
		List<ChunkStyle>        styles           = new List<Mono.TextEditor.ChunkStyle> ();
		Dictionary<string, int> styleLookupTable = new Dictionary<string, int> (); 
		Dictionary<string, string> customPalette = new Dictionary<string, string> (); 

		public Color GetColorFromDefinition (string colorName)
		{
			IColorDefinition definition;
			if (colors.TryGetValue (colorName, out definition))
				return definition.Color;
			return new Gdk.Color (0, 0, 0); 
		}
		
		public const string DefaultString = "default";
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

		public const string LineNumberFgString = "lineNumber";
		public virtual Color LineNumberFg {
			get {
				return GetColorFromDefinition (LineNumberFgString);
			}
		}

		public const string LineNumberBgString = "lineNumberBg";
		public virtual Color LineNumberBg {
			get {
				return GetColorFromDefinition (LineNumberBgString);
			}
		}

		public const string LineNumberFgHighlightedString = "lineNumberHighlighted";
		public virtual Color LineNumberFgHighlighted {
			get {
				return GetColorFromDefinition (LineNumberFgHighlightedString);
			}
		}

		public const string IconBarBgString = "iconBar";
		public virtual Color IconBarBg {
			get {
				return GetColorFromDefinition (IconBarBgString);
			}
		}

		public const string IconBarSeperatorString = "iconBarSeperator";
		public virtual Color IconBarSeperator {
			get {
				return GetColorFromDefinition (IconBarSeperatorString);
			}
		}

		public const string FoldLineString = "foldLine";
		public virtual Color FoldLine {
			get {
				return GetColorFromDefinition (FoldLineString);
			}
		}

		public const string FoldLineHighlightedString = "foldLineHighlighted";
		public virtual Color FoldLineHighlighted {
			get {
				return GetColorFromDefinition (FoldLineHighlightedString);
			}
		}
		
		public const string FoldBgString = "foldBg";
		public virtual Color FoldBg {
			get {
				return GetColorFromDefinition (FoldBgString);
			}
		}

		public const string BackgroundString = "background";
		public virtual Color Background {
			get {
				return GetColorFromDefinition (BackgroundString);
			}
		}

		public const string SelectedBgString = "selectedBackground";
		public virtual Color SelectedBg {
			get {
				return GetColorFromDefinition (SelectedBgString);
			}
		}
		
		public const string SelectedFgString = "selectedForeground";
		public virtual Color SelectedFg {
			get {
				return GetColorFromDefinition (SelectedFgString);
			}
		}

		public const string LineMarkerString = "lineMarker";
		public virtual Color LineMarker {
			get {
				return GetColorFromDefinition (LineMarkerString);
			}
		}

		public const string RulerString = "ruler";
		public virtual Color Ruler {
			get {
				return GetColorFromDefinition (RulerString);
			}
		}

		public const string WhitespaceMarkerString = "whitespaces";
		public virtual Color WhitespaceMarker {
			get {
				return GetColorFromDefinition (WhitespaceMarkerString);
			}
		}

		public const string InvalidLineMarkerString = "invalidLines";
		public virtual Color InvalidLineMarker {
			get {
				return GetColorFromDefinition (InvalidLineMarkerString);
			}
		}
		
		public const string FoldToggleMarkerString = "foldToggleMarker";
		public virtual Color FoldToggleMarker {
			get {
				return GetColorFromDefinition (FoldToggleMarkerString);
			}
		}

		public const string BracketHighlightBgString = "bracketHighlightBg";
		public virtual Color BracketHighlightBg {
			get {
				return GetColorFromDefinition (BracketHighlightBgString);
			}
		}
		
		public const string BracketHighlightRectangleString = "bracketHighlightRectangle";
		public virtual Color BracketHighlightRectangle {
			get {
				return GetColorFromDefinition (BracketHighlightRectangleString);
			}
		}
		
		public const string BookmarkColor2String = "bookmarkColor2";
		public virtual Color BookmarkColor2 {
			get {
				return GetColorFromDefinition (BookmarkColor2String);
			}
		}
		
		public const string BookmarkColor1String = "bookmarkColor1";
		public virtual Color BookmarkColor1 {
			get {
				return GetColorFromDefinition (BookmarkColor1String);
			}
		}

		public const string CaretForegroundString = "caretForeground";
		public virtual Color CaretForeground {
			get {
				return GetColorFromDefinition (CaretForegroundString);
			}
		}
		
		public const string SearchTextBgString = "searchTextBg";
		public Color SearchTextBg {
			get {
				return GetColorFromDefinition (SearchTextBgString);
			}
		}

		public const string BreakpointFgString = "breakpointFg";
		public Color BreakpointFg {
			get {
				return GetColorFromDefinition (BreakpointFgString);
			}
		}

		public const string BreakpointBgString = "breakpointBg";
		public Color BreakpointBg {
			get {
				return GetColorFromDefinition (BreakpointBgString);
			}
		}

		public const string BreakpointMarkerColor2String = "breakpointMarkerColor2";
		public Color BreakpointMarkerColor2 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor2String);
			}
		}

		public const string BreakpointMarkerColor1String = "breakpointMarkerColor1";
		public Color BreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor1String);
			}
		}

		public const string CurrentDebugLineFgString = "currentDebugLineFg";
		public Color CurrentDebugLineFg {
			get {
				return GetColorFromDefinition (CurrentDebugLineFgString);
			}
		}
		
		public const string CurrentDebugLineBgString = "currentDebugLineBg";
		public Color CurrentDebugLineBg {
			get {
				return GetColorFromDefinition (CurrentDebugLineBgString);
			}
		}

		public const string CurrentDebugLineMarkerColor2String = "currentDebugLineMarkerColor2";
		public Color CurrentDebugLineMarkerColor2 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor2String);
			}
		}

		public const string CurrentDebugLineMarkerColor1String = "currentDebugLineMarkerColor1";
		public Color CurrentDebugLineMarkerColor1 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor1String);
			}
		}

		public const string CurrentDebugLineMarkerBorderString = "currentDebugLineMarkerBorder";
		public Color CurrentDebugLineMarkerBorder {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerBorderString);
			}
		}

		public const string InvalidBreakpointBgString = "invalidBreakpointBg";
		public Color InvalidBreakpointBg {
			get {
				return GetColorFromDefinition (InvalidBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerColor1String = "invalidBreakpointMarkerColor1";
		public Color InvalidBreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerColor1String);
			}
		}

		public const string DisabledBreakpointBgString = "disabledBreakpointBg";
		public Color DisabledBreakpointBg {
			get {
				return GetColorFromDefinition (DisabledBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerBorderString = "invalidBreakpointMarkerBorder";
		public Color InvalidBreakpointMarkerBorder {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerBorderString);
			}
		}
		
		public const string ErrorUnderlineString = "errorUnderline";
		public Color ErrorUnderline {
			get {
				return GetColorFromDefinition (ErrorUnderlineString);
			}
		}
		
		public const string WarningUnderlineString = "warningUnderline";
		public Color WarningUnderline {
			get {
				return GetColorFromDefinition (WarningUnderlineString);
			}
		}
		
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
		
		public Style ()
		{
			colors[DefaultString] = new ColorDefinition (new Gdk.Color (0, 0, 0));
			colors[BackgroundString] = new ColorDefinition (new Gdk.Color (255, 255, 255));
			
			colors[CaretString]           = new ReferencedColorDefinition (this, DefaultString);
			colors[CaretForegroundString] = new ReferencedColorDefinition (this, BackgroundString);

			colors[LineNumberBgString]            = new ReferencedColorDefinition (this, BackgroundString);
			colors[LineNumberFgString]            = new ColorDefinition (new Gdk.Color (172, 168, 153));
			colors[LineNumberFgHighlightedString] = new ColorDefinition (new Gdk.Color (122, 118, 103));
			
			colors[FoldBgString]              = new ReferencedColorDefinition (this, BackgroundString);
			colors[FoldLineString]            = new ReferencedColorDefinition (this, LineNumberFgString);
			colors[FoldLineHighlightedString] = new ReferencedColorDefinition (this, LineNumberFgHighlightedString);
			colors[FoldToggleMarkerString]    = new ReferencedColorDefinition (this, DefaultString);
			
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
			
			SetStyle ("default", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), false, false));
			SetStyle ("comment", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("altcomment", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), true, false));
			SetStyle ("todocomment", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), true, false));
			SetStyle ("commentkw", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0,255), false, true));
			SetStyle ("commenttag", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), false, true));
			
			SetStyle ("digit", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			SetStyle ("literal", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255), false, false));
			SetStyle ("punctuation", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), false, false));
			
			SetStyle ("kw:access", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:operator", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:selection", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:iteration", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:jump", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:context", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:exceptions", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:literals", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:modifiers", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:types", new Mono.TextEditor.ChunkStyle (new Gdk.Color ( 46, 139,  87), true, false));
			SetStyle ("kw:namespaces", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:properties", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:declarations", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:parameter", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:operatordecl", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:misc", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			SetStyle ("kw:void", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), true, false));
			
			SetStyle ("preprocessor", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 128 , 0), false, false));
			SetStyle ("preprocessorkw", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 128, 0), true, false));

			SetStyle ("markup", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0x8A , 0x8C), false, false));
			SetStyle ("markupTag", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0x6A, 0x5A, 0xCD), false, false));
		}
		
		void SetStyle (string name, ChunkStyle style)
		{
			if (!styleLookupTable.ContainsKey (name)) {
				styleLookupTable.Add (name, styles.Count);
				styles.Add (style);
			} else {
				styles[styleLookupTable[name]] = style;
			}
		}
		
		public ChunkStyle GetChunkStyle (string name)
		{
			if (!styleLookupTable.ContainsKey (name)) 
				return null;
			return this.styles [styleLookupTable[name]];
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
		
		Gdk.Color GetColorFromString (string colorString)
		{
			if (customPalette.ContainsKey (colorString))
				return this.GetColorFromString (customPalette[colorString]);
			
			Gdk.Color result = new Color ();
			if (!Gdk.Color.Parse (colorString, ref result)) {
				throw new Exception ("Can't parse color: " + colorString);
			}
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
		public static Style Read (XmlReader reader)
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
					string name = reader.GetAttribute ("name"); 
					if (IsChunkStyle (name)) {
						result.SetChunkStyle (name,
						                 reader.GetAttribute ("weight"),
						                 reader.GetAttribute ("color"));
					} else {
						result.SetColor (name, reader.GetAttribute ("color"));
					}
					return true;
				}
				return false;
			});
			
			return result;
		}
	}
}
