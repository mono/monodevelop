//
// BuildOuputWidget.cs
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
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Core;
using Microsoft.Build.Logging;
using Microsoft.Build.Framework;
using System.Text;

namespace MonoDevelop.Ide.Gui.Components
{
	class BuildOutputWidget : VBox
	{
		FilePath filename;
		TextEditor editor;
		CompactScrolledWindow scrolledWindow;

		public BuildOutputWidget (FilePath filename)
		{
			this.filename = filename;

			editor = TextEditorFactory.CreateNewEditor ();
			editor.IsReadOnly = true;
			editor.FileName = filename;

			scrolledWindow = new CompactScrolledWindow ();
			scrolledWindow.Add (editor);

			PackStart (scrolledWindow);
			ShowAll ();

			ReadFile ();
		}

		protected override void OnDestroyed ()
		{
			editor.Dispose ();
			base.OnDestroyed ();
		}

		StringBuilder buildOutput;

		void ReadFile()
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

			buildOutput = new StringBuilder();
			binlogReader.Replay (filename.FullPath);
			editor.Text = buildOutput.ToString();

			binlogReader.BuildStarted -= BinLog_BuildStarted;
			binlogReader.BuildFinished -= BinLog_BuildFinished;
			binlogReader.ErrorRaised -= BinLog_ErrorRaised;
			binlogReader.ProjectStarted -= BinLog_ProjectStarted;
			binlogReader.ProjectFinished -= BinLog_ProjectFinished;
			binlogReader.TargetStarted -= BinLog_TargetStarted;
			binlogReader.TargetFinished -= BinLog_TargetFinished;
			binlogReader.TaskStarted -= BinLog_TaskStarted;
			binlogReader.TaskFinished -= BinLog_TaskFinished;
		}

		int currentTabPosition = 0;

		private void InsertText (string text, bool incrementTab, bool decrementTab)
		{
			if (incrementTab)
				currentTabPosition++;
			if (decrementTab)
				currentTabPosition--;

			for (int i = 0; i < currentTabPosition; i++)
				buildOutput.Append ("\t");
			buildOutput.AppendLine (text);
		}

		private void BinLog_BuildStarted (object sender, BuildStartedEventArgs e)
		{
			InsertText (e.Message, true, false);
		}

		private void BinLog_BuildFinished (object sender, BuildFinishedEventArgs e)
		{
			InsertText (e.Message, false, true);
		}

		private void BinLog_ErrorRaised (object sender, BuildErrorEventArgs e)
		{
			InsertText (e.Message, false, false);
		}

		private void BinLog_ProjectStarted (object sender, ProjectStartedEventArgs e)
		{
			InsertText (e.Message, true, false);
		}

		private void BinLog_ProjectFinished (object sender, ProjectFinishedEventArgs e)
		{
			InsertText (e.Message, false, true);
		}

		private void BinLog_TargetStarted (object sender, TargetStartedEventArgs e)
		{
			InsertText (e.Message, true, false);
		}

		private void BinLog_TargetFinished (object sender, TargetFinishedEventArgs e)
		{
			InsertText (e.Message, false, true);
		}

		private void BinLog_TaskStarted (object sender, TaskStartedEventArgs e)
		{
			InsertText (e.Message, true, false);
		}

		private void BinLog_TaskFinished (object sender, TaskFinishedEventArgs e)
		{
			InsertText (e.Message, false, true);
		}
	}
}
