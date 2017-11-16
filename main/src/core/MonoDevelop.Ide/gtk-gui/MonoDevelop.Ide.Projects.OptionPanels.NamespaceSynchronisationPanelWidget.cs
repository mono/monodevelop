#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal partial class NamespaceSynchronisationPanelWidget
	{
		private global::Gtk.VBox vbox2;

		private global::Gtk.CheckButton checkAssociateNamespacesDirectories;

		private global::Gtk.Alignment alignment1;

		private global::Gtk.VBox namespaceAssociationBox;

		private global::Gtk.CheckButton checkDefaultAsRoot;

		private global::Gtk.HBox hbox1;

		private global::Gtk.Label label1;

		private global::Gtk.VBox vbox4;

		private global::Gtk.RadioButton radioFlat;

		private global::Gtk.RadioButton radioHierarch;

		private global::Gtk.HBox hbox2;

		private global::Gtk.Frame previewFrame;

		private global::Gtk.Label GtkLabel6;

		private global::Gtk.CheckButton checkVSStyleResourceNames;

		private global::Gtk.Label label2;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.NamespaceSynchronisationPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.NamespaceSynchronisationPanelWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.NamespaceSynchronisationPanelWidget.Gtk.Container+ContainerChild
			this.vbox2 = new global::Gtk.VBox();
			this.vbox2.Name = "vbox2";
			this.vbox2.Spacing = 12;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkAssociateNamespacesDirectories = new global::Gtk.CheckButton();
			this.checkAssociateNamespacesDirectories.CanFocus = true;
			this.checkAssociateNamespacesDirectories.Name = "checkAssociateNamespacesDirectories";
			this.checkAssociateNamespacesDirectories.Label = global::Mono.Unix.Catalog.GetString("_Associate namespaces with directory names");
			this.checkAssociateNamespacesDirectories.DrawIndicator = true;
			this.checkAssociateNamespacesDirectories.UseUnderline = true;
			this.vbox2.Add(this.checkAssociateNamespacesDirectories);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkAssociateNamespacesDirectories]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox2.Gtk.Box+BoxChild
			this.alignment1 = new global::Gtk.Alignment(0F, 0F, 1F, 1F);
			this.alignment1.Name = "alignment1";
			this.alignment1.LeftPadding = ((uint)(24));
			// Container child alignment1.Gtk.Container+ContainerChild
			this.namespaceAssociationBox = new global::Gtk.VBox();
			this.namespaceAssociationBox.Name = "namespaceAssociationBox";
			this.namespaceAssociationBox.Spacing = 12;
			// Container child namespaceAssociationBox.Gtk.Box+BoxChild
			this.checkDefaultAsRoot = new global::Gtk.CheckButton();
			this.checkDefaultAsRoot.CanFocus = true;
			this.checkDefaultAsRoot.Name = "checkDefaultAsRoot";
			this.checkDefaultAsRoot.Label = global::Mono.Unix.Catalog.GetString("Use _default namespace as root");
			this.checkDefaultAsRoot.DrawIndicator = true;
			this.checkDefaultAsRoot.UseUnderline = true;
			this.namespaceAssociationBox.Add(this.checkDefaultAsRoot);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.namespaceAssociationBox[this.checkDefaultAsRoot]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child namespaceAssociationBox.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.Yalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Directory structure:");
			this.hbox1.Add(this.label1);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.label1]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.vbox4 = new global::Gtk.VBox();
			this.vbox4.Name = "vbox4";
			this.vbox4.Spacing = 6;
			// Container child vbox4.Gtk.Box+BoxChild
			this.radioFlat = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("_Flat"));
			this.radioFlat.CanFocus = true;
			this.radioFlat.Name = "radioFlat";
			this.radioFlat.DrawIndicator = true;
			this.radioFlat.UseUnderline = true;
			this.radioFlat.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.vbox4.Add(this.radioFlat);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.radioFlat]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox4.Gtk.Box+BoxChild
			this.radioHierarch = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("_Hierarchical"));
			this.radioHierarch.CanFocus = true;
			this.radioHierarch.Name = "radioHierarch";
			this.radioHierarch.DrawIndicator = true;
			this.radioHierarch.UseUnderline = true;
			this.radioHierarch.Group = this.radioFlat.Group;
			this.vbox4.Add(this.radioHierarch);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox4[this.radioHierarch]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.hbox1.Add(this.vbox4);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.vbox4]));
			w6.Position = 1;
			this.namespaceAssociationBox.Add(this.hbox1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.namespaceAssociationBox[this.hbox1]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			// Container child namespaceAssociationBox.Gtk.Box+BoxChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			this.hbox2.Spacing = 6;
			// Container child hbox2.Gtk.Box+BoxChild
			this.previewFrame = new global::Gtk.Frame();
			this.previewFrame.WidthRequest = 400;
			this.previewFrame.Name = "previewFrame";
			this.previewFrame.ShadowType = ((global::Gtk.ShadowType)(4));
			this.GtkLabel6 = new global::Gtk.Label();
			this.GtkLabel6.Name = "GtkLabel6";
			this.GtkLabel6.LabelProp = global::Mono.Unix.Catalog.GetString("Preview");
			this.previewFrame.LabelWidget = this.GtkLabel6;
			this.hbox2.Add(this.previewFrame);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.previewFrame]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			this.namespaceAssociationBox.Add(this.hbox2);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.namespaceAssociationBox[this.hbox2]));
			w9.Position = 2;
			this.alignment1.Add(this.namespaceAssociationBox);
			this.vbox2.Add(this.alignment1);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.alignment1]));
			w11.Position = 1;
			// Container child vbox2.Gtk.Box+BoxChild
			this.checkVSStyleResourceNames = new global::Gtk.CheckButton();
			this.checkVSStyleResourceNames.CanFocus = true;
			this.checkVSStyleResourceNames.Name = "checkVSStyleResourceNames";
			this.checkVSStyleResourceNames.Label = global::Mono.Unix.Catalog.GetString("<b>Use _Visual Studio-style resource names</b>\nVisual Studio generates a default " +
					"ID for embedded resources,\ninstead of simply using the resource\'s filename.");
			this.checkVSStyleResourceNames.DrawIndicator = true;
			this.checkVSStyleResourceNames.UseUnderline = true;
			this.checkVSStyleResourceNames.Remove(this.checkVSStyleResourceNames.Child);
			// Container child checkVSStyleResourceNames.Gtk.Container+ContainerChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Use _Visual Studio-style resource names</b>\nVisual Studio generates a default " +
					"ID for embedded resources, instead of simply using the resource\'s filename.");
			this.label2.UseMarkup = true;
			this.label2.UseUnderline = true;
			this.label2.Wrap = true;
			this.checkVSStyleResourceNames.Add(this.label2);
			this.vbox2.Add(this.checkVSStyleResourceNames);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.vbox2[this.checkVSStyleResourceNames]));
			w13.Position = 2;
			w13.Expand = false;
			w13.Fill = false;
			this.Add(this.vbox2);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.checkVSStyleResourceNames.Toggled += new global::System.EventHandler(this.UpdatePolicyNameList);
		}
	}
}
#pragma warning restore 436
