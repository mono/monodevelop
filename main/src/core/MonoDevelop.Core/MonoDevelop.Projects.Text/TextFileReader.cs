// TextFileReader.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
using System.IO;

namespace MonoDevelop.Projects.Text
{
	public class TextFileReader: TextReader
	{
		StringReader reader;
		string sourceEncoding;
		
		public TextFileReader (string fileName)
		{
			TextFile file = TextFile.ReadFile (fileName);
			reader = new StringReader (file.Text);
			sourceEncoding = file.SourceEncoding;
		}
		
		public override void Close ()
		{
			reader.Close ();
		}
		
		public override int Peek ()
		{
			return reader.Peek ();
		}
		
		public override int Read ()
		{
			return reader.Read ();
		}
		
		public override int Read (char[] buffer, int index, int len)
		{
			return reader.Read (buffer, index, len);
		}
		
		public override string ReadLine ()
		{
			return reader.ReadLine ();
		}
		
		public override string ReadToEnd ()
		{
			return reader.ReadToEnd ();
		}
		
		public string SourceEncoding {
			get { return sourceEncoding; }
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && reader != null)
				reader.Close ();

			reader = null;
			base.Dispose (disposing);
		}
	}
}
