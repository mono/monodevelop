#pragma warning disable 436

namespace Mono.Instrumentation.Monitor
{
	public partial class InstrumentationViewerDialog
	{
		private global::Gtk.UIManager UIManager;

		private global::Gtk.Action FileAction;

		private global::Gtk.Action openAction;

		private global::Gtk.Action connectAction;

		private global::Gtk.Action ExitAction;

		private global::Gtk.Action ToolsAction;

		private global::Gtk.Action FlushMemoryAction;

		private global::Gtk.VBox dialog1_VBox;

		private global::Gtk.MenuBar menubar1;

		private global::Gtk.VBox vbox2;

		private global::Gtk.HPaned hpaned;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeCounters;

		private global::Gtk.VBox vbox3;

		private global::Gtk.EventBox headerBox;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label labelHeader;

		private global::Gtk.HBox buttonsBox;

		private global::Gtk.Button buttonSave;

		private global::Gtk.Button buttonSaveAs;

		private global::Gtk.Button buttonDelete;

		private global::Gtk.Alignment viewBox;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget Mono.Instrumentation.Monitor.InstrumentationViewerDialog
			this.UIManager = new global::Gtk.UIManager();
			global::Gtk.ActionGroup w1 = new global::Gtk.ActionGroup("Default");
			this.FileAction = new global::Gtk.Action("FileAction", global::Mono.Unix.Catalog.GetString("File"), null, null);
			this.FileAction.ShortLabel = global::Mono.Unix.Catalog.GetString("File");
			w1.Add(this.FileAction, null);
			this.openAction = new global::Gtk.Action("openAction", global::Mono.Unix.Catalog.GetString("_Open"), null, "gtk-open");
			this.openAction.ShortLabel = global::Mono.Unix.Catalog.GetString("_Open");
			w1.Add(this.openAction, null);
			this.connectAction = new global::Gtk.Action("connectAction", global::Mono.Unix.Catalog.GetString("C_onnect"), null, "gtk-connect");
			this.connectAction.ShortLabel = global::Mono.Unix.Catalog.GetString("C_onnect");
			w1.Add(this.connectAction, null);
			this.ExitAction = new global::Gtk.Action("ExitAction", global::Mono.Unix.Catalog.GetString("Exit"), null, null);
			this.ExitAction.ShortLabel = global::Mono.Unix.Catalog.GetString("Exit");
			w1.Add(this.ExitAction, null);
			this.ToolsAction = new global::Gtk.Action("ToolsAction", global::Mono.Unix.Catalog.GetString("Tools"), null, null);
			this.ToolsAction.ShortLabel = global::Mono.Unix.Catalog.GetString("Tools");
			w1.Add(this.ToolsAction, null);
			this.FlushMemoryAction = new global::Gtk.Action("FlushMemoryAction", global::Mono.Unix.Catalog.GetString("Flush Memory"), null, null);
			this.FlushMemoryAction.ShortLabel = global::Mono.Unix.Catalog.GetString("Flush Memory");
			w1.Add(this.FlushMemoryAction, null);
			this.UIManager.InsertActionGroup(w1, 0);
			this.AddAccelGroup(this.UIManager.AccelGroup);
			this.Name = "Mono.Instrumentation.Monitor.InstrumentationViewerDialog";
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Container child Mono.Instrumentation.Monitor.InstrumentationViewerDialog.Gtk.Container+ContainerChild
			this.dialog1_VBox = new global::Gtk.VBox();
			this.dialog1_VBox.Name = "dialog1_VBox";
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.UIManager.AddUiFromString(@"<ui><menubar name='menubar1'><menu name='FileAction' action='FileAction'><menuitem name='openAction' action='openAction'/><menuitem name='connectAction' action='connectAction'/><separator/><menuitem name='ExitAction' action='ExitAction'/></menu><menu name='ToolsAction' action='ToolsAction'><menuitem name='FlushMemoryAction' action='FlushMemoryAction'/></menu></menubar></ui>");
			this.menubar1 = ((global::Gtk.MenuBar)(this.UIManager.GetWidget("/menubar1")));
			this.menubar1.Name = "menubar1";
			this.dialog1_VBox.Add(this.menubar1);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.dialog1_VBox[this.menubar1]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(9));
			// Container child vbox2.Gtk.Box+BoxChild
			this.hpaned = new global::Gtk.HPaned();
			this.hpaned.CanFocus = true;
			this.hpaned.Name = "hpaned";
			this.hpaned.Position = 159;
			// Container child hpaned.Gtk.Paned+PanedChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeCounters = new global::Gtk.TreeView();
			this.treeCounters.CanFocus = true;
			this.treeCounters.Name = "treeCounters";
			this.treeCounters.HeadersVisible = false;
			this.GtkScrolledWindow.Add(this.treeCounters);
			this.hpaned.Add(this.GtkScrolledWindow);
			global::Gtk.Paned.PanedChild w4 = ((global::Gtk.Paned.PanedChild)(this.hpaned[this.GtkScrolledWindow]));
			w4.Resize = false;
			w4.Shrink = false;
			// Container child hpaned.Gtk.Paned+PanedChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.headerBox = new global::Gtk.EventBox();
			this.headerBox.Name = "headerBox";
			// Container child headerBox.Gtk.Container+ContainerChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			this.hbox1.BorderWidth = ((uint)(3));
			// Container child hbox1.Gtk.Box+BoxChild
			this.labelHeader = new global::Gtk.Label();
			this.labelHeader.Name = "labelHeader";
			this.labelHeader.Xalign = 0F;
			this.hbox1.Add(this.labelHeader);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.labelHeader]));
			w5.Position = 0;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonsBox = new global::Gtk.HBox();
			this.buttonsBox.Name = "buttonsBox";
			this.buttonsBox.Spacing = 6;
			// Container child buttonsBox.Gtk.Box+BoxChild
			this.buttonSave = new global::Gtk.Button();
			this.buttonSave.CanFocus = true;
			this.buttonSave.Name = "buttonSave";
			this.buttonSave.UseUnderline = true;
			this.buttonSave.Relief = ((global::Gtk.ReliefStyle)(2));
			this.buttonSave.Label = global::Mono.Unix.Catalog.GetString("Save");
			global::Gtk.Image w6 = new global::Gtk.Image();
			w6.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "gtk-save", global::Gtk.IconSize.Button);
			this.buttonSave.Image = w6;
			this.buttonsBox.Add(this.buttonSave);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.buttonsBox[this.buttonSave]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child buttonsBox.Gtk.Box+BoxChild
			this.buttonSaveAs = new global::Gtk.Button();
			this.buttonSaveAs.CanFocus = true;
			this.buttonSaveAs.Name = "buttonSaveAs";
			this.buttonSaveAs.UseUnderline = true;
			this.buttonSaveAs.Relief = ((global::Gtk.ReliefStyle)(2));
			this.buttonSaveAs.Label = global::Mono.Unix.Catalog.GetString("Copy");
			global::Gtk.Image w8 = new global::Gtk.Image();
			w8.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "gtk-copy", global::Gtk.IconSize.Button);
			this.buttonSaveAs.Image = w8;
			this.buttonsBox.Add(this.buttonSaveAs);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.buttonsBox[this.buttonSaveAs]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			// Container child buttonsBox.Gtk.Box+BoxChild
			this.buttonDelete = new global::Gtk.Button();
			this.buttonDelete.CanFocus = true;
			this.buttonDelete.Name = "buttonDelete";
			this.buttonDelete.UseUnderline = true;
			this.buttonDelete.Relief = ((global::Gtk.ReliefStyle)(2));
			this.buttonDelete.Label = global::Mono.Unix.Catalog.GetString("Delete");
			global::Gtk.Image w10 = new global::Gtk.Image();
			w10.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "gtk-delete", global::Gtk.IconSize.Button);
			this.buttonDelete.Image = w10;
			this.buttonsBox.Add(this.buttonDelete);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.buttonsBox[this.buttonDelete]));
			w11.Position = 2;
			w11.Expand = false;
			w11.Fill = false;
			this.hbox1.Add(this.buttonsBox);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.buttonsBox]));
			w12.Position = 1;
			w12.Expand = false;
			w12.Fill = false;
			this.headerBox.Add(this.hbox1);
			this.vbox3.Add(this.headerBox);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.headerBox]));
			w14.Position = 0;
			w14.Expand = false;
			w14.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.viewBox = new global::Gtk.Alignment(0.5F, 0.5F, 1F, 1F);
			this.viewBox.Name = "viewBox";
			this.vbox3.Add(this.viewBox);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.viewBox]));
			w15.Position = 1;
			this.hpaned.Add(this.vbox3);
			this.vbox2.Add(this.hpaned);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hpaned]));
			w17.Position = 0;
			this.dialog1_VBox.Add(this.vbox2);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.dialog1_VBox[this.vbox2]));
			w18.Position = 1;
			this.Add(this.dialog1_VBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 942;
			this.DefaultHeight = 587;
			this.Show();
			this.DeleteEvent += new global::Gtk.DeleteEventHandler(this.OnDeleteEvent);
			this.openAction.Activated += new global::System.EventHandler(this.OnOpenActionActivated);
			this.ExitAction.Activated += new global::System.EventHandler(this.OnExitActionActivated);
			this.FlushMemoryAction.Activated += new global::System.EventHandler(this.OnFlushMemoryActionActivated);
			this.buttonSave.Clicked += new global::System.EventHandler(this.OnButtonSaveClicked);
			this.buttonSaveAs.Clicked += new global::System.EventHandler(this.OnButtonSaveAsClicked);
			this.buttonDelete.Clicked += new global::System.EventHandler(this.OnButtonDeleteClicked);
		}
	}
}
#pragma warning restore 436
