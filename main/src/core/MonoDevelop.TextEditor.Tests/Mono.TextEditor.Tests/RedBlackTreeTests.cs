// 
// RedBlackTreeTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor.Utils;
using NUnit.Framework;

namespace Mono.TextEditor.Tests
{
	[TestFixture()]
	public class RedBlackTreeTests
	{
		class TestNode : IRedBlackTreeNode, IComparable
		{
			int val;
			
			public TestNode (int val)
			{
				this.val = val;
			}

			#region IRedBlackTreeNode implementation
			public void UpdateAugmentedData ()
			{
			}

			public IRedBlackTreeNode Parent {
				get;
				set;
			}

			public IRedBlackTreeNode Left {
				get;
				set;
			}

			public IRedBlackTreeNode Right {
				get;
				set;
			}

			public RedBlackColor Color {
				get;
				set;
			}
			#endregion

			#region IComparable implementation
			public int CompareTo (object obj)
			{
				return val.CompareTo (((TestNode)obj).val);
			}
			#endregion
		}
		
		[Test()]
		public void TestRemoveBug ()
		{
			var tree = new RedBlackTree<TestNode> ();
			TestNode t1 = new TestNode (1);
			TestNode t2 = new TestNode (2);
			TestNode t3 = new TestNode (3);
			
			tree.Add (t1);
			tree.InsertRight (t1, t2);
			tree.InsertLeft (t1, t3);
			tree.Remove (t1);
			Assert.AreEqual (2, tree.Count);
		} 
		
		[Test()]
		public void TestAddBug ()
		{
			var tree = new RedBlackTree<TestNode> ();
			TestNode t1 = new TestNode (1);
			TestNode t2 = new TestNode (2);
			TestNode t3 = new TestNode (3);
			
			tree.Add (t1);
			tree.Add (t2);
			tree.Add (t3);
			Assert.AreEqual (3, tree.Count);
		} 
	}
}

