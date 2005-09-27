using System;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

using Gtk;
using Vte;

namespace MonoDevelop.Ide.Gui.Pads
{
	public class TerminalPad : IPadContent
	{
		public event EventHandler IconChanged;
		public event EventHandler TitleChanged;

		private static readonly string GCONF_PATH = "/apps/gnome-terminal/profiles/";

		private Gtk.Frame    frame  = new Frame ();
		private Vte.Terminal term;
		private GConf.Client client;

		IProjectService projectService = (IProjectService) ServiceManager.GetService (typeof (IProjectService));
		PropertyService propertyService = (PropertyService) ServiceManager.GetService (typeof (PropertyService));

		public string Id {
			get {
				return "MonoDevelop.Ide.Gui.Pads.TerminalPad";
			}
		}
		
		public string DefaultPlacement {
			get {
				return "Bottom";
			}
		}
		
		public Widget Control {
			get {
				return frame;
			}
		}
		
		public string Title {
			get {
				return GettextCatalog.GetString ("Terminal");
			}
		}
		
		public string Icon {
			get {
				return MonoDevelop.Core.Gui.Stock.OutputIcon;
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
			client = new GConf.Client ();
			term   = new Terminal ();
			
			LoadProfile ("Default");
			
			//FIXME: whats a good default here
			//term.SetSize (80, 5);
			
			Reset ();
			
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
			
			/*
			Services.TaskService.CompilerOutputChanged += (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (SetOutput));
			projectService.StartBuild += (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (SelectMessageView));
			projectService.CombineClosed += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineClosed));
			projectService.CombineOpened += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineOpen));
			*/
		}

		void Reset ()
		{
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
		}

		void OnChildExited (object o, EventArgs args)
		{
			term.Reset (true, true);
			Reset ();
		}

		void OnCombineOpen (object sender, CombineEventArgs e)
		{
			term.Reset (false, false);
			Reset ();
		}
		
		void OnCombineClosed (object sender, CombineEventArgs e)
		{
			term.Reset (false, true);
			Reset ();
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
			term.Feed (Services.TaskService.CompilerOutput.Replace ("\n", "\r\n"));
		}
		
		void SetOutput (object sender, EventArgs e)
		{
			if (WorkbenchSingleton.Workbench.WorkbenchLayout.IsVisible (this)) {
				SetOutput2 ();
			}
			else {
				term.Feed (Services.TaskService.CompilerOutput.Replace ("\n", "\r\n"));
			}
		}

		void LoadProfile (string profile)
		{
			string path = GCONF_PATH + profile;

			bool      allow_bold          = true;
			string    background_color    = "#FFFFFF";
			string    backspace_binding   = "ascii-del";
			bool      cursor_blink        = true;
			string    delete_binding      = "escape-sequence";
			string    font                = "monospace 12";
			string    foreground_color    = "#000000";
			string    palette             = "#000000:#AA0000:#00AA00:#AA5500:#0000AA:#AA00AA:#00AAAA:#AAAAAA:#555555:#FF5555:#55FF55:#FFFF55:#5555FF:#FF55FF:#55FFFF:#FFFFFF";
			bool      scroll_background   = false;
			int       scrollback_lines    = 500;
			bool      scroll_on_keystroke = true;
			bool      scroll_on_output    = false;
			bool      silent_bell         = false;
			string    word_chars          = "-A-Za-z0-9,./?%&#:_";

			try { allow_bold = (bool) client.Get (path + "/allow_bold"); } catch {}
			try { background_color = (string) client.Get (path + "/background_color"); } catch {}
			try { backspace_binding = (string) client.Get (path + "/backspace_binding"); } catch {}
			try { cursor_blink = (bool) client.Get (path + "/cursor_blink"); } catch {}
			try { delete_binding = (string) client.Get (path + "/delete_binding"); } catch {}
			try { font = (string) client.Get (path + "/font"); } catch {}
			try { foreground_color = (string) client.Get (path + "/foreground_color"); } catch {}
			try { palette = (string) client.Get (path + "/palette"); } catch {}
			try { scroll_background = (bool) client.Get (path + "/scroll_background"); } catch {}
			try { scrollback_lines = (int) client.Get (path + "/scrollback_lines"); } catch {}
			try { scroll_on_keystroke = (bool) client.Get (path + "/scroll_on_keystroke"); } catch {}
			try { scroll_on_output = (bool) client.Get (path + "/scroll_on_output"); } catch {}
			try { silent_bell = (bool) client.Get (path + "/silent_bell"); } catch {}
			try { word_chars = (string) client.Get (path + "/word_chars"); } catch {}

			Gdk.Color bg = new Gdk.Color ();
			Gdk.Color.Parse (background_color, ref bg);

			term.AllowBold         = allow_bold;
			term.ColorBackground   = bg;
		    term.BackspaceBinding  = ParseEraseBinding (backspace_binding);
			term.CursorBlinks      = cursor_blink;
			term.DeleteBinding     = ParseEraseBinding (delete_binding);
			term.FontFromString    = font;
			term.ScrollBackground  = scroll_background;
			term.ScrollbackLines   = scrollback_lines;
			term.ScrollOnKeystroke = scroll_on_keystroke;
			term.ScrollOnOutput    = scroll_on_output;
			term.AudibleBell       = !silent_bell;
			term.WordChars         = word_chars;

			// FIXME: palette
			string[] colors = palette.Split (':');
			Gdk.Color[] palette_colors = new Gdk.Color[colors.Length];
			for (int i = 0; i < palette_colors.Length; i++) {
				Gdk.Color tmp = new Gdk.Color ();
				Gdk.Color.Parse (colors[i], ref tmp);
				palette_colors[i] = tmp;
			}
			if (palette_colors.Length >= 2) {
				term.SetColors (palette_colors[0],
					palette_colors[palette_colors.Length - 1],
					palette_colors,
					palette_colors.Length);
			}

			term.Emulation = "xterm";
			term.MouseAutohide = true;
			term.Encoding = "UTF-8";
		}

		TerminalEraseBinding ParseEraseBinding (string str)
		{
			TerminalEraseBinding erase;
			switch (str) {
				case "control-h":
					erase = TerminalEraseBinding.AsciiBackspace;
					break;
				case "escape-sequence":
					erase = TerminalEraseBinding.DeleteSequence;
					break;
				case "ascii-del":
				default:
					erase = TerminalEraseBinding.AsciiDelete;
					break;
			}
			return erase;
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

	}
}

