// 
// OpenFileDialogHandler.cs
//  
// Author:
//       Carlos Alberto Cortez <calberto.cortez@gmail.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Windows.Forms;
using CustomControls.Controls;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Platform;

namespace MonoDevelop.Platform
{
	public class OpenFileDialogHandler : IOpenFileDialogHandler
	{		
		public bool Run (OpenFileDialogData data)
		{			
			var parentWindow = data.TransientFor ?? MessageService.RootWindow;
			FileDialog fileDlg = null;
			if (data.Action == Gtk.FileChooserAction.Open)
				fileDlg = new OpenFileDialog ();
			else
				fileDlg = new SaveFileDialog ();
			
			var dlg = new CustomOpenFileDialog (fileDlg, data);
				
			SelectFileDialogHandler.SetCommonFormProperties (data, dlg.FileDialog);
			
			using (dlg) {
                WinFormsRoot root = new WinFormsRoot ();
                if (dlg.ShowDialog (root) == DialogResult.Cancel) {
					parentWindow.Present ();
                    return false;
				}
	
				FilePath[] paths = new FilePath [fileDlg.FileNames.Length];
				for (int n = 0; n < fileDlg.FileNames.Length; n++)	
					paths [n] = fileDlg.FileNames [n];
				data.SelectedFiles = paths;
				
				if (dlg.SelectedEncodingId != null)
					data.Encoding = dlg.SelectedEncodingId;
				if (dlg.SelectedViewer != null) {
					data.SelectedViewer = dlg.SelectedViewer;
					data.CloseCurrentWorkspace = dlg.CloseCurrentWorkspace;
				}
			}
			
			parentWindow.Present ();
			return true;
		}
	}
}

