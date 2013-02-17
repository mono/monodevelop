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
using System.Globalization;

namespace Mono.TextEditor.Highlighting
{
	public class ColorScheme
	{
		Dictionary<string, ChunkStyle> styleLookupTable = new Dictionary<string, ChunkStyle> (); 
		Dictionary<string, string> customPalette = new Dictionary<string, string> (); 
		
		public IEnumerable<string> ColorNames {
			get {
				return styleLookupTable.Keys;
			}
		}
		
		public Cairo.Color GetColorFromDefinition (string colorName)
		{
			return this.GetChunkStyle (colorName).CairoColor;
		}
		
		#region Named colors
		
		public const string DefaultString = "text";
		public virtual ChunkStyle Default {
			get {
				return GetChunkStyle (DefaultString);
			}
		}

		public const string TooltipString = "tooltip";
		public virtual ChunkStyle Tooltip {
			get {
				return GetChunkStyle (TooltipString);
			}
		}

		public const string LineNumberString = "linenumber";
		public virtual ChunkStyle LineNumber {
			get {
				return GetChunkStyle (LineNumberString);
			}
		}

		public const string IconBarBgString = "iconbar";
		public virtual Cairo.Color IconBarBg {
			get {
				return GetColorFromDefinition (IconBarBgString);
			}
		}

		public const string IconBarSeperatorString = "iconbar.separator";
		public virtual Cairo.Color IconBarSeperator {
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

		public const string FoldMarginString = "fold.margin";
		public virtual ChunkStyle FoldMargin {
			get {
				return GetChunkStyle (FoldMarginString);
			}
		}

		public const string LineChangedBgString = "marker.line.changed";
		public virtual Cairo.Color LineChangedBg {
			get {
				return GetColorFromDefinition (LineChangedBgString);
			}
		}
		
		public const string LineDirtyBgString = "marker.line.dirty";
		public virtual Cairo.Color LineDirtyBg {
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
		
		public const string InactiveSelectionString = "text.selection.inactive";
		public virtual ChunkStyle InactiveSelection {
			get {
				return GetChunkStyle (InactiveSelectionString);
			}
		}

		public const string LineMarkerString = "marker.line";
		public virtual Cairo.Color LineMarker {
			get {
				return GetColorFromDefinition (LineMarkerString);
			}
		}

		public const string RulerString = "marker.ruler";
		public virtual Cairo.Color Ruler {
			get {
				return GetColorFromDefinition (RulerString);
			}
		}

		public const string BracketHighlightRectangleString = "marker.bracket";

		public virtual ChunkStyle BracketHighlightRectangle {
			get {
				return GetChunkStyle (BracketHighlightRectangleString);
			}
		}
		
		public const string UsagesHighlightRectangleString = "marker.usages";
		public virtual ChunkStyle UsagesHighlightRectangle {
			get {
				return GetChunkStyle (UsagesHighlightRectangleString);
			}
		}
		
		public const string BookmarkColor2String = "marker.bookmark.color2";
		public virtual Cairo.Color BookmarkColor2 {
			get {
				return GetColorFromDefinition (BookmarkColor2String);
			}
		}
		
		public const string BookmarkColor1String = "marker.bookmark.color1";
		public virtual Cairo.Color BookmarkColor1 {
			get {
				return GetColorFromDefinition (BookmarkColor1String);
			}
		}
		
		public const string ReadOnlyTextBgString = "text.background.readonly";
		public Cairo.Color ReadOnlyTextBg {
			get {
				return GetColorFromDefinition (ReadOnlyTextBgString);
			}
		}
		
		public const string SearchTextBgString = "text.background.searchresult";
		public Cairo.Color SearchTextBg {
			get {
				return GetColorFromDefinition (SearchTextBgString);
			}
		}
		
		public const string SearchTextMainBgString = "text.background.searchresult-main";
		public Cairo.Color SearchTextMainBg {
			get {
				return GetColorFromDefinition (SearchTextMainBgString);
			}
		}

		public const string BreakpointString = "marker.breakpoint";
		public Cairo.Color BreakpointFg {
			get {
				return GetChunkStyle (BreakpointString).CairoColor;
			}
		}
		public Cairo.Color BreakpointBg {
			get {
				return GetChunkStyle (BreakpointString).CairoBackgroundColor;
			}
		}

		public const string BreakpointMarkerColor2String = "marker.breakpoint.color2";
		public Cairo.Color BreakpointMarkerColor2 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor2String);
			}
		}

