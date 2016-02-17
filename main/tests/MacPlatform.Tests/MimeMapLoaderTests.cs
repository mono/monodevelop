//
// MimeMapLoaderTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using MonoDevelop.MacIntegration;
using NUnit.Framework;

namespace MacPlatform.Tests
{
	[TestFixture]
	public class MimeMapLoaderTests
	{
		[TestCase ("text/css\t\t\t\t\tcss", "text/css", ".css")]
		[TestCase ("#text/abc\ntext/css\t\t\t\t\tcss", "text/css", ".css")]
		[TestCase ("video/mp4\t\t\t\t\tmp4", "video/mp4", ".mp4")]
		[TestCase ("text/x-c\t\t\t\t\tc h", "text/x-c", ".c")]
		[TestCase ("text/x-c\t\t\t\t\tc h", "text/x-c", ".h")]
		public void MimeMap (string line, string mimeType, string extension)
		{
			var map = new Dictionary<string, string> ();
			var stringReader = new StringReader (line);
			var loader = new MimeMapLoader (map);
			loader.LoadMimeMap (stringReader);

			string matchedMimeType = null;
			map.TryGetValue (extension, out matchedMimeType);

			Assert.AreEqual (mimeType, matchedMimeType);
		}
	}
}

