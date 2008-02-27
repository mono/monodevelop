// WebDeployOptionsPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.AspNet;

namespace MonoDevelop.AspNet.Deployment
{
	
	public partial class WebDeployOptionsPanelWidget : Gtk.Bin
	{
		Gtk.ListStore targetList = new Gtk.ListStore (typeof (string), typeof(WebDeployTarget));
		const int LISTCOL_TEXT = 0;
		const int LISTCOL_TARGET = 1;
		
		//because MD doesn't use an instant-apply settings model, we have to make a local 
		//copy of the collection to amke sure that cancelling changes works
		WebDeployTargetCollection localCollection; 
		
		public WebDeployOptionsPanelWidget (AspNetAppProject project)
		{
			localCollection = project.WebDeployTargets.Clone ();
			
			//fill model and set it up
			this.Build ();
			foreach (WebDeployTarget target in localCollection) {
				targetList.AppendValues (target.GetMarkup (), target);
			}
			targetView.HeadersVisible = false;
			targetList.SetSortFunc (LISTCOL_TEXT, delegate (TreeModel m, TreeIter a, TreeIter b) {
				return string.Compare ((string) m.GetValue (a, LISTCOL_TEXT), (string) m.GetValue (b, LISTCOL_TEXT));
			});
			targetList.SetSortColumnId (LISTCOL_TEXT, SortType.Ascending);
			
			//set up the view
			targetView.Model = targetList;
			targetView.AppendColumn ("", new Gtk.CellRendererText (), "markup", LISTCOL_TEXT);			
			targetView.Selection.Changed += delegate (object sender, EventArgs e) {
				UpdateButtonState ();
			};
			
			UpdateButtonState ();
		}
		
		public void Store (AspNetAppProject project)
		{
			project.WebDeployTargets.Clear ();
			foreach (WebDeployTarget target in localCollection)
				project.WebDeployTargets.Add ((WebDeployTarget) target.Clone ());
		}
		
		protected virtual void AddActivated (object sender, System.EventArgs e)	
		{
			WebDeployTarget newTarget = new WebDeployTarget ();
			localCollection.Add (newTarget);
			TreeIter newIter = targetList.AppendValues (newTarget.GetMarkup(), newTarget);
			targetView.Selection.SelectIter (newIter);
			RunEditor (newTarget);
			UpdateTextForIter (newIter);
		}
		
		void UpdateTextForIter (TreeIter iter)
		{
			WebDeployTarget target = (WebDeployTarget) targetList.GetValue (iter, LISTCOL_TARGET);
			targetList.SetValue (iter, LISTCOL_TEXT, target.GetMarkup ());
		}
		
		protected virtual void RemoveActivated (object sender, System.EventArgs e)
		{
			TreeIter iter;
			if(targetView.Selection.GetSelected (out iter)) {
				WebDeployTarget targetToRemove = (WebDeployTarget) targetList.GetValue (iter, LISTCOL_TARGET);
				localCollection.Remove (targetToRemove);                                                                  
				targetList.Remove (ref iter);
			}
			
			if (targetList.IterIsValid (iter) || (localCollection.Count > 0 && targetList.IterNthChild (out iter, localCollection.Count - 1)))
				targetView.Selection.SelectIter (iter);
		}
		
		protected virtual void EditActivated (object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (targetView.Selection.GetSelected (out iter)) {
				WebDeployTarget target = (WebDeployTarget) targetList.GetValue (iter, LISTCOL_TARGET);
				RunEditor (target);
				UpdateTextForIter (iter);
			}
		}
		
		void UpdateButtonState ()
		{
			TreeIter iter;
			bool selected = targetView.Selection.GetSelected (out iter);
			editButton.Sensitive = selected;
			removeButton.Sensitive = selected;
		}
		
		Gtk.Window GetParentWindow (Gtk.Widget child)
		{
			Gtk.Widget widget = child;
			Gtk.Window window = null;
			do {
				window = widget as Gtk.Window;
				widget = widget.Parent;
			} while (window == null && widget != null);
			return window;
		}
		
		void RunEditor (WebDeployTarget target)
		{
			WebDeployTargetEditor targetEditor = new WebDeployTargetEditor ();
			targetEditor.Load (target);
			Gtk.Window parent = GetParentWindow (this);
			if (parent != null)
				targetEditor.TransientFor = parent;
			targetEditor.Show ();
			ResponseType result = (ResponseType) targetEditor.Run ();
			if (result == ResponseType.Ok)
				targetEditor.Save (target);
			targetEditor.Destroy ();
		}
	}
}
