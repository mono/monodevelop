
using System;

namespace MonoDevelop.CodeAnalysis
{
	[System.ComponentModel.Category("MonoDevelop.CodeAnalysis")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ViolationWidget : Gtk.Bin
	{
		public ViolationWidget()
		{
			this.Build();
		}
	}
}
