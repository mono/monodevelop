
using System;
using System.Collections;
using Monodoc;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui
{
	public class HelpOperations
	{
		HelpViewer helpViewer;

		public void ShowHelp (string topic)
		{
			if (helpViewer == null) {
				helpViewer = new HelpViewer ();
				helpViewer.LoadUrl (topic);
				IdeApp.Workbench.OpenDocument (helpViewer, true);		
				helpViewer.WorkbenchWindow.Closed += new EventHandler (CloseWindowEvent);
			} else {
				helpViewer.LoadUrl (topic);
				helpViewer.WorkbenchWindow.SelectWindow ();
			}
		}

		public void ShowDocs (string text, Node matched_node, string url)
		{
			if (helpViewer == null) {
				helpViewer = new HelpViewer ();
				helpViewer.Render (text, matched_node, url);
				IdeApp.Workbench.OpenDocument (helpViewer, true);
				helpViewer.WorkbenchWindow.Closed += new EventHandler (CloseWindowEvent);
			} else {
				helpViewer.Render (text, matched_node, url);
				helpViewer.WorkbenchWindow.SelectWindow ();
			}
		}

		void CloseWindowEvent (object sender, EventArgs e)
		{
			helpViewer = null;
		}
	}
}
