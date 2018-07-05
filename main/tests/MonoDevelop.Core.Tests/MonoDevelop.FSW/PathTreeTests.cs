//
// PathTreeTests.cs
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
using System.Linq;
using MonoDevelop.Core;
using NUnit.Framework;

namespace MonoDevelop.FSW
{
	[TestFixture]
	public class PathTreeTests
	{
		static object id = new object ();

		[Test]
		public void CreateEmptyTree ()
		{
			var tree = new PathTree ();
			var node = tree.rootNode;

			if (!Platform.IsWindows) {
				node = node.FirstChild;
				Assert.AreEqual ("/", node.FullPath);
			}

			Assert.IsNull (node.FirstChild);
			Assert.IsNull (node.LastChild);
			Assert.IsNull (node.Next);
		}


		static readonly string prefix = Platform.IsWindows ? "C:\\" : "/";
		static string MakePath (params string [] segments) => Path.Combine (prefix, Path.Combine (segments));

		static PathTree CreateTree ()
		{
			var tree = new PathTree ();

			// a
			// + b
			//   + c
			//   + d
			//   + e
			//   + f
			//     + f1
			//     + f2
			//   + g
			//     + g1
			//     + g2
			tree.AddNode (MakePath ("a", "b", "g", "g1"), id);
			tree.AddNode (MakePath ("a", "b", "g"), id);
			tree.AddNode (MakePath ("a", "b", "c"), id);
			tree.AddNode (MakePath ("a", "b", "e"), id);
			tree.AddNode (MakePath ("a", "b", "d"), id);
			tree.AddNode (MakePath ("a", "b", "f"), id);
			tree.AddNode (MakePath ("a", "b", "f", "f1"), id);
			tree.AddNode (MakePath ("a", "b", "f", "f2"), id);
			tree.AddNode (MakePath ("a", "b", "g", "g2"), id);

			return tree;
		}

		[Test]
		public void CreateSimpleTree ()
		{
			var tree = CreateTree ();

			PathTreeNode root, pathRoot, a, b, c, d, e, f, f1, f2, g, g1, g2, x, y, z;

			root = tree.rootNode;
			pathRoot = root.FirstChild;
			a = pathRoot.FirstChild;
			b = a.FirstChild;
			c = b.FirstChild;
			d = c.Next;
			e = d.Next;
			f = e.Next;
			f1 = f.FirstChild;
			f2 = f1.Next;
			g = f.Next;
			g1 = g.FirstChild;
			g2 = g1.Next;

			Assert.AreEqual (1, root.ChildrenCount);
			Assert.AreSame (pathRoot, root.FirstChild);

			// rootNode -> a
			Assert.AreEqual (nameof (a), a.Segment);
			Assert.IsNull (a.Next);
			Assert.AreSame (a, pathRoot.LastChild);
			Assert.AreEqual (1, pathRoot.ChildrenCount);
			Assert.IsNull (a.Previous);
			Assert.AreSame (pathRoot, a.Parent);

			// a -> b
			Assert.AreEqual (nameof (b), b.Segment);
			Assert.AreSame (a.LastChild, b);
			Assert.IsNull (b.Next);
			Assert.AreEqual (1, a.ChildrenCount);
			Assert.AreSame (a, b.Parent);

			// b -> c, d, e, f, g
			Assert.AreEqual (nameof (c), c.Segment);
			Assert.AreEqual (nameof (d), d.Segment);
			Assert.AreEqual (nameof (e), e.Segment);
			Assert.AreEqual (nameof (f), f.Segment);
			Assert.AreEqual (nameof (g), g.Segment);
			Assert.AreSame (b, c.Parent);
			Assert.AreSame (b, d.Parent);
			Assert.AreSame (b, e.Parent);
			Assert.AreSame (b, f.Parent);
			Assert.AreSame (b, g.Parent);
			Assert.AreEqual (5, b.ChildrenCount);

			Assert.AreSame (b.LastChild, g);

			// c, d, e
			Assert.IsNull (c.FirstChild);
			Assert.AreEqual (0, c.ChildrenCount);
			Assert.IsNull (d.FirstChild);
			Assert.AreEqual (0, d.ChildrenCount);
			Assert.IsNull (e.FirstChild);
			Assert.AreEqual (0, e.ChildrenCount);
			Assert.IsNull (g.Next);

			// f -> f1, f2
			Assert.AreSame (f2, f.LastChild);
			Assert.AreEqual (nameof (f1), f1.Segment);
			Assert.AreEqual (nameof (f2), f2.Segment);
			Assert.AreEqual (2, f.ChildrenCount);
			Assert.AreEqual (0, f1.ChildrenCount);
			Assert.AreEqual (0, f2.ChildrenCount);
			Assert.AreSame (f, f1.Parent);
			Assert.AreSame (f, f2.Parent);

			Assert.IsNull (f1.Previous);
			Assert.AreSame (f1, f2.Previous);
			Assert.AreSame (f2, f1.Next);
			Assert.IsNull (f2.Next);

			// g -> g1, g2
			Assert.AreEqual (nameof (g1), g1.Segment);
			Assert.AreEqual (nameof (g2), g2.Segment);
			Assert.AreSame (g2, g.LastChild);
			Assert.AreEqual (2, g.ChildrenCount);
			Assert.AreEqual (0, g1.ChildrenCount);
			Assert.AreEqual (0, g2.ChildrenCount);

			Assert.AreSame (g, g1.Parent);
			Assert.AreSame (g, g2.Parent);

			Assert.IsNull (g1.Previous);
			Assert.AreSame (g1, g2.Previous);
			Assert.AreSame (g2, g1.Next);
			Assert.IsNull (g2.Next);
			// a
			// ...
			// z
			// + y
			//   + x

			tree.AddNode (MakePath ("z", "y", "x"), id);

			z = a.Next;
			y = z.FirstChild;
			x = y.FirstChild;

			// root -> z
			Assert.AreEqual (nameof (z), z.Segment);
			Assert.AreSame (z, pathRoot.LastChild);
			Assert.AreSame (a.Next, z);
			Assert.AreEqual (2, pathRoot.ChildrenCount);
			Assert.IsNull (z.Next);

			// z -> y
			Assert.AreEqual (nameof (z), z.Segment);
			Assert.AreSame (y, z.LastChild);
			Assert.AreEqual (1, z.ChildrenCount);
			Assert.IsNull (y.Next);

			// y -> x
			Assert.AreEqual (nameof (x), x.Segment);
			Assert.AreEqual (1, y.ChildrenCount);
			Assert.IsNull (x.FirstChild);
			Assert.IsNull (x.LastChild);
			Assert.IsNull (x.Next);
		}

