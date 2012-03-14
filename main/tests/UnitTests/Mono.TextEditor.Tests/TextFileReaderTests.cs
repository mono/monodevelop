// 
// TextFileReaderTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
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
using System.IO;
using System.Text;
using System.Linq;
using NUnit.Framework;
using Mono.TextEditor.Utils;
using System.Reflection;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class TextFileReaderTests
	{
		static Encoding GetEncoding (byte[] input)
		{
			var method = typeof (TextFileReader).GetMethod ("AutoDetectEncoding", BindingFlags.Static | BindingFlags.NonPublic);
			using (var ms = new MemoryStream (input)) {
				return (Encoding)method.Invoke (null, new object[] { ms });
			}
		}

		[Test()]
		public void TestUTF8 ()
		{
			byte[] input = Encoding.UTF8.GetBytes ("Hello World\u2122");
			var result = GetEncoding (input);
			Assert.IsTrue (result != null);
			Assert.AreEqual (Encoding.UTF8, result);
		}

		[Test()]
		public void TestUTF16 ()
		{
			byte[] input = Encoding.Unicode.GetBytes ("Hello World");
			var result = GetEncoding (input);
			Assert.IsTrue (result != null);
			Assert.AreEqual (Encoding.Unicode, result);
		}

		[Test()]
		public void TestUTF16BE ()
		{
			byte[] input = Encoding.BigEndianUnicode.GetBytes ("Hello World");
			var result = GetEncoding (input);
			Assert.IsTrue (result != null);
			Assert.AreEqual (Encoding.BigEndianUnicode, result);
		}

		/// <summary>
		/// Bug 1803 - UTF-8 conversion issue 
		/// </summary>
		[Test()]
		public void TestBug1803 ()
		{
			// 0xA9 == (c) was the problem for UTF8
			byte[] input = new byte[] { (byte)'/', (byte)'/', (byte)' ', 0xA9 };

			var result = GetEncoding (input);
			Assert.IsTrue (result != null);
			Assert.AreNotEqual (Encoding.UTF8, result);
		}
	}
}

