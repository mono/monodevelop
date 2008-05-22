// BaseDirectoryPanel.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	public class BaseDirectoryPanel: IOptionsPanel
	{
		BaseDirectoryPanelWidget widget;
		IWorkspaceObject obj;
		
		public BaseDirectoryPanel()
		{
		}

		public void Initialize (OptionsDialog dialog, object dataObject)
		{
			obj = dataObject as IWorkspaceObject;
		}

		public Gtk.Widget CreatePanelWidget ()
		{
			widget = new BaseDirectoryPanelWidget ();
			widget.BaseDirectory = obj.BaseDirectory;
			return widget;
		}

		public bool IsVisible ()
		{
			return obj != null;
		}

		public bool ValidateChanges ()
		{
			if (widget.BaseDirectory.Length > 0 && !System.IO.Directory.Exists (widget.BaseDirectory)) {
				MessageService.ShowError (GettextCatalog.GetString ("Directory not found: {0}", widget.BaseDirectory));
				return false;
			}
			return true;
		}

		public void ApplyChanges ()
		{
			obj.BaseDirectory = widget.BaseDirectory;
		}
	}
}
