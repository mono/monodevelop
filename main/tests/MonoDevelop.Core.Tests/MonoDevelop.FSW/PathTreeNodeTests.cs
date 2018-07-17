//
// PathTreeNodeTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using MonoDevelop.Core;
using NUnit.Framework;

namespace MonoDevelop.FSW
{
	[TestFixture]
	public class PathTreeNodeTests
	{
		string [] seps = {
			"",
			Path.DirectorySeparatorChar.ToString(),
		};

		static readonly string prefix = Platform.IsWindows ? "C:\\" : "/";
		static string MakePath (params string [] segments) => Path.Combine (prefix, Path.Combine (segments));

		[TestCaseSource (nameof (seps))]
		public void CreateSubTree (string sep)
		{
			var path = MakePath ("a", "b", "c") + sep;

			var (first, leaf) = PathTreeNode.CreateSubTree (path, 0);

			PathTreeNode a;
			if (Platform.IsWindows) {
				AssertPathTreeSubtree (first, "C:");
				Assert.AreEqual (1, first.ChildrenCount);

				a = first.FirstChild;
				Assert.AreSame (first, a.Parent);
			} else {
				a = first;
			}

			AssertPathTreeSubtree (a, "a");
			Assert.AreEqual (1, a.ChildrenCount);

			var b = a.FirstChild;
			Assert.AreSame (a, b.Parent);
			AssertPathTreeSubtree (b, "b");
			Assert.AreEqual (1, b.ChildrenCount);

			var c = b.FirstChild;
			Assert.AreSame (b, c.Parent);
			AssertPathTreeSubtree (c, "c");
			Assert.AreEqual (0, c.ChildrenCount);
			Assert.AreSame (c, leaf);

			Assert.IsNull (c.FirstChild);

			void AssertPathTreeSubtree (PathTreeNode node, string segment)
			{
				Assert.AreEqual (segment, node.Segment);
				Assert.IsNull (node.Next);
				Assert.AreSame (node.FirstChild, node.LastChild);
			}
		}

		[TestCase (0)]
		[TestCase (1)] // Should not crash
		public void EmptySubTrie (int startIndex)
		{
			var (node, leaf) = PathTreeNode.CreateSubTree (string.Empty, startIndex);
			Assert.IsNull (node);
			Assert.IsNull (leaf);
		}

		[Test]
		public void JustSlash ()
		{
			var (node, leaf) = PathTreeNode.CreateSubTree (Path.DirectorySeparatorChar.ToString (), 0);
			Assert.IsNull (node);
			Assert.IsNull (leaf);
		}
	}
}
