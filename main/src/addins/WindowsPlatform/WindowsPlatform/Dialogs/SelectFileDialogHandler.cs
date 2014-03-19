// 
// SelectFileDialogHandler.cs
//  
// Author:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
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
using System.IO;
using System.Linq;
using Gtk;
using Microsoft.WindowsAPICodePack.Dialogs;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Platform
{
	class SelectFileDialogHandler : ISelectFileDialogHandler
	{
		public bool Run (SelectFileDialogData data)
		{
			var parent = data.TransientFor ?? MessageService.RootWindow;

			CommonFileDialog dialog;
			if (data.Action == FileChooserAction.Open || data.Action == FileChooserAction.SelectFolder)
				dialog = new CommonOpenFileDialog ();
			else
				dialog = new CommonSaveFileDialog ();

			SetCommonFormProperties (data, dialog);

			if (!GdkWin32.RunModalWin32Dialog (dialog, parent))
				return false;

			GetCommonFormProperties (data, dialog);

			return true;
		}

		internal static void SetCommonFormProperties (SelectFileDialogData data, CommonFileDialog dialog)
		{
			if (!string.IsNullOrEmpty (data.Title))
				dialog.Title = data.Title;

			dialog.InitialDirectory = data.CurrentFolder;

			var fileDialog = dialog as CommonOpenFileDialog;
			if (fileDialog != null) {
				fileDialog.Multiselect = data.SelectMultiple;
				if (data.Action == FileChooserAction.SelectFolder) {
					fileDialog.IsFolderPicker = true;
					return;
				}
			}

			SetFilters (data, dialog);

			dialog.DefaultFileName = data.InitialFileName;
		}

		internal static void GetCommonFormProperties (SelectFileDialogData data, CommonFileDialog dialog)
		{
			var fileDialog = dialog as CommonOpenFileDialog;
			if (fileDialog != null)
				data.SelectedFiles = fileDialog.FileNames.Select (f => (FilePath) f).ToArray ();
			else
				data.SelectedFiles = new[] {(FilePath) dialog.FileName};
		}

		static void SetFilters (SelectFileDialogData data, CommonFileDialog dialog)
		{
			foreach (var f in data.Filters) {
				var filter = new CommonFileDialogFilter {
					DisplayName = f.Name,
					ShowExtensions = true
				};
				foreach (var p in f.Patterns)
					filter.Extensions.Add (p);
				dialog.Filters.Add (filter);
			}

			SetDefaultExtension (data, dialog);
		}
		
		static void SetDefaultExtension (SelectFileDialogData data, CommonFileDialog dialog)
		{
			var defExt = data.DefaultFilter.Patterns[0];

			// FileDialog doesn't show the file extension when saving a file,
			// so we try to look for the precise filter if none was specified.
			if (!string.IsNullOrEmpty (data.InitialFileName) && data.Action == FileChooserAction.Save && defExt == "*") {
				string ext = Path.GetExtension (data.InitialFileName);
				if (!string.IsNullOrEmpty (ext)) {
					var pattern = "*" + ext;
					foreach (var f in data.Filters) {
						foreach (var p in f.Patterns) {
							if (string.Equals (p, pattern, StringComparison.OrdinalIgnoreCase)) {
								dialog.DefaultExtension = p;
								return;
							}
						}
					}
				}
			}

			dialog.DefaultExtension = defExt;
		}
	}
}
