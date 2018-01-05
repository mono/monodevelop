// 
// OpenFileDialogHandler.cs
//  
// Authors:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Text;
using Microsoft.WindowsAPICodePack.Shell;
using System.Windows;

namespace MonoDevelop.Platform
{
	public class OpenFileDialogHandler : IOpenFileDialogHandler
	{
		public bool Run (OpenFileDialogData data)
		{
			var parent = data.TransientFor ?? MessageService.RootWindow;
			CommonFileDialog dialog;
			if (data.Action == FileChooserAction.Open) {
				dialog = new CustomCommonOpenFileDialog {
					EnsureFileExists = true
				};
			} else
				dialog = new CustomCommonSaveFileDialog ();

			dialog.SetCommonFormProperties (data);

			CustomCommonFileDialogComboBox encodingCombo = null;
			if (data.ShowEncodingSelector) {
				var group = new CommonFileDialogGroupBox ("encoding", "Encoding:");
				encodingCombo = new CustomCommonFileDialogComboBox ();

				BuildEncodingsCombo (encodingCombo, data.Action != FileChooserAction.Save, data.Encoding);
				group.Items.Add (encodingCombo);
				dialog.Controls.Add (group);

				encodingCombo.SelectedIndexChanged += (sender, e) => {
					if (encodingCombo.SelectedIndex == encodingCombo.Items.Count - 1) {
						var dlg = new System.Windows.Window {
							Title = "Choose encodings",
							Content = new SelectEncodingControl(),
							SizeToContent = SizeToContent.WidthAndHeight
						};
						if (dlg.ShowDialog ().Value) {
							BuildEncodingsCombo (encodingCombo, data.Action != FileChooserAction.Save, data.Encoding);
							dialog.ApplyControlPropertyChange ("Items", encodingCombo);
						}
					}
				};
			}

			CustomCommonFileDialogComboBox viewerCombo = null;
			CommonFileDialogCheckBox closeSolution = null;
			if (data.ShowViewerSelector && data.Action == FileChooserAction.Open) {
				var group = new CommonFileDialogGroupBox ("openWith", "Open with:");

				viewerCombo = new CustomCommonFileDialogComboBox {
					Enabled = false
				};
				group.Items.Add (viewerCombo);
				dialog.Controls.Add (group);

				if (encodingCombo != null || IdeApp.Workspace.IsOpen) {
					viewerCombo.SelectedIndexChanged += (o, e) => {
						bool solutionWorkbenchSelected = ((ViewerComboItem)viewerCombo.Items [viewerCombo.SelectedIndex]).Viewer == null;
						if (closeSolution != null)
							closeSolution.Visible = solutionWorkbenchSelected;
						if (encodingCombo != null)
							encodingCombo.Enabled = !solutionWorkbenchSelected;
					};
				}

				if (IdeApp.Workspace.IsOpen) {
					var group2 = new CommonFileDialogGroupBox ();

					// "Close current workspace" is too long and splits the text on 2 lines.
					closeSolution = new CommonFileDialogCheckBox ("Close workspace", true) {
						Visible = false
					};
					group2.Items.Add (closeSolution);
					dialog.Controls.Add (group2);
				}

				dialog.SelectionChanged += (sender, e) => {
					try {
						var files = GetSelectedItems (dialog);
						var file = files.Count == 0 ? null : files[0];
						bool hasBench = FillViewers (viewerCombo, file);
						if (closeSolution != null)
							closeSolution.Visible = hasBench;
						if (encodingCombo != null)
							encodingCombo.Enabled = !hasBench;
						dialog.ApplyControlPropertyChange ("Items", viewerCombo);
					} catch (Exception ex) {
						LoggingService.LogInternalError (ex);
					}
				};
			}

			if (!GdkWin32.RunModalWin32Dialog (dialog, parent))
				return false;

			dialog.GetCommonFormProperties (data);
			if (encodingCombo != null)
				data.Encoding = ((EncodingComboItem)encodingCombo.Items [encodingCombo.SelectedIndex]).Encoding;

			if (viewerCombo != null) {
				if (closeSolution != null)
					data.CloseCurrentWorkspace = closeSolution.Visible && closeSolution.IsChecked;
				int index = viewerCombo.SelectedIndex;
				if (index != -1)
					data.SelectedViewer = ((ViewerComboItem)viewerCombo.Items [index]).Viewer;
			}

			return true;
		}

