
using System;
using System.Collections.Specialized;

namespace Stetic
{
	public class Configuration
	{
		// Main window position
		public int WindowX;
		public int WindowY;
		public int WindowWidth;
		public int WindowHeight;
		public Gdk.WindowState WindowState;
		
		public bool ShowNonContainerWarning = true;
		
		public StringCollection WidgetLibraries = new StringCollection ();
	}
}
