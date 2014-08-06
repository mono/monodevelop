// 
// LineInterceptingTextWriter.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.IO;

namespace MonoDevelop.AspNet.Execution
{
	
	/// <summary>
	/// This is used to watch for a specific lines in a textwriter. It wraps an (optional) real writer,
	/// parses lines, and raises events.
	/// </summary>
	class LineInterceptingTextWriter : TextWriter
	{
		readonly TextWriter innerWriter;
		System.Text.StringBuilder sb = new System.Text.StringBuilder ();
		Action onNewLine;
		bool prevCharWasCR;
		
		public LineInterceptingTextWriter (TextWriter innerWriter, Action onNewLine)
		{
			this.innerWriter = innerWriter;
			this.onNewLine = onNewLine;
			this.MaxLineLength = 256;
		}
		
		public bool FinishedIntercepting {
			get {
				return sb == null;
			}
			set {
				if (value)
					sb = null;
			}
		}
		
		/// <summary>
		/// The number of lines that have been captured.
		/// </summary>
		public int LineCount { get; private set; }
		
		/// <summary>
		/// The maximum number of characters that will be captured per line.
		/// </summary>
		public int MaxLineLength { get; private set; }
		
		public string GetLine ()
		{
			return sb.ToString ();
		}
		
		public override void Write (char value)
		{
			if (innerWriter != null)
				innerWriter.Write (value);
		}
		
		//the ProcessWrapper only feeds output to this method
		public override void Write (string value)
		{
			if (sb != null) {
				for (int i = 0; i < value.Length; i++) {
					char c = value[i];
					if (c == '\n' || c == '\r') {
						if (!prevCharWasCR || c == '\r') {
							LineCount++;
							if (sb != null && sb.Length > 0)
								onNewLine ();
							if (sb != null)
								sb.Length = 0;
						}
					} else if (sb != null && sb.Length < MaxLineLength) {
						sb.Append (c);
					}
					prevCharWasCR = c == '\r';
				}
			}
			if (innerWriter != null)
				innerWriter.Write (value);
		}
		
		public override void Flush ()
		{
			if (innerWriter != null)
				innerWriter.Flush ();
		}
		
		public override void Close ()
		{
			base.Close ();
			if (innerWriter != null)
				innerWriter.Close ();
		}
		
		public override System.Text.Encoding Encoding {
			get {
				if (innerWriter != null)
					return innerWriter.Encoding;
				return System.Text.Encoding.Default;
			}
		}
	}
}
