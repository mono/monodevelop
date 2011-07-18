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
		Label noContentLabel;
		Button addButton;
		VBox contentBox;
		string addMessage;
		string noContentMessage;
		
		public event EventHandler CreateNew;
		
		protected virtual void OnCreateNew (EventArgs e)
		{
			EventHandler handler = this.CreateNew;
			if (handler != null)
				handler (this, e);
		}

		public ExpanderList (string noContentMessage, string addMessage)
		{
			this.noContentMessage = noContentMessage;
			this.addMessage = addMessage;
			
			Clear ();
		}

		public void Clear ()
		{
			if (contentBox != null)
				contentBox.Destroy ();
			
			noContentLabel = new Label ();
			noContentLabel.Text = noContentMessage;
			
			addButton = new Button ();
			addButton.Label = addMessage;
			addButton.Relief = ReliefStyle.None;
			addButton.Clicked += delegate {
				OnCreateNew (EventArgs.Empty);
			};
			
			contentBox = new VBox ();
			contentBox.PackStart (this.noContentLabel, true, true, 6);
			contentBox.PackEnd (addButton, false, true, 0);
			
			PackStart (contentBox, true, true, 6);
			
			ShowAll ();
		}
		int expanders = 0;
		
		public ClosableExpander AddListItem (string name, Widget widget, PObject obj)
		{
			if (noContentLabel != null) {
				contentBox.Remove (noContentLabel);
				noContentLabel.Destroy ();
				noContentLabel = null;
			}
			
			var expander = new ClosableExpander ();
			expanders++;
			expander.ContentLabel = name;
			expander.SetWidget (widget);
			expander.BorderWidth = 4;
			expander.Closed += delegate(object sender, EventArgs e) {
				expanders--;
				var expanderWidget = (ClosableExpander)sender;
				obj.Remove ();
				contentBox.Remove (expanderWidget);
				expanderWidget.Destroy ();
				if (expanders == 0)
					Clear ();
			};
			contentBox.PackStart (expander, true, true, 0);
			contentBox.Show ();
			expander.Expanded = false;
			return expander;
		}
	}
}