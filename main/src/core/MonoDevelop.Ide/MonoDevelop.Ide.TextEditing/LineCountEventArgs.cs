//
// LineCountEventArgs.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using MonoDevelop.Core;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide.TextEditing
{
	
	public class LineCountEventArgs : TextFileEventArgs
	{
		int lineNumber;
		int lineCount;
		int column;
		
		public int LineNumber {
			get {
				return lineNumber;
			}
		}
		
		public int LineCount {
			get {
				return lineCount;
			}
		}
		
		public int Column {
			get {
				return column;
			}
		}
		
		public LineCountEventArgs (ITextFile textFile, int lineNumber, int lineCount, int column) : base (textFile)
		{
			this.lineNumber = lineNumber;
			this.lineCount  = lineCount;
			this.column     = column;
		}
		
		public override string ToString ()
		{
			return String.Format ("[LineCountEventArgs: LineNumber={0}, LineCount={1}, Column={2}]", lineNumber, lineCount, column);
		}
	}}
