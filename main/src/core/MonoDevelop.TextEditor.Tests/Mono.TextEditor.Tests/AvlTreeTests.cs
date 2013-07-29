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

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	public class AvlTreeTests
	{
		class TestNode : IAvlNode, System.IComparable
		{
			int val;

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

			IAvlNode IAvlNode.Parent {
				get;
				set;
			}

			IAvlNode IAvlNode.Left {
				get;
				set;
			}

			IAvlNode IAvlNode.Right {
				get;
				set;
			}

			int IAvlNode.Balance {
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
		public void TestRemoveCase2 ()
		{
			var tree = new AvlTree<TestNode> ();
			var t1 = new TestNode (1);
			var t2 = new TestNode (2);
			var t3 = new TestNode (3);

			tree.Add (t2);
			tree.Add (t1);
			tree.Add (t3);
			Assert.AreEqual (3, tree.Count);

			Assert.IsTrue (tree.Remove (t1));

			Assert.AreEqual (2, tree.Count);
			Assert.IsFalse (tree.Contains (t1));
			Assert.IsTrue (tree.Contains (t2));
			Assert.IsTrue (tree.Contains (t3));
		}

		[Test]
		public void TestRemoveCase3 ()
		{
			var tree = new AvlTree<TestNode> ();
			var t1 = new TestNode (1);
			var t2 = new TestNode (2);
			var t3 = new TestNode (3);

			tree.Add (t2);
			tree.Add (t1);
			tree.Add (t3);
			Assert.AreEqual (3, tree.Count);

			Assert.IsTrue (tree.Remove (t3));

			Assert.AreEqual (2, tree.Count);
			Assert.IsTrue (tree.Contains (t1));
			Assert.IsTrue (tree.Contains (t2));
			Assert.IsFalse (tree.Contains (t3));
		} 

		[Test]
		public void TestAdd ()
		{
			var tree = new AvlTree<TestNode> ();
			var t1 = new TestNode (1);
			var t2 = new TestNode (2);
			var t3 = new TestNode (3);

			tree.Add (t1);
			tree.Add (t2);
			tree.Add (t3);
			Assert.AreEqual (3, tree.Count);
			Assert.IsTrue (tree.Contains (t1));
			Assert.IsTrue (tree.Contains (t2));
			Assert.IsTrue (tree.Contains (t3));
		} 
	}
}

