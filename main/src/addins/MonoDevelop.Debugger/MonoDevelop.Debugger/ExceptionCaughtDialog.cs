// 
// ExceptionCaughtDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Debugging.Client;
using MonoDevelop.Core;
using Gtk;
using System.Threading;

namespace MonoDevelop.Debugger
{
	public partial class ExceptionCaughtDialog : Gtk.Dialog
	{
		Gtk.TreeStore stackStore;
		ExceptionInfo exception;
		bool destroyed;
		
		public ExceptionCaughtDialog (ExceptionInfo exception)
		{
			this.Build ();
			HasSeparator = false;
			
			stackStore = new TreeStore (typeof(string), typeof(string), typeof(int), typeof(int));
			treeStack.Model = stackStore;
			treeStack.AppendColumn ("", new CellRendererText (), "text", 0);
			treeStack.ShowExpanders = false;
			
			valueView.AllowExpanding = true;
			valueView.Frame = DebuggingService.CurrentFrame;
			this.exception = exception;
			
			exception.Changed += HandleExceptionChanged;
			
			Fill ();
		}

		void HandleExceptionChanged (object sender, EventArgs e)
		{
			Gtk.Application.Invoke (delegate {
				Fill ();
			});
		}
		
		void Fill ()
		{
			if (destroyed)
				return;
			
			stackStore.Clear ();
			valueView.ClearValues ();

			labelType.Markup = GettextCatalog.GetString ("<b>{0}</b> has been thrown", exception.Type);
			labelMessage.Text = exception.Message;
			
			ShowStackTrace (exception, false);
			
			valueView.AddValue (exception.Instance);
			valueView.ExpandRow (new TreePath ("0"), false);
		}
		
		void ShowStackTrace (ExceptionInfo exc, bool showExceptionNode)
		{
			TreeIter it = TreeIter.Zero;
			if (showExceptionNode) {
				treeStack.ShowExpanders = true;
				string tn = exc.Type + ": " + exc.Message;
				it = stackStore.AppendValues (tn, null, 0, 0);
			}

			foreach (ExceptionStackFrame frame in exc.StackTrace) {
				if (!it.Equals (TreeIter.Zero))
					stackStore.AppendValues (it, frame.DisplayText, frame.File, frame.Line, frame.Column);
				else
					stackStore.AppendValues (frame.DisplayText, frame.File, frame.Line, frame.Column);
			}
			
			ExceptionInfo inner = exc.InnerException;
			if (inner != null)
				ShowStackTrace (inner, true);
		}
		
		protected override bool OnDeleteEvent (Gdk.Event evnt)
		{
			Destroy ();
			return false;
		}
		
		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			Destroy ();
		}
		
		protected override void OnDestroyed ()
		{
			destroyed = true;
			exception.Changed -= HandleExceptionChanged;
			base.OnDestroyed ();
		}
		
	}
}