		[Test]
		public void AssertSameNodeIsReturned ()
		{
			var tree = new PathTree ();

			var b = tree.AddNode (MakePath ("a", "b"), id);

			var firstA = tree.FindNode (MakePath ("a"));
			var newA = tree.AddNode (MakePath ("a"), id);

			Assert.AreSame (firstA, newA);
			Assert.AreSame (b, firstA.FirstChild);
			Assert.AreSame (b, firstA.LastChild);
		}

		[Test]
		public void AssertNodeRemoved ()
		{
			var tree = CreateTree ();

			var b = tree.FindNode (MakePath ("a", "b"));
			Assert.AreEqual (nameof (b), b.Segment);

			// b -> c
			var c = b.FirstChild;
			Assert.AreEqual (nameof (c), c.Segment);

			// Remove first
			var c2 = tree.RemoveNode (MakePath ("a", "b", "c"), id);
			Assert.AreSame (c, c2);

			Assert.IsNull (tree.FindNode (MakePath ("a", "b", "c")));

			// b -> d
			var d = b.FirstChild;
			Assert.AreNotSame (c, d);
			Assert.AreEqual (nameof (d), d.Segment);

			// b -> g
			var g = b.LastChild;
			Assert.AreEqual (nameof (g), g.Segment);

			// Remove g
			var gRemoved = tree.RemoveNode (MakePath ("a", "b", "g"), id);
			Assert.AreSame (g, gRemoved);

			Assert.IsNotNull (tree.FindNode (MakePath ("a", "b", "g")));
			Assert.IsFalse (gRemoved.IsLive);

			var g1 = tree.FindNode (MakePath ("a", "b", "g", "g1"));
			Assert.IsNotNull (g1);
			Assert.IsTrue (g1.IsLive);

			var g2 = tree.FindNode (MakePath ("a", "b", "g", "g2"));
			Assert.IsNotNull (g2);
			Assert.IsTrue (g2.IsLive);

			// Remove g1
			var g1Removed = tree.RemoveNode (MakePath ("a", "b", "g", "g1"), id);
			Assert.AreSame (g1, g1Removed);

			Assert.IsNull (tree.FindNode (MakePath ("a", "b", "g", "g1")));
			Assert.AreSame (g2, g.FirstChild);

			// Remove g2
			var g2Removed = tree.RemoveNode (MakePath ("a", "b", "g", "g2"), id);
			Assert.AreSame (g2, g2Removed);

			Assert.IsNull (tree.FindNode (MakePath ("a", "b", "g", "g2")));
			Assert.IsNull (tree.FindNode (MakePath ("a", "b", "g")));

			// b -> f
			var f = b.LastChild;
			Assert.AreEqual (nameof (f), f.Segment);

			// Remove middle
			var e = tree.FindNode (MakePath ("a", "b", "e"));
			Assert.IsNotNull (e);

			var e2 = tree.RemoveNode (MakePath ("a", "b", "e"), id);
			Assert.AreSame (e, e2);

			Assert.IsNull (tree.FindNode (MakePath ("a", "b", "e")));

			Assert.AreSame (d, b.FirstChild);
			Assert.AreSame (f, b.LastChild);
			Assert.AreSame (f, d.Next);
		}

