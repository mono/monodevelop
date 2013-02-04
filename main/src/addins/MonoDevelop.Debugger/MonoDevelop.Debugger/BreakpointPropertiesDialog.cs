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
using System.Collections.Generic;

using MonoDevelop.Core;
using Mono.Debugging.Client;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	public partial class BreakpointPropertiesDialog : Gtk.Dialog
	{
		string[] parsedParamTypes;
		string parsedFunction;
		Breakpoint bp;
		bool isNew;
		
		public BreakpointPropertiesDialog (Breakpoint bp, bool isNew)
		{
			this.Build ();
			
			this.isNew = isNew;
			this.bp = bp;
			
			spinColumn.Adjustment.Upper = int.MaxValue;
			spinColumn.Adjustment.Lower = 1;
			spinLine.Adjustment.Upper = int.MaxValue;
			spinLine.Adjustment.Lower = 1;
			
			if (bp is FunctionBreakpoint) {
				FunctionBreakpoint fb = (FunctionBreakpoint) bp;
				
				labelFileFunction.LabelProp = GettextCatalog.GetString ("Function:");
				
				if (fb.ParamTypes != null) {
					// FIXME: support non-C# syntax based on fb.Language
					entryFileFunction.Text = fb.FunctionName + " (" + string.Join (", ", fb.ParamTypes) + ")";
				} else
					entryFileFunction.Text = fb.FunctionName;
				
				if (!isNew) {
					// We don't use ".Sensitive = false" because we want the user to be able to select & copy the text.
					entryFileFunction.ModifyBase (Gtk.StateType.Normal, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
					entryFileFunction.ModifyBase (Gtk.StateType.Active, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
					entryFileFunction.IsEditable = false;
				}
				
				// Function breakpoints only support breaking on the first line
				hboxLineColumn.Destroy ();
				labelLine.Destroy ();
				table1.NRows--;
			} else {
				labelFileFunction.LabelProp = GettextCatalog.GetString ("File:");
				entryFileFunction.Text = ((Breakpoint) bp).FileName;
				
				// We don't use ".Sensitive = false" because we want the user to be able to select & copy the text.
				entryFileFunction.ModifyBase (Gtk.StateType.Normal, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
				entryFileFunction.ModifyBase (Gtk.StateType.Active, Style.Backgrounds [(int)Gtk.StateType.Insensitive]);
				entryFileFunction.IsEditable = false;
				
				spinColumn.Value = bp.Column;
				spinLine.Value = bp.Line;
				
				if (!isNew) {
					spinColumn.IsEditable = false;
					spinColumn.Sensitive = false;
					spinLine.IsEditable = false;
					spinLine.Sensitive = false;
				}
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
			
			Project project = null;
			if (!string.IsNullOrEmpty (bp.FileName))
				project = IdeApp.Workspace.GetProjectContainingFile (bp.FileName);
			
			if (project != null) {
				// Check the startup project of the solution too, since the current project may be a library
				SolutionEntityItem startup = project.ParentSolution.StartupItem;
				boxConditionOptions.Sensitive = DebuggingService.IsFeatureSupported (project, DebuggerFeatures.ConditionalBreakpoints) ||
					DebuggingService.IsFeatureSupported (startup, DebuggerFeatures.ConditionalBreakpoints);
				boxAction.Sensitive = DebuggingService.IsFeatureSupported (project, DebuggerFeatures.Tracepoints) ||
					DebuggingService.IsFeatureSupported (startup, DebuggerFeatures.Tracepoints);
			}
			
			UpdateControls ();
		}
		
		void UpdateControls ()
		{
			boxTraceExpression.Sensitive = radioActionTrace.Active;
			boxCondition.Sensitive = !radioBreakAlways.Active;
		}
		
		static bool TryParseFunction (string signature, out string function, out string[] paramTypes)
		{
			int paramListStart = signature.IndexOf ('(');
			int paramListEnd = signature.IndexOf (')');

			if (paramListStart == -1 && paramListEnd == -1) {
				function = signature;
				paramTypes = null;
				return true;
			}

			if (paramListEnd != signature.Length - 1) {
				paramTypes = null;
				function = null;
				return false;
			}
			
			function = signature.Substring (0, paramListStart).Trim ();
			
			paramListStart++;

			if (!FunctionBreakpoint.TryParseParameters (signature, paramListStart, paramListEnd, out paramTypes)) {
				paramTypes = null;
				function = null;
				return false;
			}
			
			return true;
		}
		
		public bool Check ()
		{
			if (bp is FunctionBreakpoint) {
				string text = entryFileFunction.Text.Trim ();

				if (text.Length == 0) {
					MessageService.ShowError (GettextCatalog.GetString ("Function name not specified"));
					return false;
				}

				if (!TryParseFunction (text, out parsedFunction, out parsedParamTypes)) {
					MessageService.ShowError (GettextCatalog.GetString ("Invalid function syntax"));
					return false;
				}
			}
			
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
			if (isNew) {
				if (bp is FunctionBreakpoint) {
					FunctionBreakpoint fb = (FunctionBreakpoint) bp;
					
					fb.FunctionName = parsedFunction;
					fb.ParamTypes = parsedParamTypes;
				} else {
					bp.SetColumn ((int) spinColumn.Value);
					bp.SetLine ((int) spinLine.Value);
				}
			}
			
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
