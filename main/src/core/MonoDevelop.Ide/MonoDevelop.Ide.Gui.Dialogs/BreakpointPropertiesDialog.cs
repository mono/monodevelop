// BreakpointPropertiesDialog.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using Mono.Debugging.Client;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class BreakpointPropertiesDialog : Gtk.Dialog
	{
		Breakpoint bp;
		
		public BreakpointPropertiesDialog (Breakpoint bp, bool isNew)
		{
			this.Build();
			this.bp = bp;
			
			entryFile.Text = bp.FileName;
			entryLine.Text = bp.Line.ToString ();
			
			if (!isNew) {
				entryFile.IsEditable = false;
				entryLine.IsEditable = false;
				entryFile.ModifyBase (Gtk.StateType.Normal, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
				entryFile.ModifyBase (Gtk.StateType.Active, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
				entryLine.ModifyBase (Gtk.StateType.Normal, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
				entryLine.ModifyBase (Gtk.StateType.Active, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
			}
			
			if (string.IsNullOrEmpty (bp.ConditionExpression)) {
				radioBreakAlways.Active = true;
			} else {
				entryCondition.Text = bp.ConditionExpression;
				if (bp.BreakIfConditionChanges)
					radioBreakChange.Active = true;
				else
					radioBreakTrue.Active = true;
			}
			
			spinHitCount.Value = bp.HitCount;
			
			if (bp.HitAction == HitAction.Break)
				radioActionBreak.Active = true;
			else {
				radioActionTrace.Active = true;
				entryTraceExpr.Text = bp.TraceExpression;
			}
			
			UpdateControls ();
		}
		
		void UpdateControls ()
		{
			boxTraceExpression.Sensitive = radioActionTrace.Active;
			boxCondition.Sensitive = !radioBreakAlways.Active;
		}
		
		public bool Check ()
		{
			if (!radioBreakAlways.Active && entryCondition.Text.Length == 0) {
				MessageService.ShowError (GettextCatalog.GetString ("Condition expression not specified"));
				return false;
			}
			if (radioActionTrace.Active && entryTraceExpr.Text.Length == 0) {
				MessageService.ShowError (GettextCatalog.GetString ("Trace expression not specified"));
				return false;
			}
			return true;
		}
		
		public void Save ()
		{
			if (!radioBreakAlways.Active) {
				bp.ConditionExpression = entryCondition.Text;
				bp.BreakIfConditionChanges = radioBreakChange.Active;
			} else
				bp.ConditionExpression = null;
			
			bp.HitCount = (int) spinHitCount.Value;
			
			if (radioActionBreak.Active)
				bp.HitAction = HitAction.Break;
			else {
				bp.HitAction = HitAction.PrintExpression;
				bp.TraceExpression = entryTraceExpr.Text;
			}
			bp.CommitChanges ();
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if (Check ()) {
				Save ();
				Respond (Gtk.ResponseType.Ok);
			}
		}

		protected virtual void OnRadioBreakAlwaysToggled (object sender, System.EventArgs e)
		{
			UpdateControls ();
		}

		protected virtual void OnRadioActionBreakToggled (object sender, System.EventArgs e)
		{
			UpdateControls ();
		}
	}
}
