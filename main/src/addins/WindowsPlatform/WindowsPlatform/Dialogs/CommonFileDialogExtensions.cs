//
// CommonFileDialogExtensions.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.Platform
{
	static class CommonFileDialogExtensions
	{
		internal static void GetCommonFormProperties (this CommonFileDialog dialog, SelectFileDialogData data)
		{
			var fileDialog = dialog as CommonOpenFileDialog;
			if (fileDialog != null)
				data.SelectedFiles = fileDialog.FileNames.Select (f => FilterFileName (data, f)).ToArray ();
			else
				data.SelectedFiles = new[] { FilterFileName (data, dialog.FileName) };
		}

		internal static void SetCommonFormProperties (this CommonFileDialog dialog, SelectFileDialogData data)
		{
			if (!string.IsNullOrEmpty (data.Title))
				dialog.Title = data.Title;

			dialog.InitialDirectory = data.CurrentFolder;

			var fileDialog = dialog as CommonOpenFileDialog;
			if (fileDialog != null) {
				fileDialog.Multiselect = data.SelectMultiple;
				fileDialog.ShowHiddenItems = data.ShowHidden;
				if (data.Action == FileChooserAction.SelectFolder) {
					fileDialog.IsFolderPicker = true;
					return;
				}
			}

			SetFilters (data, dialog);

			dialog.DefaultFileName = data.InitialFileName;
		}

		static FilePath FilterFileName (SelectFileDialogData data, string fileName)
		{
			FilePath result = fileName;
			// FileDialog doesn't show the file extension when saving a file and chooses the extension based
			// the file filter. But * is no valid extension so the default file name extension needs to be set in that case.
			if (result.Extension == ".*") {
				var ext = Path.GetExtension (data.InitialFileName);
				if (!string.IsNullOrEmpty (ext))
					result = result.ChangeExtension (ext);
			}
			return result;
		}

		static void SetDefaultExtension (SelectFileDialogData data, CommonFileDialog dialog)
		{
			var defExt = data.DefaultFilter == null ? null : data.DefaultFilter.Patterns.FirstOrDefault ();
			if (defExt == null)
				return;
			// FileDialog doesn't show the file extension when saving a file,
			// so we try to look for the precise filter if none was specified.
			if (!string.IsNullOrEmpty (data.InitialFileName) && data.Action == FileChooserAction.Save && defExt == "*") {
				string ext = Path.GetExtension (data.InitialFileName);
				if (!string.IsNullOrEmpty (ext)) {
					var pattern = "*" + ext;
					foreach (var f in data.Filters) {
						foreach (var p in f.Patterns) {
							if (string.Equals (p, pattern, StringComparison.OrdinalIgnoreCase)) {
								dialog.DefaultExtension = p.TrimStart ('*', '.');
								return;
							}
						}
					}
				}
			}

			defExt = defExt.Trim();
			defExt = defExt.Replace("*.", null);
			defExt = defExt.Replace(".", null);

			dialog.DefaultExtension = defExt;
		}

		static void SetFilters (SelectFileDialogData data, CommonFileDialog dialog)
		{
			foreach (var f in data.Filters)
				dialog.Filters.Add (new CommonFileDialogFilter (f.Name, string.Join (",", f.Patterns)));

			SetDefaultExtension (data, dialog);
		}
	}
}

