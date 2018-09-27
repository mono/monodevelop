//
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
using System.Threading;

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

			monitor.LogObject (new BuildSessionStartedEvent ());
			monitor.Log.Write ("Custom project built");
			monitor.LogObject (new BuildSessionFinishedEvent ());

			var nodes = bo.GetRootNodes (true);
			var dataSource = new BuildOutputDataSource (nodes);
			var child = dataSource.GetChild (dataSource.GetChild (null, 0), 0);

			Assert.That (dataSource.GetChildrenCount (null), Is.EqualTo (2));
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

		BuildOutput GenerateCustomBuild (int items)
		{
			var processor = new BuildOutputProcessor (null, false);

			for (int i = 0; i < items; i++) {
				var projectMsg = $"Project {i}";
				processor.AddNode (BuildOutputNodeType.Project, projectMsg, projectMsg, true, DateTime.Now);
				for (int j = 0; j < items * 10; j++) {
					var targetMsg = $"Target {i}.{j}";
					processor.AddNode (BuildOutputNodeType.Target, targetMsg, targetMsg, true, DateTime.Now);
					for (int k = 0; k < items * 100; k++) {
						var taskMsg = $"Task {i}.{j}.{k}";
						processor.AddNode (BuildOutputNodeType.Task, taskMsg, taskMsg, true, DateTime.Now);

						var msg = $"Message {i}.{j}.{k}";
						processor.AddNode (BuildOutputNodeType.Message, msg, msg, false, DateTime.Now);

						processor.EndCurrentNode (taskMsg, DateTime.Now);
					}
					processor.EndCurrentNode (targetMsg, DateTime.Now);
				}
				processor.EndCurrentNode (projectMsg, DateTime.Now);
			}

			var bo = new BuildOutput ();
			bo.AddProcessor (processor);
			return bo;
		}

		[Test]
		public async Task CustomProject_DataSearch ()
		{
			var bo = GenerateCustomBuild (1);

			var nodes = bo.GetRootNodes (true);
			var search = new BuildOutputDataSearch (nodes);
			int matches = 0;
			var visited = new HashSet<BuildOutputNode> ();
			for (var match = await search.FirstMatch ("Message "); match != null; match = search.NextMatch ()) {
				if (visited.Contains (match)) {
					break;
				}

				visited.Add (match);
				matches++;
			}

			Assert.That (matches, Is.EqualTo (1000));
		}

		[Test]
		public async Task CustomProject_SearchCanBeCanceled ()
		{
			BuildOutputNode firstMatch = null;

			var bo = GenerateCustomBuild (10);

			var search = new BuildOutputDataSearch (bo.GetRootNodes (true));
			for (int i = 0; i < 100; i++) {
				await Task.WhenAll (Task.Run (async () => firstMatch = await search.FirstMatch ("Message ")),
									Task.Delay (100).ContinueWith (t => search.Cancel ()));

				Assert.Null (firstMatch, "Got a first match while search was canceled");
				Assert.True (search.IsCanceled, "Search was not canceled");
			}
		}

		[Test]
		public void BuildOutputNode_Search ()
		{
			var result = GetTestNodes ();
			var results = new List<BuildOutputNode> ();
			result [0].Search (results, "Error", CancellationToken.None);
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
