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
using System.Text;
using System.Threading;

using Gtk;
using Mono.Debugging.Client;
using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	public partial class ExceptionCaughtDialog : Gtk.Dialog
	{
		ExceptionInfo exception;
		bool destroyed;
		
		public ExceptionCaughtDialog (ExceptionInfo exception)
		{
			this.Build ();
			
			HasSeparator = false;
			
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
			
			valueView.ClearValues ();

			labelType.Markup = GettextCatalog.GetString ("<b>{0}</b> has been thrown", exception.Type);
			labelMessage.Text = string.IsNullOrEmpty (exception.Message)?
			                    string.Empty: 
			                    exception.Message;
			
			if (!exception.IsEvaluating) {
				StringBuilder stack = new StringBuilder ();
				ShowStackTrace (stack, exception);
				stackTextView.Buffer.Text = stack.ToString ();
				
				if (exception.Instance != null) {
					valueView.AddValue (exception.Instance);
					valueView.ExpandRow (new TreePath ("0"), false);
				}
			}
		}
		
		void ShowStackTrace (StringBuilder stack, ExceptionInfo ex)
		{
			ExceptionInfo inner = ex.InnerException;
			
			if (inner != null) {
				stack.AppendFormat ("{0}: {1} ---> ", ex.Type, ex.Message);
				ShowStackTrace (stack, inner);
				stack.AppendLine ("  --- End of inner exception stack trace ---");
			} else {
				stack.AppendFormat ("{0}: {1}\n", ex.Type, ex.Message);
			}
			
			foreach (ExceptionStackFrame frame in ex.StackTrace) {
				stack.Append ("  ");
				stack.AppendLine (frame.DisplayText);
			}
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

