//
// RenameRefactoringTests.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using NUnit.Framework;
using NUnit.Framework.Internal;
using MonoDevelop.Refactoring.Rename;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Refactoring.Tests
{
	[TestFixture]
	class RenameRefactoringTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharp;

		async Task CheckRename (string input)
		{
			var sb = StringBuilderCache.Allocate ();
			try {
				bool inLink = false;
				int linkStart = 0;
				var expectedLinks = new List<TextSegment> ();
				foreach (var ch in input) {
					if (ch == '$') {
						if (inLink) {
							expectedLinks.Add (new TextSegment (linkStart, sb.Length - linkStart));
						} else {
							linkStart = sb.Length;
						}
						inLink = !inLink;
						continue;
					}
					sb.Append (ch);
				}

				using (var testCase = await SetupTestCase (sb.ToString())) {
					var model = await testCase.Document.AnalysisDocument.GetSemanticModelAsync ();
					ISymbol symbol = null;
					var root = await model.SyntaxTree.GetRootAsync ();
					foreach (var l in expectedLinks) {
						var node = root.FindToken (l.Offset).Parent;
						if (node == null)
							continue;
						var symbolInfo = model.GetSymbolInfo (node);
						if (symbolInfo.Symbol == null)
							continue;
						symbol = symbolInfo.Symbol;
						break;
					}
					Assert.NotNull (symbol, "No symbol found.");

					var links = await RenameRefactoring.GetTextLinksAsync (testCase.Document, 0, symbol);
					Assert.AreEqual (expectedLinks.Count, links [0].Links.Count);
					foreach (var l in links [0].Links) {
						var expected = expectedLinks.FirstOrDefault (el => l.Offset == el.Offset);
						Assert.NotNull (expected, "No expected link found at : " + l.Offset);
						Assert.AreEqual (expected.Length, l.Length);
					}
				}
			} finally {
				StringBuilderCache.Free (sb);
			}
		}

		/// <summary>
		/// Bug 641295: Renaming method should not trigger removal of generic type arguments
		/// </summary>
		[Test]
		public async Task TestVSTS641295 ()
		{
			await CheckRename (@"using System;

namespace RenameGenericMethodParameterInference
{
	class MainClass
	{
		static T $Apply$<T> (int arg) => default (T);

		static void TakeInt (int i) { }

		public static void Main (string [] args)
		{
			TakeInt ($Apply$<int> (0));
			TakeInt ($Apply$<int> (0));
		}
	}
}");
		}


		/// <summary>
		/// Bug 40464 - Rename refactoring a method parameter causes subsequent document elements to be removed.
		/// </summary>
		[Test]
		public async Task TestBugzilla40464 ()
		{
			await CheckRename (@"using System;

namespace RenameGenericMethodParameterInference
{
	class MainClass
	{
		/// <summary>
		/// Apply the specified value and dataReader.
		/// </summary>
		/// <param name=""$value$""> Value.</param>
		/// <param name=""dataReader"" > Data reader.</param>
		/// <typeparam name=""T""> The 1st type parameter.</typeparam>
		/// <exception cref=""ArgumentNullException""> Thrown</exception>
		static T Apply<T> (object $value$, object dataReader)
		{
			if ($value$ == null) {
				throw new ArgumentNullException (nameof ($value$));
			}
		}
	}
}");
		}
	}
}