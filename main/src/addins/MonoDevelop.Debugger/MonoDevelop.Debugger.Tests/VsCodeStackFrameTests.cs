//
// VsCodeStackFrameTests.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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

using NUnit.Framework;

using MonoDevelop.Debugger.VsCodeDebugProtocol;

namespace MonoDevelop.Debugger.Tests
{
	[TestFixture]
	public class VsCodeStackFrameTests
	{
		[Test]
		public void TestHexDecode ()
		{
			var result = VsCodeStackFrame.HexToByteArray ("fFaAbB0012a1");

			Assert.AreEqual ((byte) 0xff, result[0], "result[0]");
			Assert.AreEqual ((byte) 0xaa, result[1], "result[1]");
			Assert.AreEqual ((byte) 0xbb, result[2], "result[2]");
			Assert.AreEqual ((byte) 0x00, result[3], "result[3]");
			Assert.AreEqual ((byte) 0x12, result[4], "result[4]");
			Assert.AreEqual ((byte) 0xa1, result[5], "result[5]");
		}
	}
}
