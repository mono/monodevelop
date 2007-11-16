//  DefaultSearchResult.cs
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

namespace MonoDevelop.Ide.Gui.Search
{
	internal class DefaultSearchResult : ISearchResult
	{
		IDocumentInformation documentInformation;
		int offset;
		int length;
		int line;
		int column;
		int position;
		
		public DefaultSearchResult (ITextIterator iter, int length)
		{
			position = iter.Position;
			offset = iter.DocumentOffset;
			line = iter.Line + 1;
			column = iter.Column + 1;
			this.length = length;
			documentInformation = iter.DocumentInformation;
		}
		
		public string FileName {
			get {
				return documentInformation.FileName;
			}
		}
		
		public IDocumentInformation DocumentInformation {
			get {
				return documentInformation;
			}
		}
		
		public int DocumentOffset {
			get {
				return offset;
			}
		}
		
		public int Position {
			get {
				return position;
			}
		}
		
		public int Length {
			get {
				return length;
			}
		}
		
		public int Line {
			get { return line; }
		}
		
		public int Column {
			get { return column; }
		}
		
		public virtual string TransformReplacePattern (string pattern)
		{
			return pattern;
		}
		
		public override string ToString()
		{
			return String.Format("[DefaultLocation: FileName={0}, Offset={1}, Length={2}]",
			                     FileName,
			                     DocumentOffset,
			                     Length);
		}
	}
}
