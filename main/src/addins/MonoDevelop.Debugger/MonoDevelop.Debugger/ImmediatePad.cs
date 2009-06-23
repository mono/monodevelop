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
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Debugger
{
	public class ImmediatePad: IPadContent
	{
		ConsoleView view;
		
		public ImmediatePad()
		{
			view = new ConsoleView ();
			view.ConsoleInput += OnViewConsoleInput;
			Pango.FontDescription font = Pango.FontDescription.FromString (DesktopService.DefaultMonospaceFont);
			font.Size = (font.Size * 8) / 10;
			view.SetFont (font);
		}

		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			if (!DebuggingService.IsDebugging) {
				view.WriteOutput ("Debug session not started.");
			} else if (DebuggingService.IsRunning) {
				view.WriteOutput ("The expression can't be evaluated while the application is running.");
			} else {
				ObjectValue val = DebuggingService.CurrentFrame.GetExpressionValue (e.Text, true, 20000);
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
			Console.WriteLine ("pp: " + val.Value + " " + val.Flags);
			string result = val.Value;
			if (string.IsNullOrEmpty (result)) {
				if (val.IsError || val.IsUnknown)
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

		public void Initialize (IPadWindow container)
		{
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
		}
	}
}
