//
// WorkingCopyFormatDialog.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc.
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
using Xwt;
using MonoDevelop.Ide;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Subversion.Gui
{
	static class WorkingCopyFormatDialog
	{
		static readonly Command UpgradeCommand = new Command (GettextCatalog.GetString ("Upgrade working copy"));
		static readonly Command DisableCommand = new Command (GettextCatalog.GetString ("Disable version control"));

		internal static void Show (bool isOld, Action action)
		{
			Action del = delegate {
				string primary;
				string secondary;
				Command[] commands;
				if (isOld) {
					primary = GettextCatalog.GetString ("The subversion working copy format is too old.");
					secondary = GettextCatalog.GetString ("Would you like to upgrade the working copy or" +
						" disable subversion integration for this solution?");
					commands = new [] {
						UpgradeCommand,
						DisableCommand
					};
				} else {
					primary = GettextCatalog.GetString ("The subversion working copy format is too new.");
					secondary = GettextCatalog.GetString ("Subversion integration will be disabled for this solution.");
					commands = new [] {
						DisableCommand
					};
				}

				if (MessageDialog.AskQuestion (primary, secondary, commands) != DisableCommand)
					action();
			};

			if (DispatchService.IsGuiThread) {
				// Already in GUI thread
				del ();
			}
			else
				DispatchService.GuiDispatch (del);
		}
	}
}

