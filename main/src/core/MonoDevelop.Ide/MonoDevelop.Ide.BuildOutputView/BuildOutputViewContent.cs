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
using Xwt;

namespace MonoDevelop.Ide.BuildOutputView
{
	public enum BuildOutputCommands
	{
		ExpandAll,
		CollapseAll,
		JumpTo
	}

	class BuildOutputViewContent : AbstractXwtViewContent
	{
		FilePath filename;
		BuildOutputWidget control;
		BuildOutput buildOutput;

		public BuildOutputViewContent (FilePath filename)
		{
			this.filename = filename;
			this.ContentName = filename;
			Counters.OpenedFromFile++;
		}

		public BuildOutputViewContent (BuildOutput buildOutput)
		{
			this.buildOutput = buildOutput;
			ContentName = $"{GettextCatalog.GetString ("Build Output")} {DateTime.Now.ToString ("h:mm tt yyyy-MM-dd")}.binlog";
			Counters.OpenedFromIDE++;
		}

		void FileNameChanged (object sender, string file)
		{
			ContentName = file;
		}

		public override Widget Widget {
			get {
				if (control != null)
					return control;
				var toolbar = WorkbenchWindow.GetToolbar (this);
				// TODO: enable native backend by default without checking NATIVE_BUILD_OUTPUT env
				var nativeEnabled = Environment.GetEnvironmentVariable ("NATIVE_BUILD_OUTPUT")?.ToLower () == "true";
				// native mode on Mac only, until we support Wpf embedding
				var engine = Xwt.Toolkit.NativeEngine.Type == ToolkitType.XamMac && nativeEnabled ? Xwt.Toolkit.NativeEngine : Xwt.Toolkit.CurrentEngine;
				engine.Invoke (() => {
					if (buildOutput != null)
						control = new BuildOutputWidget (buildOutput, ContentName, toolbar);
					else
						control = new BuildOutputWidget (filename, toolbar);
					control.FileNameChanged += FileNameChanged;
				});
				return control;
			}
		}

		public override bool IsReadOnly {
			get {
				return true;
			}
		}

		public override bool IsFile {
			get {
				return true;
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

		bool disposed = false;

		public override void Dispose ()
		{
			if (!disposed) {
				if (control != null) {
					control.FileNameChanged -= FileNameChanged;
					control.Dispose ();
				}
				disposed = true;
			}
			base.Dispose ();
		}

		internal Task GoToError (string description, string project)
		{
			return control.GoToError (description, project);
		}

		internal Task GoToWarning (string description, string project)
		{
			return control.GoToWarning (description, project);
		}

		internal Task GoToMessage (string description, string project)
		{
			return control.GoToMessage (description, project);
		}

		[CommandHandler (EditCommands.Copy)]
		public void Copy ()
		{
			control.ClipboardCopy ();
		}

		[CommandHandler (TextEditorCommands.LineEnd)]
		public void ExpandAll ()
		{
			control.ExpandAll ();
		}

		[CommandHandler (TextEditorCommands.LineStart)]
		public void CollapseAll ()
		{
			control.CollapseAll ();
		}

		[CommandHandler (TextEditorCommands.DocumentStart)]
		public void GoToFirstNode ()
		{
			control.GoToFirstNode ();
		}

		[CommandHandler (TextEditorCommands.DocumentEnd)]
		public void GoToLastNode ()
		{
			control.GoToLastNode ();
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

		[CommandUpdateHandler (EditCommands.Copy)]
		public void UpdateCopyHandler (CommandInfo cinfo)
		{
			cinfo.Enabled = control.CanClipboardCopy ();
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

		[CommandHandler (FileCommands.Save)]
		public override Task Save ()
		{
			Counters.SavedToFile++;
			return control.SaveAs ();
		}

		[CommandUpdateHandler (FileCommands.Save)]
		public void UpdateSaveHandler (CommandInfo cinfo)
		{
			cinfo.Enabled = control.IsDirty;
		}
	}
}
