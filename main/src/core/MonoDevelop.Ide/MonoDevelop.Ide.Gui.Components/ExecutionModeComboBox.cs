// 
// ExecutionModeComboBox.cs
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
using Gtk;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Core;


namespace MonoDevelop.Ide.Gui.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ExecutionModeComboBox : Gtk.Bin
	{
		List<IExecutionMode> modes = new List<IExecutionMode> ();
		
		public event EventHandler SelectionChanged;
		
		public ExecutionModeComboBox ()
		{
			this.Build ();
			
			comboMode.RowSeparatorFunc = delegate (TreeModel model, TreeIter iter) {
				string item = (string) comboMode.Model.GetValue (iter, 0);
				return item == "--";
			};
		}
		
		public delegate bool ExecutionModeIncludeFilter (IExecutionMode mode);
		
		public void Load (CommandExecutionContext ctx, bool includeDefault, bool includeDefaultCustomizer)
		{
			Load (ctx, includeDefault, includeDefaultCustomizer, null);
		}
		
		public void Load (CommandExecutionContext ctx, bool includeDefault, bool includeDefaultCustomizer, ExecutionModeIncludeFilter filter)
		{
			bool separate = false;
			foreach (List<IExecutionMode> modeList in ExecutionModeCommandService.GetExecutionModeCommands (ctx, includeDefault, includeDefaultCustomizer)) {
				bool addedSome = false;
				foreach (IExecutionMode mode in modeList) {
					if (filter == null || filter (mode)) {
						if (separate) {
							modes.Add (null);
							comboMode.AppendText ("--");
							separate = false;
						}
						modes.Add (mode);
						comboMode.AppendText (mode.Name);
						addedSome = true;
					}
				}
				separate = addedSome;
			}
		}

		protected virtual void OnComboModeChanged (object sender, System.EventArgs e)
		{
			if (SelectionChanged != null)
				SelectionChanged (this, e);
		}
		
		public bool IsDefaultSelected {
			get {
				return comboMode.Active != -1 && SelectedMode == null;
			}
		}
		
		public IExecutionMode SelectedMode {
			get {
				if (comboMode.Active == -1)
					return null;
				return modes [comboMode.Active];
			}
			set {
				if (value == null)
					comboMode.Active = 0;
				else
					comboMode.Active = modes.IndexOf (value);
			}
		}
	}
}
