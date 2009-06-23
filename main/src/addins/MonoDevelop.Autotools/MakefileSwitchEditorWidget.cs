// 
// MakefileSwitchEditorWidget.cs
//  
// Author:
//       Jérémie "Garuma" Laval <jeremie.laval@gmail.com>
// 
// Copyright (c) 2009 Jérémie "Garuma" Laval
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

namespace MonoDevelop.Autotools
{
	
	[System.ComponentModel.ToolboxItem(true)]
	public partial class MakefileSwitchEditorWidget : Gtk.Bin
	{
		ListStore store;
		TarballDeployTarget target;
		
		public MakefileSwitchEditorWidget(TarballDeployTarget target)
		{
			this.target = target;
			this.Build();
			InitTreeview ();
			store = new ListStore (typeof (string), typeof (string), typeof(string), typeof (Switch));
			itemTv.Model = store;
			
			foreach (Switch s in target.GetSwitches ()) {
				store.AppendValues (s.SwitchName, s.Define, s.HelpStr, s);
			}
			
			this.ShowAll ();
		}
		
		void InitTreeview ()
		{
			CellRendererText txt1 = new CellRendererText ();
			txt1.Editable = true;
			txt1.Edited += delegate(object o, EditedArgs args) {
				HandleEdited (o, args, 0);
			};
			
			CellRendererText txt2 = new CellRendererText ();
			txt2.Editable = true;
			txt2.Edited += delegate(object o, EditedArgs args) {
				HandleEdited (o, args, 1);
			};
			
			CellRendererText txt3 = new CellRendererText ();
			txt3.Editable = true;
			txt3.Edited += delegate(object o, EditedArgs args) {
				HandleEdited (o, args, 2);
			};
			
			itemTv.AppendColumn ("Name", txt1, "text", 0);
			itemTv.AppendColumn ("Define", txt2, "text", 1);
			itemTv.AppendColumn ("Help string", txt3, "text", 2);
		}

		void HandleEdited (object o, EditedArgs args, int column)
		{
			TreeIter iter;	
			
			store.GetIterFromString (out iter, args.Path);
			if (!store.IterIsValid (iter))
				return;
			
			string newText = args.NewText;
			if (column == 0)
				newText = Switch.EspaceSwitchName (newText);
			else if (column == 1)
				newText = Switch.EscapeSwitchDefine (newText);
			
			store.SetValue (iter, column, newText);
			
			Switch s = store.GetValue (iter, 3) as Switch;
			if (s != null)
				target.RemoveSwitch (s);
			
			s = new Switch (store.GetValue (iter, 0) as string,
			                store.GetValue (iter, 1) as string,
			                store.GetValue (iter, 2) as string);
			store.SetValue (iter, 3, s);
			target.AddSwitch (s);
		}
		
		protected virtual void OnAddBtnClicked (object sender, System.EventArgs e)
		{
			const string defaultValue = "NEW";
			store.AppendValues (defaultValue, defaultValue, defaultValue, null);
		}
		
		protected virtual void OnRemBtnClicked (object sender, System.EventArgs e)
		{
			TreeSelection selection;
			if ((selection = itemTv.Selection) == null)
				return;
			
			TreeIter iter;
			selection.GetSelected (out iter);
			if (!store.IterIsValid (iter))
				return;
			
			Switch s = store.GetValue (iter, 3) as Switch;
			if (s != null)
				target.RemoveSwitch (s);
			
			store.Remove (ref iter);
		}
		
	}
}
