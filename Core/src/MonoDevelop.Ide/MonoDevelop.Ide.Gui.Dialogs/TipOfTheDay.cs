// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃÂ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Xml;

using Gtk;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class TipOfTheDayWindow: Gtk.Window
	{
		string[] tips;
		int currentTip = 0;

		public TipOfTheDayWindow (): base (WindowType.Toplevel)
		{
			Build ();
			TransientFor = IdeApp.Workbench.RootWindow;

			noshowCheckbutton.Active = PropertyService.Get ("MonoDevelop.Core.Gui.Dialog.TipOfTheDayView.ShowTipsAtStartup", false);
			noshowCheckbutton.Toggled += new EventHandler (OnNoshow);
			nextButton.Clicked += new EventHandler (OnNext);
			closeButton.Clicked += new EventHandler (OnClose);
			DeleteEvent += new DeleteEventHandler (OnDelete);

 			XmlDocument doc = new XmlDocument();
 			doc.Load (PropertyService.DataPath +
				  System.IO.Path.DirectorySeparatorChar + "options" +
				  System.IO.Path.DirectorySeparatorChar + "TipsOfTheDay.xml");
			ParseTips (doc.DocumentElement);
			
			tipTextview.Buffer.Clear ();
			tipTextview.Buffer.InsertAtCursor (tips[currentTip]);
		}

		private void ParseTips (XmlElement el)
		{
 			XmlNodeList nodes = el.ChildNodes;
 			tips = new string[nodes.Count];
			
 			for (int i = 0; i < nodes.Count; i++) {
 				tips[i] = Runtime.StringParserService.Parse (nodes[i].InnerText);
 			}
			
 			currentTip = (new Random ().Next ()) % nodes.Count;
		}

		void OnNoshow (object obj, EventArgs args)
		{
			PropertyService.Set ("MonoDevelop.Core.Gui.Dialog.TipOfTheDayView.ShowTipsAtStartup",
						    noshowCheckbutton.Active);
		}

		void OnNext (object obj, EventArgs args)
		{
			tipTextview.Buffer.Clear ();
			currentTip = ++currentTip % tips.Length;
			tipTextview.Buffer.InsertAtCursor (tips[currentTip]);
		}

		void OnClose (object obj, EventArgs args)
		{
			Hide ();
			Destroy ();
		}

		void OnDelete (object obj, DeleteEventArgs args)
		{
			Hide ();
			Destroy ();
		}
	}
}
