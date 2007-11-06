//  DefaultSelection.cs
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

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// Default implementation of the <see cref="MonoDevelop.TextEditor.Document.ISelection"/> interface.
	/// </summary>
	public class DefaultSelection : ISelection
	{
		IDocument document = null;
		bool      isRectangularSelection = false;
		Point     startPosition = new Point(-1, -1);
		Point     endPosition   = new Point(-1, -1);
		
		public Point StartPosition {
			get {
				return startPosition;
			}
			set {
				startPosition = value;
			}
		}
		
		public Point EndPosition {
			get {
				return endPosition;
			}
			set {
				endPosition = value;
			}
		}
		
		public int Offset {
			get {
				return document.PositionToOffset(startPosition);
			}
		}
		
		public int EndOffset {
			get {
				return document.PositionToOffset(endPosition);
			}
		}
		
		public int Length {
			get {
				return EndOffset - Offset;
			}
		}
		
		/// <value>
		/// Returns true, if the selection is empty
		/// </value>
		public bool IsEmpty {
			get {
				return startPosition == endPosition;
			}
		}
		
		/// <value>
		/// Returns true, if the selection is rectangular
		/// </value>
		// TODO : make this unused property used.
		public bool IsRectangularSelection {
			get {
				return isRectangularSelection;
			}
			set {
				isRectangularSelection = value;
			}
		}
		
		/// <value>
		/// The text which is selected by this selection.
		/// </value>
		public string SelectedText {
			get {
				if (document != null) {
					if (Length < 0) {
						return null;
					}
					return document.GetText(Offset, Length);
				}
				return null;
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="DefaultSelection"/>
		/// </summary>	
		public DefaultSelection(IDocument document, Point startPosition, Point endPosition)
		{
			this.document      = document;
			this.startPosition = startPosition;
			this.endPosition   = endPosition;
		}
		
		/// <summary>
		/// Converts a <see cref="DefaultSelection"/> instance to string (for debug purposes)
		/// </summary>
		public override string ToString()
		{
			return String.Format("[DefaultSelection : StartPosition={0}, EndPosition={1}]", startPosition, endPosition);
		}
		public bool ContainsPosition(Point position)
		{
			return startPosition.Y < position.Y && position.Y  < endPosition.Y ||
			       startPosition.Y == position.Y && startPosition.X <= position.X && (startPosition.Y != endPosition.Y || position.X <= endPosition.X) ||
			       endPosition.Y == position.Y && startPosition.Y != endPosition.Y && position.X <= endPosition.X;
		}
		
		public bool ContainsOffset(int offset)
		{
			return Offset <= offset && offset <= EndOffset;
		}
	}
}
