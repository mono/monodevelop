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
		Color comment, str, punctuation, keyword1, keyword2, keyword3, keyword4, preprocessor; 

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
			comment      = new Gdk.Color (0, 0, 255);
			str          = new Gdk.Color (255, 0, 255);
			punctuation  = new Gdk.Color (0, 0, 0);
			keyword1     = new Gdk.Color (165, 42, 42);
			keyword2     = new Gdk.Color (46, 139, 87);
			keyword3     = new Gdk.Color (165, 42, 42);
			keyword4     = new Gdk.Color (0, 200, 0);
			preprocessor = new Gdk.Color (0, 200, 0);
			def          = new Gdk.Color (0, 0, 0); 
			
			lineNumberBg = new Gdk.Color (255, 255, 255);
			lineNumberFg = new Gdk.Color (172, 168, 153);
			lineNumberFgHighlighted = new Gdk.Color (122, 118, 103);
			
			foldLine            = new Gdk.Color (172, 168, 153);
			foldLineHighlighted = new Gdk.Color (122, 118, 103);
			foldBg              = new Gdk.Color (255, 255, 255);
			foldToggleMarker    = new Gdk.Color (0, 0, 0);
			
			background = new Gdk.Color (255, 255, 255);
			selectedBg   = new Gdk.Color (96, 87, 210);
			selectedFg   = new Gdk.Color (255, 255, 255);
			lineMarker  = new Gdk.Color (200, 255, 255);
			ruler       = new Gdk.Color (187, 187, 187);
			whitespaceMarker = new Gdk.Color (187, 187, 187);
			invalidLineMarker = new Gdk.Color (210, 0, 0);
			
		}
		static readonly string[] colorTable = new string[] { "Comment", 
			                    "String",
			                    "Punctuation",
			                    "Keyword1",
			                    "Keyword2",
			                    "Keyword3",
			                    "Keyword4",
			                    "PreProcessorDirective" };
		
		public static int ColorTableCount {
			get {
				return colorTable.Length;
			}
		}
		
		public static string GetColorName (int number)
		{
			return colorTable[number];
		}
		
		public static int GetColorNumber (string name)
		{
			for (int i = 0; i < colorTable.Length; i++) {
				if (colorTable[i] == name)
					return i;
			}
			return -1;
		}
		
		public Gdk.Color GetColor (string name)
		{
			switch (name) {
			case "Comment":
				return comment;
			case "String":
				return str;
			case "Punctuation":
				return punctuation;
			case "Keyword1":
				return keyword1;
			case "Keyword2":
				return keyword2;
			case "Keyword3":
				return keyword3;
			case "Keyword4":
				return keyword4;
			case "PreProcessorDirective":
				return preprocessor;
			}
			return def;
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
				
			case "Comment":
				this.comment = color;
				break;
			case "String":
				this.str = color;
				break;
			case "Punctuation":
				this.punctuation = color;
				break;
			case "PreProcessor":
				this.preprocessor = color;
				break;
			case "Keyword1":
				this.keyword1 = color;
				break;
			case "Keyword2":
				this.keyword2 = color;
				break;
			case "Keyword3":
				this.keyword3 = color;
				break;
			case "Keyword4":
				this.keyword4 = color;
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
					result.SetColor (reader.GetAttribute ("name"),
					                 reader.ReadElementContentAsString ());
					return true;
				}
				return false;
			});
			
			return result;
		}
	}
}
