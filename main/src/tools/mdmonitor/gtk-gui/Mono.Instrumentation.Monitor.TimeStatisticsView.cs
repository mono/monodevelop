#pragma warning disable 436

namespace Mono.Instrumentation.Monitor
{
	public partial class TimeStatisticsView
	{
		private global::Gtk.VBox vbox3;

		private global::Gtk.HBox hbox3;

		private global::Gtk.Button buttonUpdate;

		private global::Gtk.CheckButton checkShowCats;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Gtk.TreeView treeView;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget Mono.Instrumentation.Monitor.TimeStatisticsView
			global::Stetic.BinContainer.Attach(this);
			this.Name = "Mono.Instrumentation.Monitor.TimeStatisticsView";
			// Container child Mono.Instrumentation.Monitor.TimeStatisticsView.Gtk.Container+ContainerChild
			this.vbox3 = new global::Gtk.VBox();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			// Container child vbox3.Gtk.Box+BoxChild
			this.hbox3 = new global::Gtk.HBox();
			this.hbox3.Name = "hbox3";
			this.hbox3.Spacing = 6;
			// Container child hbox3.Gtk.Box+BoxChild
			this.buttonUpdate = new global::Gtk.Button();
			this.buttonUpdate.CanFocus = true;
			this.buttonUpdate.Name = "buttonUpdate";
			this.buttonUpdate.UseStock = true;
			this.buttonUpdate.UseUnderline = true;
			this.buttonUpdate.Relief = ((global::Gtk.ReliefStyle)(2));
			this.buttonUpdate.Label = "gtk-refresh";
			this.hbox3.Add(this.buttonUpdate);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.buttonUpdate]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child hbox3.Gtk.Box+BoxChild
			this.checkShowCats = new global::Gtk.CheckButton();
			this.checkShowCats.CanFocus = true;
			this.checkShowCats.Name = "checkShowCats";
			this.checkShowCats.Label = global::Mono.Unix.Catalog.GetString("Show Categories");
			this.checkShowCats.DrawIndicator = true;
			this.checkShowCats.UseUnderline = true;
			this.hbox3.Add(this.checkShowCats);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox3[this.checkShowCats]));
			w2.Position = 1;
			this.vbox3.Add(this.hbox3);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.hbox3]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			this.treeView = new global::Gtk.TreeView();
			this.treeView.CanFocus = true;
			this.treeView.Name = "treeView";
			this.treeView.RulesHint = true;
			this.GtkScrolledWindow.Add(this.treeView);
			this.vbox3.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox3[this.GtkScrolledWindow]));
			w5.Position = 1;
			this.Add(this.vbox3);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.buttonUpdate.Clicked += new global::System.EventHandler(this.OnButtonUpdateClicked);
			this.checkShowCats.Toggled += new global::System.EventHandler(this.OnCheckShowCatsToggled);
		}
	}
}
#pragma warning restore 436
