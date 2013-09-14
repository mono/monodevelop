//
// Location.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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
using System;

namespace MonoDevelop.CSSBinding.Parse.Models
{
	public struct Location : IEquatable<Location>
	{
		public Location (string fileName, int line, int column) : this()
		{
			FileName = fileName;
			Column = column;
			Line = line;
		}

		public int Line { get; private set; }
		public int Column { get; private set; }
		public string FileName { get; private set; }

		public static Location Empty {
			get { return new Location (null, -1, -1); }
		}

		public Location AddLine ()
		{
			return new Location (this.FileName, this.Line + 1, 1);
		}

		public Location AddCol ()
		{
			return AddCols (1);
		}

		public Location AddCols (int number)
		{
			return new Location (this.FileName, this.Line, this.Column + number);
		}

		public override string ToString ()
		{
			return string.Format("[{0} ({1},{2})]", FileName, Line, Column);
		}

		public bool Equals (Location other)
		{
			return other.Line == Line && other.Column == Column && other.FileName == FileName;
		}
	}
}

