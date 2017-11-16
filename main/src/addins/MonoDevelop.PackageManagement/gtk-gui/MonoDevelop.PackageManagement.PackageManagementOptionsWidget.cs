#pragma warning disable 436

namespace MonoDevelop.PackageManagement
{
	internal partial class PackageManagementOptionsWidget
	{
		private global::Gtk.VBox mainVBox;

		private global::Gtk.HBox restorePackagesLabelHBox;

		private global::Gtk.Label restorePackagesLabel;

		private global::Gtk.Label restorePackagesPaddingLabel;

		private global::Gtk.CheckButton automaticPackageRestoreOnOpeningSolutionCheckBox;

		private global::Gtk.HBox packageUpdatesLabelHBox;

		private global::Gtk.Label packageUpdatesLabel;

		private global::Gtk.Label packageUpdatesPaddingLabel;

		private global::Gtk.CheckButton checkForPackageUpdatesOnOpeningSolutionCheckBox;

		private global::Gtk.Label bottomLabel;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.PackageManagement.PackageManagementOptionsWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.PackageManagement.PackageManagementOptionsWidget";
			// Container child MonoDevelop.PackageManagement.PackageManagementOptionsWidget.Gtk.Container+ContainerChild
			this.mainVBox = new global::Gtk.VBox();
			this.mainVBox.Name = "mainVBox";
			this.mainVBox.Spacing = 6;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.restorePackagesLabelHBox = new global::Gtk.HBox();
			this.restorePackagesLabelHBox.Name = "restorePackagesLabelHBox";
			this.restorePackagesLabelHBox.Spacing = 6;
			// Container child restorePackagesLabelHBox.Gtk.Box+BoxChild
			this.restorePackagesLabel = new global::Gtk.Label();
			this.restorePackagesLabel.Name = "restorePackagesLabel";
			this.restorePackagesLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Package Restore</b>");
			this.restorePackagesLabel.UseMarkup = true;
			this.restorePackagesLabelHBox.Add(this.restorePackagesLabel);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.restorePackagesLabelHBox[this.restorePackagesLabel]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child restorePackagesLabelHBox.Gtk.Box+BoxChild
			this.restorePackagesPaddingLabel = new global::Gtk.Label();
			this.restorePackagesPaddingLabel.Name = "restorePackagesPaddingLabel";
			this.restorePackagesLabelHBox.Add(this.restorePackagesPaddingLabel);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.restorePackagesLabelHBox[this.restorePackagesPaddingLabel]));
			w2.Position = 1;
			this.mainVBox.Add(this.restorePackagesLabelHBox);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.restorePackagesLabelHBox]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.automaticPackageRestoreOnOpeningSolutionCheckBox = new global::Gtk.CheckButton();
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.CanFocus = true;
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.Name = "automaticPackageRestoreOnOpeningSolutionCheckBox";
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.Label = global::Mono.Unix.Catalog.GetString("_Automatically restore packages when opening a solution.");
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.DrawIndicator = true;
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.UseUnderline = true;
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.BorderWidth = ((uint)(10));
			this.mainVBox.Add(this.automaticPackageRestoreOnOpeningSolutionCheckBox);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.automaticPackageRestoreOnOpeningSolutionCheckBox]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.packageUpdatesLabelHBox = new global::Gtk.HBox();
			this.packageUpdatesLabelHBox.Name = "packageUpdatesLabelHBox";
			this.packageUpdatesLabelHBox.Spacing = 6;
			// Container child packageUpdatesLabelHBox.Gtk.Box+BoxChild
			this.packageUpdatesLabel = new global::Gtk.Label();
			this.packageUpdatesLabel.Name = "packageUpdatesLabel";
			this.packageUpdatesLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<b>Package Updates</b>");
			this.packageUpdatesLabel.UseMarkup = true;
			this.packageUpdatesLabelHBox.Add(this.packageUpdatesLabel);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.packageUpdatesLabelHBox[this.packageUpdatesLabel]));
			w5.Position = 0;
			w5.Expand = false;
			w5.Fill = false;
			// Container child packageUpdatesLabelHBox.Gtk.Box+BoxChild
			this.packageUpdatesPaddingLabel = new global::Gtk.Label();
			this.packageUpdatesPaddingLabel.Name = "packageUpdatesPaddingLabel";
			this.packageUpdatesLabelHBox.Add(this.packageUpdatesPaddingLabel);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.packageUpdatesLabelHBox[this.packageUpdatesPaddingLabel]));
			w6.Position = 1;
			this.mainVBox.Add(this.packageUpdatesLabelHBox);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.packageUpdatesLabelHBox]));
			w7.Position = 2;
			w7.Expand = false;
			w7.Fill = false;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox = new global::Gtk.CheckButton();
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.CanFocus = true;
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.Name = "checkForPackageUpdatesOnOpeningSolutionCheckBox";
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.Label = global::Mono.Unix.Catalog.GetString("Check for package _updates when opening a solution.");
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.DrawIndicator = true;
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.UseUnderline = true;
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.BorderWidth = ((uint)(10));
			this.mainVBox.Add(this.checkForPackageUpdatesOnOpeningSolutionCheckBox);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.checkForPackageUpdatesOnOpeningSolutionCheckBox]));
			w8.Position = 3;
			w8.Expand = false;
			w8.Fill = false;
			// Container child mainVBox.Gtk.Box+BoxChild
			this.bottomLabel = new global::Gtk.Label();
			this.bottomLabel.Name = "bottomLabel";
			this.mainVBox.Add(this.bottomLabel);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.mainVBox[this.bottomLabel]));
			w9.Position = 4;
			this.Add(this.mainVBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.automaticPackageRestoreOnOpeningSolutionCheckBox.Toggled += new global::System.EventHandler(this.AutomaticPackageRestoreOnOpeningSolutionCheckBoxToggled);
			this.checkForPackageUpdatesOnOpeningSolutionCheckBox.Toggled += new global::System.EventHandler(this.AutomaticPackageRestoreOnOpeningSolutionCheckBoxToggled);
		}
	}
}
#pragma warning restore 436
