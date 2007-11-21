// DeleteFileDialog.cs
//
// Author:
//   Ankit Jain  <jankit@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Ide.Gui.Dialogs
{
	
	
	public partial class DeleteFileDialog : Gtk.Dialog
	{
		
		public DeleteFileDialog(string question)
		{
			this.Build();
			this.QuestionLabel.Text = question;
		}

		public new bool Run ()
		{
			int response = base.Run ();
			Hide ();
			return (response == (int) Gtk.ResponseType.Ok);
		}

		protected virtual void OnYesButtonClicked(object sender, System.EventArgs e)
		{
			this.Respond (Gtk.ResponseType.Ok);
		}

		protected virtual void OnNoButtonClicked(object sender, System.EventArgs e)
		{
			this.Respond (Gtk.ResponseType.Cancel);
		}
		
		public bool DeleteFromDisk {
			get {
				return cbDeleteFromDisk.Active;
			}
		}
		
	}
}
