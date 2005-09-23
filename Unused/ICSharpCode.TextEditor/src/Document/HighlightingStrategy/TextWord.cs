// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Diagnostics;

namespace MonoDevelop.TextEditor.Document
{
	
	public enum TextWordType {
		Word,
		Space,
		Tab
	}
	
	/// <summary>
	/// This class represents single words with color information, two special versions of a word are 
	/// spaces and tabs.
	/// </summary>
	public class TextWord
	{
		HighlightColor  color;
		LineSegment     word;
		IDocument       document;
		
		int          offset;
		int          length;
		TextWordType type;
		
		static TextWord spaceWord = new TextWord(TextWordType.Space);
		static TextWord tabWord   = new TextWord(TextWordType.Tab);
		
		public bool hasDefaultColor;
		
		static public TextWord Space {
			get {
				return spaceWord;
			}
		}
		
		static public TextWord Tab {
			get {
				return tabWord;
			}
		}
		
		public int  Length {
			get {
				if (type == TextWordType.Word) {
					return length;
				} 
				return 1;
			}
		}
		
		public bool HasDefaultColor {
			get {
				return hasDefaultColor;
			}
		}
		
		public TextWordType Type {
			get {
				return type;
			}
		}
		
//		string       myword = null;
		public string Word {
			get {
				return document.GetText(word.Offset + offset, length);
//				if (myword == null) {
//					myword = document.GetText(word.Offset + offset, length);
//				}
//				return myword;
			}
		}
		
		public Pango.FontDescription Font {
			get {
				return color.Font;
			}
		}
		
		public Color Color {
			get {
				return color.Color;
			}
		}
		
		public HighlightColor SyntaxColor {
			get {
				return color;
			}
			set {
				color = value;
			}
		}
		
		public bool IsWhiteSpace {
			get {
				return type == TextWordType.Space || type == TextWordType.Tab;
			}
		}
		
		// TAB
		private TextWord(TextWordType type)
		{
			this.type = type;
		}
		
		public TextWord(IDocument document, LineSegment word, int offset, int length, HighlightColor color, bool hasDefaultColor)
		{
			Debug.Assert(document != null);
			Debug.Assert(word != null);
			Debug.Assert(color != null);
			
			this.document = document;
			this.word  = word;
			this.offset = offset;
			this.length = length;
			this.color = color;
			this.hasDefaultColor = hasDefaultColor;
			this.type  = TextWordType.Word;
		}
		
		/// <summary>
		/// Converts a <see cref="TextWord"/> instance to string (for debug purposes)
		/// </summary>
		public override string ToString()
		{
			return "[TextWord: Word = " + Word + ", Font = " + Font.Family + ", Color = " + Color + "]";
		}
	}
}
