//  TextAreaUpdate.cs
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

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This enum describes all implemented request types
	/// </summary>
	public enum TextAreaUpdateType {
		WholeTextArea,
		SingleLine,
		SinglePosition,
		PositionToLineEnd,
		PositionToEnd,
		LinesBetween
	}
	
	/// <summary>
	/// This class is used to request an update of the textarea
	/// </summary>
	public class TextAreaUpdate
	{
		Point              position;
		TextAreaUpdateType type;
		
		public TextAreaUpdateType TextAreaUpdateType {
			get {
				return type;
			}
		}
		
		public Point Position {
			get {
				return position;
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>	
		public TextAreaUpdate(TextAreaUpdateType type)
		{
			this.type = type;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>	
		public TextAreaUpdate(TextAreaUpdateType type, Point position)
		{
			this.type     = type;
			this.position = position;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>	
		public TextAreaUpdate(TextAreaUpdateType type, int startLine, int endLine)
		{
			this.type     = type;
			this.position = new Point(startLine, endLine);
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>	
		public TextAreaUpdate(TextAreaUpdateType type, int singleLine)
		{
			this.type     = type;
			this.position = new Point(0, singleLine);
		}
		
		public override string ToString()
		{
			return String.Format("[TextAreaUpdate: Type={0}, Position={1}]", type, position);
		}
	}
}
