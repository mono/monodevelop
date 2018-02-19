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
using System.IO;
using Microsoft.Build.Logging;
using Microsoft.Build.Framework;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.BuildOutputView
{
	class MSBuildOutputProcessor : BuildOutputProcessor
	{
		readonly BinaryLogReplayEventSource binlogReader = new BinaryLogReplayEventSource ();
		readonly StringInternPool stringPool = new StringInternPool ();

		public MSBuildOutputProcessor (string filePath, bool removeFileOnDispose) : base (filePath, removeFileOnDispose)
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

			try {
				binlogReader.Replay (FileName);
			} catch (Exception ex) {
				LoggingService.LogError ($"Can't process {FileName}: {ex.ToString ()}");
			}
		}

		private void BinLog_BuildStarted (object sender, BuildStartedEventArgs e)
		{
			var message = stringPool.Add (e.Message);
			AddNode (BuildOutputNodeType.Build, 
			         message, 
			         message, 
			         true,
			         e.Timestamp);
		}

		private void BinLog_BuildFinished (object sender, BuildFinishedEventArgs e)
		{
			EndCurrentNode (stringPool.Add (e.Message), e.Timestamp);
		}

		private void BinLog_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			var message = stringPool.Add (e.Message);
			AddNode (BuildOutputNodeType.Error, 
			         message, 
			         message, 
			         false,
			         e.Timestamp,
			         stringPool.Add (e.File),
			         stringPool.Add (e.ProjectFile),
			         e.LineNumber);
		}

		private void BinLog_WarningRaised (object sender, BuildWarningEventArgs e)
		{
			var message = stringPool.Add (e.Message);
			AddNode (BuildOutputNodeType.Warning, 
			         message, 
			         message, 
			         false,
			         e.Timestamp,
			         stringPool.Add (e.File),
			         stringPool.Add (e.ProjectFile),
			         e.LineNumber);
		}

		void BinlogReader_MessageRaised (object sender, BuildMessageEventArgs e)
		{
			if (e.BuildEventContext != null && (e.BuildEventContext.NodeId == 0 &&
			                                    e.BuildEventContext.ProjectContextId == 0 &&
			                                    e.BuildEventContext.ProjectInstanceId == 0 &&
			                                    e.BuildEventContext.TargetId == 0 &&
			                                    e.BuildEventContext.TaskId == 0)) {
				// These are the "Detailed summary" lines
				// TODO: we should probably parse them and associate those stats
				// with the correct build step, so that we get stats for those
			} else {
				this.ProcessMessageEvent (e, stringPool);
			}
		}

		private void BinLog_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			AddNode (BuildOutputNodeType.Project,
			         stringPool.Add (Path.GetFileName (e.ProjectFile)), 
			         stringPool.Add (e.Message), 
			         true,
			         e.Timestamp);
		}

		private void BinLog_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			EndCurrentNode (stringPool.Add (e.Message), e.Timestamp);
		}

		private void BinLog_TargetStarted (object sender, TargetStartedEventArgs e)
		{
			AddNode (BuildOutputNodeType.Target, 
			         stringPool.Add (e.TargetName), 
			         stringPool.Add (e.Message), 
			         true, 
			         e.Timestamp);
		}

		private void BinLog_TargetFinished (object sender, TargetFinishedEventArgs e)
		{
			EndCurrentNode (stringPool.Add (e.Message), e.Timestamp);
		}

		private void BinLog_TaskStarted (object sender, TaskStartedEventArgs e)
		{
			if (e.TaskName == "Message") {
				// <Task Message></Task> are removed, we just display the messages
				return;
			}
			AddNode (BuildOutputNodeType.Task,
			         stringPool.Add (e.TaskName), 
			         stringPool.Add (e.Message), 
			         true,
			         e.Timestamp);
		}

		private void BinLog_TaskFinished (object sender, TaskFinishedEventArgs e)
		{
			if (e.TaskName == "Message") {
				// <Task Message></Task> are removed, we just display the messages
				return;
			}

			EndCurrentNode (stringPool.Add (e.Message), e.Timestamp);
		}
	}

	static class BinaryLogHelpers
	{
		const string TaskParameterMessagePrefix = @"Task Parameter:";

		public static void ProcessMessageEvent (this MSBuildOutputProcessor processor, BuildMessageEventArgs e, StringInternPool stringPool)
		{
			if (String.IsNullOrEmpty (e.Message)) {
				return;
			}

			switch (e.Message[0]) {
			case 'T':
				if (e.Message.StartsWith (TaskParameterMessagePrefix)) {
					string content = e.Message.Substring (TaskParameterMessagePrefix.Length)
					                  .Replace ("\n\r", " ")
					                  .Replace ('\n', ' ');
					int equalSign = content.IndexOf ('=');
					if (equalSign < 0) {
						break;
					}

					content = $"{content.Substring (0, equalSign).Trim ()} = {content.Substring (equalSign + 1, content.Length - equalSign - 1).Trim ()}";
					processor.CurrentNode.AddParameter (stringPool.Add (content), stringPool.Add (e.Message));
					return;
				}
				break;
			}

			string shortMessage = stringPool.Add (e.Message.Replace ("\n\r", " ").Replace ('\n', ' '));
			processor.AddNode (e.Importance == MessageImportance.Low ? BuildOutputNodeType.Diagnostics : BuildOutputNodeType.Message,
			                   shortMessage, stringPool.Add (e.Message),
			                   false, 
			                   e.Timestamp,
			                   stringPool.Add (e.File),
			                   stringPool.Add (e.ProjectFile),
			                   e.LineNumber);
		}
	}
}
