//
// CSharpProjectPropertiesTests.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class CSharpProjectPropertiesTests
	{
		[Test]
		public void TestRemovalOfDefineSymbolsDuplications()
		{
			Test ("  asd  ");
			Test ("  asd  ;asd", "asd");
			Test ("  asd  ;asd  ", "asd  ");
			Test ("  asd");
			Test (" asd ");
			Test (" asd");
			Test ("asd");
			Test ("asd  ");
			Test (" asd; asdR");
			Test (" asd; asdR");
			Test (" asd; asdR");
			Test ("asd; asd ", "asd ");
			Test ("asd; asd  ", "asd  ");
			Test ("asd; asd  ;AA ", "asd  ;AA ");
			Test ("asd; asd  ;AA", "asd  ;AA");
			Test ("asd; TT\t\tasd", "TT\t\tasd");
			Test ("asd; asd TT\t\t", "asd TT\t\t");
			Test ("asd\t\t\t\tasd ", "asd ");
		}

		static void Test (string str, string expected = null)
		{
			if (expected == null)
				expected = str;
			Assert.AreEqual (expected, MonoDevelop.CSharp.Project.CodeGenerationPanelWidget.RemoveDuplicateDefinedSymbols (str));
		}
	}
}

