//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
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
using System.Text;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// A string writer that allows you to specify the text encoding to
	/// be used when generating the string.
	/// </summary>
	/// <remarks>
	/// This class is used when generating xml strings using a writer and
	/// the encoding in the xml processing instruction needs to be changed.
	/// The xml encoding string will be the encoding specified in the constructor 
	/// of this class (i.e. UTF-8, UTF-16)</remarks>
	public class EncodedStringWriter : StringWriter
	{
		Encoding encoding = Encoding.UTF8;
		
		/// <summary>
		/// Creates a new string writer that will generate a string with the
		/// specified encoding.  
		/// </summary>
		/// <remarks>The encoding will be used when generating the 
		/// xml encoding header (i.e. UTF-8, UTF-16).</remarks>
		public EncodedStringWriter(Encoding encoding)
		{
			this.encoding = encoding;
		}
		
		/// <summary>
		/// Gets the text encoding that will be used when generating
		/// the string.
		/// </summary>
		public override Encoding Encoding {
			get {
				return encoding;
			}
		}
	}
}
