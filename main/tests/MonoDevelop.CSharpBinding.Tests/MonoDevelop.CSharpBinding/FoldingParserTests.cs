// 
// FoldingParserTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.Linq;
using NUnit.Framework;

using MonoDevelop.CSharp.Parser;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	public class FoldingParserTests
	{
		static ParsedDocument Test (string code)
		{
			var parser = new CSharpFoldingParser ();
			var sb = new StringBuilder ();
			var openStack = new Stack<DocumentLocation> ();

			int line = 1;
			int col = 1;

			var foldingList = new List<DocumentRegion> ();

			for (int i = 0; i < code.Length; i++) {
				char ch = code [i];
				switch (ch) {
				case '[':
					openStack.Push (new DocumentLocation (line, col));
					break;
				case ']':
					foldingList.Add (new DocumentRegion (openStack.Pop (), new DocumentLocation (line, col)));
					break;
				default:
					if (ch == '\n') {
						line++;
						col = 1;
					} else {
						col++;
					}
					sb.Append (ch);
					break;
				}
			}

			var doc = parser.Parse ("a.cs", sb.ToString ());
			var generatedFoldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (foldingList.Count, generatedFoldings.Count, "Folding count differs.");
			foreach (var generated in generatedFoldings) {
				Assert.IsTrue (foldingList.Any (f => f == generated.Region), "fold not found:" + generated.Region);
			}
			return doc;
		}
		
		[Test]
		public void TestMultiLineComment ()
		{
			Test (@"class Test {

} [/* 

Comment 

*/]

class SomeNew {


}");
		}
		
		[Test]
		public void TestSingleLineComment ()
		{
			Test (@"class Test {
	public static void Main (string args)
	{
		Something (); // Hello World
	}
");
		
		}
		
		[Test]
		public void TestFileHeader ()
		{
			var doc = Test (@"[// 
// EnumMemberDeclaration.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the ""Software""), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.]
using System;");
			foreach (var cmt in doc.GetCommentsAsync().Result) {
				Assert.IsFalse (cmt.Text.StartsWith ("//"));
			}

		}
		
		[Test]
		public void TestRegions ()
		{
			var doc = Test (@"class Test
{
	[#region TestRegion
	void FooBar ()
	{
	}
	#endregion]
}");
			Assert.AreEqual (1, doc.GetFoldingsAsync().Result.Count ());
			Assert.AreEqual ("TestRegion", doc.GetFoldingsAsync().Result.First ().Name);
		}
		
		[Test]
		public void TestTwoRegions ()
		{
			var doc = Test (@"class Test
{
	[#region TestRegion
	void FooBar ()
	{
	}
	#endregion]
	
	[#region TestRegion2
	void FooBar2 ()
	{
	}
	#endregion]
}");
			Assert.AreEqual (2, doc.GetFoldingsAsync().Result.Count ());
			Assert.AreEqual ("TestRegion", doc.GetFoldingsAsync().Result.First ().Name);
			Assert.AreEqual ("TestRegion2", doc.GetFoldingsAsync().Result.Skip (1).First ().Name);
		}
		

		[Test]
		public void TestDocComment ()
		{
			var doc = Test (@"class Test
{
	[/// <summary>
	/// Test
	/// </summary>]
	void FooBar ()
	{
	}
}");
			foreach (var cmt in doc.GetCommentsAsync().Result) {
				Assert.IsFalse (cmt.Text.StartsWith ("///"));
				Assert.IsTrue (cmt.IsDocumentation);
			}
		}

		[Test]
		public void TestNestedSingeLineComment ()
		{
			Test (@"[/* 

// Comment 

*/]");
		}		
		
		
		
		[Test]
		public void TestNestedMultiLineComment ()
		{
			Test (@"[/* 

/*

*/]");
		}

		// Bug 8896 - Strange "jump" behaviour when clicking on a search result, which makes the cursor go to the wrong location
		[Test]
		public void TestBug8896 ()
		{
			var doc = Test (@"class Test
{
	void FooBar () // this should 
	{ // not be 
	} // folded
}");
			Assert.AreEqual (0, doc.GetFoldingsAsync().Result.Count ());
		}

		static ParsedDocument GetDocument (string code)
		{
			var parser = new CSharpFoldingParser ();
			return parser.Parse ("a.cs", code);
		}

		[Test]
		public void TestSingleLineCommentFoldings ()
		{
			var doc = GetDocument (@"//               MyFoldingText
//
//");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("// MyFoldingText ...", foldings [0].Name);
		}

		[Test]
		public void TestSingleLineFoldingsCommentEmptyText ()
		{
			var doc = GetDocument (@"//
//
//");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("//  ...", foldings [0].Name); // y it's 2 spaces - no error
		}

		[Test]
		public void TestBlockCommentFoldings ()
		{
			var doc = GetDocument (@"/*               MyFoldingText
*/");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("/* MyFoldingText ...", foldings [0].Name);
		}

		[Test]
		public void TestBlockCommentEmptyFirstLineFoldings ()
		{
			var doc = GetDocument (@"/* 
*              MyFoldingText
*/");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("/*  ...", foldings [0].Name);
		}

		[Test]
		public void TestBlockCommentEmptyFoldings ()
		{
			var doc = GetDocument (@"/* 
*/");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("/*  ...", foldings [0].Name);
		}


		[Test]
		public void TestDocCommentFoldings ()
		{
			var doc = GetDocument (@"/// FooBar
///
/// Test");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("/// FooBar ...", foldings [0].Name);
		}

		[Test]
		public void TestDocCommentWithSummaryFoldings ()
		{
			var doc = GetDocument (@"
		/// <summary>
		/// FooBar
		/// </summary>");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("/// <summary> FooBar ...", foldings [0].Name);
		}


		[Test]
		public void TestNonSummaryTag ()
		{
			var doc = GetDocument (@"
		/// <remarks>
		/// Test
		/// </remarks>");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("/// <remarks> ...", foldings [0].Name);
		}
		[Test]
		public void TestComplexSummary ()
		{
			var doc = GetDocument (@"
		///  evlxngefgvlsefqvl <see>Test</see>
		/// <summary>
		/// FooBar
		/// </summary>  <see>Test</see>");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("/// <summary> FooBar ...", foldings [0].Name);
		}

		[Test]
		public void TestDocCommentEmpty ()
		{
			var doc = GetDocument (@"
		/// 
		/// 
		/// ");
			var foldings = doc.GetFoldingsAsync ().Result;
			Assert.AreEqual (1, foldings.Count ());
			Assert.AreEqual ("///  ...", foldings [0].Name);
		}

		[Test]
		public void TestIssue4693 ()
		{
			Test (@"
[// fold1
// fold1]

namespace Foo
{
	[// fold2
	// fold2]

	class Test
	{
		public static void Main ()
		{
			// nofold
			// nofold
			// nofold
		}
	}
}
		 ");
		}

		[Test]
		public void TestIssue4868 ()
		{
			Test (@"
[/* fold1
 fold1 */]

namespace Foo
{
	[/* fold2
	 fold2 */]

	class Test
	{
		public static void Main ()
		{
			/* nofold
			   nofold
			   nofold */
		}
	}
}
		 ");

		}
	}
}

