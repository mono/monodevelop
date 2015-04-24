using System;
using Gtk;
using System.Reflection;
using System.Xml;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Dialogs
{
    internal partial class TipOfTheDayWindow : Gtk.Window
	{
        List<string> tips = new List<string> ();
        int currentTip;

        public TipOfTheDayWindow()
            : base (WindowType.Toplevel)
        {
            Build ();
            TransientFor = IdeApp.Workbench.RootWindow;

            if (IdeApp.Preferences.ShowTipsAtStartup)
                noshowCheckbutton.Active = true;

            XmlDocument xmlDocument = new XmlDocument ();
            xmlDocument.Load (System.IO.Path.Combine (System.IO.Path.Combine (PropertyService.DataPath, "options"), "TipsOfTheDay.xml"));

            foreach (XmlNode xmlNode in xmlDocument.DocumentElement.ChildNodes) {
                tips.Add (StringParserService.Parse (xmlNode.InnerText));
            }
           
            if (tips.Count != 0) 
                currentTip = new Random ().Next () % tips.Count;
            else
                currentTip = -1;

            tipTextview.Buffer.Clear ();
            if (currentTip != -1) 
                tipTextview.Buffer.InsertAtCursor (tips[currentTip]);

            noshowCheckbutton.Toggled += new EventHandler (OnNoShow);
            nextButton.Clicked += new EventHandler (OnNextClicked);
            closeButton.Clicked += new EventHandler (OnCloseClicked);
            DeleteEvent += new DeleteEventHandler (OnCloseClicked);
            
        }

        void OnNoShow (object o, EventArgs e)
        {
            IdeApp.Preferences.ShowTipsAtStartup.Value = noshowCheckbutton.Active;
        }

        void OnNextClicked (object o, EventArgs e)
        {
            if (tips.Count == 0)
                return;

            currentTip = currentTip + 1;
            if (currentTip >= tips.Count)
                currentTip = 0;

            tipTextview.Buffer.Clear ();
            tipTextview.Buffer.InsertAtCursor (tips[currentTip]);
        }

        void OnCloseClicked (object o, EventArgs e)
        {
            Hide ();
            Destroy ();
        }
	}

    class TipOfTheDayStartup : CommandHandler {
        protected override void Run ()
        {
			if (IdeApp.Preferences.ShowTipsAtStartup) {
                new TipOfTheDayWindow ().Show ();
            }
        }
    }
}
