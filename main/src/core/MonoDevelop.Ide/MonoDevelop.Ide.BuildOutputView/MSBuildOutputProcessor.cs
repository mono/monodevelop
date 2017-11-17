//
// MSBuildOutputProcessor.cs
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
using Microsoft.Build.Logging;
using Microsoft.Build.Framework;

namespace MonoDevelop.Ide.BuildOutputView
{
	class MSBuildOutputProcessor : BuildOutputProcessor
	{
		readonly BinaryLogReplayEventSource binlogReader = new BinaryLogReplayEventSource ();

		public MSBuildOutputProcessor (string filePath) : base (filePath)
		{
			binlogReader.BuildStarted += BinLog_BuildStarted;
			binlogReader.BuildFinished += BinLog_BuildFinished;
			binlogReader.ErrorRaised += BinLog_ErrorRaised;
			binlogReader.WarningRaised += BinLog_WarningRaised;
			binlogReader.MessageRaised += BinlogReader_MessageRaised;
			binlogReader.ProjectStarted += BinLog_ProjectStarted;
			binlogReader.ProjectFinished += BinLog_ProjectFinished;
			binlogReader.TargetStarted += BinLog_TargetStarted;
			binlogReader.TargetFinished += BinLog_TargetFinished;
			binlogReader.TaskStarted += BinLog_TaskStarted;
			binlogReader.TaskFinished += BinLog_TaskFinished;
		}

		public override void Process ()
		{
			if (!NeedsProcessing) {
				return;
			}

			Clear ();
			base.Process ();

			binlogReader.Replay (FileName);
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

		private void BinLog_WarningRaised (object sender, BuildWarningEventArgs e)
		{
			AddNode (BuildOutputNodeType.Warning, e.Message, false);
		}

		void BinlogReader_MessageRaised (object sender, BuildMessageEventArgs e)
		{
			AddNode (e.Importance == MessageImportance.Low ? BuildOutputNodeType.Diagnostics : BuildOutputNodeType.Message, e.Message, false);
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
	}
}