		public const string BreakpointMarkerColor1String = "marker.breakpoint.color1";
		public Cairo.Color BreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor1String);
			}
		}

		public const string CurrentDebugLineString = "marker.debug.currentline";
		public Cairo.Color CurrentDebugLineFg {
			get {
				return GetChunkStyle (CurrentDebugLineString).CairoColor;
			}
		}
		
		public Cairo.Color CurrentDebugLineBg {
			get {
				return GetChunkStyle (CurrentDebugLineString).CairoBackgroundColor;
			}
		}

		public const string CurrentDebugLineMarkerColor2String = "marker.debug.currentline.color2";
		public Cairo.Color CurrentDebugLineMarkerColor2 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor2String);
			}
		}

		public const string CurrentDebugLineMarkerColor1String = "marker.debug.currentline.color1";
		public Cairo.Color CurrentDebugLineMarkerColor1 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor1String);
			}
		}

		public const string CurrentDebugLineMarkerBorderString = "marker.debug.currentline.border";
		public Cairo.Color CurrentDebugLineMarkerBorder {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerBorderString);
			}
		}
		
		public const string DebugStackLineString = "marker.debug.stackline";
		public Cairo.Color DebugStackLineFg {
			get {
				return GetChunkStyle (DebugStackLineString).CairoColor;
			}
		}
		
		public Cairo.Color DebugStackLineBg {
			get {
				return GetChunkStyle (DebugStackLineString).CairoBackgroundColor;
			}
		}

		public const string DebugStackLineMarkerColor2String = "marker.debug.stackline.color2";
		public Cairo.Color DebugStackLineMarkerColor2 {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerColor2String);
			}
		}

		public const string DebugStackLineMarkerColor1String = "marker.debug.stackline.color1";
		public Cairo.Color DebugStackLineMarkerColor1 {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerColor1String);
			}
		}

		public const string DebugStackLineMarkerBorderString = "marker.debug.stackline.border";
		public Cairo.Color DebugStackLineMarkerBorder {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerBorderString);
			}
		}

		public const string InvalidBreakpointBgString = "marker.breakpoint.invalid.background";
		public Cairo.Color InvalidBreakpointBg {
			get {
				return GetColorFromDefinition (InvalidBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerColor1String = "marker.breakpoint.invalid.color1";
		public Cairo.Color InvalidBreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerColor1String);
			}
		}

		public const string DisabledBreakpointBgString = "marker.breakpoint.disabled.background";
		public Cairo.Color DisabledBreakpointBg {
			get {
				return GetColorFromDefinition (DisabledBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerBorderString = "marker.breakpoint.invalid.border";
		public Cairo.Color InvalidBreakpointMarkerBorder {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerBorderString);
			}
		}
		
		public const string ErrorUnderlineString = "marker.underline.error";
		public Cairo.Color ErrorUnderline {
			get {
				return GetColorFromDefinition (ErrorUnderlineString);
			}
		}
		
		public const string WarningUnderlineString = "marker.underline.warning";
		public Cairo.Color WarningUnderline {
			get {
				return GetColorFromDefinition (WarningUnderlineString);
			}
		}
		
		public const string HintUnderlineString = "marker.underline.hint";
		public Cairo.Color HintUnderline {
			get {
				return GetColorFromDefinition (HintUnderlineString);
			}
		}
		
		public const string SuggestionUnderlineString = "marker.underline.suggestion";
		public Cairo.Color SuggestionUnderline {
			get {
				return GetColorFromDefinition (SuggestionUnderlineString);
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
			set;
		}
		
		public string Description {
			get;
			set;
		}
		
		public static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue);
		}
		
		public static Gdk.Color ToGdkColor (Cairo.Color color)
		{
			return new Gdk.Color ((byte)(color.R  * 255),
			                        (byte)(color.G * 255),
			                        (byte)(color.B * 255));
		}
				
		public static Cairo.Color ToCairoColor (Gdk.Color color, double alpha)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue,
			                        alpha);
		}
		
		protected ColorScheme ()
		{
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
		
		void SetStyleFromWeb (string name, string colorString)
		{
			var color = new Color ();
			if (!Gdk.Color.Parse (colorString, ref color)) 
				throw new Exception ("Can't parse color: " + colorString);
			SetStyle (name, new ChunkStyle (color));
		}

		void SetStyleFromWeb (string name, string colorString, string bgColorString)
		{
			var color = new Color ();
			if (!Gdk.Color.Parse (colorString, ref color)) 
				throw new Exception ("Can't parse color: " + colorString);
			var bgColor = new Color ();
			if (!Gdk.Color.Parse (bgColorString, ref bgColor)) 
				throw new Exception ("Can't parse color: " + bgColorString);
			SetStyle (name, new ChunkStyle (color, bgColor));
		}
		
		void SetStyle (string name, byte r, byte g, byte b, byte bg_r, byte bg_g, byte bg_b)
		{
			SetStyle (name, new ChunkStyle (new Gdk.Color (r, g, b), new Gdk.Color (bg_r, bg_g, bg_b)));
		}
			
		void SetStyle (string name, byte r, byte g, byte b, ChunkProperties properties)
		{
			SetStyle (name, new ChunkStyle (new Gdk.Color (r, g, b), properties));
		}
			
/*		void SetStyle (string name, byte r, byte g, byte b, byte bg_r, byte bg_g, byte bg_b, ChunkProperties properties)
		{
			SetStyle (name, new ChunkStyle (new Gdk.Color (r, g, b), new Gdk.Color (bg_r, bg_g, bg_b), properties));
		}*/
		
		public ChunkStyle GetDefaultChunkStyle ()
		{
			ChunkStyle style;
			if (!styleLookupTable.TryGetValue (DefaultString, out style)) {
				style = new ChunkStyle (ToGdkColor (GetColorFromDefinition (DefaultString)));
				styleLookupTable[DefaultString] = style;
			}
			return style;
		}

		public ChunkStyle GetChunkStyle (Chunk chunk)
		{
			if (chunk == null)
				throw new ArgumentNullException ("chunk");
			return GetChunkStyle (chunk.Style);
		}
		
		public ChunkStyle GetChunkStyle (string name)
		{
			if (name == null)
				return GetDefaultChunkStyle ();
			var style = InternalGetChunkStyle (name);
			if (style == null)
				style =SyntaxModeService.DefaultColorStyle.InternalGetChunkStyle (name);
			return style ?? GetDefaultChunkStyle ();
		}

		internal ChunkStyle InternalGetChunkStyle (string name)
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
			return null;
		}
		
		public void SetChunkStyle (string name, string weight, string foreColor, string backColor)
		{
			var color = !string.IsNullOrEmpty (foreColor) ? this.GetColorFromString (foreColor) : new Cairo.Color (0, 0, 0);
			var bgColor = !string.IsNullOrEmpty (backColor) ? this.GetColorFromString (backColor) : new Cairo.Color (0, 0, 0);
			var properties = ChunkProperties.None;
			if (weight != null) {
				if (weight.ToUpper ().IndexOf ("BOLD") >= 0)
					properties |= ChunkProperties.Bold;
				if (weight.ToUpper ().IndexOf ("ITALIC") >= 0)
					properties |= ChunkProperties.Italic;
				if (weight.ToUpper ().IndexOf ("UNDERLINE") >= 0)
					properties |= ChunkProperties.Underline;
			}
			ChunkStyle chunkStyle;
			if (string.IsNullOrEmpty (backColor)) {
				chunkStyle = new ChunkStyle (color, properties);
			} else if (string.IsNullOrEmpty (foreColor)) {
				chunkStyle = new ChunkStyle () {
					ChunkProperties =properties,
					CairoBackgroundColor = bgColor
				};
			} else {
				chunkStyle = new ChunkStyle (color, bgColor, properties);
			}


			SetStyle (name, chunkStyle);
		}
		
		public void SetChunkStyle (string name, ChunkStyle style)
		{
			SetStyle (name, style);
		}
		
		static int GetNumber (string str, int offset)
		{
			return int.Parse (str.Substring (offset, 2), NumberStyles.HexNumber);
		}
		
		public Cairo.Color GetColorFromString (string colorString)
		{
			string refColorString;
			if (customPalette.TryGetValue (colorString, out refColorString))
				return this.GetColorFromString (refColorString);
			ChunkStyle style;
			if (styleLookupTable.TryGetValue (colorString, out style))
				return style.CairoColor;
			if (colorString.Length > 0 && colorString[0] == '#') {
				if (colorString.Length == 4) {
					// #RGB -> #RRGGBB
					colorString = string.Format ("#{0}{0}{1}{1}{2}{2}", colorString[1], colorString[2], colorString[3]);
				}
				if (colorString.Length == 5) {
					// #RAGB -> #AARRGGBB
					colorString = string.Format ("#{0}{0}{1}{1}{2}{2}{3}{3}", colorString[1], colorString[2], colorString[3], colorString[4]);
				}
				if (colorString.Length == 9) {
					// #AARRGGBB
					return new Cairo.Color ( GetNumber (colorString, 3) / 255.0, GetNumber (colorString, 5) / 255.0, GetNumber (colorString, 7) / 255.0, GetNumber (colorString, 1) / 255.0);
				}
				if (colorString.Length == 7) {
					// #RRGGBB
					return new Cairo.Color ( GetNumber (colorString, 1) / 255.0, GetNumber (colorString, 3) / 255.0, GetNumber (colorString, 5) / 255.0);
				}
				throw new ArgumentException ("colorString", "colorString must either be #RRGGBB (length 7) or #AARRGGBB (length 9) your string " + colorString + " is invalid because it has a length of " + colorString.Length);
			}

			var color = System.Drawing.ColorTranslator.FromHtml (colorString);
			if (!color.IsEmpty)
				return new Cairo.Color (color.R / 255.0, color.G / 255.0, color.B / 255.0);
			throw new Exception ("Failed to parse color or find named color '" + colorString + "'");
		}
		
		public const string NameAttribute = "name";
		
		static void ReadStyleTree (XmlReader reader, ColorScheme result, string curName, string curWeight, string curColor, string curBgColor)
		{
			string name    = reader.GetAttribute ("name"); 
			string weight  = reader.GetAttribute ("weight") ?? curWeight;
			string color   = reader.GetAttribute ("color");
			string bgColor = reader.GetAttribute ("bgColor");
			string fullName;
			if (String.IsNullOrEmpty (curName)) {
				fullName = name;
			} else {
				fullName = curName + "." + name;
			}
			if (!string.IsNullOrEmpty (color) || !string.IsNullOrEmpty (bgColor)) {
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
		
		public static ColorScheme LoadFrom (XmlReader reader)
		{
			var result = new ColorScheme ();
			XmlReadHelper.ReadList (reader, "EditorStyle", delegate () {
				switch (reader.LocalName) {
				case "EditorStyle":
					result.Name = reader.GetAttribute (NameAttribute);
					result.Description = reader.GetAttribute ("_description");
					return true;
				case "Color":
					result.customPalette [reader.GetAttribute ("name")] = reader.GetAttribute ("value");
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
		
		static string GetColorString (double c)
		{
			int conv = (int)(c * 255.0);
			return string.Format ("{0:X2}", conv);
		}
		
		static string GetColorString (Cairo.Color cairoColor)
		{
			var result = new System.Text.StringBuilder ();
			result.Append ("#");
			if (cairoColor.A != 1.0)
				result.Append (GetColorString (cairoColor.A));
			result.Append (GetColorString (cairoColor.R));
			result.Append (GetColorString (cairoColor.G));
			result.Append (GetColorString (cairoColor.B));
			
			return result.ToString ();
		}
		
		public void Save (string fileName)
		{
			var writer = new XmlTextWriter (fileName, System.Text.UTF8Encoding.UTF8);
			writer.Formatting = Formatting.Indented;
			
			writer.WriteStartElement ("EditorStyle");
			writer.WriteAttributeString (NameAttribute, Name);
			writer.WriteAttributeString ("_description", Description);
			
			foreach (var style in new Dictionary<string, ChunkStyle> (this.styleLookupTable)) {
				writer.WriteStartElement ("Style");
				writer.WriteAttributeString ("name", style.Key);
				writer.WriteAttributeString ("color", GetColorString (style.Value.CairoColor));
				if (style.Value.GotBackgroundColorAssigned)
					writer.WriteAttributeString ("bgColor", GetColorString (style.Value.CairoBackgroundColor));
				if ((style.Value.ChunkProperties & (ChunkProperties.Bold | ChunkProperties.Italic)) != 0)
					writer.WriteAttributeString ("weight", style.Value.ChunkProperties.ToString ());
				writer.WriteEndElement ();
			}
			
			writer.WriteEndElement ();
			writer.Close ();
		}
		
		public ColorScheme Clone ()
		{
			ColorScheme clone = (ColorScheme)MemberwiseClone ();
			clone.styleLookupTable = new Dictionary<string, ChunkStyle> (styleLookupTable);
			return clone;
		}
		
		public virtual void UpdateFromGtkStyle (Gtk.Style style)
		{
		}
	}
}
