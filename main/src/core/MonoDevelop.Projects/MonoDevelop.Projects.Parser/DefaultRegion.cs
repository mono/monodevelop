//  DefaultRegion.cs
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
using System.Diagnostics;

namespace MonoDevelop.Projects.Parser {
	
	[Serializable]
	public class DefaultRegion : System.MarshalByRefObject, IRegion
	{
		protected int beginLine = -1;
		protected int endLine = -1;
		protected int beginColumn = -1;
		protected int endColumn = -1;
		protected string fileName;

		public virtual int BeginLine {
			get {
				return beginLine;
			}
			set {
				beginLine = value; 
			}
		}

		public virtual int BeginColumn {
			get {
				return beginColumn;
			}
			set {
				beginColumn = value;
			}
		}

		/// <value>
		/// if the end column is == -1 the end line is -1 too
		/// this stands for an unknwon end
		/// </value>
		public virtual int EndColumn {
			get {
				return endColumn;
			}
			set {
				endColumn = value;
			}
		}

		/// <value>
		/// if the end line is == -1 the end column is -1 too
		/// this stands for an unknwon end
		/// </value>
		public virtual int EndLine {
			get {
				return endLine;
			}
			set {
				endLine = value;
			}
		}

		public string FileName {
			get { 
				return fileName; 
			}
			set {
				fileName = value;
			}
		}
		
		public DefaultRegion(Point start, Point end) : this(start.Y, start.X, end.Y, end.X)
		{
		}
		
		public DefaultRegion(int beginLine, int beginColumn)
		{
			this.beginLine   = beginLine;
			this.beginColumn = beginColumn;
		}

		public DefaultRegion(int beginLine, int beginColumn, int endLine, int endColumn)
		{
			this.beginLine   = beginLine;
			this.beginColumn = beginColumn;
			this.endLine     = endLine;
			this.endColumn   = endColumn;
		}

		/// <remarks>
		/// Returns true, if the given coordinates (row, column) are in the region.
		/// This method assumes that for an unknown end the end line is == -1
		/// </remarks>
		public bool IsInside(int row, int column)
		{
			return row >= BeginLine &&
			      (row <= EndLine   || EndLine == -1) &&
			      (row != BeginLine || column >= BeginColumn) &&
			      (row != EndLine   || column <= EndColumn);
		}

		public override string ToString()
		{
			return String.Format("[Region: BeginLine = {0}, EndLine = {1}, BeginColumn = {2}, EndColumn = {3}]",
			                     beginLine,
			                     endLine,
			                     beginColumn,
			                     endColumn);
		}
		
		public virtual int CompareTo(IRegion value)
		{
			int cmp;
			if (0 != (cmp = (BeginLine - value.BeginLine))) {
				return cmp;
			}
			
			if (0 != (cmp = (BeginColumn - value.BeginColumn))) {
				return cmp;
			}
			
			if (0 != (cmp = (EndLine - value.EndLine))) {
				return cmp;
			}
			
			return EndColumn - value.EndColumn;
		}
		
		int IComparable.CompareTo(object value) {
			return CompareTo((IRegion)value);
		}
	}
}
