// 
// TextBreakerTests.cs
//  
// Author:
//       IBBoard <dev@ibboard.co.uk>
// 
// Copyright (c) 2011 IBBoard
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
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using NUnit.Framework;
using System.Collections.Generic;
using MonoDevelop.Core.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	class TextBreakerTests : TextEditorTestBase
	{
		[Test()]
		public void TestTextBreakerWithSingleWord ()
		{
			var segments = BreakAllLines ("Word");
			Assert.That (segments.Count, Is.EqualTo (1));
			Assert.That (segments [0].Offset, Is.EqualTo (0));
			Assert.That (segments [0].Length, Is.EqualTo (4));
		}

		[Test()]
		public void TestTextBreakerWithSingleWordWrappedInSpaces ()
		{
			var segments = BreakAllLines (" Word ");
			Assert.That (segments.Count, Is.EqualTo (3));
			Assert.That (segments [0].Offset, Is.EqualTo (0));
			Assert.That (segments [0].Length, Is.EqualTo (1));
			Assert.That (segments [1].Offset, Is.EqualTo (1));
			Assert.That (segments [1].Length, Is.EqualTo (4));
			Assert.That (segments [2].Offset, Is.EqualTo (5));
			Assert.That (segments [2].Length, Is.EqualTo (1));
		}

		[Test()]
		public void TestTextBreakerWithMultipleLines ()
		{
			var segments = BreakAllLines ("SomeText\nTwo Words");
			Assert.That (segments.Count, Is.EqualTo (4));
			Assert.That (segments [0].Offset, Is.EqualTo (0));
			Assert.That (segments [0].Length, Is.EqualTo (8));
			Assert.That (segments [1].Offset, Is.EqualTo (9));
			Assert.That (segments [1].Length, Is.EqualTo (3));
			Assert.That (segments [2].Offset, Is.EqualTo (12));
			Assert.That (segments [2].Length, Is.EqualTo (1));
			Assert.That (segments [3].Offset, Is.EqualTo (13));
			Assert.That (segments [3].Length, Is.EqualTo (5));
		}

		[Test()]
		public void Bug666274_CheckLeftHandSideWordBreaking ()
		{
			var segments = BreakAllLines ("			//Set points in panel");
			Assert.That(segments.Count, Is.EqualTo(12));
		}

		[Test()]
		public void Bug666274_CheckRightHandSideWordBreaking ()
		{
			var segments = BreakAllLines (@"			if (WarFoundryCore.CurrentArmy != null)
			{
				lblTotalPoints.Text = Translation.GetTranslation(""statusPanelPoints"", ""{0}pts of {1} pts"", WarFoundryCore.CurrentArmy.Points, WarFoundryCore.CurrentArmy.MaxPoints);
			}
			else
			{
				lblTotalPoints.Text = """";
			}");
			Assert.That (segments.Count, Is.EqualTo (97));
		}

		public TextEditorData CreateData (string editorText)
		{
			return new TextEditorData (new TextDocument (editorText));
		}

		public List<ISegment> BreakAllLines (String editorText)
		{
			return BreakAllLines (CreateData (editorText));
		}

		public List<ISegment> BreakAllLines (TextEditorData data)
		{
			return TextBreaker.BreakLinesIntoWords (data.Document, 1, data.LineCount, false);
		}
	}
}

