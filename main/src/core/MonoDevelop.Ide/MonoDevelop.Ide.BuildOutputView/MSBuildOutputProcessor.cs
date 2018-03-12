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
using System.Linq;
using System.Collections;
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
			}
			catch (Exception ex) {
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
			         !String.IsNullOrEmpty (e.File) ? stringPool.Add (e.File) : null,
			         !String.IsNullOrEmpty (e.ProjectFile) ? stringPool.Add (e.ProjectFile) : null,
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
			         !String.IsNullOrEmpty (e.File) ? stringPool.Add (e.File) : null,
					 !String.IsNullOrEmpty (e.ProjectFile) ? stringPool.Add (e.ProjectFile) : null,
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
				ProcessMessageEvent (this, e, stringPool);
			}
		}

		private void BinLog_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			if (CurrentNode.NodeType == BuildOutputNodeType.Build) {
				foreach (DictionaryEntry x in e.Properties) {
					var key = (string)x.Key;
					if (key == "SolutionFilename") {
						this.CurrentNode.Message = (string)x.Value;
						continue;
					} else if (key == "Configuration") {
						this.CurrentNode.Configuration = (string)x.Value;
						continue;
					} else if (key == "Platform") {
						this.CurrentNode.Platform = (string)x.Value;
						continue;
					}
				}
			}
			
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

		#region Static helpers

		const string TaskParameterMessagePrefix = @"Task Parameter:";
		const string TargetMessagePrefix = "Target \"";
		const string SkippedSuffix = " skipped";
		const string SkippingTargetMessagePrefix = "Skipping target ";

		static void ProcessMessageEvent (MSBuildOutputProcessor processor, BuildMessageEventArgs e, StringInternPool stringPool)
		{
			if (String.IsNullOrEmpty (e.Message)) {
				return;
			}

			switch (e.Message[0]) {
			case 'S':
				if (e.Message.StartsWith (SkippingTargetMessagePrefix, StringComparison.Ordinal)) {
					// "Skipping target ..." messages
					if (e.Message [SkippingTargetMessagePrefix.Length] == '"') {
						int nextQuoteIndex = e.Message.IndexOf ('"', SkippingTargetMessagePrefix.Length + 1);
						if (nextQuoteIndex >= 0) {
							if (processor.CurrentNode.NodeType == BuildOutputNodeType.Target &&
							    e.Message.IndexOf (processor.CurrentNode.Message,
							                       SkippingTargetMessagePrefix.Length + 1,
							                       nextQuoteIndex - 1 - SkippingTargetMessagePrefix.Length,
							                       StringComparison.Ordinal) == SkippingTargetMessagePrefix.Length + 1) {
								processor.CurrentNode.NodeType = BuildOutputNodeType.TargetSkipped;
								return;
							}
						}
					}
				}
				break;
			case 'T':
				if (e.Message.StartsWith (TaskParameterMessagePrefix, StringComparison.Ordinal)) {
					// Task parameters are added to a special folder
					if (ProcessTaskParameter (processor, e, stringPool)) {
						return;
					}
				} else if (e.Message.StartsWith (TargetMessagePrefix, StringComparison.Ordinal)) {
					// "Target ... skipped" messages
					int nextQuoteIndex = e.Message.IndexOf ('"', TargetMessagePrefix.Length + 1);
					if (nextQuoteIndex >= 0) {
						if (e.Message.IndexOf (SkippedSuffix, nextQuoteIndex + 1, SkippedSuffix.Length, StringComparison.Ordinal) == nextQuoteIndex + 1) {
							processor.AddNode (BuildOutputNodeType.TargetSkipped,
							                   stringPool.Add (e.Message, TargetMessagePrefix.Length, nextQuoteIndex - TargetMessagePrefix.Length),
							                   stringPool.Add (e.Message),
							                   false,
							                   e.Timestamp);
						}
					}
				}
				break;
			}

			string shortMessage = stringPool.Add (e.Message);
			processor.AddNode (e.Importance == MessageImportance.Low ? BuildOutputNodeType.Diagnostics : BuildOutputNodeType.Message,
			                   shortMessage, shortMessage,
			                   false, 
			                   e.Timestamp,
			                   !String.IsNullOrEmpty (e.File) ? stringPool.Add (e.File) : null,
			                   !String.IsNullOrEmpty (e.ProjectFile) ? stringPool.Add (e.ProjectFile) : null,
			                   e.LineNumber);
		}

		static bool ProcessTaskParameter (MSBuildOutputProcessor processor, BuildMessageEventArgs e, StringInternPool stringPool)
		{
			if (e.Message.IndexOf ('\n') == - 1) {
				var message = stringPool.Add (e.Message, TaskParameterMessagePrefix.Length, e.Message.Length - TaskParameterMessagePrefix.Length);
				processor.CurrentNode.AddParameter (message, message);
			} else {
				string content = e.Message.Substring (TaskParameterMessagePrefix.Length);
				int equalSign = content.IndexOf ('=');
				if (equalSign < 0) {
					return false;
				}

				content = $"{content.Substring (0, equalSign).Trim ()}={content.Substring (equalSign + 1, content.Length - equalSign - 1).Trim ()}";
				processor.CurrentNode.AddParameter (stringPool.Add (content), stringPool.Add (e.Message));
			}

			return true;
		}

		#endregion
	}
}
