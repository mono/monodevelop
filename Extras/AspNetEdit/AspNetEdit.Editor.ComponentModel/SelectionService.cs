 /* 
 * SelectionService.cs -tracks selected components
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
using System.ComponentModel.Design;
using System.Collections;
using System.ComponentModel;

namespace AspNetEdit.Editor.ComponentModel
{
	public class SelectionService : ISelectionService
	{
		private ArrayList selections = new ArrayList ();
		private object primary;

		public bool GetComponentSelected (object component)
		{
			return selections.Contains (component);
		}

		public ICollection GetSelectedComponents ()
		{
			return (ArrayList.ReadOnly (selections) as ICollection);
		}

		public object PrimarySelection
		{
			get { return primary; }
		}

		public int SelectionCount
		{
			get { return selections.Count; }
		}

		//This is effectively .NET 2.0's SelectionTypes.Auto
		//It duplicates VS.NET's selection behaviour
		public void SetSelectedComponents (System.Collections.ICollection components)
		{
			OnSelectionChanging ();

			//use control and shift modifier to change selection
			//TODO: find a better way of checking key modifier status: this is BAD
			int x, y;
			bool modified = false;
			Gdk.Window someWindow = Gdk.Window.AtPointer (out x, out y);
			if (someWindow != null) {
				Gdk.ModifierType mt;
				someWindow.GetPointer (out x, out y, out mt);
				modified = ((mt & Gdk.ModifierType.ShiftMask) == Gdk.ModifierType.ShiftMask) || ((mt & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask);
			}
			
			if (components == null || components.Count == 0)
			{
				selections.Clear ();
				primary = null;
			}
			else {
				foreach (object comp in components) {
					if (! (comp is IComponent))
						throw new ArgumentException ("All elements in collection must be components");

					//what do we do with the component?
					if (!selections.Contains (comp))
					{
						//a simple replacement
						if (!modified) {
							selections.Clear ();
							selections.Add (comp);
						}
						//add to selection and make primary
						else
							selections.Add (comp);
						
						primary = comp;
					}
					else
					{
						//only deselect or change selection if other components
						//i.e. can't toggle selection status if is only component
						if (selections.Count > 1)
							//change primary selection
							if (!modified)
								primary = comp;
							//remove and replace primary selection
							else {
								selections.Remove (comp);
								if (selections.Count > 0)
									primary = selections[selections.Count - 1];
							}
					}				
				}
			}
			
			//fire event to let everyone know, especially PropertyGrid
			OnSelectionChanged();
		}

		public void SetSelectedComponents(System.Collections.ICollection components, SelectionTypes selectionType)
		{
			//TODO: Use .NET 2.0 SelectionTypes Primary, Add, Replace, Remove, Toggle
			if ((selectionType & SelectionTypes.Valid) == SelectionTypes.Valid)
				SetSelectedComponents(components);
		}

		public event EventHandler SelectionChanged;
		public event EventHandler SelectionChanging;

		protected void OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, EventArgs.Empty);
		}

		protected void OnSelectionChanging()
		{
			if (SelectionChanging != null)
				SelectionChanging(this, EventArgs.Empty);
		}
	}
}
