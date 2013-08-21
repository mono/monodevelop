//
// AvlTreeTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using System.Linq;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	public class AvlTreeTests
	{
		class TestNode : IAvlNode, System.IComparable
		{
			public readonly int val;

			public TestNode (int val)
			{
				this.val = val;
			}

			public void UpdateAugmentedData ()
			{
			}

			#region IComparable implementation

			public int CompareTo (object other)
			{
				return val.CompareTo (((TestNode)other).val);
			}

			#endregion

			public override string ToString ()
			{
				return string.Format ("[TestNode " + val + "]");
			}

			#region IAvlNode implementation

			public IAvlNode Parent {
				get;
				set;
			}

			public IAvlNode Left {
				get;
				set;
			}

			public IAvlNode Right {
				get;
				set;
			}

			public sbyte Balance {
				get;
				set;
			}

			#endregion

		}

		[Test]
		public void TestRemove ()
		{
			var tree = new AvlTree<TestNode> ();
			var t1 = new TestNode (1);
			var t2 = new TestNode (2);
			var t3 = new TestNode (3);

			tree.Add (t2);
			tree.Add (t1);
			tree.Add (t3);
			Assert.AreEqual (3, tree.Count);

			Assert.IsTrue (tree.Remove (t2));

			Assert.AreEqual (2, tree.Count);
			Assert.IsTrue (tree.Contains (t1));
			Assert.IsFalse (tree.Contains (t2));
			Assert.IsTrue (tree.Contains (t3));
		}

		[Test]
		public void TestAddInOrder ()
		{
			var tree = new AvlTree<TestNode> ();
			tree.Add (new TestNode (1));
			tree.Add (new TestNode (2));
			tree.Add (new TestNode (3));
			Assert.AreEqual (3, tree.Count);
			Assert.AreEqual ("1,2,3,", GetContent (tree));
		}

		[Test]
		public void TestAddReverseOrder ()
		{
			var tree = new AvlTree<TestNode> ();
			tree.Add (new TestNode (3));
			tree.Add (new TestNode (2));
			tree.Add (new TestNode (1));
			Assert.AreEqual (3, tree.Count);
			Assert.AreEqual ("1,2,3,", GetContent (tree));
		}

		[Test]
		public void TestAddOutOfOrder ()
		{
			var tree = new AvlTree<TestNode> ();
			tree.Add (new TestNode (3));
			tree.Add (new TestNode (1));
			tree.Add (new TestNode (2));
			Assert.AreEqual (3, tree.Count);
			Assert.AreEqual ("1,2,3,", GetContent (tree));
		}

		static string GetContent (AvlTree<TestNode> tree)
		{
			var sb = new StringBuilder ();
			foreach (var t in tree) {
				sb.Append (t.val + ",");
			}
			return sb.ToString ();
		}

		[Ignore]
		[Test]
		public void TestAddCase2 ()
		{
			var tree = new AvlTree<TestNode> ();
			var t3 = new TestNode (3);
			var t24 = new TestNode (24);
			var t26 = new TestNode (26);

			tree.Add (t3);

			Assert.AreEqual (1, tree.Count);
			tree.Remove (t3);

			tree.Add (new TestNode (37));
			tree.Add (new TestNode (70));
			tree.Add (new TestNode (12));

			Assert.AreEqual (3, tree.Count); 

			tree.Add (new TestNode (90));
			tree.Add (new TestNode (25));
			tree.Add (new TestNode (99));
			tree.Add (new TestNode (91));
			tree.Add (t24); 
			tree.Add (new TestNode (28));
			tree.Add (t26); 

			// Should do a single left rotation on node with key 12
			tree.Remove (t24); 
			Assert.IsTrue (tree.Root.Left == t26, "was:" + tree.Root.Left);
		}

		[Test]
		public void TestTreeRoationAtLeftChildAfterDeletingRoot ()
		{
			var tree = new AvlTree<TestNode> ();
			int[] keys = { 86, 110, 122, 2, 134, 26, 14, 182 };
			int[] expectedKeys = { 2, 14, 26, 86, 122, 134, 182 };

			foreach (var key in keys) {
				tree.Add (new TestNode (key));
			}
			tree.Remove (tree.First (t => t.val == 110));

			var node = tree.Root.AvlGetOuterLeft ();
			foreach (var expected in expectedKeys) {
				Assert.AreEqual (expected, node.val);
				node = node.AvlGetNextNode ();
			}
		}

		[Ignore]
		[Test]
		public void TestDetachNodesAtLeftChildAfterDeletingRoot()
		{
			var tree = new AvlTree<TestNode> ();
			int[] keys = { 110, 122, 2, 134, 86, 14, 26, 182 };
			foreach (var key in keys) {
				tree.Add (new TestNode (key));
			}
			tree.Remove (tree.First (t => t.val == 110));
			Assert.AreEqual (26, ((TestNode)tree.First (t => t.val == 14).Right).val);
		}

		[Ignore]
		[Test]
		public void TestRemoveInRightSubtree()
		{
			var tree = new AvlTree<TestNode> ();
			int[] keys = { 8, 4, 13, 6, 15, 7, 10, 5, 14, 2, 11, 3, 9, 1 };
			foreach (var key in keys) {
				tree.Add (new TestNode (key));
			}
			tree.Remove (tree.First (t => t.val == 13));
			Assert.AreEqual (11, ((TestNode)tree.First (t => t.val == 8).Right).val);
		}

		[Test]
		public void TestRemoveInLeftSubtree()
		{
			var tree = new AvlTree<TestNode> ();
			int[] keys = { 8, 4, 12, 6, 7, 16, 10, 5, 11, 9, 17, 5, 14, 2, 13, 1, 3 };
			foreach (var key in keys) {
				tree.Add (new TestNode (key));
			}

			tree.Remove (tree.First (t => t.val == 16));

			Assert.AreEqual( 8, tree.Root.val );
			Assert.AreEqual( 12, ((TestNode)tree.Root.Right).val );
			Assert.AreEqual( 14, ((TestNode)tree.Root.Right.Right).val );

			Assert.AreEqual (13, ((TestNode)tree.First (t => t.val == 14).Left).val);
		}

		[Test]
		public void TestReverseOrderRemoval ()
		{
			var tree = new AvlTree<TestNode> ();
			TestNode[] nodes = new TestNode[10];
			for (int i = 0; i < 10; i++) {
				tree.Add (nodes [i] = new TestNode (i));
			}
			Assert.AreEqual (10, tree.Count);

			for (int i = 0; i < 10; i++) {
				Assert.IsTrue (tree.Contains (nodes[9 - i]), "case : " + (9 - i));
				tree.Remove (nodes[9 - i]);
				Assert.IsFalse (tree.Contains (nodes[9 - i]), "case : " + (9 - i));
			}
			Assert.AreEqual (0, tree.Count);
		} 

		[Test]
		public void TestInOrderRemoval ()
		{
			var tree = new AvlTree<TestNode> ();
			TestNode[] nodes = new TestNode[10];
			for (int i = 0; i < 10; i++) {
				tree.Add (nodes [i] = new TestNode (i));
			}
			Assert.AreEqual (10, tree.Count);

			for (int i = 0; i < 10; i++) {
				Assert.IsTrue (tree.Contains (nodes[i]), "case : " + i);
				tree.Remove (nodes[i]);
				Assert.IsFalse (tree.Contains (nodes[i]), "case : " + i);
			}
			Assert.AreEqual (0, tree.Count);
		} 
	}
}

