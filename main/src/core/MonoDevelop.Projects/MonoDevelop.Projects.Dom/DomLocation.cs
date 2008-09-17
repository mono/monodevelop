//
// DomLocation.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

namespace MonoDevelop.Projects.Dom
{
	public struct DomLocation : IComparable<DomLocation>, IEquatable<DomLocation>
	{
		int line, column;
		
		public bool IsEmpty {
			get {
				return Line < 0;
			}
		}
		
		public int Line {
			get {
				return line;
			}
			set {
				line = value;
			}
		}

		public int Column {
			get {
				return column;
			}
			set {
				column = value;
			}
		}
		
		public static DomLocation Empty {
			get {
				return new DomLocation (-1, -1);
			}
		}
		
		public DomLocation (int line, int column)
		{
			this.line   = line;
			this.column = column;
		}
		
		public override bool Equals (object other)
		{
			if (!(other is DomLocation)) 
				return false;
			return (DomLocation)other == this;
		}

		public override int GetHashCode ()
		{
			return line + column*5000;
		}

		
		public bool Equals (DomLocation other)
		{
			return other == this;
		}
		
		public int CompareTo (DomLocation other)
		{
			if (this == other)
				return 0;
			if (this < other)
				return -1;
			return 1;
		}
		
		public override string ToString ()
		{
			return String.Format ("[DomLocation: Line={0}, Column={1}]", Line, Column);
		}

		public static DomLocation FromInvariantString (string invariantString)
		{
			if (invariantString.ToUpper () == "EMPTY")
				return DomLocation.Empty;
			string[] splits = invariantString.Split (',', '/');
			if (splits.Length == 2) 
				return new DomLocation (Int32.Parse (splits[0]), Int32.Parse (splits[1]));
			return DomLocation.Empty;
		}
		
		public string ToInvariantString ()
		{
			if (IsEmpty)
				return "Empty";
			return String.Format ("{0}/{1}", Line, Column);
		}
		
		public static bool operator==(DomLocation left, DomLocation right)
		{
			return left.Line == right.Line && left.Column == right.Column;
		}
		
		public static bool operator!=(DomLocation left, DomLocation right)
		{
			return left.Line != right.Line || left.Column != right.Column;
		}
		
		public static bool operator<(DomLocation left, DomLocation right)
		{
			return left.Line < right.Line || left.Line == right.Line && left.Column < right.Column;
		}
		public static bool operator>(DomLocation left, DomLocation right)
		{
			return left.Line > right.Line || left.Line == right.Line && left.Column > right.Column;
		}
		public static bool operator<=(DomLocation left, DomLocation right)
		{
			return !(left > right);
		}
		public static bool operator>=(DomLocation left, DomLocation right)
		{
			return !(left < right);
		}
	}
}
