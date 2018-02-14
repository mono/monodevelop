//
// BuildOutputView.cs
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
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutputViewContent : ViewContent
	{
		FilePath filename;
		BuildOutputWidget control;

		public BuildOutputViewContent (FilePath filename)
		{
			this.filename = filename;
			this.ContentName = filename;
			control = new BuildOutputWidget (filename);
			control.ProcessLogsAsync (false);
			control.FileSaved += FileNameChanged;
		}

		public BuildOutputViewContent (BuildOutput buildOutput)
		{
			ContentName = $"{GettextCatalog.GetString ("Build Output")} {DateTime.Now.ToString ("hh:mm:ss")}.binlog";
			control = new BuildOutputWidget (buildOutput, ContentName);
			control.FileSaved += FileNameChanged;
		}

		public Task ProcessLogs (bool showDiagnostics)
		{
			return control.ProcessLogs (showDiagnostics);
		}

		void FileNameChanged (object sender, string newName)
		{
			ContentName = newName;
		}

		public override Control Control {
			get {
				return control.ToGtkWidget ();
			}
		}

		public override bool IsReadOnly {
			get {
				return true;
			}
		}

		public override bool IsFile {
			get {
				return System.IO.File.Exists (filename.FullPath);
			}
		}

		public override bool IsViewOnly {
			get {
				return true;
			}
		}

		public override string TabPageLabel {
			get {
				return filename.FileName ?? GettextCatalog.GetString ("Build Output");
			}
		}

		public override void Dispose ()
		{
			control.Dispose ();
			base.Dispose ();
		}

		internal void GoToError (string description, string project)
		{
			control.GoToError (description, project);
		}

		internal void GoToWarning (string description, string project)
		{
			control.GoToWarning (description, project);
		}

		internal void GoToMessage (string description, string project)
		{
			control.GoToMessage (description, project);
		}

		[CommandHandler (SearchCommands.Find)]
		public void Find ()
		{
			control.FocusOnSearchEntry ();
		}

		[CommandHandler (SearchCommands.FindNext)]
		public void FindNext ()
		{
			control.FindNext (this, EventArgs.Empty);
		}

		[CommandHandler (SearchCommands.FindPrevious)]
		public void FindPrevious ()
		{
			control.FindPrevious (this, EventArgs.Empty);
		}

		[CommandUpdateHandler (SearchCommands.FindNext)]
		public void UpdateFindNextHandler (CommandInfo cinfo)
		{
			cinfo.Enabled = control.IsSearchInProgress;
		}

		[CommandUpdateHandler (SearchCommands.FindPrevious)]
		public void UpdateFindPreviousHandler (CommandInfo cinfo)
		{
			cinfo.Enabled = control.IsSearchInProgress;
		}
	}
}
