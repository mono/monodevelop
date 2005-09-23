// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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

