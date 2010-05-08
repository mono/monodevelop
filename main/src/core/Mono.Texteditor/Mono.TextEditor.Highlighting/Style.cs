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
		Dictionary<string, ChunkStyle> styleLookupTable = new Dictionary<string, ChunkStyle> (); 
		Dictionary<string, string> customPalette = new Dictionary<string, string> (); 
		
		public Color GetColorFromDefinition (string colorName)
		{
			return this.GetChunkStyle (colorName).Color;
		}
		
		#region Named colors
		
		public const string DefaultString = "text";
		public virtual ChunkStyle Default {
			get {
				return GetChunkStyle (DefaultString);
			}
		}
		
		public const string CaretString = "caret";
		public virtual ChunkStyle Caret {
			get {
				return GetChunkStyle (CaretString);
			}
		}

		public const string LineNumberString = "linenumber";
		public virtual ChunkStyle LineNumber {
			get {
				return GetChunkStyle (LineNumberString);
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
		public virtual ChunkStyle FoldLine {
			get {
				return GetChunkStyle (FoldLineString);
			}
		}

		public const string FoldLineHighlightedString = "fold.highlight";
		public virtual Color FoldLineHighlighted {
			get {
				return GetColorFromDefinition (FoldLineHighlightedString);
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

		public const string SelectionString = "text.selection";
		public virtual ChunkStyle Selection {
			get {
				return GetChunkStyle (SelectionString);
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
		
		public const string BracketHighlightRectangleString = "marker.bracket";
		public virtual ChunkStyle BracketHighlightRectangle {
			get {
				return GetChunkStyle (BracketHighlightRectangleString);
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

		
		public const string SearchTextBgString = "text.background.searchresult";
		public Color SearchTextBg {
			get {
				return GetColorFromDefinition (SearchTextBgString);
			}
		}
		
		public const string SearchTextMainBgString = "text.background.searchresult-main";
		public Color SearchTextMainBg {
			get {
				return GetColorFromDefinition (SearchTextMainBgString);
			}
		}

		public const string BreakpointString = "marker.breakpoint";
		public Color BreakpointFg {
			get {
				return GetChunkStyle (BreakpointString).Color;
			}
		}
		public Color BreakpointBg {
			get {
				return GetChunkStyle (BreakpointString).BackgroundColor;
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

		public const string CurrentDebugLineString = "marker.debug.currentline";
		public Color CurrentDebugLineFg {
			get {
				return GetChunkStyle (CurrentDebugLineString).Color;
			}
		}
		
		public Color CurrentDebugLineBg {
			get {
				return GetChunkStyle (CurrentDebugLineString).BackgroundColor;
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
		
		public const string DebugStackLineString = "marker.debug.stackline";
		public Color DebugStackLineFg {
			get {
				return GetChunkStyle (DebugStackLineString).Color;
			}
		}
		
		public Color DebugStackLineBg {
			get {
				return GetChunkStyle (DebugStackLineString).BackgroundColor;
			}
		}

		public const string DebugStackLineMarkerColor2String = "marker.debug.stackline.color2";
		public Color DebugStackLineMarkerColor2 {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerColor2String);
			}
		}

		public const string DebugStackLineMarkerColor1String = "marker.debug.stackline.color1";
		public Color DebugStackLineMarkerColor1 {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerColor1String);
			}
		}

		public const string DebugStackLineMarkerBorderString = "marker.debug.stackline.border";
		public Color DebugStackLineMarkerBorder {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerBorderString);
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
		
		public const string PrimaryTemplateColorString = "marker.template.primary_template";
		public virtual ChunkStyle PrimaryTemplate {
			get {
				return GetChunkStyle (PrimaryTemplateColorString);
			}
		}
		
		public const string PrimaryTemplateHighlightedColorString = "marker.template.primary_highlighted_template";
		public virtual ChunkStyle PrimaryTemplateHighlighted {
			get {
				return GetChunkStyle (PrimaryTemplateHighlightedColorString);
			}
		}
		
		public const string SecondaryTemplateColorString = "marker.template.secondary_template";
		public virtual ChunkStyle SecondaryTemplate {
			get {
				return GetChunkStyle (SecondaryTemplateColorString);
			}
		}
		
		public const string SecondaryTemplateHighlightedColorString = "marker.template.secondary_highlighted_template";
		public virtual ChunkStyle SecondaryTemplateHighlighted {
			get {
				return GetChunkStyle (SecondaryTemplateHighlightedColorString);
			}
		}
		#endregion
		
		public string Name {
			get;
			private set;
		}
		
		public string Description {
			get;
			private set;
		}
		
		public static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue);
		}
		
		protected Style ()
		{
			SetStyle (DefaultString, 0, 0, 0, 255, 255, 255);
			GetChunkStyle (DefaultString).ChunkProperties |= ChunkProperties.TransparentBackground;
			
			SetStyle (CaretString, DefaultString);

			SetStyle (LineNumberString, 172, 168, 153, 255, 255, 255);
			SetStyle (LineNumberFgHighlightedString, 122, 118, 103);
			
			SetStyle (IconBarBgString,        255, 255, 255);
			SetStyle (IconBarSeperatorString, 172, 168, 153);
			
			SetStyle (FoldLineString, LineNumberString);
			SetStyle (FoldLineHighlightedString, IconBarSeperatorString);
			SetStyle (FoldToggleMarkerString, DefaultString);
			
			SetStyle (LineDirtyBgString,   255, 238, 98);
			SetStyle (LineChangedBgString, 108, 226, 108);
			
			SetStyle (SelectionString,     255, 255, 255, 96, 87, 210);
			
			SetStyle (LineMarkerString,    200, 255, 255);
			SetStyle (RulerString,         187, 187, 187);
			SetStyle (WhitespaceMarkerString, RulerString);
			
			SetStyle (InvalidLineMarkerString,     210,   0,   0);
			
			SetStyle (BreakpointString,            255, 255, 255, 125, 0, 0);
			
			SetStyle (BreakpointMarkerColor1String, 255, 255, 255);
			SetStyle (BreakpointMarkerColor2String, 125, 0, 0);

			SetStyle (DisabledBreakpointBgString,   237, 220, 220);
			
			SetStyle (CurrentDebugLineString,               0,   0,   0, 255, 255, 0);
			SetStyle (CurrentDebugLineMarkerColor1String, 255, 255,   0);
			SetStyle (CurrentDebugLineMarkerColor2String, 255, 255, 204);
			SetStyle (CurrentDebugLineMarkerBorderString, 102, 102,   0);
			
			SetStyle (DebugStackLineString,               0,   0,   0, 128, 255, 128);
			SetStyle (DebugStackLineMarkerColor1String, 128, 255, 128);
			SetStyle (DebugStackLineMarkerColor2String, 204, 255, 204);
			SetStyle (DebugStackLineMarkerBorderString,  51, 102,  51); 
			
			SetStyle (InvalidBreakpointBgString, 237, 220, 220);
			SetStyle (InvalidBreakpointMarkerColor1String, 237, 220, 220);
			SetStyle (InvalidBreakpointMarkerBorderString, 125, 0, 0);
			SetStyle (SearchTextBgString, 255, 226, 185);
			SetStyle (SearchTextMainBgString, 243, 221, 72);
			
			SetStyle (BracketHighlightRectangleString, 128, 128, 128, 196, 196, 196);
			
			SetStyle (BookmarkColor1String, 255, 255, 255);
			SetStyle (BookmarkColor2String, 105, 156, 235);
			
			SetStyle (ErrorUnderlineString, 255, 0, 0);
			SetStyle (WarningUnderlineString, 30, 30, 255);
			
			SetStyle ("diff.line-added",          0, 0x8B, 0x8B, ChunkProperties.None);
			SetStyle ("diff.line-removed",     0x6A, 0x5A, 0xCD, ChunkProperties.None);
			SetStyle ("diff.line-changed",     "text.preprocessor");
			SetStyle ("diff.header",              0, 128,     0, BOLD);
			SetStyle ("diff.header-seperator",    0,   0,   255);
			SetStyle ("diff.header-oldfile",   "diff.header");
			SetStyle ("diff.header-newfile",   "diff.header");
			SetStyle ("diff.location",         "keyword.misc");
			
			SetStyle (PrimaryTemplateColorString, 0xB4, 0xE4, 0xB4, 0xB4, 0xE4, 0xB4);
			SetStyle (PrimaryTemplateHighlightedColorString, 0, 0, 0, 0xB4, 0xE4, 0xB4);
			 
			SetStyle (SecondaryTemplateColorString,            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
			SetStyle (SecondaryTemplateHighlightedColorString, 0x7F, 0x7F, 0x7F, 0xFF, 0xFF, 0xFF);
			
			SetStyle ("warning.light.color1", 250, 250, 210);
			SetStyle ("warning.light.color2", 230, 230, 190);
			
			SetStyle ("warning.dark.color1",  235, 235, 180);
			SetStyle ("warning.dark.color2",  215, 215, 160);
			
			SetStyle ("warning.line.top",     199, 199, 141);
			SetStyle ("warning.line.bottom",  199, 199, 141);
			SetStyle ("warning.text",           0,   0,   0);

			SetStyle ("error.light.color1",   240, 200, 200);
			SetStyle ("error.light.color2",   230, 180, 180);
			
			SetStyle ("error.dark.color1",    235, 180, 180);
			SetStyle ("error.dark.color2",    215, 160, 160);
			
			SetStyle ("error.line.top",       193, 143, 143);
			SetStyle ("error.line.bottom",    193, 143, 143);
			SetStyle ("error.text",             0,    0,   0);
		}
		
		protected void PopulateDefaults ()
		{
			SetStyle ("text",                      0,    0,    0);
			SetStyle ("text.punctuation",          0,    0,    0);
			SetStyle ("text.link",                 0,    0,  255);
			SetStyle ("text.preprocessor",         0,  128,    0);
			SetStyle ("text.preprocessor.keyword", 0,  128,    0, BOLD);
			SetStyle ("text.markup",               0, 0x8A, 0x8C);
			SetStyle ("text.markup.tag",        0x6A, 0x5A, 0xCD);
			
			SetStyle ("comment",              0,   0, 255);
			SetStyle ("comment.line",         0,   0, 255);
			SetStyle ("comment.block",        0,   0, 255);
			SetStyle ("comment.doc",          0,   0, 255);
			SetStyle ("comment.tag",        128, 128, 128, ITALIC);
			SetStyle ("comment.tag.line",   128, 128, 128, ITALIC);
			SetStyle ("comment.tag.block" , 128, 128, 128, ITALIC);
			SetStyle ("comment.tag.doc",    128, 128, 128, ITALIC);
			SetStyle ("comment.keyword",      0,   0, 255, ITALIC);
			SetStyle ("comment.keyword.todo", 0,   0, 255, BOLD);
			
			SetStyle ("constant",               255,   0, 255);
			SetStyle ("constant.digit",         255,   0, 255);
			SetStyle ("constant.language",      165,  42,  42, BOLD);
			SetStyle ("constant.language.void", 165,  42,  42, BOLD);
			
			SetStyle ("string",        255, 0, 255);
			SetStyle ("string.single", 255, 0, 255);
			SetStyle ("string.double", 255, 0, 255);
			SetStyle ("string.other",  255, 0, 255);
			
			SetStyle ("keyword.semantic.type", 0, 0x8A , 0x8C);
			
			SetStyle ("keyword", 0, 0, 0, BOLD);
			
			SetStyle ("keyword.access",      165,  42,  42, BOLD);
			SetStyle ("keyword.operator",    165,  42,  42, BOLD);
			SetStyle ("keyword.operator.declaration", 165,  42,  42, BOLD);
			SetStyle ("keyword.selection",   165,  42,  42, BOLD);
			SetStyle ("keyword.iteration",   165,  42,  42, BOLD);
			SetStyle ("keyword.jump",        165,  42,  42, BOLD);
			SetStyle ("keyword.context",     165,  42,  42, BOLD);
			SetStyle ("keyword.exceptions",  165,  42,  42, BOLD);
			SetStyle ("keyword.modifier",    165,  42,  42, BOLD);
			SetStyle ("keyword.type",         46, 139,  87, BOLD);
			SetStyle ("keyword.namespace",   165,  42,  42, BOLD);
			SetStyle ("keyword.property",    165,  42,  42, BOLD);
			SetStyle ("keyword.declaration", 165,  42,  42, BOLD);
			SetStyle ("keyword.parameter",   165,  42,  42, BOLD);
			SetStyle ("keyword.misc",        165,  42,  42, BOLD);
		}
		
		const ChunkProperties BOLD   = ChunkProperties.Bold;
		const ChunkProperties ITALIC = ChunkProperties.Bold;
		
		void SetStyle (string name, ChunkStyle style)
		{
			styleLookupTable[name] = style;
		}
		
		void SetStyle (string name, string referencedStyleName)
		{
			styleLookupTable[name] = new ReferencedChunkStyle (this, referencedStyleName);
		}
		
		void SetStyle (string name, byte r, byte g, byte b)
		{
			SetStyle (name, new ChunkStyle (new Gdk.Color (r, g, b)));
		}
		
		void SetStyle (string name, byte r, byte g, byte b, byte bg_r, byte bg_g, byte bg_b)
		{
			SetStyle (name, new ChunkStyle (new Gdk.Color (r, g, b), new Gdk.Color (bg_r, bg_g, bg_b)));
		}
			
		void SetStyle (string name, byte r, byte g, byte b, ChunkProperties properties)
		{
			SetStyle (name, new ChunkStyle (new Gdk.Color (r, g, b), properties));
		}
			
		void SetStyle (string name, byte r, byte g, byte b, byte bg_r, byte bg_g, byte bg_b, ChunkProperties properties)
		{
			SetStyle (name, new ChunkStyle (new Gdk.Color (r, g, b), new Gdk.Color (bg_r, bg_g, bg_b), properties));
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
				//	Console.WriteLine ("Chunk style {0} fell back to {1}", name, fallbackName);
					return style;
				}
				dotIndex = fallbackName.LastIndexOf ('.');
			}
			
		//	Console.WriteLine ("Chunk style {0} fell back to default", name);
			return GetDefaultChunkStyle ();
		}
		
		public void SetChunkStyle (string name, string weight, string foreColor, string backColor)
		{
			Gdk.Color color   = !String.IsNullOrEmpty (foreColor) ? this.GetColorFromString (foreColor) : Gdk.Color.Zero;
			Gdk.Color bgColor = !String.IsNullOrEmpty (backColor) ? this.GetColorFromString (backColor) : Gdk.Color.Zero;
			ChunkProperties properties = ChunkProperties.None;
			if (weight != null) {
				if (weight.ToUpper ().IndexOf ("BOLD") >= 0)
					properties |= ChunkProperties.Bold;
				if (weight.ToUpper ().IndexOf ("ITALIC") >= 0)
					properties |= ChunkProperties.Italic;
				if (weight.ToUpper ().IndexOf ("UNDERLINE") >= 0)
					properties |= ChunkProperties.Underline;
			}
			SetStyle (name, new ChunkStyle (color, bgColor, properties));
		}
		
		public Gdk.Color GetColorFromString (string colorString)
		{
			if (customPalette.ContainsKey (colorString))
				return this.GetColorFromString (customPalette[colorString]);
			if (styleLookupTable.ContainsKey (colorString)) 
				return styleLookupTable[colorString].Color;
			Gdk.Color result = new Color ();
			if (!Gdk.Color.Parse (colorString, ref result)) 
				throw new Exception ("Can't parse color: " + colorString);
			return result;
		}
		
		public const string NameAttribute = "name";
		
		static void ReadStyleTree (XmlReader reader, Style result, string curName, string curWeight, string curColor, string curBgColor)
		{
			string name    = reader.GetAttribute ("name"); 
			string weight  = reader.GetAttribute ("weight") ?? curWeight;
			string color   = reader.GetAttribute ("color") ?? curColor;
			string bgColor = reader.GetAttribute ("bgColor") ?? curBgColor;
			string fullName;
			if (String.IsNullOrEmpty (curName)) {
				fullName = name;
			} else {
				fullName = curName + "." + name;
			}
			if (!String.IsNullOrEmpty (color)) {
				result.SetChunkStyle (fullName, weight, color, bgColor);
			}
			XmlReadHelper.ReadList (reader, "Style", delegate () {
				switch (reader.LocalName) {
				case "Style":
					ReadStyleTree (reader, result, fullName, weight, color, bgColor);
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
					result.Name        = reader.GetAttribute (NameAttribute);
					result.Description = reader.GetAttribute ("_description");
					return true;
				case "Color":
					result.customPalette[reader.GetAttribute ("name")] = reader.GetAttribute ("value");
					return true;
				case "Style":
					ReadStyleTree (reader, result, null, null, null, null);
					return true;
				}
				return false;
			});
			result.GetChunkStyle (DefaultString).ChunkProperties |= ChunkProperties.TransparentBackground;
			return result;
		}
		
		public virtual void UpdateFromGtkStyle (Gtk.Style style)
		{
		}
	}
}
