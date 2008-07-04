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
		
		Color def, background, selectedBg, selectedFg;
		Color searchTextBg;
		Color caret;
		Color lineMarker, ruler, whitespaceMarker, invalidLineMarker;
		Color caretForeground;
		
		Color breakpointBg, breakpointFg;
		Color breakpointMarkerColor1, breakpointMarkerColor2;
		Color disabledBreakpointBg;
		Color currentDebugLineBg, currentDebugLineFg;
		Color currentDebugLineMarkerColor1, currentDebugLineMarkerColor2, currentDebugLineMarkerBorder;
		Color invalidBreakpointBg;
		Color invalidBreakpointMarkerColor1, invalidBreakpointMarkerBorder;
		
		Color bookmarkColor1, bookmarkColor2;
		
		Color lineNumberFg, lineNumberBg, lineNumberFgHighlighted;
		
		Color iconBarBg, iconBarSeperator;
		Color bracketHighlightBg, bracketHighlightRectangle;
		Color foldLine, foldLineHighlighted, foldBg, foldToggleMarker;
		List<ChunkStyle>        styles           = new List<Mono.TextEditor.ChunkStyle> ();
		Dictionary<string, int> styleLookupTable = new Dictionary<string, int> (); 
		Dictionary<string, string> customPalette = new Dictionary<string, string> (); 
		
		public virtual Color Default {
			get {
				return def;
			}
		}
		
		public virtual Color Caret {
			get {
				return caret;
			}
		}

		public virtual Color LineNumberFg {
			get {
				return lineNumberFg;
			}
		}

		public virtual Color LineNumberBg {
			get {
				return lineNumberBg;
			}
		}

		public virtual Color LineNumberFgHighlighted {
			get {
				return lineNumberFgHighlighted;
			}
		}

		public virtual Color IconBarBg {
			get {
				return iconBarBg;
			}
		}

		public virtual Color IconBarSeperator {
			get {
				return iconBarSeperator;
			}
		}

		public virtual Color FoldLine {
			get {
				return foldLine;
			}
		}

		public virtual Color FoldLineHighlighted {
			get {
				return foldLineHighlighted;
			}
		}

		public virtual Color FoldBg {
			get {
				return foldBg;
			}
		}

		public virtual Color Background {
			get {
				return background;
			}
		}

		public virtual Color SelectedBg {
			get {
				return selectedBg;
			}
		}
		
		public virtual Color SelectedFg {
			get {
				return selectedFg;
			}
		}

		public virtual Color LineMarker {
			get {
				return lineMarker;
			}
		}

		public virtual Color Ruler {
			get {
				return ruler;
			}
		}

		public virtual Color WhitespaceMarker {
			get {
				return whitespaceMarker;
			}
		}

		public virtual Color InvalidLineMarker {
			get {
				return invalidLineMarker;
			}
		}
		public virtual Color FoldToggleMarker {
			get {
				return foldToggleMarker;
			}
		}

		public virtual Color BracketHighlightBg {
			get {
				return bracketHighlightBg;
			}
		}
		
		public virtual Color BracketHighlightRectangle {
			get {
				return bracketHighlightRectangle;
			}
		}
		
		public virtual Color BookmarkColor2 {
			get {
				return bookmarkColor2;
			}
		}
		
		public virtual Color BookmarkColor1 {
			get {
				return bookmarkColor1;
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

		public virtual Color CaretForeground {
			get {
				return caretForeground;
			}
		}

		public Color SearchTextBg {
			get {
				return searchTextBg;
			}
			set {
				searchTextBg = value;
			}
		}

		public Color BreakpointFg {
			get {
				return breakpointFg;
			}
		}

		public Color BreakpointBg {
			get {
				return breakpointBg;
			}
		}

		public Color BreakpointMarkerColor2 {
			get {
				return breakpointMarkerColor2;
			}
		}

		public Color BreakpointMarkerColor1 {
			get {
				return breakpointMarkerColor1;
			}
		}

		public Color CurrentDebugLineFg {
			get {
				return currentDebugLineFg;
			}
			set {
				currentDebugLineFg = value;
			}
		}

		public Color CurrentDebugLineBg {
			get {
				return currentDebugLineBg;
			}
			set {
				currentDebugLineBg = value;
			}
		}

		public Color CurrentDebugLineMarkerColor2 {
			get {
				return currentDebugLineMarkerColor2;
			}
		}

		public Color CurrentDebugLineMarkerColor1 {
			get {
				return currentDebugLineMarkerColor1;
			}
		}

		public Color CurrentDebugLineMarkerBorder {
			get {
				return currentDebugLineMarkerBorder;
			}
		}

		public Color InvalidBreakpointBg {
			get {
				return invalidBreakpointBg;
			}
			set {
				invalidBreakpointBg = value;
			}
		}

		public Color InvalidBreakpointMarkerColor1 {
			get {
				return invalidBreakpointMarkerColor1;
			}
		}

		public Color DisabledBreakpointBg {
			get {
				return disabledBreakpointBg;
			}
		}

		public Color InvalidBreakpointMarkerBorder {
			get {
				return invalidBreakpointMarkerBorder;
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
			
			def          = new Gdk.Color (0, 0, 0); 
			
			caret        = new Gdk.Color (0, 0, 0); 
			caretForeground	 = new Gdk.Color (255, 255, 255);
			
			lineNumberBg = new Gdk.Color (255, 255, 255);
			lineNumberFg = new Gdk.Color (172, 168, 153);
			lineNumberFgHighlighted = new Gdk.Color (122, 118, 103);
			
			foldLine            = new Gdk.Color (172, 168, 153);
			foldLineHighlighted = new Gdk.Color (122, 118, 103);
			foldBg              = new Gdk.Color (255, 255, 255);
			foldToggleMarker    = new Gdk.Color (0, 0, 0);
			
			background = new Gdk.Color (255, 255, 255);
			selectedBg = new Gdk.Color (96, 87, 210);
			selectedFg = new Gdk.Color (255, 255, 255);
			lineMarker = new Gdk.Color (200, 255, 255);
			ruler      = new Gdk.Color (187, 187, 187);
			whitespaceMarker  = new Gdk.Color (187, 187, 187);
			invalidLineMarker = new Gdk.Color (210, 0, 0);
			
			breakpointBg = new Gdk.Color (125, 0, 0);
			breakpointFg = new Gdk.Color (255, 255, 255);
			
			this.breakpointMarkerColor1 = new Gdk.Color (255, 255, 255);
			this.breakpointMarkerColor2 = new Gdk.Color (125, 0, 0);
			this.disabledBreakpointBg = new Gdk.Color (237, 220, 220);
			this.currentDebugLineBg = new Gdk.Color (255, 255, 0);
			this.currentDebugLineFg = new Gdk.Color (0, 0, 0);
			
			this.currentDebugLineMarkerColor1 = new Gdk.Color (255, 255, 0);
			this.currentDebugLineMarkerColor2 = new Gdk.Color (255, 255, 204);
			this.currentDebugLineMarkerBorder = new Gdk.Color (102, 102, 0);
			this.invalidBreakpointBg          = new Gdk.Color (237, 220, 220);
			this.invalidBreakpointMarkerColor1 = new Gdk.Color (237, 220, 220);
			this.invalidBreakpointMarkerBorder = new Gdk.Color (125, 0, 0);
			
			searchTextBg = new Gdk.Color (250, 250, 0);
			
			bracketHighlightBg        = new Gdk.Color (196, 196, 196);
			bracketHighlightRectangle = new Gdk.Color (128, 128, 128);
			
			bookmarkColor1 = new Gdk.Color (255, 255, 255);
			bookmarkColor2 = new Gdk.Color (105, 156, 235);
			
			SetStyle ("default", new Mono.TextEditor.ChunkStyle (def, false, false));
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
			Gdk.Color color = GetColor (value);
			SetStyle (name, new Mono.TextEditor.ChunkStyle (color, 
			                                                weight == null ? false : weight.ToUpper ().IndexOf ("BOLD") >= 0,
			                                                weight == null ? false : weight.ToUpper ().IndexOf ("ITALIC") >= 0));
		}
		
		public static bool IsChunkStyle (string name)
		{
			switch (name) {
			case "default":
			case "background":
			case "caret":
			case "caretForeground":
			case "selectedBackground":
			case "selectedForeground":
			case "lineMarker":
			case "ruler":
			case "whitespaces":
			case "invalidLines":
			case "lineNumber":
			case "lineNumberHighlighted":
			case "lineNumberBg":
			case "iconBar":
			case "iconBarSeperator":
			case "foldLine":
			case "foldBg":
			case "foldLineHighlighted":
			case "foldToggleMarker":
			case "bracketHighlightRectangle":
			case "bracketHighlightBg":
			case "bookmarkColor1":
			case "bookmarkColor2":
			case "searchTextBg":
			
			case "breakpointBg":
			case "breakpointFg":
			case "breakpointMarkerColor1":
			case "breakpointMarkerColor2":
			case "disabledBreakpointBg":
			case "currentDebugLineBg":
			case "currentDebugLineFg":
			case "currentDebugLineMarkerColor1":
			case "currentDebugLineMarkerColor2":
			case "currentDebugLineMarkerBorder":
			case "invalidBreakpointBg":
			case "invalidBreakpointMarkerColor1":
			case "invalidBreakpointMarkerBorder":
				return false;
			}
			return true;
		}
		
		Gdk.Color GetColor (string colorString)
		{
			if (customPalette.ContainsKey (colorString))
				return GetColor (customPalette[colorString]);
			
			Gdk.Color result = new Color ();
			if (!Gdk.Color.Parse (colorString, ref result)) {
				throw new Exception ("Can't parse color: " + colorString);
			}
			return result;
		}
		
		public void SetColor (string name, string value)
		{
			Gdk.Color color = GetColor (value);
			switch (name) {
			case "default":
				this.def = color;
				SetStyle ("default", new Mono.TextEditor.ChunkStyle (color, false, false));
				break;
			case "caret":
				this.caret = color;
				break;
			case "caretForeground":
				this.caretForeground = color;
				break;
			case "background":
				this.background = color;
				break;
			case "selectedBackground":
				this.selectedBg = color;
				break;
			case "selectedForeground":
				this.selectedFg = color;
				break;
			case "lineMarker":
				this.lineMarker = color;
				break;
			case "ruler":
				this.ruler = color;
				break;
			case "whitespaces":
				this.whitespaceMarker = color;
				break;
			case "invalidLines":
				this.invalidLineMarker = color;
				break;
			case "lineNumber":
				this.lineNumberFg = color;
				break;
			case "lineNumberHighlighted":
				this.lineNumberFgHighlighted = color;
				break;
			case "lineNumberBg":
				this.lineNumberBg = color;
				break;
			case "iconBar":
				this.iconBarBg = color;
				break;
			case "iconBarSeperator":
				this.iconBarSeperator = color;
				break;
			case "foldLine":
				this.foldLine = color;
				break;
			case "foldBg":
				this.foldBg = color;
				break;
			case "foldLineHighlighted":
				this.foldLineHighlighted = color;
				break;
			case "foldToggleMarker":
				this.foldToggleMarker = color;
				break;
			case "bracketHighlightRectangle":
				this.bracketHighlightRectangle = color;
				break;
			case "bracketHighlightBg":
				this.bracketHighlightBg = color;
				break;
			case "bookmarkColor1":
				this.bookmarkColor1 = color;
				break;
			case "bookmarkColor2":
				this.bookmarkColor2 = color;
				break;
			case "searchTextBg":
				this.searchTextBg = color;
				break;
			case "breakpointBg":
				this.breakpointBg = color;
				break;
			case "breakpointFg":
				this.breakpointFg = color;
				break;
			case "breakpointMarkerColor1":
				this.breakpointMarkerColor1 = color;
				break;
			case "breakpointMarkerColor2":
				this.breakpointMarkerColor2 = color;
				break;
			case "disabledBreakpointBg":
				this.disabledBreakpointBg = color;
				break;
			case "currentDebugLineBg":
				this.currentDebugLineBg = color;
				break;
			case "currentDebugLineFg":
				this.currentDebugLineFg = color;
				break;
			case "currentDebugLineMarkerColor1":
				this.currentDebugLineMarkerColor1 = color;
				break;
			case "currentDebugLineMarkerColor2":
				this.currentDebugLineMarkerColor2 = color;
				break;
			case "currentDebugLineMarkerBorder":
				this.currentDebugLineMarkerBorder = color;
				break;
			case "invalidBreakpointBg":
				this.invalidBreakpointBg = color;
				break;
			case "invalidBreakpointMarkerColor1":
				this.invalidBreakpointMarkerColor1 = color;
				break;
			case "invalidBreakpointMarkerBorder":
				this.invalidBreakpointMarkerBorder = color;
				break;
			default:
				throw new Exception ("color  " + name + " invalid.");
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
