// ReplaceEventArgs.cs
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
using System.Text;

namespace Mono.TextEditor
{
	public class ReplaceEventArgs : System.EventArgs
	{
		public int Offset {
			get;
			private set;
		}

		public int Count {
			get;
			private set;
		}
		
		public string Value {
			get;
			set;
		}
		
		public ReplaceEventArgs (int offset, int count, string value)
		{
			this.Offset = offset;
			this.Count  = count;
			this.Value  = value;
		}
		
		public override string ToString ()
		{
			return String.Format ("[ReplaceEventArgs: Offset={0}, Count={1}, Value={2}]",
			                      this.Offset,
			                      this.Count,
			                      this.Value);
		}

	}
}
