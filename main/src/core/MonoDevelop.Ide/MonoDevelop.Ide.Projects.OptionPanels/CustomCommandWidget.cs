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
using MonoDevelop.Components.AtkCocoaHelper;

using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class CustomCommandWidget : Gtk.Bin
	{
		CustomCommand cmd;
		WorkspaceObject entry;
		bool updating;
		CustomCommandType[] supportedTypes;
		
		string[] commandNames = {
			GettextCatalog.GetString ("Before Build"),
			GettextCatalog.GetString ("Build"),
			GettextCatalog.GetString ("After Build"),
			GettextCatalog.GetString ("Before Execute"),
			GettextCatalog.GetString ("Execute"),
			GettextCatalog.GetString ("After Execute"),
			GettextCatalog.GetString ("Before Clean"),
			GettextCatalog.GetString ("Clean"),
			GettextCatalog.GetString ("After Clean"),
			GettextCatalog.GetString ("Custom Command")
		};
		
		public CustomCommandWidget (WorkspaceObject entry, CustomCommand cmd, ConfigurationSelector configSelector, CustomCommandType[] supportedTypes)
		{
			this.Build();

			// Turn these off so that their children can be focused
			CanFocus = false;
			vbox1.CanFocus = false;
			// Turn this off because otherwise it creates a keyboard trap and the focus cannot move off it.
			comboType.CanFocus = false;

			this.supportedTypes = supportedTypes;
			this.cmd = cmd;
			
			updating = true;
			
			if (cmd == null)
				comboType.AppendText (GettextCatalog.GetString ("(Select a project operation)"));
			
			foreach (var ct in supportedTypes)
				comboType.AppendText (commandNames [(int)ct]);
			
			updating = false;
			
			this.entry = entry;
			UpdateControls ();
			this.WidgetFlags |= Gtk.WidgetFlags.NoShowAll;
			
			StringTagModelDescription tagModel;
			if (entry is SolutionFolderItem)
				tagModel = ((SolutionFolderItem)entry).GetStringTagModelDescription (configSelector);
			else if (entry is WorkspaceItem)
				tagModel = ((WorkspaceItem)entry).GetStringTagModelDescription ();
			else
				tagModel = new StringTagModelDescription ();

			tagSelectorDirectory.TagModel = tagModel;
			tagSelectorDirectory.TargetEntry = workingdirEntry;
			tagSelectorDirectory.ButtonAccessible.SetCommonAttributes ("CustomCommand.TagSelectorDirectory",
										  							   GettextCatalog.GetString ("Tag Selector"),
																	   GettextCatalog.GetString ("Insert a custom tag into the directory entry"));
			
			tagSelectorCommand.TagModel = tagModel;
			tagSelectorCommand.TargetEntry = entryCommand;
			tagSelectorCommand.ButtonAccessible.SetCommonAttributes ("CustomCommand.TagSelector",
				                                                     GettextCatalog.GetString ("Tag Selector"),
					                                                 GettextCatalog.GetString ("Insert a custom tag into the command entry"));

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			comboType.SetCommonAccessibilityAttributes ("CustomCommands.OperationType",
														GettextCatalog.GetString ("Select a project operation"),
														GettextCatalog.GetString ("Select the type of project operation to add a custom command for"));
			buttonRemove.SetCommonAccessibilityAttributes ("CustomCommands.Remove", null, GettextCatalog.GetString ("Click to remove this custom command"));

			entryCommand.SetCommonAccessibilityAttributes ("CustomCommand.CommandEntry", GettextCatalog.GetString ("Command"),
			                                               GettextCatalog.GetString ("Enter the custom command"));
			entryCommand.SetAccessibilityLabelRelationship (label3);
			buttonBrowse.SetCommonAccessibilityAttributes ("CustomCommand.CommandBrowse", "", GettextCatalog.GetString ("Use a file selector to select a custom command"));

			entryName.SetCommonAccessibilityAttributes ("CustomCommands.WorkingDirectory",
			                                            GettextCatalog.GetString ("Working Directory"),
			                                            GettextCatalog.GetString ("Enter the directory for the command to execute in"));
			entryName.SetAccessibilityLabelRelationship (label1);

			checkExternalCons.SetCommonAccessibilityAttributes ("CustomCommands.RunOnExtConsole", null, GettextCatalog.GetString ("Check for the command to run on an external console"));
			checkPauseCons.SetCommonAccessibilityAttributes ("CustomCommands.Pause", null, GettextCatalog.GetString ("Check to pause the console output"));
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
				comboType.Active = Array.IndexOf (supportedTypes, cmd.Type);
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
			if (entryCommand.Text.IndexOf (' ') != -1)
				entryCommand.Text = '"' + entryCommand.Text + '"';
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
						cmd.Type = supportedTypes [comboType.Active - 1];
						updating = true;
						comboType.RemoveText (0);
						updating = false;
						if (CommandCreated != null)
							CommandCreated (this, EventArgs.Empty);
					}
				} else
					cmd.Type = supportedTypes [comboType.Active];
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
