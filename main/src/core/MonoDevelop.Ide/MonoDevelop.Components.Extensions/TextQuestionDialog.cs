// 
// TextQuestionDialog.cs
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
using Gtk;

namespace MonoDevelop.Components.Extensions
{
	public interface ITextQuestionDialogHandler : IDialogHandler<TextQuestionDialogData>
	{
	}
	
	public class TextQuestionDialogData : PlatformDialogData
	{
		public string Question { get; set; }
		public string Caption { get; set; }
		public string Value { get; set; }
		public bool IsPassword { get; set; }
	}
	
	public class TextQuestionDialog : PlatformDialog<TextQuestionDialogData>
	{
		public string Question {
			get { return data.Question; }
			set { data.Question = value; }
		}
		
		public string Caption {
			get { return data.Caption; }
			set { data.Caption = value; }
		}
		
		public string Value {
			get { return data.Value; }
			set { data.Value = value; }
		}
		
		public bool IsPassword {
			get { return data.IsPassword; }
			set { data.IsPassword = value; }
		}
		
		protected override bool RunDefault ()
		{
			Dialog md = null;
			try {
				md = new Dialog (Caption, TransientFor, DialogFlags.Modal | DialogFlags.DestroyWithParent) {
					HasSeparator = false,
					BorderWidth = 6,
				};
				
				var questionLabel = new Label (Question) {
					UseMarkup = true,
					Xalign = 0.0F,
				};
				md.VBox.PackStart (questionLabel, true, false, 6);
				
				var responseEntry = new Entry (Value ?? "") {
					Visibility = !IsPassword,
				};
				responseEntry.Activated += (sender, e) => {
					md.Respond (ResponseType.Ok);
				};
				md.VBox.PackStart (responseEntry, false, true, 6);
				
				md.AddActionWidget (new Button (Gtk.Stock.Cancel), ResponseType.Cancel);
				md.AddActionWidget (new Button (Gtk.Stock.Ok), ResponseType.Ok);

				md.DefaultResponse = ResponseType.Cancel;
				
				md.Child.ShowAll ();
				
				var response = (ResponseType) MonoDevelop.Ide.MessageService.RunCustomDialog (md, TransientFor);
				if (response == ResponseType.Ok) {
					Value = responseEntry.Text;
					return true;
				}
				
				return false;
			} finally {
				if (md != null)
					md.Destroy ();
			}
		}
	}
}

