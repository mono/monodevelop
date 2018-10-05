//
// PointToLocationTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Threading;
using MonoDevelop.Ide;
using NUnit.Framework;
using System.Linq;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	class PointToLocationTests : TextEditorTestBase
	{
		[Test]
		public void TestBug674618 ()
		{
			var textEditor = new MonoTextEditor ();
			textEditor.Text = "🎉";
			var layout = textEditor.TextViewMargin.GetLayout (textEditor.Lines.First ());
			layout.GetPixelSize (out var w, out var h);
			var loc = textEditor.TextViewMargin.PointToLocation (w - 1, 0, snapCharacters: true);
			Assert.AreEqual (3, loc.Column);

		}
	}
}
