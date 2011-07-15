// 
// ExpanderList.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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

namespace MonoDevelop.MacDev.PlistEditor
{
	public class ExpanderList : VBox
	{
		Label noContentLabel = new Label ();
		Button addButton = new Button ();
		VBox contentBox = new VBox ();
		
		public string NoContentMessage {
			get {
				return noContentLabel.Text;
			}
			set {
				noContentLabel.Text = value;
			}
		}
		
		public event EventHandler CreateNew;
		
		protected virtual void OnCreateNew (EventArgs e)
		{
			EventHandler handler = this.CreateNew;
			if (handler != null)
				handler (this, e);
		}

		public ExpanderList (string noContentLabel, string addMessage)
		{
			NoContentMessage = noContentLabel;
			
			contentBox.PackStart (this.noContentLabel, true, true, 6);
			
			addButton.Label = addMessage;
			addButton.Relief = ReliefStyle.None;
			addButton.Clicked += delegate {
				OnCreateNew (EventArgs.Empty);
			};
			contentBox.PackEnd (addButton, false, true, 0);
			
			PackStart (contentBox, true, true, 6);
			
			ShowAll ();
		}
		
		public ClosableExpander AddListItem (string name, Widget widget)
		{
			if (noContentLabel != null) {
				contentBox.Remove (noContentLabel);
				noContentLabel.Destroy ();
				noContentLabel = null;
			}
			
			var expander = new ClosableExpander ();
			expander.ContentLabel = name;
			expander.SetWidget (widget);
			expander.BorderWidth = 4;
			contentBox.PackStart (expander, true, true, 0);
			contentBox.Show ();
			expander.Expanded = false;
			return expander;
		}
	}
}
