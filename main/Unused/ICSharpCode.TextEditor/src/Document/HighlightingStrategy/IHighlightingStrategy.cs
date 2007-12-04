//  IHighlightingStrategy.cs
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
using System.Text;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// A highlighting strategy for a buffer.
	/// </summary>
	public interface IHighlightingStrategy
	{
		/// <value>
		/// The name of the highlighting strategy, must be unique
		/// </value>
		string Name {
			get;
		}
		
		/// <value>
		/// The file extenstions on which this highlighting strategy gets
		/// used
		/// </value>
		string[] Extensions {
			set;
			get;
		}
		
		Hashtable Properties {
			get;
		}
		
		// returns special color. (BackGround Color, Cursor Color and so on)
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightColor   GetColorFor(string name);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightRuleSet GetRuleSet(Span span);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		HighlightColor   GetColor(IDocument document, LineSegment keyWord, int index, int length);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		void MarkTokens(IDocument document, ArrayList lines);
		
		/// <remarks>
		/// Used internally, do not call
		/// </remarks>
		void MarkTokens(IDocument document);
	}
}
