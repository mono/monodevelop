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
		Color lineMarker, ruler, whitespaceMarker, invalidLineMarker;
		
		Color lineNumberFg, lineNumberBg, lineNumberFgHighlighted;
		Color iconBarBg, iconBarSeperator;
		Color foldLine, foldLineHighlighted, foldBg, foldToggleMarker;
		List<ChunkStyle>        styles           = new List<Mono.TextEditor.ChunkStyle> ();
		Dictionary<string, int> styleLookupTable = new Dictionary<string, int> (); 
		
		public virtual Color Default {
			get {
				return def;
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

		public Style ()
		{
			def          = new Gdk.Color (0, 0, 0); 
			
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
			
			SetStyle ("Comment1", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("Comment2", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), true, false));
			SetStyle ("Comment3", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, false));
			SetStyle ("Comment4", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), false, true));
			
			SetStyle ("Digit", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), true, false));
			SetStyle ("Literal", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), true, false));
			SetStyle ("Punctuation", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), true, false));
			
			SetStyle ("Keyword1", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165, 42, 42), true, false));
			SetStyle ("Keyword2", new Mono.TextEditor.ChunkStyle (new Gdk.Color (46, 139, 87), true, false));
			SetStyle ("Keyword3", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165, 42, 42), true, false));
			SetStyle ("Keyword4", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 200, 0), true, false));
			
			SetStyle ("PreProcessorDirective1", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 200, 0), false, false));
			SetStyle ("PreProcessorDirective2", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 200, 0), true, false));
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
				return ChunkStyle.Default;
			return this.styles [styleLookupTable[name]];
		}
		
		public void SetChunkStyle (string name, string weight, string value)
		{
			Gdk.Color color = new Color ();
			if (!Gdk.Color.Parse (value, ref color)) {
				throw new Exception ("Can't parse color: " + value);
			}
			SetStyle (name, new Mono.TextEditor.ChunkStyle (color, 
			                                                weight == null ? false : weight.ToUpper ().IndexOf ("BOLD") > 0,
			                                                weight == null ? false : weight.ToUpper ().IndexOf ("ITALIC") > 0));
		}
		
		public static bool IsChunkStyle (string name)
		{
			switch (name) {
			case "Default":
			case "Background":
			case "SelectedBackground":
			case "SelectedForeground":
			case "LineMarker":
			case "Ruler":
			case "Whitespaces":
			case "InvalidLines":
			case "LineNumber":
			case "LineNumberHighlighted":
			case "LineNumberBg":
			case "IconBar":
			case "IconBarSeperator":
			case "FoldLine":
			case "FoldBg":
			case "FoldLineHighlighted":
			case "FoldToggleMarker":
				return false;
			}
			return true;
		}
		
		public void SetColor (string name, string value)
		{
			Gdk.Color color = new Color ();
			if (!Gdk.Color.Parse (value, ref color)) {
				throw new Exception ("Can't parse color: " + value);
			}
			switch (name) {
			case "Default":
				this.def = color;
				break;
			case "Background":
				this.background = color;
				break;
			case "SelectedBackground":
				this.selectedBg = color;
				break;
			case "SelectedForeground":
				this.selectedFg = color;
				break;
			case "LineMarker":
				this.lineMarker = color;
				break;
			case "Ruler":
				this.ruler = color;
				break;
			case "Whitespaces":
				this.whitespaceMarker = color;
				break;
			case "InvalidLines":
				this.invalidLineMarker = color;
				break;
			case "LineNumber":
				this.lineNumberFg = color;
				break;
			case "LineNumberHighlighted":
				this.lineNumberFgHighlighted = color;
				break;
			case "LineNumberBg":
				this.lineNumberBg = color;
				break;
			case "IconBar":
				this.iconBarBg = color;
				break;
			case "IconBarSeperator":
				this.iconBarSeperator = color;
				break;
			case "FoldLine":
				this.foldLine = color;
				break;
			case "FoldBg":
				this.foldBg = color;
				break;
			case "FoldLineHighlighted":
				this.foldLineHighlighted = color;
				break;
			case "FoldToggleMarker":
				this.foldToggleMarker = color;
				break;
			default:
				throw new Exception ("color  " + name + " invalid.");
			}
		}
		
		public static Style Read (XmlReader reader)
		{
			Style result = new Style ();
			XmlReadHelper.ReadList (reader, "EditorStyle", delegate () {
				switch (reader.LocalName) {
				case "EditorStyle":
					result.name        = reader.GetAttribute ("_name");
					result.description = reader.GetAttribute ("_description");
					return true;
					
				case "Color":
					string name = reader.GetAttribute ("name"); 
					if (IsChunkStyle (name)) {
						result.SetChunkStyle (name,
						                 reader.GetAttribute ("weight"),
						                 reader.ReadElementContentAsString ());
					} else {
						result.SetColor (name,
						                 reader.ReadElementContentAsString ());
					}
					return true;
				}
				return false;
			});
			
			return result;
		}
	}
}
