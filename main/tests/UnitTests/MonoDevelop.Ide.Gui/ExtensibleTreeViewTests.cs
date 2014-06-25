//
// ExtensibleTreeViewTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide.Gui.Components;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Gui
{
	[TestFixture]
	public class ExtensibleTreeViewTests: UnitTests.TestBase
	{
		MyProjectBuilder myProjectBuilder;
		MyProjectBuilderExtension myProjectBuilderExtension;
		MyFileBuilder myFileBuilder;
		ExtensibleTreeView tree;

		[SetUp]
		public virtual void TestSetup ()
		{
			tree = new ExtensibleTreeView ();
			myProjectBuilder = new MyProjectBuilder ();
			myProjectBuilderExtension = new MyProjectBuilderExtension ();
			myFileBuilder = new MyFileBuilder ();
			tree.Initialize (new NodeBuilder[] { myProjectBuilder, myProjectBuilderExtension, myFileBuilder }, new TreePadOption[0]);
		}

		[TearDown]
		public virtual void TestTearDown ()
		{
			if (tree != null)
				tree.Dispose ();
		}

		[Test]
		public void InitializeAndDispose ()
		{
			Assert.AreEqual (1, myProjectBuilder.initialized);
			Assert.AreEqual (1, myProjectBuilderExtension.initialized);
			Assert.AreEqual (1, myFileBuilder.initialized);

			Assert.AreEqual (0, myProjectBuilder.disposed);
			Assert.AreEqual (0, myProjectBuilderExtension.disposed);
			Assert.AreEqual (0, myFileBuilder.disposed);

			tree.Dispose ();

			Assert.AreEqual (1, myProjectBuilder.initialized);
			Assert.AreEqual (1, myProjectBuilderExtension.initialized);
			Assert.AreEqual (1, myFileBuilder.initialized);

			Assert.AreEqual (1, myProjectBuilder.disposed);
			Assert.AreEqual (1, myProjectBuilderExtension.disposed);
			Assert.AreEqual (1, myFileBuilder.disposed);

			tree = null;
		}

		[Test]
		public void AddRoots ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);

			var b = tree.AddChild (p1);
			Assert.AreSame (p1, b.DataItem);
			Assert.IsTrue (b.HasChildren ());
			Assert.IsTrue (b.MoveToFirstChild ());
			Assert.AreSame (f1, b.DataItem);
			Assert.IsTrue (b.MoveNext ());
			Assert.AreSame (f2, b.DataItem);

			var p2 = new MyProject { Name = "project2" };
			b = tree.AddChild (p2);
			Assert.AreSame (p2, b.DataItem);
		}
	
		[Test]
		public void GetNodeAtObject ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);
			var p2 = new MyProject { Name = "project2" };

			tree.AddChild (p1).MoveToFirstChild ();
			tree.AddChild (p2).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (p2);
			Assert.NotNull (nav);
			Assert.AreEqual (p2, nav.DataItem);
			nav.MoveToFirstChild ();

			nav = tree.GetNodeAtObject (p1);
			Assert.NotNull (nav);
			Assert.AreEqual (p1, nav.DataItem);
			nav.MoveToFirstChild ();

			nav = tree.GetNodeAtObject (f1);
			Assert.NotNull (nav);
			Assert.AreEqual (f1, nav.DataItem);

			nav = tree.GetNodeAtObject (f2);
			Assert.NotNull (nav);
			Assert.AreEqual (f2, nav.DataItem);

			nav = tree.GetNodeAtObject (new MyProject ());
			Assert.IsNull (nav);
		}

		[Test]
		public void BasicNavigation ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);
			var p2 = new MyProject { Name = "project2" };

			tree.AddChild (p1).MoveToFirstChild ();
			tree.AddChild (p2).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (p1);
			Assert.AreEqual (p1, nav.DataItem);

			Assert.IsFalse (nav.MoveToParent ());

			Assert.IsTrue (nav.HasChildren ());
			Assert.AreEqual (p1, nav.DataItem);

			Assert.IsTrue (nav.MoveToFirstChild ());
			Assert.IsFalse (nav.HasChildren ());

			Assert.IsTrue (nav.MoveNext ());
			Assert.AreEqual (f2, nav.DataItem);

			Assert.IsFalse (nav.MoveToFirstChild ());
			Assert.IsFalse (nav.HasChildren ());
			Assert.IsFalse (nav.MoveNext ());

			Assert.IsTrue (nav.MoveToParent ());
			Assert.AreEqual (p1, nav.DataItem);

			Assert.IsTrue (nav.MoveNext ());
			Assert.AreEqual (p2, nav.DataItem);

			Assert.IsFalse (nav.MoveNext ());
			Assert.IsFalse (nav.MoveToParent ());
		}


		[Test]
		public void MoveToChild ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);

			var p2 = new MyProject { Name = "file1" };
			var f3 = new MyFile { Name = "file3" };
			p1.Children.Add (p2);
			p2.Children.Add (f3);

			tree.AddChild (p1).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (p1);
			Assert.IsTrue (nav.HasChild ("file2"));
			Assert.AreSame (p1, nav.DataItem);
			Assert.IsTrue (nav.MoveToChild ("file2"));
			Assert.AreSame (f2, nav.DataItem);

			nav = tree.GetNodeAtObject (p1);
			Assert.IsTrue (nav.HasChild ("file1", typeof(MyFile)));
			Assert.AreSame (p1, nav.DataItem);
			Assert.IsTrue (nav.MoveToChild ("file1", typeof(MyFile)));
			Assert.AreSame (f1, nav.DataItem);

			nav = tree.GetNodeAtObject (p1);
			Assert.IsTrue (nav.HasChild ("file1", typeof(MyProject)));
			Assert.AreSame (p1, nav.DataItem);
			Assert.IsTrue (nav.MoveToChild ("file1", typeof(MyProject)));
			Assert.AreSame (p2, nav.DataItem);

			nav = tree.GetNodeAtObject (p1);
			Assert.IsFalse (nav.HasChild ("file3"));
			Assert.AreSame (p1, nav.DataItem);
			Assert.IsFalse (nav.MoveToChild ("file3"));
			Assert.AreSame (p1, nav.DataItem);

			nav = tree.GetNodeAtObject (p1);
			Assert.IsFalse (nav.HasChild ("file1", typeof(string)));
			Assert.AreSame (p1, nav.DataItem);
			Assert.IsFalse (nav.MoveToChild ("file1", typeof(string)));
			Assert.AreSame (p1, nav.DataItem);
		}

		[Test]
		public void MoveToNextObject ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);
			var p2 = new MyProject { Name = "project2" };
			p2.Children.Add (f1);
			var hs = new HashSet<object> { p1, p2 };

			tree.AddChild (p1).MoveToFirstChild ();
			tree.AddChild (p2).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (f1);
			Assert.IsTrue (hs.Remove (nav.GetParentDataItem (typeof(MyProject), false)));

			Assert.IsTrue (nav.MoveToNextObject ());
			Assert.IsTrue (hs.Remove (nav.GetParentDataItem (typeof(MyProject), false)));
			var last = nav.CurrentPosition;

			Assert.IsFalse (nav.MoveToNextObject ());

			// Calling moveToNextObject directly when in the last object

			nav = tree.GetNodeAtPosition (last);
			Assert.IsFalse (nav.MoveToNextObject ());

			// When there is no next object

			nav = tree.GetNodeAtObject (f2);
			Assert.IsFalse (nav.MoveToNextObject ());
		}

		[Test]
		public void MoveToRoot ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);
			var p2 = new MyProject { Name = "project2" };
			tree.AddChild (p1).MoveToFirstChild ();
			tree.AddChild (p2).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (f2);
			Assert.AreSame (f2, nav.DataItem);

			nav.MoveToRoot ();
			Assert.AreSame (p1, nav.DataItem);
		}

		[Test]
		public void CloneNavigator ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);

			tree.AddChild (p1).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (p1);
			Assert.AreSame (p1, nav.DataItem);

			var nav2 = nav.Clone ();
			Assert.AreSame (p1, nav.DataItem);
			Assert.AreSame (p1, nav2.DataItem);

			Assert.IsTrue (nav2.MoveToFirstChild ());
			Assert.AreSame (p1, nav.DataItem);
			Assert.AreSame (f1, nav2.DataItem);

			var nav3 = nav2.Clone ();
			Assert.AreSame (p1, nav.DataItem);
			Assert.AreSame (f1, nav2.DataItem);
			Assert.AreSame (f1, nav3.DataItem);

			Assert.IsTrue (nav3.MoveNext ());
			Assert.AreSame (p1, nav.DataItem);
			Assert.AreSame (f1, nav2.DataItem);
			Assert.AreSame (f2, nav3.DataItem);

			Assert.IsTrue (nav.MoveToFirstChild ());
			Assert.AreSame (f1, nav.DataItem);
			Assert.AreSame (f1, nav2.DataItem);
			Assert.AreSame (f2, nav3.DataItem);
		}

		[Test]
		public void CurrentPosition ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);

			tree.AddChild (p1).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (p1);
			Assert.AreSame (p1, nav.DataItem);
			var cp1 = nav.CurrentPosition;

			Assert.IsTrue (nav.MoveToFirstChild ());
			Assert.AreSame (f1, nav.DataItem);
			var cp2 = nav.CurrentPosition;

			Assert.IsTrue (nav.MoveNext ());
			Assert.AreSame (f2, nav.DataItem);
			var cp3 = nav.CurrentPosition;

			nav = tree.GetNodeAtPosition (cp1);
			Assert.AreEqual (cp1, nav.CurrentPosition);
			Assert.AreSame (p1, nav.DataItem);

			nav = tree.GetNodeAtPosition (cp2);
			Assert.AreEqual (cp2, nav.CurrentPosition);
			Assert.AreSame (f1, nav.DataItem);

			nav = tree.GetNodeAtPosition (cp3);
			Assert.AreEqual (cp3, nav.CurrentPosition);
			Assert.AreSame (f2, nav.DataItem);
		}

		[Test]
		public void GetParentDataItem ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);

			var p2 = new MyProject { Name = "project1" };
			var f21 = new MyFile { Name = "file1" };
			var f22 = new MyFile { Name = "file2" };
			p2.Children.Add (f21);
			p2.Children.Add (f22);

			p1.Children.Add (p2);

			tree.AddChild (p1).MoveToFirstChild ();

			var nav = tree.GetNodeAtObject (p2);
			nav.Expanded = true;
			Assert.AreSame (p2, nav.DataItem);

			Assert.AreSame (p2, nav.GetParentDataItem (typeof(MyProject), true));
			Assert.AreSame (p1, nav.GetParentDataItem (typeof(MyProject), false));

			nav = tree.GetNodeAtObject (f22);
			Assert.AreSame (p2, nav.GetParentDataItem (typeof(MyProject), true));
			Assert.AreSame (p2, nav.GetParentDataItem (typeof(MyProject), false));
			Assert.AreSame (f22, nav.GetParentDataItem (typeof(MyFile), true));
			Assert.IsNull (nav.GetParentDataItem (typeof(MyFile), false));
		}

		[Test]
		public void NodeAddedRemovedEvents ()
		{
			var p1 = new MyProject { Name = "project1" };
			var f1 = new MyFile { Name = "file1" };
			var f2 = new MyFile { Name = "file2" };
			p1.Children.Add (f1);
			p1.Children.Add (f2);
			var p2 = new MyProject { Name = "project2" };
			var f21 = new MyFile { Name = "file21" };
			p2.Children.Add (f1);
			p2.Children.Add (f21);

			tree.AddChild (p1).MoveToFirstChild ();
			tree.AddChild (p2).MoveToFirstChild ();

			// NOTE: file1 is added twice to the tree, but the OnNodeAdded overload is called only once.

			Assert.AreEqual (2, myProjectBuilder.nodeAdded);
			Assert.AreEqual (2, myProjectBuilderExtension.nodeAdded);
			Assert.AreEqual (3, myFileBuilder.nodeAdded);
			Assert.AreEqual (0, myProjectBuilder.nodeRemoved);
			Assert.AreEqual (0, myProjectBuilderExtension.nodeRemoved);
			Assert.AreEqual (0, myFileBuilder.nodeRemoved);

			// Updating the node doesn't update the children

			ResetCounters ();
			var b = tree.BuilderContext.GetTreeBuilder (p1);
			b.Update ();

			Assert.AreEqual (0, myProjectBuilder.nodeAdded);
			Assert.AreEqual (0, myProjectBuilderExtension.nodeAdded);
			Assert.AreEqual (0, myFileBuilder.nodeAdded);
			Assert.AreEqual (0, myProjectBuilder.nodeRemoved);
			Assert.AreEqual (0, myProjectBuilderExtension.nodeRemoved);
			Assert.AreEqual (0, myFileBuilder.nodeRemoved);

			// Update all children of a project

			b.UpdateChildren ();
			Assert.AreEqual (0, myProjectBuilder.nodeAdded);
			Assert.AreEqual (0, myProjectBuilderExtension.nodeAdded);
			Assert.AreEqual (1, myFileBuilder.nodeAdded);
			Assert.AreEqual (0, myProjectBuilder.nodeRemoved);
			Assert.AreEqual (0, myProjectBuilderExtension.nodeRemoved);
			Assert.AreEqual (1, myFileBuilder.nodeRemoved);

			// Remove a project

			ResetCounters ();
			b.MoveToObject (p2);
			b.Remove ();
			Assert.AreEqual (1, myProjectBuilder.nodeRemoved);
			Assert.AreEqual (1, myProjectBuilderExtension.nodeRemoved);
			Assert.AreEqual (1, myFileBuilder.nodeRemoved);

			// Add a project

			ResetCounters ();
			tree.AddChild (p2).MoveToFirstChild ();
			Assert.AreEqual (1, myProjectBuilder.nodeAdded);
			Assert.AreEqual (1, myProjectBuilderExtension.nodeAdded);
			Assert.AreEqual (1, myFileBuilder.nodeAdded);

			// Destroy the tree

			ResetCounters ();
			tree.Dispose ();

			Assert.AreEqual (2, myProjectBuilder.nodeRemoved);
			Assert.AreEqual (2, myProjectBuilderExtension.nodeRemoved);
			Assert.AreEqual (3, myFileBuilder.nodeRemoved);

			tree = null;
		}

		void ResetCounters ()
		{
			myProjectBuilder.nodeAdded = myProjectBuilderExtension.nodeAdded = myFileBuilder.nodeAdded = 0;
			myProjectBuilder.nodeRemoved = myProjectBuilderExtension.nodeRemoved = myFileBuilder.nodeRemoved = 0;
		}
	}

	class MyProject
	{
		public string Name;

		public List<object> Children = new List<object> ();

		public override string ToString ()
		{
			return string.Format ("[MyProject] " + Name);
		}
	}

	class MyFile
	{
		public string Name;

		public override string ToString ()
		{
			return string.Format ("[MyFile] " + Name);
		}
	}

	abstract class TestTypeNodeBuilder: TypeNodeBuilder
	{
		public int initialized;
		public int disposed;
		public int nodeAdded;
		public int nodeRemoved;

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			base.BuildNode (treeBuilder, dataObject, nodeInfo);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			base.BuildChildNodes (treeBuilder, dataObject);
		}

		protected override void Initialize ()
		{
			base.Initialize ();
			initialized++;
		}

		public override void Dispose ()
		{
			base.Dispose ();
			disposed++;
		}

		public override void OnNodeAdded (object dataObject)
		{
			nodeAdded++;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			nodeRemoved++;
		}
	}

	abstract class TestNodeBuilderExtension: NodeBuilderExtension
	{
		public int initialized;
		public int disposed;
		public int nodeAdded;
		public int nodeRemoved;

		protected override void Initialize ()
		{
			base.Initialize ();
			initialized++;
		}

		public override void Dispose ()
		{
			base.Dispose ();
			disposed++;
		}

		public override void OnNodeAdded (object dataObject)
		{
			nodeAdded++;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			nodeRemoved++;
		}
	}

	class MyProjectBuilder: TestTypeNodeBuilder
	{
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((MyProject)dataObject).Name;
		}

		public override Type NodeDataType {
			get {
				return typeof(MyProject);
			}
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var p = (MyProject)dataObject;
			foreach (var c in p.Children)
				treeBuilder.AddChild (c);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var p = (MyProject)dataObject;
			return p.Children.Count > 0;
		}
	}

	class MyProjectBuilderExtension: TestNodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(MyProject).IsAssignableFrom (dataType);
		}
	}

	class MyFileBuilder: TestTypeNodeBuilder
	{
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((MyFile)dataObject).Name;
		}

		public override Type NodeDataType {
			get {
				return typeof(MyFile);
			}
		}
	}
}

