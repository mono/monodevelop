// 
// AlertDialog.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Dialogs;
using System.Collections.Generic;

namespace MonoDevelop.Components.Extensions
{
	public interface IAlertDialogHandler : IDialogHandler<AlertDialogData>
	{
	}
	
	public class AlertDialogData : PlatformDialogData
	{
		public MessageDescription Message { get; internal set; }
		public bool ApplyToAll { get; set; }
		public AlertButton ResultButton { get; set; }
		
		public IList<AlertButton> Buttons { get { return Message.Buttons; } }
		public IList<AlertOption> Options { get { return Message.Options; } }
	}
	
	public class AlertDialog : PlatformDialog<AlertDialogData>
	{
		public AlertDialog (MessageDescription message)
		{
			data.Message = message;
		}
		
		public new AlertButton Run ()
		{
			if (data.Message.ApplyToAllButton != null)
				return data.Message.ApplyToAllButton;
			
			base.Run ();
			if (data.ApplyToAll)
				data.Message.ApplyToAllButton = data.ResultButton;
			
			return data.ResultButton;
		}
		
		protected override bool RunDefault ()
		{
			var alertDialog = new GtkAlertDialog (data.Message);
			alertDialog.FocusButton (data.Message.DefaultButton);
			MessageService.ShowCustomDialog (alertDialog, data.TransientFor);
			if (alertDialog.ApplyToAll)
				data.ApplyToAll = true;
			data.ResultButton = alertDialog.ResultButton;
			return true;
		}
	}
}

