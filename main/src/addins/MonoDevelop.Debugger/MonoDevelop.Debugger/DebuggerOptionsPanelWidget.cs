// 
// DebuggerOptionsPanelWidget.cs
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
using MonoDevelop.Core.Gui.Dialogs;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public class DebuggerOptionsPanel: OptionsPanel
	{
		DebuggerOptionsPanelWidget w;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return w = new DebuggerOptionsPanelWidget ();
		}
		
		public override void ApplyChanges ()
		{
			w.Store ();
		}
	}

	[System.ComponentModel.ToolboxItem(true)]
	public partial class DebuggerOptionsPanelWidget : Gtk.Bin
	{
		Gtk.ListStore engineStore;
		DebuggerSessionOptions options;
		
		public DebuggerOptionsPanelWidget ()
		{
			this.Build ();
			options = DebuggingService.GetUserOptions ();
			projectCodeOnly.Active = options.ProjectAssembliesOnly;
			checkAllowEval.Active = options.EvaluationOptions.AllowTargetInvoke;
			checkToString.Active = options.EvaluationOptions.AllowToStringCalls;
			checkShowBaseGroup.Active = !options.EvaluationOptions.FlattenHierarchy;
			checkGroupPrivate.Active = options.EvaluationOptions.GroupPrivateMembers;
			checkGroupStatic.Active = options.EvaluationOptions.GroupStaticMembers;
			checkToString.Sensitive = checkAllowEval.Active;
			spinTimeout.Value = options.EvaluationOptions.EvaluationTimeout;
			
			// Debugger priorities
			engineStore = new Gtk.ListStore (typeof(string), typeof(string));
			engineList.Model = engineStore;
			engineList.AppendColumn ("", new Gtk.CellRendererText (), "text", 1);
			
			foreach (IDebuggerEngine engine in DebuggingService.GetDebuggerEngines ()) {
				engineStore.AppendValues (engine.Id, engine.Name);
			}
			UpdatePriorityButtons ();
			engineList.Selection.Changed += HandleEngineListSelectionChanged;
		}

		void HandleEngineListSelectionChanged (object sender, EventArgs e)
		{
			UpdatePriorityButtons ();
		}
		
		public void Store ()
		{
			EvaluationOptions ops = options.EvaluationOptions;
			ops.AllowTargetInvoke = checkAllowEval.Active;
			ops.AllowToStringCalls = checkToString.Active;
			ops.FlattenHierarchy = !checkShowBaseGroup.Active;
			ops.GroupPrivateMembers = checkGroupPrivate.Active;
			ops.GroupStaticMembers = checkGroupStatic.Active;
			int t = (int) spinTimeout.Value;
			ops.EvaluationTimeout = t;
			
			options.ProjectAssembliesOnly = projectCodeOnly.Active;
			options.EvaluationOptions = ops;
			
			DebuggingService.SetUserOptions (options);
			
			Gtk.TreeIter it;
			List<string> prios = new List<string> ();
			if (engineStore.GetIterFirst (out it)) {
				do {
					string id = (string) engineStore.GetValue (it, 0);
					prios.Add (id);
				} while (engineStore.IterNext (ref it));
			}
			DebuggingService.EnginePriority = prios.ToArray ();
		}
		
		protected virtual void OnCheckAllowEvalToggled (object sender, System.EventArgs e)
		{
			checkToString.Sensitive = checkAllowEval.Active;
		}
		
		void UpdatePriorityButtons ()
		{
			Gtk.TreePath[] paths = engineList.Selection.GetSelectedRows ();
			if (paths.Length > 0) {
				Gtk.TreePath p = paths [0];
				Gtk.TreeIter it;
				engineStore.GetIter (out it, p);
				buttonDown.Sensitive = engineStore.IterNext (ref it);
				buttonUp.Sensitive = p.Prev ();
			} else {
				buttonDown.Sensitive = buttonUp.Sensitive = false;
			}
		}
		
		protected virtual void OnButtonUpClicked (object sender, System.EventArgs e)
		{
			Gtk.TreePath[] paths = engineList.Selection.GetSelectedRows ();
			if (paths.Length > 0) {
				Gtk.TreePath p = paths [0];
				Gtk.TreeIter it1, it2;
				engineStore.GetIter (out it2, p);
				if (p.Prev () && engineStore.GetIter (out it1, p)) {
					engineStore.Swap (it1, it2);
					UpdatePriorityButtons ();
				}
			}
		}
		
		protected virtual void OnButtonDownClicked (object sender, System.EventArgs e)
		{
			Gtk.TreeIter i1;
			if (engineList.Selection.GetSelected (out i1)) {
				Gtk.TreeIter i2 = i1;
				if (engineStore.IterNext (ref i2)) {
					engineStore.Swap (i1, i2);
					UpdatePriorityButtons ();
				}
			}
		}
	}
}
