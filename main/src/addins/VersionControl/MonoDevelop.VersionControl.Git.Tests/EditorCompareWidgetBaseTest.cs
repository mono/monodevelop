// 
// EditorCompareWidgetBaseTest.cs
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
using NUnit.Framework;
using Mono.TextEditor.Utils;
using Cairo;

namespace MonoDevelop.VersionControl.Views
{
	[TestFixture()]
	public class EditorCompareWidgetBaseTest
	{
		private delegate void ColorAssertion (Color color);

		[Test()]
		public void TestRemovalLineColorIsRed ()
		{
			CheckCombinationsAreColor (new Hunk (0, 0, 1, 0), AssertIsRed);
			CheckCombinationsAreColor (new Hunk (0, 0, 2, 0), AssertIsRed);
		}

		[Test()]
		public void TestAdditionLineColorIsGreen ()
		{
			CheckCombinationsAreColor (new Hunk (0, 0, 0, 1), AssertIsGreen);
			CheckCombinationsAreColor (new Hunk (0, 0, 0, 2), AssertIsGreen);
		}

		[Test()]
		public void TestAdditionAndRemovalLineColorIsGreen ()
		{
			CheckCombinationsAreColor (new Hunk (0, 0, 1, 1), AssertIsBlue);
			CheckCombinationsAreColor (new Hunk (0, 0, 2, 2), AssertIsBlue);
			CheckCombinationsAreColor (new Hunk (0, 0, 1, 2), AssertIsBlue);
			CheckCombinationsAreColor (new Hunk (0, 0, 2, 1), AssertIsBlue);
		}

		[Ignore("No dark border colors with new flat design, borders have the same color")]
		[Test()]
		public void TestDarkColorsAreDarker ()
		{
			CheckDarkColoursAreDarker (new Hunk (0, 0, 1, 0));
			CheckDarkColoursAreDarker (new Hunk (0, 0, 0, 1));
			CheckDarkColoursAreDarker (new Hunk (0, 0, 1, 1));
		}

		private void CheckCombinationsAreColor (Hunk hunk, ColorAssertion assertion)
		{
			assertion (GetColor (hunk, true, true));
			assertion (GetColor (hunk, true, false));
			assertion (GetColor (hunk, false, true));
			assertion (GetColor (hunk, false, false));
		}

		private Color GetColor (Hunk hunk, bool removeSide, bool dark)
		{
			return EditorCompareWidgetBase.GetColor (hunk, removeSide, dark, 1.0);
		}

		private void AssertIsRed (Color color)
		{
			Assert.Greater (color.R, color.G);
			Assert.Greater (color.R, color.B);
		}

		private void AssertIsGreen (Color color)
		{
			Assert.Greater (color.G, color.R);
			Assert.Greater (color.G, color.B);
		}

		private void AssertIsBlue (Color color)
		{
			Assert.Greater (color.B, color.G);
			Assert.Greater (color.B, color.R);
		}

		private void CheckDarkColoursAreDarker (Hunk hunk)
		{
			Color dark = GetColor (hunk, true, true);
			Color light = GetColor (hunk, true, false);
			Assert.Less (dark.R, light.R);
			Assert.Less (dark.B, light.B);
			Assert.Less (dark.G, light.G);
		}
	}
}

