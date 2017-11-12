//
// BuildOutputProcessor.cs
//
// Author:
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
using System.Collections.Generic;
using Microsoft.Build.Logging;
using Microsoft.Build.Framework;
using MonoDevelop.Ide.Editor;
using System.Text;

namespace MonoDevelop.Ide.Gui.Components
{
	enum BuildOutputNodeType
	{
		Build,
		Project,
		Target,
		Task,
		Error,
		Message
	}

	class BuildOutputNode
	{
		public BuildOutputNodeType NodeType { get; set; }
		public string Message { get; set; }
		public BuildOutputNode Parent { get; set; }
		public IList<BuildOutputNode> Children { get; } = new List<BuildOutputNode> ();
	}

	class BuildOutputProcessor
	{
		List<BuildOutputNode> rootNodes;
		BuildOutputNode currentNode;

		public BuildOutputProcessor (string fileName)
		{
			FileName = fileName;
		}

		public string FileName { get; }

		public void Process ()
		{
			var binlogReader = new BinaryLogReplayEventSource ();
			binlogReader.BuildStarted += BinLog_BuildStarted;
			binlogReader.BuildFinished += BinLog_BuildFinished;
			binlogReader.ErrorRaised += BinLog_ErrorRaised;
			binlogReader.ProjectStarted += BinLog_ProjectStarted;
			binlogReader.ProjectFinished += BinLog_ProjectFinished;
			binlogReader.TargetStarted += BinLog_TargetStarted;
			binlogReader.TargetFinished += BinLog_TargetFinished;
			binlogReader.TaskStarted += BinLog_TaskStarted;
			binlogReader.TaskFinished += BinLog_TaskFinished;

			currentNode = null;
			rootNodes = new List<BuildOutputNode> ();
			binlogReader.Replay (FileName);
		}

		private void AddNode (BuildOutputNodeType nodeType, string message, bool isStart)
		{
			var node = new BuildOutputNode { NodeType = nodeType, Message = message };
			if (currentNode == null) {
				rootNodes.Add (node);
			} else {
				currentNode.Children.Add (node);
				node.Parent = currentNode;
			}

			if (isStart) {
				currentNode = node;
			}
		}

		private void EndCurrentNode (string message)
		{
			AddNode (BuildOutputNodeType.Message, message, false);
			currentNode = currentNode?.Parent;
		}

		private void BinLog_BuildStarted (object sender, BuildStartedEventArgs e)
		{
			AddNode (BuildOutputNodeType.Build, e.Message, false);
		}

		private void BinLog_BuildFinished (object sender, BuildFinishedEventArgs e)
		{
			EndCurrentNode (e.Message);
		}

		private void BinLog_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			AddNode (BuildOutputNodeType.Error, e.Message, false);
		}

		private void BinLog_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			AddNode (BuildOutputNodeType.Project, e.Message, true);
		}

		private void BinLog_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			EndCurrentNode (e.Message);
		}

		private void BinLog_TargetStarted (object sender, TargetStartedEventArgs e)
		{
			AddNode (BuildOutputNodeType.Target, e.Message, true);
		}

		private void BinLog_TargetFinished (object sender, TargetFinishedEventArgs e)
		{
			EndCurrentNode (e.Message);
		}

		private void BinLog_TaskStarted (object sender, TaskStartedEventArgs e)
		{
			AddNode (BuildOutputNodeType.Task, e.Message, true);
		}

		private void BinLog_TaskFinished (object sender, TaskFinishedEventArgs e)
		{
			EndCurrentNode (e.Message);
		}

		private void ProcessChildren (IList<BuildOutputNode> children, int tabPosition, StringBuilder buildOutput, List<IFoldSegment> segments)
		{
			foreach (var child in children) {
				ProcessNode (child, tabPosition + 1, buildOutput, segments); 
			}
		}

		private void ProcessNode (BuildOutputNode node, int tabPosition, StringBuilder buildOutput, List<IFoldSegment> segments)
		{
			for (int i = 0; i < tabPosition; i++) buildOutput.Append ("\t");

			int currentPosition = buildOutput.Length;
			buildOutput.AppendLine (node.Message);

			ProcessChildren (node.Children, tabPosition, buildOutput, segments);
		}

		public (string, IList<IFoldSegment>) ToTextEditor ()
		{
			var buildOutput = new StringBuilder ();
			var foldingSegments = new List<IFoldSegment> ();

			foreach (var node in rootNodes) {
				ProcessNode (node, 0, buildOutput, foldingSegments);
			}

			return (buildOutput.ToString (), foldingSegments);
		}
	}
}
