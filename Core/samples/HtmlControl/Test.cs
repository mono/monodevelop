using System;
using Gtk;
using GtkSharp;
using MonoDevelop.Gui.HtmlControl;

class HtmlTest
{
	Window win;
	MozillaControl html;
	Button go;
	Entry url;
	Statusbar status;
	
	static void Main ()
	{
		Application.Init ();
		new HtmlTest ();
		Application.Run ();
	}

	HtmlTest ()
	{
		win = new Window ("HtmlControl test");
		win.SetDefaultSize (600, 450);
		win.DeleteEvent += new DeleteEventHandler (OnWinDelete);

		VBox vbox = new VBox (false, 0);

		Toolbar tbar = new Toolbar ();
		tbar.ToolbarStyle = ToolbarStyle.Icons;
		
		Button back = new Button ();
		back.Child = new Image (Stock.GoBack, IconSize.SmallToolbar);
		back.Relief = ReliefStyle.None;
		back.Clicked += new EventHandler (OnBackClicked);
		tbar.AppendWidget (back, "Go Back", "");

		Button forward = new Button ();
		forward.Child = new Image (Stock.GoForward, IconSize.SmallToolbar);
		forward.Relief = ReliefStyle.None;
		forward.Clicked += new EventHandler (OnForwardClicked);
		tbar.AppendWidget (forward, "Go Forward", "");
		
		Button stop = new Button ();
		stop.Child = new Image (Stock.Stop, IconSize.SmallToolbar);
		stop.Relief = ReliefStyle.None;
		stop.Clicked += new EventHandler (OnStopClicked);
		tbar.AppendWidget (stop, "Stop", "");

		Button refresh = new Button ();
		refresh.Child = new Image (Stock.Refresh, IconSize.SmallToolbar);
		refresh.Relief = ReliefStyle.None;
		refresh.Clicked += new EventHandler (OnRefreshClicked);
		tbar.AppendWidget (refresh, "Refresh", "");
		
		vbox.PackStart (tbar, false, true, 0);

		url = new Entry ();
		url.WidthChars = 50;
		url.Activated += new EventHandler (OnUrlActivated);
		tbar.AppendWidget (url, "Location", "");

		go = new Button ();
		go.Child = new Image (Stock.Ok, IconSize.SmallToolbar);
		go.Relief = ReliefStyle.None;
		go.Clicked += new EventHandler (OnGoClicked);
		tbar.AppendWidget (go, "Go", "");

		html = new MozillaControl ();
		html.NetStart += new EventHandler (OnNetStart);
		html.NetStop += new EventHandler (OnNetStop);
		html.TitleChange += new EventHandler (OnTitleChanged);
		html.LinkMsg += new EventHandler (OnLinkMessage);
		html.Html = "<html><head><link rel=\"stylesheet\" type=\"text/css\" href=\"test.css\" /></head><body>testing</body></html>";
		
		html.ShowAll ();
		vbox.PackStart (html, true, true, 0);

		status = new Statusbar ();
		vbox.PackStart (status, false, true, 0);
		
		win.Add (vbox);
		win.ShowAll ();
		html.InitializeWithBase ("file://" + Environment.CurrentDirectory);
	}

	void OnWinDelete (object o, DeleteEventArgs args)
	{
		Application.Quit ();
	}

	void OnLinkMessage (object o, EventArgs args)
	{
		status.Push (1, html.LinkMessage);
	}
	
	void OnNetStart (object o, EventArgs args)
	{
		status.Push (1, "Loading ...");
	}

	void OnNetStop (object o, EventArgs args)
	{
		status.Push (1, "Done.");
	}

	void OnTitleChanged (object o, EventArgs args)
	{
		win.Title = html.Title;
	}

	void OnGoClicked (object o, EventArgs args)
	{
		html.LoadUrl (url.Text);
	}

	void OnUrlActivated (object o, EventArgs args)
	{
		OnGoClicked (o, args);
	}

	void OnHtmlTitle (object o, EventArgs args)
	{
		win.Title = html.Title;
	}

	void OnBackClicked (object o, EventArgs args)
	{
		html.GoBack ();
	}
	
	void OnForwardClicked (object o, EventArgs args)
	{
		html.GoForward ();
	}

	void OnStopClicked (object o, EventArgs args)
	{
		html.Stop ();
	}

	void OnRefreshClicked (object o, EventArgs args)
	{
		html.Refresh ();
	}
}
