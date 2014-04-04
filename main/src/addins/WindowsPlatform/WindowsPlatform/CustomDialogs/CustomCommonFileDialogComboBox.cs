//
// CustomCommonFileDialogComboBox.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2014 Marius Ungureanu
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
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;

namespace MonoDevelop.Platform
{
	public class CustomCommonFileDialogComboBox : CommonFileDialogComboBox
	{
		int oldCount;

		public CustomCommonFileDialogComboBox ()
		{
		}

		public CustomCommonFileDialogComboBox (string name) : base (name)
		{
		}

		internal override void Attach (IFileDialogCustomize dialog)
		{
			base.Attach (dialog);

			// Keep track of old selection.
			SelectedIndexChanged += delegate {
				oldCount = Items.Count;
			};
		}

		internal void Update(IFileDialogCustomize dialog)
		{
			Debug.Assert(dialog != null, "CommonFileDialogComboBox.Attach: dialog parameter can not be null");

			// Remove the control items.
			// Don't do RemoveAllControlItems. It's not implemented natively.
			for (int index = 0; index < oldCount; ++index)
				dialog.RemoveControlItem (Id, index);

			// Re-add the combo box items
			for (int index = 0; index < Items.Count; ++index) {
				string text = Items [index].Text;

				dialog.AddControlItem (Id, index, text);
			}

			// If we didn't, go select.
			if (Enabled) {
				SelectedIndex = 0;
				ApplyPropertyChange ("SelectedIndex");
			}

			oldCount = Items.Count;

			// Sync additional properties
			SyncUnmanagedProperties ();
		}
	}
}

