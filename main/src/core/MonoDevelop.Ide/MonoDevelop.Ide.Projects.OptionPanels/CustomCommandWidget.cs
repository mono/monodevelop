// CustomCommandWidget.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class CustomCommandWidget : Gtk.Bin
	{
		CustomCommand cmd;
		IWorkspaceObject entry;
		bool updating;
		
		public CustomCommandWidget (IWorkspaceObject entry, CustomCommand cmd, ConfigurationSelector configSelector)
		{
			this.Build();
			this.cmd = cmd;
			if (cmd != null) {
				updating = true;
				comboType.RemoveText (0);
				updating = false;
			}
			
			this.entry = entry;
			UpdateControls ();
			this.WidgetFlags |= Gtk.WidgetFlags.NoShowAll;
			
			StringTagModelDescription tagModel;
			if (entry is SolutionItem)
				tagModel = ((SolutionItem)entry).GetStringTagModelDescription (configSelector);
			else if (entry is WorkspaceItem)
				tagModel = ((WorkspaceItem)entry).GetStringTagModelDescription ();
			else
				tagModel = new StringTagModelDescription ();

			tagSelectorDirectory.TagModel = tagModel;
			tagSelectorDirectory.TargetEntry = workingdirEntry;
			
			tagSelectorCommand.TagModel = tagModel;
			tagSelectorCommand.TargetEntry = entryCommand;
		}
		
		public CustomCommand CustomCommand {
			get { return cmd; }
		}
		
		void UpdateControls ()
		{
			updating = true;
			
			boxData.Visible = tableData.Visible = buttonRemove.Visible = (cmd != null);
				
			if (cmd == null) {
				comboType.Active = 0;
			}
			else {
				Array array = Enum.GetValues (typeof (CustomCommandType));
				comboType.Active = Array.IndexOf (array, cmd.Type);
				labelName.Visible = entryName.Visible = (cmd.Type == CustomCommandType.Custom);
				entryName.Text = cmd.Name ?? "";
				entryCommand.Text = cmd.Command ?? "";
				checkExternalCons.Active = cmd.ExternalConsole;
				checkPauseCons.Active = cmd.PauseExternalConsole;
				checkPauseCons.Sensitive = cmd.ExternalConsole;
				workingdirEntry.Text = cmd.WorkingDir ?? "";
			}
			updating = false;
		}

		protected virtual void OnButtonBrowseClicked(object sender, System.EventArgs e)
		{
			var dlg = new SelectFileDialog (GettextCatalog.GetString ("Select File")) {
				CurrentFolder = entry.BaseDirectory,
				SelectMultiple = false,
				TransientFor = this.Toplevel as Gtk.Window,
			};
			if (!dlg.Run ())
				return;
			if (System.IO.Path.IsPathRooted (dlg.SelectedFile))
				entryCommand.Text = FileService.AbsoluteToRelativePath (entry.BaseDirectory, dlg.SelectedFile);
			else
				entryCommand.Text = dlg.SelectedFile;
		}

		protected virtual void OnEntryCommandChanged(object sender, System.EventArgs e)
		{
			if (!updating)
				cmd.Command = entryCommand.Text;
		}

		protected virtual void OnEntryNameChanged(object sender, System.EventArgs e)
		{
			if (!updating)
				cmd.Name = entryName.Text;
		}

		protected virtual void OnComboTypeChanged(object sender, System.EventArgs e)
		{
			if (!updating) {
				if (cmd == null) {
					if (comboType.Active != 0) {
						// Selected a command type. Create the command now
						cmd = new CustomCommand ();
						cmd.Type = (CustomCommandType) (comboType.Active - 1);
						updating = true;
						comboType.RemoveText (0);
						updating = false;
						if (CommandCreated != null)
							CommandCreated (this, EventArgs.Empty);
					}
				} else
					cmd.Type = (CustomCommandType) (comboType.Active);
				UpdateControls ();
				if (cmd.Type == CustomCommandType.Custom)
					entryName.GrabFocus ();
				else
					entryCommand.GrabFocus ();
			}
		}

		protected virtual void OnCheckPauseConsClicked(object sender, System.EventArgs e)
		{
			if (!updating)
				cmd.PauseExternalConsole = checkPauseCons.Active;
		}

		protected virtual void OnCheckExternalConsClicked(object sender, System.EventArgs e)
		{
			if (!updating) {
				cmd.ExternalConsole = checkExternalCons.Active;
				UpdateControls ();
			}
		}

		protected virtual void OnButtonRemoveClicked(object sender, System.EventArgs e)
		{
			if (CommandRemoved != null)
				CommandRemoved (this, EventArgs.Empty);
		}
		
		protected virtual void OnWorkingdirEntryChanged (object sender, System.EventArgs e)
		{
			if (!updating) {
				cmd.WorkingDir = workingdirEntry.Text;
				UpdateControls ();
				workingdirEntry.GrabFocus ();
			}
		}

		public event EventHandler CommandCreated;
		public event EventHandler CommandRemoved;
	}
}
