//  TipOfTheDay.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
 				tips[i] = StringParserService.Parse (nodes[i].InnerText);
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
