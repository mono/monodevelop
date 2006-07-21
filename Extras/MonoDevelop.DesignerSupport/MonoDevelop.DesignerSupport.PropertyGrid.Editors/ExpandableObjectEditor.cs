/* 
 * ExpandableObjectEditor.cs - Temporary editor until we get expandable object support in main grid
 * 
 * Part of PropertyGrid - A Gtk# widget that displays and allows 
 * editing of all of an object's public properties 
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
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
using Gtk;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections;

namespace AspNetEdit.UI.PropertyEditors
{
	class ExpandableObjectEditor : BaseEditor
	{
		private PropertyGrid grid;

		public ExpandableObjectEditor (GridRow parentRow)
			: base (parentRow)
		{
		}

		public override bool InPlaceEdit
		{
			get { return false; }
		}

		public override bool DialogueEdit
		{
			get { return true; }
		}

		public override bool EditsReadOnlyObject {
			get { return true; }
		}
		
		public override void LaunchDialogue ()
		{
			//dialogue and buttons
			Dialog dialog = new Dialog ();
			dialog.Title = "Expandable Object Editor ";
			dialog.Modal = true;
			dialog.AllowGrow = true;
			dialog.AllowShrink = true;
			dialog.Modal = true;
			dialog.AddActionWidget (new Button (Stock.Cancel), ResponseType.Cancel);
			dialog.AddActionWidget (new Button (Stock.Ok), ResponseType.Ok);
			
			//propGrid
			grid = new PropertyGrid (parentRow.ParentGrid.EditorManager);
			grid.CurrentObject = parentRow.PropertyValue;
			grid.WidthRequest = 200;
			grid.ShowHelp = false;
			dialog.VBox.PackStart (grid, true, true, 5);

			//show and get response
			dialog.ShowAll ();
			ResponseType response = (ResponseType) dialog.Run();
			dialog.Destroy ();
			
			//if 'OK' put items back in collection
			if (response == ResponseType.Ok)
			{
			}
			
			//clean up so we start fresh if launched again
		}
	}
}