		//for some reason, the API pack doesn't expose this method from the COM interface
		static List<string> GetSelectedItems (CommonFileDialog dialog)
		{
			var filenames = new List<string> ();
			var nativeDialog = (IFileOpenDialog)dialog.nativeDialog;
			IShellItemArray resultsArray;
			uint count;

			var hr = nativeDialog.GetSelectedItems(out resultsArray);
			if (hr != 0) {
				var e = Marshal.GetExceptionForHR(hr);

				//we get E_FAIL when there is no selection
				if (hr == -2147467259) {
					return filenames;
				} else if (e is FileNotFoundException) {
					return filenames;
				}

				throw e;
			}

			hr = (int)resultsArray.GetCount (out count);
			if (hr != 0)
				throw Marshal.GetExceptionForHR (hr);

			for (int i = 0; i < count; ++i) {
				var item = CommonFileDialog.GetShellItemAt (resultsArray, i);
				string val = CommonFileDialog.GetFileNameFromShellItem (item);
				filenames.Add (val);
			}

			return filenames;
		}

		static void BuildEncodingsCombo (CustomCommonFileDialogComboBox combo, bool showAutoDetected, Encoding selectedEncoding)
		{
			combo.Items.Clear ();
			int i = 0;

			if (showAutoDetected) {
				combo.Items.Add (new EncodingComboItem (null, GettextCatalog.GetString ("Auto Detected")));
				combo.SelectedIndex = 0;
				i = 1;
			}

			foreach (var e in TextEncoding.ConversionEncodings) {
				combo.Items.Add (new EncodingComboItem (Encoding.GetEncoding (e.CodePage), string.Format ("{0} ({1})", e.Name, e.Id)));
				if (selectedEncoding != null && e.CodePage == selectedEncoding.CodePage)
					combo.SelectedIndex = i;
				i++;
			}
			if (combo.SelectedIndex == -1)
				combo.SelectedIndex = 0;
			combo.Items.Add (new EncodingComboItem (null, GettextCatalog.GetString ("Add or Remove...")));
		}

		class EncodingComboItem : CommonFileDialogComboBoxItem
		{
			Encoding encoding;

			public EncodingComboItem (Encoding encoding, string label) : base (label)
			{
				this.encoding = encoding;
			}

			public Encoding Encoding {
				get {
					return encoding;
				}
			}
		}

		static bool FillViewers (CustomCommonFileDialogComboBox combo, string fileName)
		{
			combo.Items.Clear ();

			if (String.IsNullOrEmpty (fileName) || Directory.Exists (fileName)) {
				combo.Enabled = false;
				return false;
			}

			int selected = -1;
			int i = 0;
			bool hasBench = false;
			var projectService = IdeApp.Services.ProjectService;
			if (projectService.IsWorkspaceItemFile (fileName) || projectService.IsSolutionItemFile (fileName)) {
				hasBench = true;
				combo.Items.Add (new ViewerComboItem (null, GettextCatalog.GetString ("Solution Workbench")));
				if (!CanBeOpenedInAssemblyBrowser (fileName))
					selected = 0;
				i++;
			}

			foreach (var vw in DisplayBindingService.GetFileViewers (fileName, null))
				if (!vw.IsExternal) {
					combo.Items.Add (new ViewerComboItem (vw, vw.Title));

					if (vw.CanUseAsDefault && selected == -1)
						selected = i;

					i++;
				}

			if (selected == -1)
				selected = 0;

			combo.Enabled = combo.Items.Count >= 1;
			if (selected > 0) {
				// Unable to set SelectedIndex until ApplyControlPropertyChange called for Items
				// which causes the combo box selection to visibly change selection twice. Instead just
				// make the default item the first one in the combo.
				var item = combo.Items[selected];
				combo.Items.RemoveAt (selected);
				combo.Items.Insert (0, item);
			}
			return hasBench;
		}

		static bool CanBeOpenedInAssemblyBrowser (FilePath filename)
		{
			return filename.Extension.ToLower () == ".exe" || filename.Extension.ToLower () == ".dll";
		}

		class ViewerComboItem : CommonFileDialogComboBoxItem
		{
			public ViewerComboItem (FileViewer viewer, string label) : base (label)
			{
				Viewer = viewer;
			}

			public FileViewer Viewer {
				get; private set;
			}
		}
	}
}

