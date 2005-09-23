using System;
using System.Collections;

using MonoDevelop.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui;

using Gtk;
using Vte;

namespace MonoDevelop.Gui.Pads
{
	public class TerminalPad : IPadContent
	{
		Frame frame = new Frame ();
		Terminal term;

		IProjectService projectService = (IProjectService) ServiceManager.Services.GetService (typeof (IProjectService));
		PropertyService propertyService = (PropertyService) ServiceManager.Services.GetService (typeof (PropertyService));

		public string Id {
			get { return "TerminalPad"; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public Widget Control {
			get {
				return frame;
			}
		}
		
		public string Title {
			get {
				//FIXME: check the string
				return GettextCatalog.GetString ("Terminal");
			}
		}
		
		public string Icon {
			get {
				return MonoDevelop.Gui.Stock.OutputIcon;
			}
		}
		
		public void Dispose ()
		{
		}
		
		public void RedrawContent ()
		{
			OnTitleChanged (null);
			OnIconChanged (null);
		}
		
		public TerminalPad ()
		{
			//FIXME look up most of these in GConf
			term = new Terminal ();
			term.ScrollOnKeystroke = true;
			term.CursorBlinks = true;
			term.MouseAutohide = true;
			term.FontFromString = "monospace 10";
			term.Encoding = "UTF-8";
			term.BackspaceBinding = TerminalEraseBinding.Auto;
			term.DeleteBinding = TerminalEraseBinding.Auto;
			term.Emulation = "xterm";

			Gdk.Color fgcolor = new Gdk.Color (0, 0, 0);
			Gdk.Color bgcolor = new Gdk.Color (0xff, 0xff, 0xff);
			Gdk.Colormap colormap = Gdk.Colormap.System;
			colormap.AllocColor (ref fgcolor, true, true);
			colormap.AllocColor (ref bgcolor, true, true);
			term.SetColors (fgcolor, bgcolor, fgcolor, 16);

			//FIXME: whats a good default here
			//term.SetSize (80, 5);

			// seems to want an array of "variable=value"
                	string[] envv = new string [Environment.GetEnvironmentVariables ().Count];
                	int i = 0;
			foreach (DictionaryEntry e in Environment.GetEnvironmentVariables ())
			{
				if (e.Key == "" || e.Value == "")
					continue;
				envv[i] = String.Format ("{0}={1}", e.Key, e.Value);
				i ++;
			}

			term.ForkCommand (Environment.GetEnvironmentVariable ("SHELL"), Environment.GetCommandLineArgs (), envv, Environment.GetEnvironmentVariable ("HOME"), false, true, true);

			term.ChildExited += new EventHandler (OnChildExited);

			VScrollbar vscroll = new VScrollbar (term.Adjustment);

			HBox hbox = new HBox ();
			hbox.PackStart (term, true, true, 0);
			hbox.PackStart (vscroll, false, true, 0);

			frame.ShadowType = Gtk.ShadowType.In;
			ScrolledWindow sw = new ScrolledWindow ();
			sw.Add (hbox);
			frame.Add (sw);
			
			Control.ShowAll ();
			
/*			Runtime.TaskService.CompilerOutputChanged += (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (SetOutput));
			projectService.StartBuild += (EventHandler) Runtime.DispatchService.GuiDispatch (new EventHandler (SelectMessageView));
			projectService.CombineClosed += (CombineEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineClosed));
			projectService.CombineOpened += (CombineEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineOpen));
*/		}

		void OnChildExited (object o, EventArgs args)
		{
			// full reset
			term.Reset (true, true);
		}

		void OnCombineOpen (object sender, CombineEventArgs e)
		{
			term.Reset (false, false);
		}
		
		void OnCombineClosed (object sender, CombineEventArgs e)
		{
			term.Reset (false, true);
		}
		
		void SelectMessageView (object sender, EventArgs e)
		{
			if (WorkbenchSingleton.Workbench.WorkbenchLayout.IsVisible (this)) {
				WorkbenchSingleton.Workbench.WorkbenchLayout.ActivatePad (this);
			}
			else { 
				if ((bool) propertyService.GetProperty ("SharpDevelop.ShowOutputWindowAtBuild", true)) {
					WorkbenchSingleton.Workbench.WorkbenchLayout.ShowPad (this);
					WorkbenchSingleton.Workbench.WorkbenchLayout.ActivatePad (this);
				}
			}
		}

		public void RunCommand (string command)
		{
			term.FeedChild (command + "\n");
		}

		void SetOutput2 ()
		{
			term.Feed (Runtime.TaskService.CompilerOutput.Replace ("\n", "\r\n"));
		}
		
		void SetOutput (object sender, EventArgs e)
		{
			if (WorkbenchSingleton.Workbench.WorkbenchLayout.IsVisible (this)) {
				SetOutput2 ();
			}
			else {
				term.Feed (Runtime.TaskService.CompilerOutput.Replace ("\n", "\r\n"));
			}
		}
		
		protected virtual void OnIconChanged (EventArgs e)
		{
			if (IconChanged != null) {
				IconChanged (this, e);
			}
		}

		protected virtual void OnTitleChanged (EventArgs e)
		{
			if (TitleChanged != null) {
				TitleChanged (this, e);
			}
		}

		public event EventHandler IconChanged;
		public event EventHandler TitleChanged;
	}
}

