
using System;

using MonoDevelop.Ide.Gui;

using MonoDevelop.DesignerSupport;
using tb = MonoDevelop.DesignerSupport.Toolbox;

namespace MonoDevelop.DesignerSupport
{
	
	public class ToolboxPad : AbstractPadContent
	{
		tb.Toolbox toolbox;
		
		public ToolboxPad ()  : base ("")
		{
			toolbox = new tb.Toolbox (DesignerSupport.Service.ToolboxService);
		}
		
		#region AbstractPadContent implementations
		
		public override Gtk.Widget Control {
			get { return toolbox; }
		}
		
		#endregion
	}
}
