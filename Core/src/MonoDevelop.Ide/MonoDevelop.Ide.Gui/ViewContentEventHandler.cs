// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1965 $</version>
// </file>

using System;

namespace MonoDevelop.Ide.Gui
{
	public delegate void ViewContentEventHandler (object sender, ViewContentEventArgs e);
	
	public class ViewContentEventArgs : System.EventArgs {
		IViewContent content;
		
		public IViewContent Content {
			get { return content; }
			set { content = value; }
		}
		
		public ViewContentEventArgs (IViewContent content)
		{
			this.content = content;
		}
	}
}
