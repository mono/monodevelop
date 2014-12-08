// 
// AddFileDialogHandler.cs
//  
// Authors:
//   Carlos Alberto Cortez <calberto.cortez@gmail.com>
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//   Marius Ungureanu <marius.ungureanu@xamarin.com>
// 
// Copyright (c) 2011 Novell, Inc. (http://wwww.novell.com)
// Copyright (C) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Platform;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{
	class AddFileDialogHandler: IAddFileDialogHandler
	{
		public bool Run (AddFileDialogData data)
		{
			var parent = data.TransientFor ?? MessageService.RootWindow;
			var dialog = new CommonOpenFileDialog ();
			dialog.SetCommonFormProperties (data);

			var buildActionCombo = new CommonFileDialogComboBox ();
			var group = new CommonFileDialogGroupBox ("overridebuildaction", "Override build action:"); 
			buildActionCombo.Items.Add (new CommonFileDialogComboBoxItem (GettextCatalog.GetString ("Default")));
			foreach (var ba in data.BuildActions) {
				if (ba == "--")
					continue;

				buildActionCombo.Items.Add (new CommonFileDialogComboBoxItem (ba));
			}

			buildActionCombo.SelectedIndex = 0;
			group.Items.Add (buildActionCombo);
			dialog.Controls.Add (group);

			if (!GdkWin32.RunModalWin32Dialog (dialog, parent))
				return false;

			dialog.GetCommonFormProperties (data);
			var idx = buildActionCombo.SelectedIndex;
			if (idx > 0)
				data.OverrideAction = buildActionCombo.Items [idx].Text;

			return true;
		}
	}
}
