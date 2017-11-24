//
// DebugSourceFilesOptionsPanel.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.Debugger
{
	class DebugSourceFilesOptionsPanel : ItemOptionsPanel
	{
		DebugSourceFilesOptionsPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			return widget = new DebugSourceFilesOptionsPanelWidget (this.ConfiguredSolution);
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}

	class DebugSourceFilesOptionsPanelWidget : VBox
	{
		Solution solution;
		Label label;
		FolderListSelector selector = new FolderListSelector ();

		public DebugSourceFilesOptionsPanelWidget (Solution solution)
		{
			this.solution = solution;

			label = new Label (BrandingService.BrandApplicationName (GettextCatalog.GetString ("Folders where MonoDevelop should look for debug source files:")));
			label.Xalign = 0;
			PackStart (label, false, false, 0);
			selector.Directories = new List<string> (SourceCodeLookup.GetDebugSourceFolders (solution));
			PackStart (selector, true, true, 0);
			ShowAll ();
			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			selector.EntryAccessible.SetCommonAttributes (null, null, GettextCatalog.GetString ("Enter a folder to search for debug source files"));
			selector.EntryAccessible.SetTitleUIElement (label.Accessible);
			label.Accessible.SetTitleFor (selector.EntryAccessible);
		}

		public void Store ()
		{
			SourceCodeLookup.SetDebugSourceFolders (solution, selector.Directories.ToArray ());
		}
	}
}
