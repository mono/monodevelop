// 
// CustomOpenFileDialog.cs
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CustomControls.Controls;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Platform;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{
	public class CustomOpenFileDialog : OpenFileDialogEx
	{
		Label encodingLabel;
		Label viewerLabel;
		EncodingBox encodingBox;
		ComboBox viewerBox;
		CheckBox closeSolutionBox;
		
		const string EncodingText = "Encoding:";
		const string ViewerText = "Open with:";
		
		const int DialogWidth = 460; // predefined/desired width
		
		public CustomOpenFileDialog (FileDialog dialog, OpenFileDialogData data)
			: base (dialog)
		{
			Initialize (data);
			
			StartLocation = AddonWindowLocation.Bottom;
		}
		
		void Initialize (OpenFileDialogData data)
		{
			SuspendLayout ();
			
			Point location = new Point (10, 5); // start location for controls.
			int padding = 5;
			int height = padding * 2; // initial/minimum height.
			
			int labelWidth = GetMaxLabelWidth (data.ShowEncodingSelector, data.ShowViewerSelector);
			
			if (data.ShowEncodingSelector) {
				encodingLabel = new Label () {
					Text = GettextCatalog.GetString (EncodingText),
					Location = location,
					AutoSize = true
				};
				
				encodingBox = new EncodingBox (data.Action != Gtk.FileChooserAction.Save) {
					Location = new Point (labelWidth + 20, location.Y),
					Width = DialogWidth - (labelWidth + 20 + padding),
					SelectedEncodingId = data.Encoding,
					Enabled = false
				};
				
				Controls.AddRange (new Control [] { encodingLabel, encodingBox });
								
				location.Y = encodingLabel.Bottom + padding;
				height += encodingBox.Height + padding;
			}
			
			if (data.ShowViewerSelector && FileDialog is OpenFileDialog) {
				viewerLabel = new Label () {
					Text = GettextCatalog.GetString (ViewerText),
					Location = location,
					AutoSize = true
				};
				
				viewerBox = new ComboBox () {
					Location = new Point (labelWidth + 20, location.Y),
					Width = DialogWidth - (labelWidth + 20 + padding),
					DropDownStyle = ComboBoxStyle.DropDownList,
					Enabled = false
				};
				
				location.Y = viewerBox.Bottom + padding;
				height += viewerBox.Height + padding;
				
				if (IdeApp.Workspace.IsOpen) {
					closeSolutionBox = new CheckBox () {
						Text = GettextCatalog.GetString ("Close current workspace"),
						Location = location,
						AutoSize = true,
						Checked = true,
						Enabled = false
					};
					
					height += closeSolutionBox.Height + padding;
				}
				
				if (encodingBox != null) {
					viewerBox.SelectedIndexChanged += delegate {
						int idx = viewerBox.SelectedIndex;
						encodingBox.Enabled = !(idx == 0 && currentViewers [0] == null);	
					};
				}
				
				Controls.AddRange (new Control [] { viewerLabel, viewerBox });
				if (closeSolutionBox != null)
					Controls.Add (closeSolutionBox);
			}
			
			AutoScaleDimensions = new SizeF (6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			Size = new Size (DialogWidth, height);
			
			ResumeLayout ();
		}
		
		public string SelectedEncodingId {
			get {
				return encodingBox == null ? null : encodingBox.SelectedEncodingId;
			}
		}
		
		public FileViewer SelectedViewer {
			get {
				if (viewerBox == null || currentViewers.Count == 0)
					return null;
				
				return currentViewers [viewerBox.SelectedIndex];
			}
		}
		
		public bool CloseCurrentWorkspace {
			get {
				return closeSolutionBox == null ? true : closeSolutionBox.Checked;
			}
		}
		
		List<FileViewer> currentViewers = new List<FileViewer> ();
		
		public override void OnFileNameChanged (string fileName)
		{			
			base.OnFileNameChanged (fileName);
			
			bool slnViewerSelected = false; // whether the selected file is a project/solution file
			
			if (viewerBox != null) {
				FillViewers (currentViewers, viewerBox, fileName);
				if (currentViewers.Count > 0 && currentViewers [0] == null)
					slnViewerSelected = true;
				
				if (closeSolutionBox != null)
					closeSolutionBox.Enabled = slnViewerSelected;
			}

			if (encodingBox != null)
				encodingBox.Enabled = !slnViewerSelected;
		}
		
		// Sort of ported from the MacSupport addin
		static void FillViewers (List<FileViewer> currentViewers, ComboBox viewerBox, string fileName)
		{
			currentViewers.Clear ();
			viewerBox.Items.Clear ();
			
			if (String.IsNullOrEmpty (fileName) || Directory.Exists (fileName)) {
				viewerBox.Enabled = false;
				return;
			}
			
			var projectService = IdeApp.Services.ProjectService;
			if (projectService.IsWorkspaceItemFile (fileName) || projectService.IsSolutionItemFile (fileName)) {
				viewerBox.Items.Add (GettextCatalog.GetString ("Solution Workbench"));
				currentViewers.Add (null);
			}
			
			foreach (var vw in DisplayBindingService.GetFileViewers (fileName, null)) {
				if (!vw.IsExternal) {
					viewerBox.Items.Add (vw.Title);
					currentViewers.Add (vw);
				}
			}
			
			viewerBox.Enabled = currentViewers.Count > 1;
			viewerBox.SelectedIndex = 0;
		}
		
		int GetMaxLabelWidth (bool showEncoding, bool showViewer)
		{
			if (!showEncoding && !showViewer)
				return 0;
			
			Graphics g = CreateGraphics ();
			int encodingWidth = 0;
			int viewerWidth = 0;
			
			if (showEncoding)
				encodingWidth = (int)g.MeasureString (GettextCatalog.GetString (EncodingText), Font).Width;
			
			if (showViewer)
				viewerWidth = (int)g.MeasureString (GettextCatalog.GetString (ViewerText), Font).Width;
			
			return Math.Max (encodingWidth, viewerWidth);
		}
	}
}
