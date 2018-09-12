//
// MacCommonFileDialogHandler.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 
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
using AppKit;
using Foundation;
using MonoDevelop.Components;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Core;

namespace MonoDevelop.MacIntegration
{
	abstract class MacCommonFileDialogHandler<TData, TState> where TData:SelectFileDialogData
	{
		protected abstract NSSavePanel OnCreatePanel (TData data);

		protected NSSavePanel CreatePanel (TData data, out TState saveState)
		{
			var panel = OnCreatePanel (data);

			SetCommonPanelProperties (data, panel);

			CreateAccessoryBox (data, panel, out saveState);
			return panel;
		}

		bool ShouldCreateAccessoryBox (TData data) => (data.Action & FileChooserAction.FileFlags) != 0;
		MDAccessoryViewBox CreateAccessoryBox (TData data, NSSavePanel panel, out TState saveState)
		{
			saveState = default(TState);
			if (!ShouldCreateAccessoryBox (data))
				return null;
			
			var box = new MDAccessoryViewBox ();

			var filterPopup = MacSelectFileDialogHandler.CreateFileFilterPopup (data, panel);
			if (filterPopup != null) {
				box.AddControl (filterPopup, GettextCatalog.GetString ("Show Files:"));
			}

			foreach (var item in OnGetAccessoryBoxControls (data, panel, out saveState)) {
				box.AddControl (item.control, item.text);
			}

			box.Layout ();
			panel.AccessoryView = box.View;
			return box;
		}

		protected virtual IEnumerable<(NSControl control, string text)> OnGetAccessoryBoxControls (TData data, NSSavePanel panel, out TState saveState)
		{
			saveState = default(TState);
			return Array.Empty<(NSControl, string)> ();
		}

		static void SetCommonPanelProperties (TData data, NSSavePanel panel)
		{
			if (MacSystemInformation.OsVersion >= MacSystemInformation.Mojave)
				IdeTheme.ApplyTheme (panel);
			panel.TreatsFilePackagesAsDirectories = true;

			if (!string.IsNullOrEmpty (data.Title))
				panel.Title = data.Title;

			if (!string.IsNullOrEmpty (data.InitialFileName))
				panel.NameFieldStringValue = data.InitialFileName;

			if (!string.IsNullOrEmpty (data.CurrentFolder))
				panel.DirectoryUrl = new NSUrl (data.CurrentFolder, true);

			panel.ParentWindow = NSApplication.SharedApplication.KeyWindow ?? NSApplication.SharedApplication.MainWindow;

			if (panel is NSOpenPanel openPanel) {
				openPanel.AllowsMultipleSelection = data.SelectMultiple;
				openPanel.ShowsHiddenFiles = data.ShowHidden;
			}
		}
	}
}
