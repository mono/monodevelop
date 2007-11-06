//  LineSegment.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Drawing;
using System.Collections;
using System.Text;

namespace MonoDevelop.TextEditor.Document
{
	public class LineSegment : AbstractSegment
	{
		int delimiterLength;
		
		ArrayList words              = null;
		Stack     highlightSpanStack = null;
		
		public override int Length {
			get	{
				return length - delimiterLength;
			}
			set {
				throw new System.NotSupportedException();
			}
		}
		
		public int TotalLength {
			get {
				return length;
			}
			set {
				length = value;
			}
		}
		
		public int DelimiterLength {
			get {
				return delimiterLength;
			}
			set {
				delimiterLength = value;
			}
		}
		
		// highlighting information
		public ArrayList Words {
			get {
				return words;
			}
			set {
				words = value;
			}
		}
		
		public HighlightColor GetColorForPosition(int x)
		{
			if (Words != null) {
				int xPos = 0;
				foreach (TextWord word in Words) {
					if (x < xPos + word.Length) {
						return word.SyntaxColor;
					}
					xPos += word.Length;
				}
			}
			return new HighlightColor(Color.Black, false, false);
		}
		
		public Stack HighlightSpanStack {
			get {
				return highlightSpanStack;
			}
			set {
				highlightSpanStack = value;
			}
		}
		
		public LineSegment(int offset, int end, int delimiterLength)
		{
			this.offset          = offset;
			this.length          = end - offset + 1;
			this.delimiterLength = delimiterLength;
		}
		
		public LineSegment(int offset, int length)
		{
			this.offset          = offset;
			this.length          = length;
			this.delimiterLength = 0;
		}
		
		/// <summary>
		/// Converts a <see cref="LineSegment"/> instance to string (for debug purposes)
		/// </summary>
		public override string ToString()
		{
			return "[LineSegment: Offset = "+ offset +", TotalLength = " + length + ", DelimiterLength = " + delimiterLength + "]";
		}
		
		// Svante Lidman: reconsider whether it was the right descision to move these methids here.
		
		/// <summary>
		/// get the string, which matches the regular expression expr,
		/// in string s2 at index
		/// </summary>
		internal string GetRegString(char[] expr, int index, IDocument document)
		{
			int j = 0;
			StringBuilder regexpr = new StringBuilder();;
			
			for (int i = 0; i < expr.Length; ++i, ++j) {
				if (index + j >= this.Length) 
					break;
				
				switch (expr[i]) {
					case '@': // "special" meaning
						++i;
						switch (expr[i]) {
							case '!': // don't match the following expression
								StringBuilder whatmatch = new StringBuilder();
								++i;
								while (i < expr.Length && expr[i] != '@') {
									whatmatch.Append(expr[i++]);
								}
								break;
							case '@': // matches @
								regexpr.Append(document.GetCharAt(this.Offset + index + j));
								break;
						}
						break;
					default:
						if (expr[i] != document.GetCharAt(this.Offset + index + j)) {
							return regexpr.ToString();
						}
					regexpr.Append(document.GetCharAt(this.Offset + index + j));
					break;
				}
			}
			return regexpr.ToString();
		}
		
		/// <summary>
		/// returns true, if the get the string s2 at index matches the expression expr
		/// </summary>
		internal bool MatchExpr(char[] expr, int index, IDocument document)
		{
			for (int i = 0, j = 0; i < expr.Length; ++i, ++j) {
				switch (expr[i]) {
					case '@': // "special" meaning
						++i;
						if (i < expr.Length) {
							switch (expr[i]) {
								case '!': // don't match the following expression
								{ StringBuilder whatmatch = new StringBuilder();
									++i;
									while (i < expr.Length && expr[i] != '@') {
										whatmatch.Append(expr[i++]);
									}
									if (this.Offset + index + j + whatmatch.Length < document.TextLength) {
										int k = 0;
										for (; k < whatmatch.Length; ++k)
											if (document.GetCharAt(this.Offset + index + j + k) != whatmatch[k])
												break;
										if (k >= whatmatch.Length) {
											return false;
										}
									}
//									--j;
									break;
								}
								case '-': // don't match the  expression before 
								{
									StringBuilder whatmatch = new StringBuilder();
									++i;
									while (i < expr.Length && expr[i] != '@') {
										whatmatch.Append(expr[i++]);
									}
									if (index - whatmatch.Length >= 0) {
										int k = 0;
										for (; k < whatmatch.Length; ++k)
											if (document.GetCharAt(this.Offset + index - whatmatch.Length + k) != whatmatch[k])
												break;
										if (k >= whatmatch.Length) {
											return false;
										}
									}
//									--j;
									break;
								}
								case '@': // matches @
									if (index + j >= this.Length || '@' != document.GetCharAt(this.Offset + index + j)) {
										return false;
									}
									break;
							}
						}
						break;
					default:
						if (index + j >= this.Length || expr[i] != document.GetCharAt(this.Offset + index + j)) {
							return false;
						}
						break;
				}
			}
			return true;
		}
	}
}
