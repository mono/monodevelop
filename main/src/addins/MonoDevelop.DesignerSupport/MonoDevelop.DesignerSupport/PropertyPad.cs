//
// PropertyPad.cs: The pad that holds the MD property grid. Can also 
//     hold custom grid widgets.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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

using MonoDevelop.Ide.Gui;

using MonoDevelop.DesignerSupport;
using pg = MonoDevelop.DesignerSupport.PropertyGrid;

namespace MonoDevelop.DesignerSupport
{
	
	public class PropertyPad : AbstractPadContent
	{
		pg.PropertyGrid grid;
		MonoDevelop.Components.InvisibleFrame frame;
		bool customWidget;
		
		public PropertyPad ()
		{
			grid = new pg.PropertyGrid ();
			frame = new MonoDevelop.Components.InvisibleFrame ();
			frame.Add (grid);
			
			frame.ShowAll ();
			DesignerSupport.Service.SetPad (this);
		}
		
		#region AbstractPadContent implementations
		
		public override Gtk.Widget Control {
			get { return frame; }
		}
		
		public override void Dispose()
		{
			DesignerSupport.Service.SetPad (null);
		}
		
		#endregion
		
		//Grid consumers must call this when they lose focus!
		public void BlankPad ()
		{
			PropertyGrid.CurrentObject = null;
		}
		
		internal pg.PropertyGrid PropertyGrid {
			get {
				if (customWidget) {
					customWidget = false;
					frame.Remove (frame.Child);
					frame.Add (grid);
				}
				
				return grid;
			}
		}
		
		internal void UseCustomWidget (Gtk.Widget widget)
		{
			customWidget = true;
			frame.Remove (frame.Child);
			frame.Add (widget);
			widget.Show ();			
		}
	}
}
