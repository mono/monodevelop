#pragma warning disable 436

namespace Mono.Instrumentation.Monitor
{
	internal partial class TimeLineViewWindow
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Button buttonExpand;

		private global::Gtk.Button buttonCollapse;

		private global::Gtk.CheckButton checkSingleThread;

		private global::Gtk.HBox hbox2;

		private global::Gtk.ScrolledWindow GtkScrolledWindow;

		private global::Mono.Instrumentation.Monitor.TimeLineView timeView;

		private global::Gtk.VBox vbox1;

		private global::Gtk.Button button3;

		private global::Gtk.VScale vscaleZoom;

		private global::Gtk.Label label2;

		private global::Gtk.Button buttonResetScale;

		private global::Gtk.VScale vscaleScale;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget Mono.Instrumentation.Monitor.TimeLineViewWindow
			this.Name = "Mono.Instrumentation.Monitor.TimeLineViewWindow";
			this.Title = global::Mono.Unix.Catalog.GetString("Time Line");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			// Container child Mono.Instrumentation.Monitor.TimeLineViewWindow.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 6;
			this.vbox2.BorderWidth = ((uint)(6));
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonExpand = new global::Gtk.Button();
			this.buttonExpand.CanFocus = true;
			this.buttonExpand.Name = "buttonExpand";
			this.buttonExpand.UseUnderline = true;
			this.buttonExpand.Label = global::Mono.Unix.Catalog.GetString("Expand All");
			this.hbox1.Add(this.buttonExpand);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.buttonExpand]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.buttonCollapse = new global::Gtk.Button();
			this.buttonCollapse.CanFocus = true;
			this.buttonCollapse.Name = "buttonCollapse";
			this.buttonCollapse.UseUnderline = true;
			this.buttonCollapse.Label = global::Mono.Unix.Catalog.GetString("Collapse All");
			this.hbox1.Add(this.buttonCollapse);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.buttonCollapse]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.checkSingleThread = new global::Gtk.CheckButton();
			this.checkSingleThread.CanFocus = true;
			this.checkSingleThread.Name = "checkSingleThread";
			this.checkSingleThread.Label = global::Mono.Unix.Catalog.GetString("Single Thread");
			this.checkSingleThread.DrawIndicator = true;
			this.checkSingleThread.UseUnderline = true;
			this.hbox1.Add(this.checkSingleThread);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.checkSingleThread]));
			w3.Position = 2;
			w3.Expand = false;
			w3.Fill = false;
			this.vbox2.Add(this.hbox1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox1]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.GtkScrolledWindow = new global::Gtk.ScrolledWindow();
			this.GtkScrolledWindow.Name = "GtkScrolledWindow";
			this.GtkScrolledWindow.ShadowType = ((global::Gtk.ShadowType)(1));
			// Container child GtkScrolledWindow.Gtk.Container+ContainerChild
			global::Gtk.Viewport w5 = new global::Gtk.Viewport();
			w5.ShadowType = ((global::Gtk.ShadowType)(0));
			// Container child GtkViewport.Gtk.Container+ContainerChild
			this.timeView = new global::Mono.Instrumentation.Monitor.TimeLineView();
			this.timeView.Name = "timeView";
			this.timeView.SingleThread = false;
			this.timeView.TimeScale = 0D;
			this.timeView.Zoom = 0;
			this.timeView.Scale = 0D;
			w5.Add(this.timeView);
			this.GtkScrolledWindow.Add(w5);
			this.hbox2.Add(this.GtkScrolledWindow);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.GtkScrolledWindow]));
			w8.Position = 0;
			// Container child hbox2.Gtk.Box+BoxChild
			this.vbox1 = new global::Gtk.VBox();
			this.vbox1.Name = "vbox1";
			this.vbox1.Spacing = 6;
			// Container child vbox1.Gtk.Box+BoxChild
			this.button3 = new global::Gtk.Button();
			this.button3.CanFocus = true;
			this.button3.Name = "button3";
			this.button3.UseUnderline = true;
			global::Gtk.Image w9 = new global::Gtk.Image();
			w9.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "gtk-zoom-100", global::Gtk.IconSize.Menu);
			this.button3.Image = w9;
			this.vbox1.Add(this.button3);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.button3]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.vscaleZoom = new global::Gtk.VScale(null);
			this.vscaleZoom.HeightRequest = 150;
			this.vscaleZoom.CanFocus = true;
			this.vscaleZoom.Name = "vscaleZoom";
			this.vscaleZoom.Adjustment.Lower = 10D;
			this.vscaleZoom.Adjustment.Upper = 300D;
			this.vscaleZoom.Adjustment.PageIncrement = 10D;
			this.vscaleZoom.Adjustment.StepIncrement = 1D;
			this.vscaleZoom.Adjustment.Value = 100D;
			this.vscaleZoom.DrawValue = true;
			this.vscaleZoom.Digits = 0;
			this.vscaleZoom.ValuePos = ((global::Gtk.PositionType)(2));
			this.vbox1.Add(this.vscaleZoom);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.vscaleZoom]));
			w11.Position = 1;
			w11.Expand = false;
			w11.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.vbox1.Add(this.label2);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.label2]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.buttonResetScale = new global::Gtk.Button();
			this.buttonResetScale.CanFocus = true;
			this.buttonResetScale.Name = "buttonResetScale";
			this.buttonResetScale.UseUnderline = true;
			global::Gtk.Image w13 = new global::Gtk.Image();
			w13.Pixbuf = global::Stetic.IconLoader.LoadIcon(this, "stock_draw-dimension-line", global::Gtk.IconSize.Menu);
			this.buttonResetScale.Image = w13;
			this.vbox1.Add(this.buttonResetScale);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.buttonResetScale]));
			w14.Position = 3;
			w14.Expand = false;
			w14.Fill = false;
			// Container child vbox1.Gtk.Box+BoxChild
			this.vscaleScale = new global::Gtk.VScale(null);
			this.vscaleScale.HeightRequest = 150;
			this.vscaleScale.CanFocus = true;
			this.vscaleScale.Name = "vscaleScale";
			this.vscaleScale.Adjustment.Lower = 10D;
			this.vscaleScale.Adjustment.Upper = 300D;
			this.vscaleScale.Adjustment.PageIncrement = 10D;
			this.vscaleScale.Adjustment.StepIncrement = 1D;
			this.vscaleScale.Adjustment.Value = 100D;
			this.vscaleScale.DrawValue = true;
			this.vscaleScale.Digits = 0;
			this.vscaleScale.ValuePos = ((global::Gtk.PositionType)(2));
			this.vbox1.Add(this.vscaleScale);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.vbox1[this.vscaleScale]));
			w15.Position = 4;
			w15.Expand = false;
			w15.Fill = false;
			this.hbox2.Add(this.vbox1);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.vbox1]));
			w16.Position = 1;
			w16.Expand = false;
			w16.Fill = false;
			this.vbox2.Add(this.hbox2);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.hbox2]));
			w17.Position = 1;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 806;
			this.DefaultHeight = 638;
			this.Show();
			this.buttonExpand.Clicked += new global::System.EventHandler(this.OnButtonExpandClicked);
			this.buttonCollapse.Clicked += new global::System.EventHandler(this.OnButtonCollapseClicked);
			this.checkSingleThread.Toggled += new global::System.EventHandler(this.OnCheckSingleThreadToggled);
			this.buttonResetScale.Clicked += new global::System.EventHandler(this.OnButtonResetScaleClicked);
			this.vscaleScale.ChangeValue += new global::Gtk.ChangeValueHandler(this.OnVscaleScaleChangeValue);
		}
	}
}
#pragma warning restore 436
