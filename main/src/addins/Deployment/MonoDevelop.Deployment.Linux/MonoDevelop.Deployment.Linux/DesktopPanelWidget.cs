
using System;

namespace MonoDevelop.Deployment.Linux
{
	
	
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class DesktopPanelWidget : Gtk.Bin
	{
		
		public DesktopPanelWidget()
		{
			this.Build();
		}
	}
}
