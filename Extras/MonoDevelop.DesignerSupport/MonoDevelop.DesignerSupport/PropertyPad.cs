
using System;

using MonoDevelop.Ide.Gui;

using MonoDevelop.DesignerSupport;
using pg = MonoDevelop.DesignerSupport.PropertyGrid;

namespace MonoDevelop.DesignerSupport
{
	
	public class PropertyPad : AbstractPadContent
	{
		pg.PropertyGrid grid;
		Gtk.VBox box;
		bool customWidget;
		
		public PropertyPad ()  : base ("")
		{
			DesignerSupport.Service.SetPropertyPad (this);
			DefaultPlacement = "right";
			
			grid = new pg.PropertyGrid ();
			box = new Gtk.VBox ();
			box.Child = grid;
			
			box.ShowAll ();
		}
		
		#region AbstractPadContent implementations
		
		public override Gtk.Widget Control {
			get { return box; }
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
					box.Child = grid;
				}
				
				return grid;
			}
		}
		
		public void UseCustomWidget (Gtk.Widget widget)
		{
			customWidget = true;
			
			box.Child = widget;
			widget.Show ();			
		}
		
		/*
		
		public void ConnectPlug (uint windowId)
		{
			if (!usingSocket) {
				usingSocket = true;
				
				socket = new Gtk.Socket ();
				socket.Destroyed += OnSocketDestroyed;
				box.Child = socket;
				socket.Show ();
				
				socket.AddId (windowId);
			}
		}
		
		void OnSocketDestroyed (object o, System.EventArgs e)
		{
			BlankPad ();
		}
		
		*/
	}
}
