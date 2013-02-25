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
		int labelsWidth;
		
		const string EncodingText = "Encoding:";
		const string ViewerText = "Open with:";
		
		public CustomOpenFileDialog (FileDialog dialog, OpenFileDialogData data)
			: base (dialog)
		{
			Initialize (data);
			
			StartLocation = AddonWindowLocation.Bottom;

			// Use the classic dialogs, as the new ones (WPF based) can't handle child controls.
			if (data.ShowEncodingSelector || data.ShowViewerSelector) {
				dialog.AutoUpgradeEnabled = false;
			}
		}


		// We are required to compute the needed height to contain our controls
		// *before* the dialog is shown, thus we do it here as well as the vertical layout.
		// The X coords for our controls are set as soon as we get access to the native
		// dialog's controls, which happens to be when the OnShown event is fired.
		void Initialize (OpenFileDialogData data)
		{
			SuspendLayout ();

			int padding = 6;
			int y = padding;
			
			labelsWidth = GetMaxLabelWidth (data.ShowEncodingSelector, data.ShowViewerSelector);
			
			if (data.ShowEncodingSelector) {
				encodingLabel = new Label () {
					Text = GettextCatalog.GetString (EncodingText),
					Top = y,
					AutoSize = true
				};
				
				encodingBox = new EncodingBox (data.Action != Gtk.FileChooserAction.Save) {
					Top = y,
					SelectedEncodingId = data.Encoding != null ? data.Encoding.CodePage : 0,
				};
				encodingBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
				
				Controls.AddRange (new Control [] { encodingLabel, encodingBox });

				y += Math.Max (encodingLabel.Height, encodingBox.Height) + padding;
			}
			
			if (data.ShowViewerSelector && FileDialog is OpenFileDialog) {
				viewerLabel = new Label () {
					Text = GettextCatalog.GetString (ViewerText),
					Top = y,
					AutoSize = true
				};
				
				viewerBox = new ComboBox () {
					Top = y,
					DropDownStyle = ComboBoxStyle.DropDownList,
					Enabled = false
				};
				viewerBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
				
				y += Math.Max (viewerLabel.Height, viewerBox.Height) + padding;
				
				if (IdeApp.Workspace.IsOpen) {
					closeSolutionBox = new CheckBox () {
						Text = GettextCatalog.GetString ("Close current workspace"),
						Top = y,
						AutoSize = true,
						Checked = true,
						Enabled = false
					};
					
					y += closeSolutionBox.Height + padding;
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
			ClientSize = new Size (ClientSize.Width, y);
			
			ResumeLayout ();
		}
		
		public int SelectedEncodingId {
			get {
				return encodingBox == null ? 0 : encodingBox.SelectedEncodingId;
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

		protected override void OnShow (EventArgs args)
		{
			base.OnShow (args);
			HorizontalLayout ();
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

		// Align our label/comobox/checkbox objects with respect to the native dialog ones.
		void HorizontalLayout ()
		{
			var labelRect = FileNameLabelRect; // Native dialog's label for filename
			int labelsX = labelRect.X;

			var comboRect = FileNameComboRect; // Native dialog's combobox for filename
			int boxesX = comboRect.X;
			int boxesWidth = comboRect.Width;

			int hPadding = 5;

			if (labelsWidth + hPadding > labelRect.Width) { // Adjust our comboBox objects if needed
				boxesX = labelsX + (labelsWidth + hPadding);
				boxesWidth = comboRect.Right - boxesX;
			}

			if (encodingLabel != null) {
				encodingLabel.Left = labelsX;

				encodingBox.Left = boxesX;
				encodingBox.Width = boxesWidth;
			}

			if (viewerLabel != null) {
				viewerLabel.Left = labelsX;

				viewerBox.Left = boxesX;
				viewerBox.Width = boxesWidth;
			}

			if (closeSolutionBox != null)
				closeSolutionBox.Left = boxesX;
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
