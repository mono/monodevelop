
using System;

using MonoDevelop.Ide.Gui;

using MonoDevelop.DesignerSupport;
using pg = MonoDevelop.DesignerSupport.PropertyGrid;

namespace MonoDevelop.DesignerSupport
{
	
	public class PropertyPad : AbstractPadContent
	{
		pg.PropertyGrid grid;
		Gtk.Frame frame;
		bool customWidget;
		
		public PropertyPad ()  : base ("")
		{
			DesignerSupport.Service.SetPropertyPad (this);
			DefaultPlacement = "right";
			
			grid = new pg.PropertyGrid ();
			frame = new Gtk.Frame ();
			frame.Add (grid);
			
			frame.ShowAll ();
		}
		
		#region AbstractPadContent implementations
		
		public override Gtk.Widget Control {
			get { return frame; }
		}
		
		public override void Dispose()
		{
			DesignerSupport.Service.SetPropertyPad (null);
		}
		
		#endregion
		
		//Grid consumers must call this when they lose focus!
		public void BlankPad ()
		{
			PropertyGrid.CurrentObject = null;
		}
		
		public pg.PropertyGrid PropertyGrid {
			get {
				if (customWidget) {
					customWidget = false;
					frame.Remove (frame.Child);
					frame.Add (grid);
				}
				
				return grid;
			}
		}
		
		public void UseCustomWidget (Gtk.Widget widget)
		{
			customWidget = true;
			frame.Remove (frame.Child);
			frame.Add (widget);
			widget.Show ();			
		}
	}
}
