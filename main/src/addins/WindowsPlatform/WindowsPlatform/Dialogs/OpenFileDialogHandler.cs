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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Gtk;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs.Controls;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Platform
{
	public class OpenFileDialogHandler : IOpenFileDialogHandler
	{
		public bool Run (OpenFileDialogData data)
		{
			var parent = data.TransientFor ?? MessageService.RootWindow;
			CommonFileDialog dialog;
			if (data.Action == FileChooserAction.Open)
				dialog = new CustomCommonOpenFileDialog ();
			else
				dialog = new CommonSaveFileDialog ();

			SelectFileDialogHandler.SetCommonFormProperties (data, dialog);

			CustomCommonFileDialogComboBox encodingCombo = null;
			if (data.ShowEncodingSelector) {
				encodingCombo = BuildEncodingsCombo (data.Action != FileChooserAction.Save, data.Encoding);

				var group = new CommonFileDialogGroupBox ("encoding", "Encoding:"); 
				group.Items.Add (encodingCombo);
				dialog.Controls.Add (group);

				encodingCombo.SelectedIndexChanged += (sender, e) => {
					if (encodingCombo.SelectedIndex == encodingCombo.Items.Count - 1) {
						// TODO: Create dialog for stuff.
					}
				};
			}

			CustomCommonFileDialogComboBox viewerCombo = null;
			if (data.ShowViewerSelector && data.Action == FileChooserAction.Open) {
				viewerCombo = new CustomCommonFileDialogComboBox ();
				viewerCombo.Enabled = false;
				var group = new CommonFileDialogGroupBox ("openWith", "Open with:");
				group.Items.Add (viewerCombo);
				dialog.Controls.Add (group);

				// TODO: Add closing workspace.
				dialog.SelectionChanged += (sender, e) => {
					try {
						var files = GetSelectedItems (dialog);
						var file = files.Count == 0 ? null : files[0];
						FillViewers (viewerCombo, file);
						if (viewerCombo.Enabled)
							dialog.ApplyControlPropertyChange ("Items", viewerCombo);
					} catch (Exception ex) {
						LoggingService.LogError (e.ToString ());
					}
				};
			}

			if (!GdkWin32.RunModalWin32Dialog (dialog, parent))
				return false;

			SelectFileDialogHandler.GetCommonFormProperties (data, dialog);
			if (encodingCombo != null)
				data.Encoding = ((EncodingComboItem)encodingCombo.Items [encodingCombo.SelectedIndex]).Encoding;

			if (viewerCombo != null ) {
				// TODO: Add closing workspace.
				//if (closeSolutionButton != null)
				//	data.CloseCurrentWorkspace = closeSolutionButton.State != NSCellStateValue.Off;
				data.SelectedViewer = ((ViewerComboItem)viewerCombo.Items[viewerCombo.SelectedIndex]).Viewer;
			}


			return true;
		}

		//for some reason, the API pack doesn't expose this method from the COM interface
		static List<string> GetSelectedItems (CommonFileDialog dialog)
		{
			var f = typeof (CommonFileDialog).GetField ("nativeDialog", BindingFlags.NonPublic | BindingFlags.Instance);

			var cfd = typeof(CommonFileDialog);
			var ife = cfd.Assembly.GetType ("Microsoft.WindowsAPICodePack.Dialogs.IFileOpenDialog");
			var isi = cfd.Assembly.GetType ("Microsoft.WindowsAPICodePack.Shell.IShellItem");
			var isia = cfd.Assembly.GetType ("Microsoft.WindowsAPICodePack.Shell.IShellItemArray");

			var gsi = ife.GetMethod ("GetSelectedItems"); // out IShellItemArray
			//HResult GetCount (out int)
			var gc = isia.GetMethod ("GetCount");
			// string GetFileNameFromShellItem(IShellItem item)
			var gffsi = cfd.GetMethod ("GetFileNameFromShellItem", BindingFlags.NonPublic | BindingFlags.Static);
			// IShellItem GetShellItemAt(IShellItemArray array, int i)
			var gsia = cfd.GetMethod ("GetShellItemAt", BindingFlags.NonPublic | BindingFlags.Static);

			var filenames = new List<string> ();
			var obj = f.GetValue (dialog);
			var p1 = new object[1];

			try {
				gsi.Invoke (obj, p1);
			} catch (Exception ex) {
				//we get E_FAIL when there is no selection
				var ce = ex.InnerException as COMException;
				if (ce != null && ce.ErrorCode == -2147467259)
					return filenames;
				throw;
			}

			var p2 = new object[1];
			var hr = (int) gc.Invoke (p1[0], p2);
			if (hr != 0)
				throw Marshal.GetExceptionForHR (hr);

			var count = (uint) p2[0];
			var p3 = new[] { p1[0], 0 };
			var p4 = new object[1];
			for (int i = 0; i < count; i++) {
				p4[0] = gsia.Invoke (null, p3);
				string val = (string)gffsi.Invoke (null, p4);
				filenames.Add (val);
			}

			return filenames;
		}

		static CustomCommonFileDialogComboBox BuildEncodingsCombo (bool showAutoDetected, Encoding selectedEncoding)
		{
			var combo = new CustomCommonFileDialogComboBox ();
	
			var encodings = SelectedEncodings.ConversionEncodings;
			if (encodings == null || encodings.Length == 0)
				encodings = SelectedEncodings.DefaultEncodings;

			if (showAutoDetected) {
				combo.Items.Add (new EncodingComboItem (null, GettextCatalog.GetString ("Auto Detected")));
				combo.SelectedIndex = 0;
			}

			for (int i = 0; i < encodings.Length; i++) {
				var codePage = encodings[i];
				var encoding = Encoding.GetEncoding (codePage);
				var mdEnc = TextEncoding.SupportedEncodings.FirstOrDefault (t => t.CodePage == codePage);
				string name = mdEnc != null
					? mdEnc.Name + " (" + mdEnc.Id + ")"
					: encoding.EncodingName + " (" + encoding.WebName + ")"; 
				var item = new EncodingComboItem (encoding, name);
				combo.Items.Add (item);
				if (encoding.Equals (selectedEncoding))
					combo.SelectedIndex = i + 1;
			}

			combo.Items.Add (new CommonFileDialogComboBoxItem (GettextCatalog.GetString ("Add or Remove...")));
			return combo;
		}

		class EncodingComboItem : CommonFileDialogComboBoxItem
		{
			public EncodingComboItem (Encoding encoding, string label) : base (label)
			{
				Encoding = encoding;
			}

			public Encoding Encoding {
				get; private set;
			}
		}

		static void FillViewers (CustomCommonFileDialogComboBox combo, string fileName)
		{
			combo.Items.Clear ();

			if (String.IsNullOrEmpty (fileName) || Directory.Exists (fileName)) {
				combo.Enabled = false;
				return;
			}

			var projectService = IdeApp.Services.ProjectService;
			if (projectService.IsWorkspaceItemFile (fileName) || projectService.IsSolutionItemFile (fileName))
				combo.Items.Add (new ViewerComboItem (null, GettextCatalog.GetString ("Solution Workbench")));

			foreach (var vw in DisplayBindingService.GetFileViewers (fileName, null))
				if (!vw.IsExternal)
					combo.Items.Add (new ViewerComboItem (vw, vw.Title));

			combo.Enabled = combo.Items.Count > 1;
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

