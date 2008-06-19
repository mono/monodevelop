//
// DomRegion.cs
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
	public struct DomRegion
	{
		public readonly static DomRegion Empty = new DomRegion (-1, -1, -1, -1);
		
		DomLocation start, end;
		
		public bool IsEmpty {
			get {
				return Start.Line < 0;
			}
		}

		public DomLocation Start {
			get {
				return start;
			}
			set {
				start = value;
			}
		}

		public DomLocation End {
			get {
				return end;
			}
			set {
				end = value;
			}
		}
		
		public DomRegion (int startLine, int endLine) : this (startLine, -1, endLine, -1)
		{
		}
		
		public DomRegion (int startLine, int startColumn, int endLine, int endColumn)
		{
			this.start = new DomLocation (startLine, startColumn);
			this.end   = new DomLocation (endLine, endColumn);
		}
		
		public DomRegion (DomLocation start, DomLocation end) : this (start.Line, start.Column, end.Line, end.Column)
		{
		}
		
		public override string ToString ()
		{
			if (this.IsEmpty)
				return string.Format ("[DomRegion: Empty]");
			
			return string.Format ("[DomRegion:{0}/{1} - {2}/{3}]",
			                      Start.Line,
			                      Start.Column,
			                      End.Line,
			                      End.Column);
		}
		
		public bool Contains (DomLocation location)
		{
			return start <= location && location <= end;
		}
		public bool Contains (int line, int column)
		{
			return Contains (new DomLocation (line, column));
		}
		
		public static bool operator==(DomRegion left, DomRegion right)
		{
			return left.Start == right.Start && left.End == right.End;
		}
		
		public static bool operator!=(DomRegion left, DomRegion right)
		{
			return left.Start != right.Start|| left.End != right.End;
		}
		
	}
}
