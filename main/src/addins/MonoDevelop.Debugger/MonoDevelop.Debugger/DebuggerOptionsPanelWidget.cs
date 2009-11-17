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
using MonoDevelop.Core.Gui.Dialogs;

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
		public DebuggerOptionsPanelWidget ()
		{
			this.Build ();
			checkAllowEval.Active = DebuggingService.AllowTargetInvoke;
			spinTimeout.Value = DebuggingService.EvaluationTimeout;
			tableEval.Sensitive = checkAllowEval.Active;
		}
		
		public void Store ()
		{
			DebuggingService.AllowTargetInvoke = checkAllowEval.Active;
			int t = (int) spinTimeout.Value;
			DebuggingService.EvaluationTimeout = t;
		}
		
		protected virtual void OnCheckAllowEvalToggled (object sender, System.EventArgs e)
		{
			tableEval.Sensitive = checkAllowEval.Active;
		}
	}
}
