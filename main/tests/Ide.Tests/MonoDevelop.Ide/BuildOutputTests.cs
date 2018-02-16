﻿//
// BuildOutputTests.cs
//
// // Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using MonoDevelop.Ide.BuildOutputView;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MonoDevelop.Ide
{
	public class BuildOutputTests : IdeTestBase
	{
		const string BuildMessage = "Build started";
		const string ErrorMessage = "Error in my test";
		const string ProjectMessage = "ProjectError.csproj";

		[Test]
		public void ProgressMonitor_Instantiation ()
		{
			var bo = new BuildOutput ();
			Assert.AreEqual (bo.GetProgressMonitor (), bo.GetProgressMonitor ());
		}

		[Test]
		public void CustomProject_ToDataSource ()
		{
			var bo = new BuildOutput ();
			var monitor = bo.GetProgressMonitor ();

			monitor.LogObject (new ProjectStartedProgressEvent ());
			monitor.Log.Write ("Custom project built");
			monitor.LogObject (new ProjectFinishedProgressEvent ());

			var nodes = bo.GetRootNodes (true);
			var dataSource = new BuildOutputDataSource (nodes);
			var child = dataSource.GetChild (dataSource.GetChild (null, 0), 0);

			Assert.That (dataSource.GetChildrenCount (null), Is.EqualTo (1));
			Assert.That (child, Is.TypeOf (typeof (BuildOutputNode)));
			Assert.That ((child as BuildOutputNode).Message, Is.EqualTo ("Custom project built"));
		}

		[Test]
		public void BuildOutputNode_Children ()
		{
			var node = new BuildOutputNode ();
			Assert.IsFalse (node.HasChildren);
			Assert.IsTrue (node.Children == null);
			var childNode = new BuildOutputNode ();
			node.AddChild (childNode);
			Assert.IsTrue (node.HasChildren);
			Assert.IsTrue (node.Children != null);
			Assert.AreSame (childNode.Parent, node);
			Assert.AreEqual (1, node.Children.Count);
		}

		[Test]
		public void CustomProject_DataSearch ()
		{
			var bo = new BuildOutput ();
			var monitor = bo.GetProgressMonitor ();

			monitor.LogObject (new ProjectStartedProgressEvent ());
			for (int i = 0; i < 100; i++) {
				monitor.Log.WriteLine ($"Message {i + 1}");
			}
			monitor.Log.WriteLine ("Custom project built");
			monitor.LogObject (new ProjectFinishedProgressEvent ());

			var nodes = bo.GetRootNodes (true);
			var dataSource = new BuildOutputDataSource (nodes);
			var search = new BuildOutputDataSearch (nodes);
			int matches = 0;
			var visited = new HashSet<BuildOutputNode> ();
			for (var match = search.FirstMatch ("Message "); match != null; match = search.NextMatch ()) {
				if (visited.Contains (match)) {
					break;
				}

				visited.Add (match);
				matches++;
			}

			Assert.That (matches, Is.EqualTo (100));
		}

		[Test]
		public void BuildOutputNode_Search ()
		{
			var result = GetTestNodes ();
			var results = new List<BuildOutputNode> ();
			result [0].Search (results, "Error");
			Assert.AreEqual (2, results.Count, "#1");
		}

		[Test]
		public void BuildOutputNode_SearchFirstNode ()
		{
			var result = GetTestNodes ();
			var node = result [0].SearchFirstNode (BuildOutputNodeType.Error);
			Assert.IsNotNull (node);
			node = result [0].SearchFirstNode (BuildOutputNodeType.Error, ErrorMessage);
			Assert.IsNotNull (node);
			node = result [0].SearchFirstNode (BuildOutputNodeType.Error, ErrorMessage + " ");
			Assert.IsNull (node);
		}

		List<BuildOutputNode> GetTestNodes ()
		{
			var result = new List<BuildOutputNode> ();
			var buildNode = new BuildOutputNode () { NodeType = BuildOutputNodeType.Build, Message = BuildMessage };
			result.Add (buildNode);
			var projectNode = new BuildOutputNode () { NodeType = BuildOutputNodeType.Project, Message = ProjectMessage };
			result.Add (projectNode);
			buildNode.AddChild (projectNode);
			var targetNode = new BuildOutputNode () { NodeType = BuildOutputNodeType.Target, Message = "Csc" };
			result.Add (targetNode);
			projectNode.AddChild (targetNode);
			var alertNode = new BuildOutputNode () { NodeType = BuildOutputNodeType.Error, Message = ErrorMessage };
			result.Add (alertNode);
			targetNode.AddChild (alertNode);
			return result;
		}
	}
}
