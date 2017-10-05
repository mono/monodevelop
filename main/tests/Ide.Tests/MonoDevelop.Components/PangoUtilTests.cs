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
		[TestCase ("Test", 3, 3, 3, 3)]
		[TestCase ("バージョン", 1, 3, 3, 1)]
		[TestCase ("バージョン", 1, 3, 4, 1)]
		public void TextIndexerWorks (string arg, int index, int indexToByteIndex, int byteIndex, int byteIndexToIndex)
		{
			var indexer = new TextIndexer (arg);

			Assert.AreEqual (indexToByteIndex, indexer.IndexToByteIndex (index));
			Assert.AreEqual (byteIndexToIndex, indexer.ByteIndexToIndex (byteIndex));
		}
	}
}
