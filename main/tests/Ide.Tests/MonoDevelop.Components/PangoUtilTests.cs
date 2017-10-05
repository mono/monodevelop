//
// PangoUtilTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 
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
using NUnit.Framework;

namespace MonoDevelop.Components
{
	[TestFixture]
	public class PangoUtilTests
	{
		[Test]
		public void TextIndexerAscii ()
		{
			var str = "Test";
			var indexer = new TextIndexer (str);

			Assert.AreEqual (0, indexer.IndexToByteIndex (0));
			Assert.AreEqual (0, indexer.ByteIndexToIndex (0));
			Assert.AreEqual (1, indexer.IndexToByteIndex (1));
			Assert.AreEqual (1, indexer.ByteIndexToIndex (1));
			Assert.AreEqual (2, indexer.IndexToByteIndex (2));
			Assert.AreEqual (2, indexer.ByteIndexToIndex (2));
			Assert.AreEqual (3, indexer.IndexToByteIndex (3));
			Assert.AreEqual (3, indexer.ByteIndexToIndex (3));
		}

		[Test]
		public void TextIndexerUnicode ()
		{
			var str = "バージョン";
			var indexer = new TextIndexer (str);

			Assert.AreEqual (0, indexer.IndexToByteIndex (0));
			Assert.AreEqual (3, indexer.IndexToByteIndex (1));
			Assert.AreEqual (6, indexer.IndexToByteIndex (2));

			Assert.AreEqual (0, indexer.ByteIndexToIndex (0));
			Assert.AreEqual (0, indexer.ByteIndexToIndex (1));
			Assert.AreEqual (0, indexer.ByteIndexToIndex (2));
			Assert.AreEqual (1, indexer.ByteIndexToIndex (3));
			Assert.AreEqual (1, indexer.ByteIndexToIndex (4));
			Assert.AreEqual (1, indexer.ByteIndexToIndex (5));
		}

		[Test]
		public void TextIndexerMixed ()
		{
			var str = "バAジョン";
			var indexer = new TextIndexer (str);

			Assert.AreEqual (0, indexer.IndexToByteIndex (0));
			Assert.AreEqual (3, indexer.IndexToByteIndex (1));
			Assert.AreEqual (4, indexer.IndexToByteIndex (2));

			Assert.AreEqual (0, indexer.ByteIndexToIndex (0));
			Assert.AreEqual (0, indexer.ByteIndexToIndex (1));
			Assert.AreEqual (0, indexer.ByteIndexToIndex (2));
			Assert.AreEqual (1, indexer.ByteIndexToIndex (3));
			Assert.AreEqual (2, indexer.ByteIndexToIndex (4));
			Assert.AreEqual (2, indexer.ByteIndexToIndex (5));
		}
	}
}
