﻿//
// VersionControlSolutionOptionsPanel.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2013 Marius Ungureanu
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

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.VersionControl
{
	public class VersionControlSolutionOptionsPanel : OptionsPanel
	{
		Xwt.CheckBox disableVersionControl;

		public override Control CreatePanelWidget ()
		{
			Xwt.VBox box = new Xwt.VBox ();
			box.Spacing = 6;
			box.Margin = 12;

			disableVersionControl = new Xwt.CheckBox (GettextCatalog.GetString ("Disable Version Control for this solution")) {
				Active = VersionControlService.IsSolutionDisabled ((Solution)DataObject),
			};
			box.PackStart (disableVersionControl);
			box.Show ();
			return box.ToGtkWidget ();
		}

		public override void ApplyChanges ()
		{
			VersionControlService.SetSolutionDisabled ((Solution)DataObject, disableVersionControl.Active);
			VersionControlService.SaveConfiguration ();
		}
	}
}
