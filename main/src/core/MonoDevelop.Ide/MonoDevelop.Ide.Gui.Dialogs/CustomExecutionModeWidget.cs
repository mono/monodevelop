// 
// CustomExecutionModeWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CustomExecutionModeWidget : Gtk.Bin, IExecutionModeEditor
	{
		List<IExecutionMode> modes;
		
		public CustomExecutionModeWidget ()
		{
			this.Build ();
		}
		
		#region IExecutionModeEditor implementation
		public void Load (CommandExecutionContext ctx, object data)
		{
			IExecutionMode curMode = null;
			if (data != null) {
				CustomArgsExecutionModeData cdata = (CustomArgsExecutionModeData) data;
				entryArgs.Text = cdata.Arguments;
				folderEntry.Path = cdata.WorkingDirectory;
				envVarList.LoadValues (cdata.EnvironmentVariables);
				curMode = ExecutionModeCommandService.GetExecutionMode (ctx, cdata.ModeId);
			}
			
			modes = new List<IExecutionMode> ();
			modes.Add (null); // The default mode;
			
			foreach (IExecutionMode m in ExecutionModeCommandService.GetExecutionModes (ctx))
				if (m.Id != "MonoDevelop.Ide.Execution.CustomArgsExecutionMode")
					modes.Add (m);
			
			foreach (IExecutionMode mode in modes) {
				if (mode == null)
					comboMode.AppendText (GettextCatalog.GetString ("Default"));
				else
					comboMode.AppendText (mode.Name);
			}
			
			comboMode.Active = modes.IndexOf (curMode);
		}
		
		public object Save ()
		{
			CustomArgsExecutionModeData cdata = new CustomArgsExecutionModeData ();
			cdata.Arguments = entryArgs.Text;
			cdata.WorkingDirectory = folderEntry.Path;
			envVarList.StoreValues (cdata.EnvironmentVariables);
			if (comboMode.Active != -1) {
				IExecutionMode mode = modes [comboMode.Active];
				if (mode != null)
					cdata.ModeId = mode.Id;
			}
			return cdata;
		}
		
		public Gtk.Widget Control {
			get {
				return this;
			}
		}
		#endregion
	}
}
