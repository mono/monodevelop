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
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class CustomCommandWidget : Gtk.Bin
	{
		CustomCommand cmd;
		IWorkspaceObject entry;
		bool updating;
		
		// snatched from MonoDevelop.Ide.Gui.OptionPanels/ExternalToolPanel.cs
		// a lot of these probably don't apply to custom build commands (e.g. ItemPath -- path of current open doc)
//		static string[,] argumentQuickInsertMenu = new string[,] {
//			{GettextCatalog.GetString ("Item Path"), "${ItemPath}"},
//			{GettextCatalog.GetString ("_Item Directory"), "${ItemDir}"},
//			{GettextCatalog.GetString ("Item file name"), "${ItemFileName}"},
//			{GettextCatalog.GetString ("Item extension"), "${ItemExt}"},
//			{"-", ""},
//			{GettextCatalog.GetString ("Current line"), "${CurLine}"},
//			{GettextCatalog.GetString ("Current column"), "${CurCol}"},
//			{GettextCatalog.GetString ("Current text"), "${CurText}"},
//			{"-", ""},
//			{GettextCatalog.GetString ("Target Path"), "${TargetPath}"},
//			{GettextCatalog.GetString ("_Target Directory"), "${TargetDir}"},
//			{GettextCatalog.GetString ("Target Name"), "${TargetName}"},
//			{GettextCatalog.GetString ("Target Extension"), "${TargetExt}"},
//			{"-", ""},
//			{GettextCatalog.GetString ("_Project Directory"), "${ProjectDir}"},
//			{GettextCatalog.GetString ("Project file name"), "${ProjectFileName}"},
//			{"-", ""},
//			{GettextCatalog.GetString ("_Solution Directory"), "${CombineDir}"},
//			{GettextCatalog.GetString ("Solution File Name"), "${CombineFileName}"},
//			{"-", ""},
//			{GettextCatalog.GetString ("MonoDevelop Startup Directory"), "${StartupPath}"},
//		};

		static string[,] projectWorkingDirInsertMenu = new string[,] {
			// Keep in sync with CustomCommand.cs
			{GettextCatalog.GetString ("_Target Directory"), "${TargetDir}"},
			{GettextCatalog.GetString ("Target _Name"), "${TargetName}"},
			{"-", ""},
			{GettextCatalog.GetString ("_Project Directory"), "${ProjectDir}"},
			{GettextCatalog.GetString ("P_roject Name"), "${ProjectName}"},
			{GettextCatalog.GetString ("Project _File"), "${ProjectFile}"},
			{"-", ""},
			{GettextCatalog.GetString ("_Solution Directory"), "${SolutionDir}"},
			{GettextCatalog.GetString ("So_lution Name"), "${SolutionName}"},
			{GettextCatalog.GetString ("Solution F_ile"), "${SolutionFile}"},
		};
		
		static string[,] entryWorkingDirInsertMenu = new string[,] {
			// Keep in sync with CustomCommand.cs
			{GettextCatalog.GetString ("Solution _Item Directory"), "${ItemDir}"},
			{GettextCatalog.GetString ("Solution Item _Name"), "${ItemName}"},
			{GettextCatalog.GetString ("Solution Item _File"), "${ItemFile}"},
			{"-", ""},
			{GettextCatalog.GetString ("_Solution Directory"), "${SolutionDir}"},
			{GettextCatalog.GetString ("So_lution Name"), "${SolutionName}"},
			{GettextCatalog.GetString ("Solution F_ile"), "${SolutionFile}"},
		};
		
		static string[,] solutionWorkingDirInsertMenu = new string[,] {
			// Keep in sync with CustomCommand.cs
			{GettextCatalog.GetString ("_Solution Directory"), "${SolutionDir}"},
			{GettextCatalog.GetString ("So_lution Name"), "${SolutionName}"},
		};
		
		public CustomCommandWidget (IWorkspaceObject entry, CustomCommand cmd)
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
			
			string[,] workingDirInsertMenu;
			if (entry is Project)
				workingDirInsertMenu = projectWorkingDirInsertMenu;
			else if (entry is SolutionEntityItem)
				workingDirInsertMenu = entryWorkingDirInsertMenu;
			else
				workingDirInsertMenu = solutionWorkingDirInsertMenu;
			
			new MenuButtonEntry (workingdirEntry, workingdirQuickInsertButton, workingDirInsertMenu);
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
				entryName.Text = cmd.Name;
				entryCommand.Text = cmd.Command;
				checkExternalCons.Active = cmd.ExternalConsole;
				checkPauseCons.Active = cmd.PauseExternalConsole;
				checkPauseCons.Sensitive = cmd.ExternalConsole;
				workingdirEntry.Text = cmd.WorkingDir;
			}
			updating = false;
		}

		protected virtual void OnButtonBrowseClicked(object sender, System.EventArgs e)
		{
			FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Select File"));
			try {
				fdiag.SetCurrentFolder (entry.BaseDirectory);
				fdiag.SelectMultiple = false;
				if (fdiag.Run () == (int) Gtk.ResponseType.Ok) {
					if (System.IO.Path.IsPathRooted (fdiag.Filename))
						entryCommand.Text = FileService.AbsoluteToRelativePath (entry.BaseDirectory, fdiag.Filename);
					else
						entryCommand.Text = fdiag.Filename;
				}
				fdiag.Hide ();
			} finally {
				fdiag.Destroy ();
			}
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
