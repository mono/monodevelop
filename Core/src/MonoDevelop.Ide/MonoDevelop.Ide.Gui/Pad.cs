

using System;
using System.Drawing;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui
{
	public class Pad
	{
		IPadWindow window;
		IWorkbench workbench;
		
		internal Pad (IWorkbench workbench, IPadContent content)
		{
			this.window = workbench.WorkbenchLayout.GetPadWindow (content);
			this.workbench = workbench;
		}
		
		public object Content {
			get { return window.Content; }
		}
		
		public string Title {
			get { return window.Title; }
		}
		
		public string Id {
			get { return window.Content.Id; }
		}
		
		public void BringToFront ()
		{
			workbench.BringToFront (window.Content);
		}
		
		public bool Visible {
			get {
				return workbench.WorkbenchLayout.IsVisible (window.Content);
			}
			set {
				if (value)
					workbench.WorkbenchLayout.ShowPad (window.Content);
				else
					workbench.WorkbenchLayout.HidePad (window.Content);
			}
		}
	}
}
