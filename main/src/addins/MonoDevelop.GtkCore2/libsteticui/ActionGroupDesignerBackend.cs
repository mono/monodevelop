
using System;

namespace Stetic
{
	internal class ActionGroupDesignerBackend: Gtk.VBox
	{
		Editor.ActionGroupEditor editor;
		ActionGroupToolbar toolbar;
		WidgetDesignerBackend groupDesign;
		
		internal ActionGroupDesignerBackend (WidgetDesignerBackend groupDesign, Editor.ActionGroupEditor editor, ActionGroupToolbar toolbar)
		{
			this.editor = editor;
			this.toolbar = toolbar;
			this.groupDesign = groupDesign;
			
			BorderWidth = 3;
			PackStart (toolbar, false, false, 0);
			PackStart (groupDesign, true, true, 3);
		}
		
		public Editor.ActionGroupEditor Editor {
			get { return editor; }
		}
		
		public ActionGroupToolbar Toolbar {
			get { return toolbar; }
		}
		
		public void UpdateObjectViewers ()
		{
			groupDesign.UpdateObjectViewers ();
		}
		
		public override void Dispose ()
		{
			this.editor = null;
			this.toolbar = null;
			this.groupDesign = null;
			base.Dispose ();
		}

	}
}
