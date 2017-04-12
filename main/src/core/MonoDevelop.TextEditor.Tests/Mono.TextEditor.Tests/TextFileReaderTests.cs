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
using MonoDevelop.Core.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class TextFileReaderTests
	{
		[Test()]
		public void TestFallback ()
		{
			byte[] input = new byte[] { 0xFF, 0x10, 0xAA, (byte)'u' , 0x8D};
			Assert.AreEqual ("?\u0010?u?", TextFileUtility.GetText (input));
		}

		[Test()]
		public void TestCodePage858 ()
		{
			byte[] input = new byte[] { (byte)'/', (byte)'/', (byte)' ', 0x9D };
			Assert.AreEqual ("// \u00D8", TextFileUtility.GetText (input));
		}

		[Test()]
		public void TestUTF8 ()
		{
			var src = "Hello World\u2122";
			byte[] input = Encoding.UTF8.GetBytes (src);
			Assert.AreEqual (src, TextFileUtility.GetText (input));
		}

		[Ignore ("Can't atm reliable detected.")]
		[Test()]
		public void TestUTF16SimpleText ()
		{
			var src = "Hello World";
			byte[] input = Encoding.Unicode.GetBytes (src);
			Assert.AreEqual (src, TextFileUtility.GetText (input));
		}

		[Test()]
		public void TestUTF16MixedText ()
		{
			var src = "Hello\u00A9 World\u2122\u008D";
			byte[] input = Encoding.Unicode.GetBytes (src);
			Assert.AreEqual (src, TextFileUtility.GetText (input));
		}
		
		[Ignore ("Can't atm reliable detected.")]
		[Test()]
		public void TestUTF16BESimpleText ()
		{
			var src = "Hello World";
			byte[] input = Encoding.BigEndianUnicode.GetBytes (src);
			Assert.AreEqual (src, TextFileUtility.GetText (input));
		}

		[Test()]
		public void TestUTF16BEMixedText ()
		{
			var src = "Hello\u00A9\u008D World\u2122";
			byte[] input = Encoding.BigEndianUnicode.GetBytes (src);
			Assert.AreEqual (src, TextFileUtility.GetText (input));
		}

		/// <summary>
		/// Bug 1803 - UTF-8 conversion issue 
		/// </summary>
		[Test()]
		public void TestBug1803 ()
		{
			// 0xA9 == (c) was the problem for UTF8
			byte[] input = new byte[] { (byte)'/', (byte)'/', (byte)' ', 0xA9 };
			Assert.AreEqual ("// \u00A9", TextFileUtility.GetText (input));
		}

		[Test()]
		public void TestCp1252 ()
		{
			var src = "Hello\u00A9 World\u00C1";
			byte[] input = Encoding.GetEncoding (1252).GetBytes (src);
			Assert.AreEqual (src, TextFileUtility.GetText (input));
		}

		[Test()]
		public void TestIsBinaryUtfInput ()
		{
			var src = "Hello\u00A9 World\u2122";
			byte[] input = Encoding.Unicode.GetBytes (src);
			Assert.IsFalse (TextFileUtility.IsBinary (input));
		}
		
		[Test()]
		public void TestIsBinaryBinaryInput ()
		{
			Assert.IsTrue (TextFileUtility.IsBinary (typeof(TextFileReaderTests).Assembly.Location));
		}

		
		/// <summary>
		/// Bug 4564 -Code point U+FEFF (Zero width nobreak space) is displayed as "ï»¿" the editor.
		/// </summary>
		[Test()]
		public void TestBug4564 ()
		{
			byte[] input = new byte[] { (byte)'a',(byte)'a', 0xEF, 0xBB, 0xBF };
			Assert.AreEqual ("aa\uFEFF", TextFileUtility.GetText (input));
		}

		[Test()]
		public void TestGB18030 ()
		{
			var src = "南北西东";
			byte[] input = Encoding.GetEncoding (54936).GetBytes (src);
			Assert.AreEqual (src, TextFileUtility.GetText (input));
		}
	}
}

