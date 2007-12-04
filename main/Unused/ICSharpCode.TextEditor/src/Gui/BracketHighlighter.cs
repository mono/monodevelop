//  BracketHighlighter.cs
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
using System.Collections;
using System.Drawing;
using System.Diagnostics;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	public class Highlight
	{
		Point openBrace;
		Point closeBrace;
		
		public Point OpenBrace {
			get {
				return openBrace;
			}
			set {
				openBrace = value;
			}
		}
		public Point CloseBrace {
			get {
				return closeBrace;
			}
			set {
				closeBrace = value;
			}
		}
		
		public Highlight(Point openBrace, Point closeBrace)
		{
			this.openBrace = openBrace;
			this.closeBrace = closeBrace;
		}
	}
	
	public class BracketHighlightingSheme
	{
		char opentag;
		char closingtag;
		
		public char OpenTag {
			get {
				return opentag;
			}
			set {
				opentag = value;
			}
		}
		
		public char ClosingTag {
			get {
				return closingtag;
			}
			set {
				closingtag = value;
			}
		}
		
		public BracketHighlightingSheme(char opentag, char closingtag)
		{
			this.opentag    = opentag;
			this.closingtag = closingtag;
		}
		public Highlight GetHighlight(IDocument document, int offset)
		{
			char word = document.GetCharAt(Math.Max(0, Math.Min(document.TextLength - 1, offset)));
			Point endP = document.OffsetToPosition(offset);
			if (word == opentag) {
				if (offset < document.TextLength) {
					int bracketOffset = TextUtilities.SearchBracketForward(document, offset + 1, opentag, closingtag);
					if (bracketOffset >= 0) {
						Point p = document.OffsetToPosition(bracketOffset);
						return new Highlight(p, endP);
					}
				}
			} else if (word == closingtag) {
				if (offset > 0) {
					int bracketOffset = TextUtilities.SearchBracketBackward(document, offset - 1, opentag, closingtag);
					if (bracketOffset >= 0) {
						Point p = document.OffsetToPosition(bracketOffset);
						return new Highlight(p, endP);
					}
				}
			}
			return null;
		}
	}
}
