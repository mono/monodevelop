// 
// ImmediatePad.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Components.Docking;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	public class ImmediatePad: IPadContent
	{
		ConsoleView view;
		bool disposed;
		
		public void Initialize (IPadWindow container)
		{
			view = new ConsoleView ();
			view.ConsoleInput += OnViewConsoleInput;
			view.SetFont (IdeApp.Preferences.CustomOutputPadFont);
			view.ShadowType = Gtk.ShadowType.None;
			view.ShowAll ();

			IdeApp.Preferences.CustomOutputPadFontChanged += HandleCustomOutputPadFontChanged;
		}

		void HandleCustomOutputPadFontChanged (object sender, EventArgs e)
		{
			view.SetFont (IdeApp.Preferences.CustomOutputPadFont);
		}

		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			if (!DebuggingService.IsDebugging) {
				view.WriteOutput ("Debug session not started.");
			} else if (DebuggingService.IsRunning) {
				view.WriteOutput ("The expression can't be evaluated while the application is running.");
			} else {
				EvaluationOptions ops = EvaluationOptions.DefaultOptions;
				var frame = DebuggingService.CurrentFrame;
				string expression = e.Text;

				ops.AllowMethodEvaluation = true;
				ops.AllowToStringCalls = true;
				ops.AllowTargetInvoke = true;
				ops.EvaluationTimeout = 20000;
				ops.EllipsizeStrings = false;

				var vres = frame.ValidateExpression (expression, ops);
				if (!vres) {
					view.WriteOutput (vres.Message);
					view.Prompt (true);
					return;
				}

				var val = frame.GetExpressionValue (expression, ops);
				if (val.IsEvaluating) {
					WaitForCompleted (val);
					return;
				}

				PrintValue (val);
			}
			view.Prompt (true);
		}
		
		void PrintValue (ObjectValue val)
		{
			string result = val.Value;
			if (string.IsNullOrEmpty (result)) {
				if (val.IsNotSupported)
					result = GettextCatalog.GetString ("Expression not supported.");
				else if (val.IsError || val.IsUnknown)
					result = GettextCatalog.GetString ("Evaluation failed.");
				else
					result = string.Empty;
			}
			view.WriteOutput (result);
		}
		
		void WaitForCompleted (ObjectValue val)
		{
			int iteration = 0;
			
			GLib.Timeout.Add (100, delegate {
				if (!val.IsEvaluating) {
					if (iteration >= 5)
						view.WriteOutput ("\n");
					PrintValue (val);
					view.Prompt (true);
					return false;
				}
				if (++iteration == 5)
					view.WriteOutput (GettextCatalog.GetString ("Evaluating") + " ");
				else if (iteration > 5 && (iteration - 5) % 10 == 0)
					view.WriteOutput (".");
				else if (iteration > 300) {
					view.WriteOutput ("\n" + GettextCatalog.GetString ("Timed out."));
					view.Prompt (true);
					return false;
				}
				return true;
			});
		}

		public void RedrawContent ()
		{
		}
		
		public Gtk.Widget Control {
			get {
				return view;
			}
		}
		
		public void Dispose ()
		{
			if (!disposed) {
				IdeApp.Preferences.CustomOutputPadFontChanged -= HandleCustomOutputPadFontChanged;
				disposed = true;
			}
		}
	}
}
