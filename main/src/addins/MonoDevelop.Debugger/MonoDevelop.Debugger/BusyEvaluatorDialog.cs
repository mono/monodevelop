// 
// BusyEvaluatorDialog.cs
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
using Mono.Debugging.Client;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	class BusyEvaluator
	{
		public BusyEvaluatorDialog Dialog {
			get;
			private set;
		}

		public void UpdateBusyState (BusyStateEventArgs args)
		{
			if (!args.IsBusy) {
				if (Dialog != null) {
					Dialog.Destroy ();
					Dialog.Dispose ();
					Dialog = null;
				}
			} else {
				if (Dialog == null)
					Dialog = new BusyEvaluatorDialog ();
				
				Dialog.Show ();
			}
		}
	}

	public partial class BusyEvaluatorDialog : Gtk.Dialog
	{
		public BusyEvaluatorDialog ()
		{
			this.Build ();
			//Prevent closing dialog via X button, user can either wait with
			//dialog open or press "Stop Debugger" button
			DeleteEvent += (o, args) => args.RetVal = true;
			Modal = true;
			TransientFor = MessageService.RootWindow;
			DestroyWithParent = true;
		}

		protected virtual void OnButtonCancelClicked (object sender, System.EventArgs e)
		{
			Hide ();
			DebuggingService.Stop ();
		}
	}
}
