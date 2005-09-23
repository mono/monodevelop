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
using MonoDevelop.Gui;
using MonoDevelop.Core.Properties;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Dialogs
{
	internal class TipOfTheDayWindow
	{
//		[Glade.Widget] Label categoryLabel;
		[Glade.Widget] TextView tipTextview;
		[Glade.Widget] CheckButton noshowCheckbutton;
		[Glade.Widget] Button nextButton;
		[Glade.Widget] Button closeButton;
		[Glade.Widget] Window tipOfTheDayWindow;

		string[] tips;
		int currentTip = 0;

		public TipOfTheDayWindow ()
		{
			Glade.XML totdXml = new Glade.XML (null, "Base.glade",
							   "tipOfTheDayWindow",
							   null);
			totdXml.Autoconnect (this);
					
			tipOfTheDayWindow.TypeHint = Gdk.WindowTypeHint.Dialog;

			noshowCheckbutton.Active = Runtime.Properties.GetProperty ("MonoDevelop.Gui.Dialog.TipOfTheDayView.ShowTipsAtStartup", false);
			noshowCheckbutton.Toggled += new EventHandler (OnNoshow);
			nextButton.Clicked += new EventHandler (OnNext);
			closeButton.Clicked += new EventHandler (OnClose);
			tipOfTheDayWindow.DeleteEvent += new DeleteEventHandler (OnDelete);

 			XmlDocument doc = new XmlDocument();
 			doc.Load (Runtime.Properties.DataDirectory +
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
			Runtime.Properties.SetProperty ("MonoDevelop.Gui.Dialog.TipOfTheDayView.ShowTipsAtStartup",
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
			tipOfTheDayWindow.Hide ();
			tipOfTheDayWindow.Dispose ();
		}

		void OnDelete (object obj, DeleteEventArgs args)
		{
			tipOfTheDayWindow.Hide ();
			tipOfTheDayWindow.Dispose ();
		}

		public void Show ()
		{
			tipOfTheDayWindow.TransientFor = (Window) WorkbenchSingleton.Workbench;
			tipOfTheDayWindow.WindowPosition = WindowPosition.CenterOnParent;
			tipOfTheDayWindow.ShowAll ();
		}
	}
}
