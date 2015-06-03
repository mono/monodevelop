// 
// AddFileDialog.cs
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
using MonoDevelop.Components.Extensions;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Components;
using Gtk;
using MonoDevelop.Core;


namespace MonoDevelop.Ide.Projects
{
	/// <summary>
	/// Dialog which allows selecting files to be added to a project
	/// </summary>
	public class AddFileDialog: SelectFileDialog<AddFileDialogData>
	{
		public AddFileDialog (string title)
		{
			Title = title;
			Action = FileChooserAction.Open;
			data.SelectMultiple = true;
		}
		
		/// <summary>
		/// Build actions from which the user can select the one to apply to the new file.
		/// </summary>
		public string[] BuildActions {
			get { return data.BuildActions; }
			set { data.BuildActions = value; }
		}
		
		/// <summary>
		/// Selected build action.
		/// </summary>
		public string OverrideAction {
			get { return data.OverrideAction; }
		}
		
		protected override bool RunDefault ()
		{
			FileSelector fdiag  = new FileSelector (data.Title);

			fdiag.ShowHidden = data.ShowHidden;
			
			//add a combo that can be used to override the default build action
			ComboBox combo = new ComboBox (data.BuildActions ?? new string[0]);
			combo.Sensitive = false;
			combo.Active = 0;
			combo.RowSeparatorFunc = delegate (TreeModel model, TreeIter iter) {
				return "--" == ((string) model.GetValue (iter, 0));
			};
			
			CheckButton check = new CheckButton (GettextCatalog.GetString ("Override default build action"));
			check.Toggled += delegate { combo.Sensitive = check.Active; };
			
			HBox box = new HBox ();
			fdiag.ExtraWidget = box;
			box.PackStart (check, false, false, 4);
			box.PackStart (combo, false, false, 4);
			box.ShowAll ();
			
			SetDefaultProperties (fdiag);
			
			int result;
			
			try {
				result = MessageService.RunCustomDialog (fdiag, data.TransientFor ?? MessageService.RootWindow);
				GetDefaultProperties (fdiag);
				if (check.Active)
					data.OverrideAction = combo.ActiveText;
				else
					data.OverrideAction = null;
				return result == (int) Gtk.ResponseType.Ok;
			} finally {
				fdiag.Destroy ();
			}
		}
	}
}
