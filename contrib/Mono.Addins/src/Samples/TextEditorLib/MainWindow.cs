using System;
using Gtk;
using Mono.Addins;
using TextEditor;

public partial class MainWindow: Gtk.Window
{	
	internal static MainWindow Instance;
	
	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Instance = this;
		Build ();
		
		AddinManager.ExtensionChanged += OnExtensionChanged;
		BuildToolbar ();
		BuildMenu ();
	}
	
	public void ConsoleWrite (string txt)
	{
		console.Show ();
		consoleView.Buffer.Text += txt;
		consoleView.ScrollToMark (consoleView.Buffer.InsertMark, 0d, false, 0d, 0d);
	}
	
	void BuildToolbar ()
	{
		// Clean the toolbar
		foreach (Gtk.Widget w in toolbar.Children)
			toolbar.Remove (w);
			
		// Add the new buttons
		foreach (ToolbarNode node in AddinManager.GetExtensionNodes ("/TextEditor/ToolbarButtons"))
			toolbar.Insert (node.GetToolItem (), -1);
		
		toolbar.ShowAll ();
	}
	
	void BuildMenu ()
	{
		// Clean the toolbar
		foreach (Gtk.Widget w in menubar.Children)
			menubar.Remove (w);
			
		// Add the new buttons
		foreach (MenuNode node in AddinManager.GetExtensionNodes ("/TextEditor/MainMenu"))
			menubar.Insert (node.GetMenuItem (), -1);
		
		// Create the menu for creating documents from templates
		
		Gtk.Menu menu = BuildTemplateItems (AddinManager.GetExtensionNodes ("/TextEditor/Templates"));
		Gtk.MenuItem it = new MenuItem ("New From Template");
		it.Submenu = menu;
		
		Gtk.MenuItem men = (Gtk.MenuItem) menubar.Children [0];
		((Gtk.Menu)men.Submenu).Insert (it, 1);
		
		menubar.ShowAll ();
	}
	
	Gtk.Menu BuildTemplateItems (ExtensionNodeList nodes)
	{
		Gtk.Menu menu = new Gtk.Menu ();
		foreach (ExtensionNode tn in nodes) {
			Gtk.MenuItem item;
			if (tn is TemplateCategoryNode) {
				TemplateCategoryNode cat = (TemplateCategoryNode) tn;
				item = new Gtk.MenuItem (cat.Name);
				item.Submenu = BuildTemplateItems (cat.ChildNodes);
			}
			else {
				FileTemplateNode t = (FileTemplateNode) tn;
				item = new Gtk.MenuItem (t.Name);
				item.Activated += delegate {
					TextEditor.TextEditorApp.NewFile (t.GetContent ());
				};
			}
			menu.Insert (item, -1);
		}
		return menu;
	}

	
	void OnExtensionChanged (object o, ExtensionEventArgs args)
	{
		if (args.PathChanged ("/TextEditor/ToolbarButtons"))
			BuildToolbar ();
		else if (args.PathChanged ("/TextEditor/MainMenu") || args.PathChanged ("/TextEditor/Templates"))
			BuildMenu ();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected virtual void OnButton1Clicked(object sender, System.EventArgs e)
	{
		console.Hide ();
	}
	
	public Gtk.TextView View {
		get { return textview; }
	}
}
