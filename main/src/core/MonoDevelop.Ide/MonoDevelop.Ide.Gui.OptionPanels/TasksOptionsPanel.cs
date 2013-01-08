//
// TaskOptionsPanel.cs
//
// Author:
//   David Makovský <yakeen@sannyas-on.net>
//
// Copyright (C) 2006 David Makovský
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
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Ide.Tasks;
using Gtk;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	
	partial class TasksPanelWidget : Gtk.Bin
	{	
		ListStore tokensStore;
		ComboBox comboPriority;

		public TasksPanelWidget ()
		{
			Build ();
			
			comboPriority = ComboBox.NewText ();
			foreach (TaskPriority priority in Enum.GetValues (typeof (TaskPriority)))
				comboPriority.AppendText (Enum.GetName (typeof (TaskPriority), priority));
			comboPriority.Changed += new EventHandler (Validate);
			comboPriority.Show ();
			vboxPriority.PackEnd (comboPriority, false, false, 0);
			
			tokensStore = new ListStore (typeof (string), typeof (int));
			tokensTreeView.AppendColumn (String.Empty, new CellRendererText (), "text", 0);
			tokensTreeView.Selection.Changed += new EventHandler (OnTokenSelectionChanged);
			tokensTreeView.Model = tokensStore;
			
			OnTokenSelectionChanged (null, null);
			
			buttonAdd.Clicked += new EventHandler (AddToken);
			buttonChange.Clicked += new EventHandler (ChangeToken);
			buttonRemove.Clicked += new EventHandler (RemoveToken);
			entryToken.Changed += new EventHandler (Validate);
		}
		
		void Validate (object sender, EventArgs args)
		{
			// validate input, if found not allowed chars replace them with '_'
			string modified = String.Empty;
			foreach (char ch in entryToken.Text)
			{
				if (!Char.IsLetterOrDigit (ch) && ch != '\\')
				{
					modified += '_';
					continue;
				}
				modified += ch;
			}
			if (entryToken.Text != modified) entryToken.Text = modified;
			
			// look if we allready have this token 
			bool found = false;
			foreach (object[] row in tokensStore)
			{
				if (entryToken.Text == (string)row[0])
				{
					found = true;
					break;
				}
			}
			
			buttonAdd.Sensitive = (entryToken.Text.Length < 1) || found ? false : true;
			
			string selectedToken = String.Empty;
			int selectedPriority = (int)TaskPriority.Normal;
			TreeIter iter;
			TreeModel model = (TreeModel)tokensStore;
			if (tokensTreeView.Selection.GetSelected (out model, out iter)) {

				selectedToken = (string)tokensStore.GetValue (iter, 0);
				selectedPriority = (int)tokensStore.GetValue (iter, 1);
			}
			
			if (selectedToken != String.Empty)
			{
				buttonRemove.Sensitive = true;
				buttonChange.Sensitive = ((entryToken.Text.Length > 1) && (entryToken.Text != selectedToken) && !found)
										  || comboPriority.Active != selectedPriority ? true : false;
			} else
			{
				buttonRemove.Sensitive = buttonChange.Sensitive = false;
			}
		}
		
		void OnTokenSelectionChanged (object sender, EventArgs args)
		{
			TreeSelection selection = sender as TreeSelection;
			if (sender != null)
			{
				TreeIter iter;
				TreeModel model = (TreeModel)tokensStore;
				if (selection.GetSelected (out model, out iter)) {
					entryToken.Text = (string)tokensStore.GetValue (iter, 0);
					comboPriority.Active = (int)tokensStore.GetValue (iter, 1);
				} else
				{
					entryToken.Text = String.Empty;
					comboPriority.Active = (int)TaskPriority.Normal;
				}
			}
		}
		
		void AddToken (object sender, EventArgs args)
		{
			TreeIter iter = tokensStore.AppendValues (entryToken.Text, comboPriority.Active);
			tokensTreeView.Selection.SelectIter (iter);
			Validate (null, null);
		}
		
		void ChangeToken (object sender, EventArgs args)
		{
			TreeIter iter;
			TreeModel model = (TreeModel)tokensStore;
			if (tokensTreeView.Selection.GetSelected (out model, out iter)) {
    			tokensStore.SetValue (iter, 0, entryToken.Text);
    			tokensStore.SetValue (iter, 1, comboPriority.Active);
			}
			Validate (null, null);
		}
		
		void RemoveToken (object sender, EventArgs args)
		{
			TreeIter iter;
			TreeModel model = (TreeModel)tokensStore;
			if (tokensTreeView.Selection.GetSelected (out model, out iter)) {
			   	tokensStore.Remove (ref iter);
			}
			Validate (null, null);
		}
		
		public void Load ()
		{
			foreach (var ctag in CommentTag.SpecialCommentTags)
				tokensStore.AppendValues (ctag.Tag, ctag.Priority);
			
			colorbuttonHighPrio.Color = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksHighPrioColor", ""));
			colorbuttonNormalPrio.Color = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksNormalPrioColor", ""));
			colorbuttonLowPrio.Color = StringToColor ((string)PropertyService.Get ("Monodevelop.UserTasksLowPrioColor", ""));
		}
		
		public void Store ()
		{
			List<CommentTag> tags = new List<CommentTag> ();
			foreach (object[] row in tokensStore)
				tags.Add (new CommentTag ((string)row[0], (int)row[1]));

			CommentTag.SpecialCommentTags = tags;
			
			PropertyService.Set ("Monodevelop.UserTasksHighPrioColor", ColorToString (colorbuttonHighPrio.Color));
			PropertyService.Set ("Monodevelop.UserTasksNormalPrioColor", ColorToString (colorbuttonNormalPrio.Color));
			PropertyService.Set ("Monodevelop.UserTasksLowPrioColor", ColorToString (colorbuttonLowPrio.Color));
		}
		
		static string ColorToString (Gdk.Color color)
		{
			return color.ToString ();
		}
		
		static Gdk.Color StringToColor (string colorStr)
		{
			string[] rgb = colorStr.Substring (colorStr.IndexOf (':') + 1).Split ('/');
			if (rgb.Length != 3) return new Gdk.Color (0, 0, 0);
			Gdk.Color color = Gdk.Color.Zero;
			try
			{
				color.Red = UInt16.Parse (rgb[0], System.Globalization.NumberStyles.HexNumber);
				color.Green = UInt16.Parse (rgb[1], System.Globalization.NumberStyles.HexNumber);
				color.Blue = UInt16.Parse (rgb[2], System.Globalization.NumberStyles.HexNumber);
			}
			catch
			{
				// something went wrong, then use neutral black color
				color = new Gdk.Color (0, 0, 0);
			}
			return color;
		}
	}
	
	internal class TasksOptionsPanel : OptionsPanel
	{
		TasksPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new TasksPanelWidget ();
			widget.Load ();
			return widget;
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