		[Test]
		public void AssertNodeNotRemovedWithMultipleRegistrations ()
		{
			var tree = CreateTree ();

			var c = tree.FindNode (MakePath ("a", "b", "c"));
			Assert.AreEqual (nameof (c), c.Segment);

			var newId = new object ();

			var c2 = tree.AddNode (MakePath ("a", "b", "c"), newId);
			Assert.AreSame (c, c2);
			Assert.IsNotNull (c2);

			var c3 = tree.RemoveNode (MakePath ("a", "b", "c"), id);
			Assert.AreSame (c2, c3);
			Assert.IsNotNull (c3);

			var c4 = tree.FindNode (MakePath ("a", "b", "c"));
			Assert.AreSame (c3, c4);
			Assert.IsNotNull (c4);

			var c5 = tree.RemoveNode (MakePath ("a", "b", "c"), newId);
			Assert.AreSame (c4, c5);
			Assert.IsNotNull (c5);

			var cRemoved = tree.FindNode (MakePath ("a", "b", "c"));
			Assert.IsNull (cRemoved);
		}

		[Test]
		public void Normalize ()
		{
			var tree = CreateTree ();

			var nodes = tree.Normalize (1).ToArray ();
			Assert.AreEqual (1, nodes.Length);
			Assert.AreEqual ("b", nodes [0].Segment);

			nodes = tree.Normalize (2).ToArray ();
			Assert.AreEqual (1, nodes.Length);
			Assert.AreEqual ("b", nodes [0].Segment);

			nodes = tree.Normalize (3).ToArray ();
			Assert.AreEqual (1, nodes.Length);
			Assert.AreEqual ("b", nodes [0].Segment);

			nodes = tree.Normalize (4).ToArray ();
			Assert.AreEqual (1, nodes.Length);
			Assert.AreEqual ("b", nodes [0].Segment);

			// b has 5 children
			nodes = tree.Normalize (5).ToArray ();
			Assert.AreEqual (5, nodes.Length);
			Assert.AreEqual ("c", nodes [0].Segment);
			Assert.AreEqual ("d", nodes [1].Segment);
			Assert.AreEqual ("e", nodes [2].Segment);
			Assert.AreEqual ("f", nodes [3].Segment);
			Assert.AreEqual ("g", nodes [4].Segment);

			// f has 2 children, but it is live
			nodes = tree.Normalize (6).ToArray ();
			Assert.AreEqual (5, nodes.Length);
			Assert.AreEqual ("c", nodes [0].Segment);
			Assert.AreEqual ("d", nodes [1].Segment);
			Assert.AreEqual ("e", nodes [2].Segment);
			Assert.AreEqual ("f", nodes [3].Segment);
			Assert.AreEqual ("g", nodes [4].Segment);

			// remove f's registration
			var node = tree.FindNode (MakePath ("a", "b", "f"));
			node.UnregisterId (id);

			// f has 2 children which should be unrolled
			nodes = tree.Normalize (6).ToArray ();
			Assert.AreEqual (6, nodes.Length);
			Assert.AreEqual ("c", nodes [0].Segment);
			Assert.AreEqual ("d", nodes [1].Segment);
			Assert.AreEqual ("e", nodes [2].Segment);
			Assert.AreEqual ("g", nodes [3].Segment);
			Assert.AreEqual ("f1", nodes [4].Segment);
			Assert.AreEqual ("f2", nodes [5].Segment);

			// g has 2 children, but it is live
			nodes = tree.Normalize (7).ToArray ();
			Assert.AreEqual (6, nodes.Length);
			Assert.AreEqual ("c", nodes [0].Segment);
			Assert.AreEqual ("d", nodes [1].Segment);
			Assert.AreEqual ("e", nodes [2].Segment);
			Assert.AreEqual ("g", nodes [3].Segment);
			Assert.AreEqual ("f1", nodes [4].Segment);
			Assert.AreEqual ("f2", nodes [5].Segment);

			// remove f's registration
			node = tree.FindNode (MakePath ("a", "b", "g"));
			node.UnregisterId (id);

			nodes = tree.Normalize (7).ToArray ();
			Assert.AreEqual (7, nodes.Length);
			Assert.AreEqual ("c", nodes [0].Segment);
			Assert.AreEqual ("d", nodes [1].Segment);
			Assert.AreEqual ("e", nodes [2].Segment);
			Assert.AreEqual ("f1", nodes [3].Segment);
			Assert.AreEqual ("f2", nodes [4].Segment);
			Assert.AreEqual ("g1", nodes [5].Segment);
			Assert.AreEqual ("g2", nodes [6].Segment);

			node = tree.FindNode (MakePath ("a"));
			node.RegisterId (id);

			nodes = tree.Normalize (1).ToArray ();
			Assert.AreEqual (1, nodes.Length);
			Assert.AreEqual ("a", nodes [0].Segment);

			nodes = tree.Normalize (7).ToArray ();
			Assert.AreEqual (1, nodes.Length);
			Assert.AreEqual ("a", nodes [0].Segment);
		}

