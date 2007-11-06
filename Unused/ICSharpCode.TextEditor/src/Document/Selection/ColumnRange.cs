//  ColumnRange.cs
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
using MonoDevelop.TextEditor.Undo;

namespace MonoDevelop.TextEditor.Document
{
	public class ColumnRange 
	{
		public static readonly ColumnRange NoColumn    = new ColumnRange(-2, -2);
		public static readonly ColumnRange WholeColumn = new ColumnRange(-1, -1);
		
		int startColumn;
		int endColumn;
		
		public int StartColumn {
			get {
				return startColumn;
			}
			set {
				startColumn = value;
			}
		}
		
		public int EndColumn {
			get {
				return endColumn;
			}
			set {
				endColumn = value;
			}
		}
		
		public ColumnRange(int startColumn, int endColumn)
		{
			this.startColumn = startColumn;
			this.endColumn = endColumn;
			
		}
		
		public override int GetHashCode()
		{
			return startColumn + (endColumn << 16);
		}
		
		public override bool Equals(object obj)
		{
			if (obj is ColumnRange) {
				return ((ColumnRange)obj).startColumn == startColumn &&
				       ((ColumnRange)obj).endColumn == endColumn;
				
			}
			return false;
		}
		
		public override string ToString()
		{
			return String.Format("[ColumnRange: StartColumn={0}, EndColumn={1}]", startColumn, endColumn);
		}
		
		
		
	}
}
