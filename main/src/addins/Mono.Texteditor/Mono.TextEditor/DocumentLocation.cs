// DocumentLocation.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;

namespace Mono.TextEditor
{
	public struct DocumentLocation
	{
		public static readonly DocumentLocation Empty = new DocumentLocation (-1, -1);
		
		public int Line { get; set; }
		public int Column { get; set; }
		
		public bool IsEmpty {
			get {
				return Line < 0 && Column < 0;
			}
		}
		
		public DocumentLocation (int line, int column)
		{
			this.Line = line;
			this.Column = column;
		}
		
		public override string ToString ()
		{
			return String.Format ("[DocumentLocation: Line={0}, Column={1}]", this.Line, this.Column);
		}
		
		#region Operations
		public static bool operator ==(DocumentLocation left, DocumentLocation right)
		{
			return left.Line == right.Line && left.Column == right.Column;
		}
		public static bool operator !=(DocumentLocation left, DocumentLocation right)
		{
			return !(left == right);
		}
		
		public static bool operator <(DocumentLocation left, DocumentLocation right)
		{
			return left.Line < right.Line || (left.Line == right.Line && left.Column < right.Line);
		}
		public static bool operator <=(DocumentLocation left, DocumentLocation right)
		{
			return !(left > right);
		}
		
		public static bool operator >(DocumentLocation left, DocumentLocation right)
		{
			return right < left;
		}
		public static bool operator >=(DocumentLocation left, DocumentLocation right)
		{
			return !(left < right);
		}
		
		public override int GetHashCode()
		{
			return unchecked (Column.GetHashCode () * Line.GetHashCode ());
		}
		
		public override bool Equals(object obj)
		{
			return obj is DocumentLocation && (DocumentLocation)obj == this; 
		}
		#endregion
	}
	
	public class DocumentLocationEventArgs : System.EventArgs
	{
		DocumentLocation location;
		
		public Mono.TextEditor.DocumentLocation Location {
			get {
				return location;
			}
		}
		
		public DocumentLocationEventArgs (DocumentLocation location)
		{
			this.location = location;
		}
	}
}