		[Test]
		public void CreateTreeAndDestructItNodeByNode ()
		{
			var tree = new PathTree ();
			var id1 = new object ();
			var id2 = new object ();

			var a = tree.AddNode (MakePath ("a"), id1);
			var b = tree.AddNode (MakePath ("a", "b"), id1);
			var c = tree.AddNode (MakePath ("a", "b", "c"), id1);

			var b2 = tree.AddNode (MakePath ("a", "b"), id2);
			Assert.AreSame (b, b2);
			Assert.AreSame (b.FirstChild, c);

			var b3 = tree.RemoveNode (MakePath ("a", "b"), id1);

			Assert.IsNotNull (tree.FindNode (MakePath ("a", "b")));
			Assert.AreSame (c, b3.FirstChild);

			var b4 = tree.RemoveNode (MakePath ("a", "b"), id2);
			Assert.IsNotNull (tree.FindNode (MakePath ("a", "b")));
			Assert.AreSame (c, b3.FirstChild);

			tree.RemoveNode (MakePath ("a", "b", "c"), id1);
			Assert.IsNull (a.FirstChild);
		}

		[Test]
		public void CreateTreeAndRegisterRoot ()
		{
			var tree = new PathTree ();

			tree.AddNode (prefix, id);
			tree.RemoveNode (prefix, id);

			var node = tree.FindNode (prefix);
			if (Platform.IsWindows) {
				Assert.IsNull (node);
			} else {
				Assert.IsNotNull (node);
				Assert.AreEqual (false, node.IsLive);
			}
		}

		[Test]
		public void TestRemovalOfNodeAddedToTheBeginning ()
		{
			var tree = new PathTree ();

			var c = tree.AddNode (MakePath ("a", "c"), id);

			var b = tree.AddNode (MakePath ("a", "b"), id);
			var a = tree.AddNode (MakePath ("a", "a"), id);

			Assert.IsNotNull (tree.FindNode (MakePath ("a", "a")));
			Assert.IsNotNull (tree.FindNode (MakePath ("a", "b")));
			Assert.IsNotNull (tree.FindNode (MakePath ("a", "c")));

			tree.RemoveNode (MakePath ("a", "b"), id);

			Assert.AreSame (c, a.Next);
			Assert.AreSame (a, c.Previous);

			tree.RemoveNode (MakePath ("a", "c"), id);

			Assert.IsNull (a.Next);
			tree.RemoveNode (MakePath ("a", "a"), id); 

			Assert.IsNull (tree.FindNode (MakePath ("a", "a")));
			Assert.IsNull (tree.FindNode (MakePath ("a", "b")));
			Assert.IsNull (tree.FindNode (MakePath ("a", "c")));
		}
	}
}
