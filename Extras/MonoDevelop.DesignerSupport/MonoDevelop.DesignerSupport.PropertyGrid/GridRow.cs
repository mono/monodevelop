/* 
 * GridRow.cs - Displays a row of a PropertyGrid 
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 * 	Michael Hutchinson <m.j.hutchinson@gmail.com>
 * 	Eric Butler <eric@extremeboredom.net>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 * Copyright (C) 2005 Eric Butler
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Reflection;
using Gtk;
using System.ComponentModel;

namespace AspNetEdit.UI
{
	public class GridRow
	{
		private PropertyGrid parent = null;
		private PropertyDescriptor propertyDescriptor = null;

		private EventBox propertyNameEventBox = null;
		internal Label propertyNameLabel = null;

		private EventBox propertyValueEventBox;
		private HBox propertyValueHBox;
		
		private bool isValidProperty = true;
		private bool editing = false;
		private bool selected = false;
		private object valueBeforeEdit;

		private PropertyEditors.BaseEditor editor;

		public GridRow (PropertyGrid parentGrid, PropertyDescriptor descriptor)
		{
			this.parent = parentGrid;
			this.propertyDescriptor = descriptor;

			// TODO: Need a better way to check if the property has no get accessor.
			// Write-only? Don't add this to the list.
			try {object propertyValue = descriptor.GetValue (parent.CurrentObject);}
			catch (Exception) {
				isValidProperty = false;
				return;
			}

			#region Name label
			
			string name = descriptor.DisplayName; 
			ParenthesizePropertyNameAttribute paren = descriptor.Attributes[typeof (ParenthesizePropertyNameAttribute)] as ParenthesizePropertyNameAttribute;
			if (paren != null && paren.NeedParenthesis)
				name = "(" + name + ")";
			
			propertyNameLabel = new Label (name);
			propertyNameLabel.Xalign = 0;
			propertyNameLabel.Xpad = 3;
			propertyNameLabel.HeightRequest = 20;

			propertyNameEventBox = new EventBox ();
			propertyNameEventBox.ModifyBg (StateType.Normal, parent.Style.White);
			propertyNameEventBox.Add (propertyNameLabel);
			propertyNameEventBox.ButtonReleaseEvent += on_label_ButtonRelease;

			if (propertyNameLabel.SizeRequest ().Width > 100) {
				propertyNameLabel.WidthRequest = 100;
				//TODO: Display tooltip of full name when truncated
				//parent.tooltips.SetTip (propertyNameLabel, descriptor.DisplayName, descriptor.DisplayName);
			}

			#endregion

			editor = parent.EditorManager.GetEditor(propertyDescriptor, this);

			propertyValueHBox = new HBox();

			//check if it needs a button. Note arrays are read-only but have buttons
			if (editor.DialogueEdit && (!propertyDescriptor.IsReadOnly || editor.EditsReadOnlyObject)) {
				Label buttonLabel = new Label ();
				buttonLabel.UseMarkup = true;
				buttonLabel.Xpad = 0; buttonLabel.Ypad = 0;
				buttonLabel.Markup = "<span size=\"small\">...</span>";
				Button dialogueButton = new Button (buttonLabel);
				dialogueButton.Clicked += new EventHandler (dialogueButton_Clicked);
				propertyValueHBox.PackEnd (dialogueButton, false, false, 0);
			}

			propertyValueEventBox = new EventBox ();
			propertyValueEventBox.ModifyBg (StateType.Normal, parent.Style.White);
			propertyValueEventBox.CanFocus = true;
			propertyValueEventBox.ButtonReleaseEvent += on_value_ButtonRelease;
			propertyValueEventBox.Focused += on_value_ButtonRelease;
			propertyValueHBox.PackStart (propertyValueEventBox, true, true, 0);

			valueBeforeEdit = propertyDescriptor.GetValue (parentGrid.CurrentObject);
			DisplayRenderWidget ();
		}

		private void on_label_ButtonRelease (object o, EventArgs e)
		{
			if (!selected)
				this.Selected = true;
		}

		private void on_value_ButtonRelease (object o, EventArgs e)
		{
			if (!selected)
				this.Selected = true;

			if (editor.InPlaceEdit && !propertyDescriptor.IsReadOnly)
				DisplayEditWidget ();
		}

		void dialogueButton_Clicked (object sender, EventArgs e)
		{
			if (!selected)
				this.Selected = true;

			editor.LaunchDialogue ();
		}

		public Widget LabelWidget {
			get { return propertyNameEventBox; }
		}

		public Widget ValueWidget {
			get { return propertyValueHBox; }
		}

		public PropertyGrid ParentGrid {
			get { return parent; }
		}

		public PropertyDescriptor PropertyDescriptor
		{
			get { return propertyDescriptor; }
		}

		#region Selection status

		public bool IsValidProperty
		{
			get { return isValidProperty; }
			set { isValidProperty = value; }
		}

		public bool Editing
		{
			get { return editing; }
			set { editing = value; }
		}

		public bool Selected
		{
			get { return selected; }
			set {
				// slightly complicated logic here to allow for selection to take
				// place from GridRow or PropertyGrid, and keep state in synch.
				// Checks parent has handled change. If not, calls change on parent
				// which then calls this again.
				if (value) {
					if (selected)
						return;

					if (parent.SelectedRow == this)
					{
						propertyNameEventBox.ModifyBg (StateType.Normal, parent.Style.Background (StateType.Selected));
						propertyNameLabel.ModifyFg (StateType.Normal, parent.Style.Text (StateType.Selected));

						parent.SetHelp(propertyDescriptor.DisplayName, propertyDescriptor.Description);
						selected = true;
					}
					else
						parent.SelectedRow = this; //fires other part of 'if', after setting parent's SelectedRow
				}
				else {
					if (!selected)
						return;

					if (parent.SelectedRow != this)	{
						propertyNameEventBox.ModifyBg (StateType.Normal, parent.Style.White);
						propertyNameLabel.ModifyFg (StateType.Normal, parent.Style.Foreground (StateType.Normal));

						if (editing)
							DisplayRenderWidget ();

						parent.ClearHelp ();
						selected = false;
					}
					else
						parent.SelectedRow = null;
				}
			}
		}

		#endregion

		//mainly intended for use by editors
		public object PropertyValue
		{
			get { return this.valueBeforeEdit; }
			set
			{
				if (value == this.valueBeforeEdit)
					return;
			
				//suppress conversion errors
				try
				{
					//try to set value
					propertyDescriptor.SetValue (parent.CurrentObject, value);
					
					//if successful, fire change event and set old value
					parent.OnPropertyValueChanged (this, this.valueBeforeEdit, value);
					this.valueBeforeEdit = value;
				}
				catch (ArgumentOutOfRangeException ex)
				{
					//out of range, restore old value
					propertyDescriptor.SetValue (parent.CurrentObject, this.valueBeforeEdit);
				}
				catch (Exception ex)
				{
					propertyDescriptor.SetValue(parent.CurrentObject, this.valueBeforeEdit);
					throw new Exception ("Error converting property " + propertyDescriptor.Name, ex);
				}

				//update display widget
				if (!editing)
					DisplayRenderWidget ();

				//TODO: Seems to lose keyboard input after value changed, despite still focussed. Fix?
				//else
				//	propertyValueEventBox.Child.GrabFocus();

			}
		}

		private void DisplayEditWidget()
		{
			if (propertyValueEventBox.Child != null)
				propertyValueEventBox.Child.Destroy ();
				
			if (!editing)
				editing = true;

			Widget editWidget = editor.GetEditWidget ();
			propertyValueEventBox.Child = editWidget;

			if (editWidget.SizeRequest ().Width > 50) {
				editWidget.WidthRequest = 50;
				//TODO: Display tooltip of full text when truncated
			}

			propertyValueEventBox.ShowAll ();
			editWidget.GrabFocus ();
		}

		private void DisplayRenderWidget ()
		{
			if (propertyValueEventBox.Child != null)
				propertyValueEventBox.Child.Destroy ();
				
			if (editing)
				editing = false;

			Widget displayWidget = editor.GetDisplayWidget ();
			propertyValueEventBox.Child = displayWidget;
			
			if (propertyValueEventBox.Child.SizeRequest ().Width > 100) {
				propertyValueEventBox.Child.WidthRequest = 100;
				//TODO: Display proper tooltip of full text when truncated
				//if (propertyValueEventBox.Child is Label)
				//	parent.tooltips.SetTip(propertyValueEventBox.Child, ((Label) propertyValueEventBox.Child).Text, string.Empty);
			}

			propertyValueEventBox.ShowAll ();
		}
	}
}
