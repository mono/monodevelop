
using Gtk;
using System;
using Mono.Unix;

namespace Stetic
{
	public class DesignerView: Gtk.Notebook
	{
		WidgetDesigner design;
		Gtk.Widget actionbox;
		WidgetInfo widget;
		
		public DesignerView (Stetic.Project project, ProjectItemInfo item)
		{
			this.widget = (WidgetInfo) item;

			// Widget design tab
			
			design = project.CreateWidgetDesigner (widget, true);
			
			// Actions design tab
			
			actionbox = design.CreateActionGroupDesigner ();
			
			// Designers tab
			
			AppendPage (design, new Gtk.Label (Catalog.GetString ("Designer")));
			AppendPage (actionbox, new Gtk.Label (Catalog.GetString ("Actions")));
			TabPos = Gtk.PositionType.Bottom;
		}
		
		public ProjectItemInfo ProjectItem {
			get { return widget; }
		}
		
		public Component Component {
			get { return widget.Component; }
		}
		
		public WidgetDesigner Designer {
			get { return design; }
		}
		
		public UndoQueue UndoQueue {
			get { return design.UndoQueue; }
		}
		
		public override void Dispose ()
		{
			design.Dispose ();
			actionbox.Dispose ();
			base.Dispose ();
		}
	}
}
