//
// AnnotateCommand.cs
//
// Author:
//   Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
//
// Copyright (C) 2009 Levi Bard
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

using MonoDevelop.VersionControl.Views;

namespace MonoDevelop.VersionControl 
{
	/// <summary>
	/// Command handler for showing annotations
	/// </summary>
	internal class ShowAnnotationsCommand: CommandHandler
	{
		/// <summary>
		/// Shows annotations for the active document
		/// </summary>
		protected override void Run ()
		{
			FilePath file = IdeApp.Workbench.ActiveDocument.FileName;
			Repository repo = VersionControlService.GetRepositoryReference (Path.GetDirectoryName (file.FullPath), file.FileName);
			AnnotateView.Show (repo, file, false);
		}

		/// <summary>
		/// Determines whether the command is valid for the active document
		/// </summary>
		protected override void Update (CommandInfo item)
		{
			FilePath file = IdeApp.Workbench.ActiveDocument.FileName;
			Repository repo = VersionControlService.GetRepositoryReference (Path.GetDirectoryName (file.FullPath), file.FileName);
			item.Visible = AnnotateView.Show (repo, file, true);
		}
	}

	/// <summary>
	/// Command handler for hiding annotations
	/// </summary>
	internal class HideAnnotationsCommand: CommandHandler
	{
		/// <summary>
		/// Hides annotations for the active document
		/// </summary>
		protected override void Run ()
		{
			FilePath file = IdeApp.Workbench.ActiveDocument.FileName;
			Repository repo = VersionControlService.GetRepositoryReference (Path.GetDirectoryName (file.FullPath), file.FileName);
			AnnotateView.Hide (repo, file, false);
		}

		/// <summary>
		/// Determines whether the command is valid for the active document
		/// </summary>
		protected override void Update (CommandInfo item)
		{
			FilePath file = IdeApp.Workbench.ActiveDocument.FileName;
			Repository repo = VersionControlService.GetRepositoryReference (Path.GetDirectoryName (file.FullPath), file.FileName);
			item.Visible = AnnotateView.Hide (repo, file, true);
		}
	}
}
