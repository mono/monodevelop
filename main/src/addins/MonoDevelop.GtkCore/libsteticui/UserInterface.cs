
using System;

namespace Stetic
{
	internal class UserInterface
	{
		UserInterface()
		{
		}
		
		public static WidgetDesignerBackend CreateWidgetDesigner (Gtk.Container widget)
		{
			Stetic.Wrapper.Container wc = Stetic.Wrapper.Container.Lookup (widget);
			return CreateWidgetDesigner (widget, wc.DesignWidth, wc.DesignHeight);
		}
		
		public static WidgetDesignerBackend CreateWidgetDesigner (Gtk.Container widget, int designWidth, int designHeight)
		{
			return new WidgetDesignerBackend (widget, designWidth, designHeight);
		}
		
		public static ActionGroupDesignerBackend CreateActionGroupDesigner (ProjectBackend project, ActionGroupToolbar groupToolbar)
		{
			Editor.ActionGroupEditor agroupEditor = new Editor.ActionGroupEditor ();
			agroupEditor.Project = project;
			WidgetDesignerBackend groupDesign = new WidgetDesignerBackend (agroupEditor, -1, -1);
			
			groupToolbar.Bind (agroupEditor);
			
			return new ActionGroupDesignerBackend (groupDesign, agroupEditor, groupToolbar);
		}
	}
}
