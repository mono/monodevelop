//  IndentFoldingStrategy.cs
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

namespace MonoDevelop.TextEditor.Document
{
	
	/// <summary>
	/// A simple folding strategy which calculates the folding level
	/// using the indent level of the line.
	/// </summary>
	public class IndentFoldingStrategy : IFoldingStrategy
	{
	
		/// <remarks>
		/// Calculates the fold level of a specific line.
		/// </remarks>
		public int GenerateFoldMarker(IDocument document, int lineNumber)
		{
			LineSegment line = document.GetLineSegment(lineNumber);
			int foldLevel = 0;
			
			while (document.GetCharAt(line.Offset + foldLevel) == '\t' && foldLevel + 1  < line.TotalLength) {
				++foldLevel;
			}
			
			return foldLevel;
		}
	
		public ArrayList GenerateFoldMarkers(IDocument document, string fileName, object parseInformation)
		{
			//FIXME: return the right info
			return new ArrayList ();
		}
	}
}

