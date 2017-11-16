#pragma warning disable 436

namespace MonoDevelop.AspNet.Projects
{
	internal partial class GtkAspNetProjectTemplateWizardPageWidget
	{
		private global::Gtk.HBox mainHBox;

		private global::Gtk.EventBox leftBorderEventBox;

		private global::Gtk.VBox configurationVBox;

		private global::Gtk.EventBox configurationTopEventBox;

		private global::Gtk.EventBox configurationTableEventBox;

		private global::Gtk.Table configurationTable;

		private global::Gtk.VBox includeLabelVBox;

		private global::Gtk.Label includeLabelPadding;

		private global::Gtk.Label includeLabel;

		private global::Gtk.VBox includeUnitTestProjectVBox;

		private global::Gtk.CheckButton includeTestProjectCheck;

		private global::Gtk.HBox includeUnitTestProjectDescriptionHBox;

		private global::Gtk.Label includeUnitTestProjectDescriptionLeftHandPadding;

		private global::Gtk.Label includeUnitTestProjectDescriptionLabel;

		private global::Gtk.VBox mvcVBox;

		private global::Gtk.CheckButton includeMvcCheck;

		private global::Gtk.HBox mvcDescriptionHBox;

		private global::Gtk.Label mvcDescriptionLeftHandPadding;

		private global::Gtk.Label mvcDescriptionLabel;

		private global::Gtk.Label paddingLabel;

		private global::Gtk.VBox testingLabelVBox;

		private global::Gtk.Label testingLabelPadding;

		private global::Gtk.Label testingLabel;

		private global::Gtk.DrawingArea testingSeparator;

		private global::Gtk.VBox webApiVBox;

		private global::Gtk.CheckButton includeWebApiCheck;

		private global::Gtk.HBox webApiDescriptionHBox;

		private global::Gtk.Label webApiDescriptionLeftHandPadding;

		private global::Gtk.Label webApiDescriptionLabel;

		private global::Gtk.VBox webFormsVBox;

		private global::Gtk.CheckButton includeWebFormsCheck;

		private global::Gtk.HBox webFormsDescriptionHBox;

		private global::Gtk.Label webFormsDescriptionLeftHandPadding;

		private global::Gtk.Label webFormsDescriptionLabel;

		private global::Gtk.EventBox configurationBottomEventBox;

		private global::Gtk.EventBox backgroundLargeImageEventBox;

		private global::Gtk.VBox backgroundLargeImageVBox;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.AspNet.Projects.GtkAspNetProjectTemplateWizardPageWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.AspNet.Projects.GtkAspNetProjectTemplateWizardPageWidget";
			// Container child MonoDevelop.AspNet.Projects.GtkAspNetProjectTemplateWizardPageWidget.Gtk.Container+ContainerChild
			this.mainHBox = new global::Gtk.HBox();
			this.mainHBox.Name = "mainHBox";
			// Container child mainHBox.Gtk.Box+BoxChild
			this.leftBorderEventBox = new global::Gtk.EventBox();
			this.leftBorderEventBox.WidthRequest = 30;
			this.leftBorderEventBox.Name = "leftBorderEventBox";
			this.mainHBox.Add(this.leftBorderEventBox);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.leftBorderEventBox]));
			w1.Position = 0;
			w1.Expand = false;
			// Container child mainHBox.Gtk.Box+BoxChild
			this.configurationVBox = new global::Gtk.VBox();
			this.configurationVBox.WidthRequest = 440;
			this.configurationVBox.Name = "configurationVBox";
			// Container child configurationVBox.Gtk.Box+BoxChild
			this.configurationTopEventBox = new global::Gtk.EventBox();
			this.configurationTopEventBox.Name = "configurationTopEventBox";
			this.configurationVBox.Add(this.configurationTopEventBox);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationTopEventBox]));
			w2.Position = 0;
			// Container child configurationVBox.Gtk.Box+BoxChild
			this.configurationTableEventBox = new global::Gtk.EventBox();
			this.configurationTableEventBox.Name = "configurationTableEventBox";
			// Container child configurationTableEventBox.Gtk.Container+ContainerChild
			this.configurationTable = new global::Gtk.Table(((uint)(6)), ((uint)(3)), false);
			this.configurationTable.Name = "configurationTable";
			this.configurationTable.RowSpacing = ((uint)(7));
			this.configurationTable.ColumnSpacing = ((uint)(6));
			// Container child configurationTable.Gtk.Table+TableChild
			this.includeLabelVBox = new global::Gtk.VBox();
			this.includeLabelVBox.Name = "includeLabelVBox";
			// Container child includeLabelVBox.Gtk.Box+BoxChild
			this.includeLabelPadding = new global::Gtk.Label();
			this.includeLabelPadding.WidthRequest = 0;
			this.includeLabelPadding.HeightRequest = 3;
			this.includeLabelPadding.Name = "includeLabelPadding";
			this.includeLabelVBox.Add(this.includeLabelPadding);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.includeLabelVBox[this.includeLabelPadding]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child includeLabelVBox.Gtk.Box+BoxChild
			this.includeLabel = new global::Gtk.Label();
			this.includeLabel.Name = "includeLabel";
			this.includeLabel.Xpad = 5;
			this.includeLabel.Xalign = 1F;
			this.includeLabel.Yalign = 0F;
			this.includeLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Include:");
			this.includeLabel.Justify = ((global::Gtk.Justification)(1));
			this.includeLabelVBox.Add(this.includeLabel);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.includeLabelVBox[this.includeLabel]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			this.configurationTable.Add(this.includeLabelVBox);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.includeLabelVBox]));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.includeUnitTestProjectVBox = new global::Gtk.VBox();
			this.includeUnitTestProjectVBox.Name = "includeUnitTestProjectVBox";
			// Container child includeUnitTestProjectVBox.Gtk.Box+BoxChild
			this.includeTestProjectCheck = new global::Gtk.CheckButton();
			this.includeTestProjectCheck.CanFocus = true;
			this.includeTestProjectCheck.Name = "includeTestProjectCheck";
			this.includeTestProjectCheck.Label = global::Mono.Unix.Catalog.GetString("Include Unit Test Project");
			this.includeTestProjectCheck.Active = true;
			this.includeTestProjectCheck.DrawIndicator = true;
			this.includeTestProjectCheck.UseUnderline = true;
			this.includeUnitTestProjectVBox.Add(this.includeTestProjectCheck);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.includeUnitTestProjectVBox[this.includeTestProjectCheck]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child includeUnitTestProjectVBox.Gtk.Box+BoxChild
			this.includeUnitTestProjectDescriptionHBox = new global::Gtk.HBox();
			this.includeUnitTestProjectDescriptionHBox.Name = "includeUnitTestProjectDescriptionHBox";
			// Container child includeUnitTestProjectDescriptionHBox.Gtk.Box+BoxChild
			this.includeUnitTestProjectDescriptionLeftHandPadding = new global::Gtk.Label();
			this.includeUnitTestProjectDescriptionLeftHandPadding.WidthRequest = 21;
			this.includeUnitTestProjectDescriptionLeftHandPadding.Name = "includeUnitTestProjectDescriptionLeftHandPadding";
			this.includeUnitTestProjectDescriptionHBox.Add(this.includeUnitTestProjectDescriptionLeftHandPadding);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.includeUnitTestProjectDescriptionHBox[this.includeUnitTestProjectDescriptionLeftHandPadding]));
			w7.Position = 0;
			w7.Expand = false;
			w7.Fill = false;
			// Container child includeUnitTestProjectDescriptionHBox.Gtk.Box+BoxChild
			this.includeUnitTestProjectDescriptionLabel = new global::Gtk.Label();
			this.includeUnitTestProjectDescriptionLabel.WidthRequest = 255;
			this.includeUnitTestProjectDescriptionLabel.Name = "includeUnitTestProjectDescriptionLabel";
			this.includeUnitTestProjectDescriptionLabel.Xalign = 0F;
			this.includeUnitTestProjectDescriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<span size=\'smaller\' color=\'grey\'>Add a Unit Test Project for testing the Web Pro" +
					"ject using NUnit</span>");
			this.includeUnitTestProjectDescriptionLabel.UseMarkup = true;
			this.includeUnitTestProjectDescriptionLabel.Wrap = true;
			this.includeUnitTestProjectDescriptionHBox.Add(this.includeUnitTestProjectDescriptionLabel);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.includeUnitTestProjectDescriptionHBox[this.includeUnitTestProjectDescriptionLabel]));
			w8.Position = 1;
			w8.Expand = false;
			w8.Fill = false;
			this.includeUnitTestProjectVBox.Add(this.includeUnitTestProjectDescriptionHBox);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.includeUnitTestProjectVBox[this.includeUnitTestProjectDescriptionHBox]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			this.configurationTable.Add(this.includeUnitTestProjectVBox);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.includeUnitTestProjectVBox]));
			w10.TopAttach = ((uint)(4));
			w10.BottomAttach = ((uint)(5));
			w10.LeftAttach = ((uint)(1));
			w10.RightAttach = ((uint)(2));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.mvcVBox = new global::Gtk.VBox();
			this.mvcVBox.Name = "mvcVBox";
			// Container child mvcVBox.Gtk.Box+BoxChild
			this.includeMvcCheck = new global::Gtk.CheckButton();
			this.includeMvcCheck.CanFocus = true;
			this.includeMvcCheck.Name = "includeMvcCheck";
			this.includeMvcCheck.Label = global::Mono.Unix.Catalog.GetString("MVC");
			this.includeMvcCheck.Active = true;
			this.includeMvcCheck.DrawIndicator = true;
			this.includeMvcCheck.UseUnderline = true;
			this.mvcVBox.Add(this.includeMvcCheck);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.mvcVBox[this.includeMvcCheck]));
			w11.Position = 0;
			w11.Expand = false;
			w11.Fill = false;
			// Container child mvcVBox.Gtk.Box+BoxChild
			this.mvcDescriptionHBox = new global::Gtk.HBox();
			this.mvcDescriptionHBox.Name = "mvcDescriptionHBox";
			// Container child mvcDescriptionHBox.Gtk.Box+BoxChild
			this.mvcDescriptionLeftHandPadding = new global::Gtk.Label();
			this.mvcDescriptionLeftHandPadding.WidthRequest = 21;
			this.mvcDescriptionLeftHandPadding.Name = "mvcDescriptionLeftHandPadding";
			this.mvcDescriptionHBox.Add(this.mvcDescriptionLeftHandPadding);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.mvcDescriptionHBox[this.mvcDescriptionLeftHandPadding]));
			w12.Position = 0;
			w12.Expand = false;
			w12.Fill = false;
			// Container child mvcDescriptionHBox.Gtk.Box+BoxChild
			this.mvcDescriptionLabel = new global::Gtk.Label();
			this.mvcDescriptionLabel.WidthRequest = 255;
			this.mvcDescriptionLabel.Name = "mvcDescriptionLabel";
			this.mvcDescriptionLabel.Xalign = 0F;
			this.mvcDescriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<span size=\'smaller\' color=\'grey\'>Modern programming model. Unit testable, choice" +
					" of templating languages</span>");
			this.mvcDescriptionLabel.UseMarkup = true;
			this.mvcDescriptionLabel.Wrap = true;
			this.mvcDescriptionHBox.Add(this.mvcDescriptionLabel);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.mvcDescriptionHBox[this.mvcDescriptionLabel]));
			w13.Position = 1;
			w13.Expand = false;
			w13.Fill = false;
			this.mvcVBox.Add(this.mvcDescriptionHBox);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.mvcVBox[this.mvcDescriptionHBox]));
			w14.Position = 1;
			w14.Expand = false;
			w14.Fill = false;
			this.configurationTable.Add(this.mvcVBox);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.mvcVBox]));
			w15.LeftAttach = ((uint)(1));
			w15.RightAttach = ((uint)(2));
			w15.XOptions = ((global::Gtk.AttachOptions)(4));
			w15.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.paddingLabel = new global::Gtk.Label();
			this.paddingLabel.WidthRequest = 132;
			this.paddingLabel.Name = "paddingLabel";
			this.paddingLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.paddingLabel);
			global::Gtk.Table.TableChild w16 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.paddingLabel]));
			w16.TopAttach = ((uint)(5));
			w16.BottomAttach = ((uint)(6));
			w16.XOptions = ((global::Gtk.AttachOptions)(4));
			w16.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.testingLabelVBox = new global::Gtk.VBox();
			this.testingLabelVBox.Name = "testingLabelVBox";
			// Container child testingLabelVBox.Gtk.Box+BoxChild
			this.testingLabelPadding = new global::Gtk.Label();
			this.testingLabelPadding.WidthRequest = 0;
			this.testingLabelPadding.HeightRequest = 3;
			this.testingLabelPadding.Name = "testingLabelPadding";
			this.testingLabelVBox.Add(this.testingLabelPadding);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.testingLabelVBox[this.testingLabelPadding]));
			w17.Position = 0;
			w17.Expand = false;
			w17.Fill = false;
			// Container child testingLabelVBox.Gtk.Box+BoxChild
			this.testingLabel = new global::Gtk.Label();
			this.testingLabel.Name = "testingLabel";
			this.testingLabel.Xpad = 5;
			this.testingLabel.Xalign = 1F;
			this.testingLabel.Yalign = 0F;
			this.testingLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Testing:");
			this.testingLabel.Justify = ((global::Gtk.Justification)(1));
			this.testingLabelVBox.Add(this.testingLabel);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.testingLabelVBox[this.testingLabel]));
			w18.Position = 1;
			w18.Expand = false;
			w18.Fill = false;
			this.configurationTable.Add(this.testingLabelVBox);
			global::Gtk.Table.TableChild w19 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.testingLabelVBox]));
			w19.TopAttach = ((uint)(4));
			w19.BottomAttach = ((uint)(5));
			w19.XOptions = ((global::Gtk.AttachOptions)(4));
			w19.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.testingSeparator = new global::Gtk.DrawingArea();
			this.testingSeparator.WidthRequest = 440;
			this.testingSeparator.HeightRequest = 1;
			this.testingSeparator.Name = "testingSeparator";
			this.configurationTable.Add(this.testingSeparator);
			global::Gtk.Table.TableChild w20 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.testingSeparator]));
			w20.TopAttach = ((uint)(3));
			w20.BottomAttach = ((uint)(4));
			w20.RightAttach = ((uint)(3));
			w20.YPadding = ((uint)(10));
			w20.XOptions = ((global::Gtk.AttachOptions)(4));
			w20.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child configurationTable.Gtk.Table+TableChild
			this.webApiVBox = new global::Gtk.VBox();
			this.webApiVBox.Name = "webApiVBox";
			// Container child webApiVBox.Gtk.Box+BoxChild
			this.includeWebApiCheck = new global::Gtk.CheckButton();
			this.includeWebApiCheck.CanFocus = true;
			this.includeWebApiCheck.Name = "includeWebApiCheck";
			this.includeWebApiCheck.Label = global::Mono.Unix.Catalog.GetString("Web API");
			this.includeWebApiCheck.Active = true;
			this.includeWebApiCheck.DrawIndicator = true;
			this.includeWebApiCheck.UseUnderline = true;
			this.webApiVBox.Add(this.includeWebApiCheck);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.webApiVBox[this.includeWebApiCheck]));
			w21.Position = 0;
			w21.Expand = false;
			w21.Fill = false;
			// Container child webApiVBox.Gtk.Box+BoxChild
			this.webApiDescriptionHBox = new global::Gtk.HBox();
			this.webApiDescriptionHBox.Name = "webApiDescriptionHBox";
			// Container child webApiDescriptionHBox.Gtk.Box+BoxChild
			this.webApiDescriptionLeftHandPadding = new global::Gtk.Label();
			this.webApiDescriptionLeftHandPadding.WidthRequest = 21;
			this.webApiDescriptionLeftHandPadding.Name = "webApiDescriptionLeftHandPadding";
			this.webApiDescriptionHBox.Add(this.webApiDescriptionLeftHandPadding);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.webApiDescriptionHBox[this.webApiDescriptionLeftHandPadding]));
			w22.Position = 0;
			w22.Expand = false;
			w22.Fill = false;
			// Container child webApiDescriptionHBox.Gtk.Box+BoxChild
			this.webApiDescriptionLabel = new global::Gtk.Label();
			this.webApiDescriptionLabel.WidthRequest = 255;
			this.webApiDescriptionLabel.Name = "webApiDescriptionLabel";
			this.webApiDescriptionLabel.Xalign = 0F;
			this.webApiDescriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<span size=\'smaller\' color=\'grey\'>Framework for creating HTTP web services</span>" +
					"");
			this.webApiDescriptionLabel.UseMarkup = true;
			this.webApiDescriptionLabel.Wrap = true;
			this.webApiDescriptionHBox.Add(this.webApiDescriptionLabel);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.webApiDescriptionHBox[this.webApiDescriptionLabel]));
			w23.Position = 1;
			w23.Expand = false;
			w23.Fill = false;
			this.webApiVBox.Add(this.webApiDescriptionHBox);
			global::Gtk.Box.BoxChild w24 = ((global::Gtk.Box.BoxChild)(this.webApiVBox[this.webApiDescriptionHBox]));
			w24.Position = 1;
			w24.Expand = false;
			w24.Fill = false;
			this.configurationTable.Add(this.webApiVBox);
			global::Gtk.Table.TableChild w25 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.webApiVBox]));
			w25.TopAttach = ((uint)(2));
			w25.BottomAttach = ((uint)(3));
			w25.LeftAttach = ((uint)(1));
			w25.RightAttach = ((uint)(2));
			w25.XOptions = ((global::Gtk.AttachOptions)(4));
			w25.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.webFormsVBox = new global::Gtk.VBox();
			this.webFormsVBox.Name = "webFormsVBox";
			// Container child webFormsVBox.Gtk.Box+BoxChild
			this.includeWebFormsCheck = new global::Gtk.CheckButton();
			this.includeWebFormsCheck.CanFocus = true;
			this.includeWebFormsCheck.Name = "includeWebFormsCheck";
			this.includeWebFormsCheck.Label = global::Mono.Unix.Catalog.GetString("Web Forms");
			this.includeWebFormsCheck.Active = true;
			this.includeWebFormsCheck.DrawIndicator = true;
			this.includeWebFormsCheck.UseUnderline = true;
			this.webFormsVBox.Add(this.includeWebFormsCheck);
			global::Gtk.Box.BoxChild w26 = ((global::Gtk.Box.BoxChild)(this.webFormsVBox[this.includeWebFormsCheck]));
			w26.Position = 0;
			w26.Expand = false;
			w26.Fill = false;
			// Container child webFormsVBox.Gtk.Box+BoxChild
			this.webFormsDescriptionHBox = new global::Gtk.HBox();
			this.webFormsDescriptionHBox.Name = "webFormsDescriptionHBox";
			// Container child webFormsDescriptionHBox.Gtk.Box+BoxChild
			this.webFormsDescriptionLeftHandPadding = new global::Gtk.Label();
			this.webFormsDescriptionLeftHandPadding.WidthRequest = 21;
			this.webFormsDescriptionLeftHandPadding.Name = "webFormsDescriptionLeftHandPadding";
			this.webFormsDescriptionHBox.Add(this.webFormsDescriptionLeftHandPadding);
			global::Gtk.Box.BoxChild w27 = ((global::Gtk.Box.BoxChild)(this.webFormsDescriptionHBox[this.webFormsDescriptionLeftHandPadding]));
			w27.Position = 0;
			w27.Expand = false;
			w27.Fill = false;
			// Container child webFormsDescriptionHBox.Gtk.Box+BoxChild
			this.webFormsDescriptionLabel = new global::Gtk.Label();
			this.webFormsDescriptionLabel.WidthRequest = 255;
			this.webFormsDescriptionLabel.Name = "webFormsDescriptionLabel";
			this.webFormsDescriptionLabel.Xalign = 0F;
			this.webFormsDescriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("<span size=\'smaller\' color=\'grey\'>Stateful programming model similar to desktop a" +
					"pplications</span>");
			this.webFormsDescriptionLabel.UseMarkup = true;
			this.webFormsDescriptionLabel.Wrap = true;
			this.webFormsDescriptionHBox.Add(this.webFormsDescriptionLabel);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(this.webFormsDescriptionHBox[this.webFormsDescriptionLabel]));
			w28.Position = 1;
			w28.Expand = false;
			w28.Fill = false;
			this.webFormsVBox.Add(this.webFormsDescriptionHBox);
			global::Gtk.Box.BoxChild w29 = ((global::Gtk.Box.BoxChild)(this.webFormsVBox[this.webFormsDescriptionHBox]));
			w29.Position = 1;
			w29.Expand = false;
			w29.Fill = false;
			this.configurationTable.Add(this.webFormsVBox);
			global::Gtk.Table.TableChild w30 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.webFormsVBox]));
			w30.TopAttach = ((uint)(1));
			w30.BottomAttach = ((uint)(2));
			w30.LeftAttach = ((uint)(1));
			w30.RightAttach = ((uint)(2));
			w30.XOptions = ((global::Gtk.AttachOptions)(4));
			w30.YOptions = ((global::Gtk.AttachOptions)(4));
			this.configurationTableEventBox.Add(this.configurationTable);
			this.configurationVBox.Add(this.configurationTableEventBox);
			global::Gtk.Box.BoxChild w32 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationTableEventBox]));
			w32.Position = 1;
			w32.Expand = false;
			w32.Fill = false;
			// Container child configurationVBox.Gtk.Box+BoxChild
			this.configurationBottomEventBox = new global::Gtk.EventBox();
			this.configurationBottomEventBox.Name = "configurationBottomEventBox";
			this.configurationVBox.Add(this.configurationBottomEventBox);
			global::Gtk.Box.BoxChild w33 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationBottomEventBox]));
			w33.Position = 2;
			this.mainHBox.Add(this.configurationVBox);
			global::Gtk.Box.BoxChild w34 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.configurationVBox]));
			w34.Position = 1;
			w34.Expand = false;
			w34.Fill = false;
			// Container child mainHBox.Gtk.Box+BoxChild
			this.backgroundLargeImageEventBox = new global::Gtk.EventBox();
			this.backgroundLargeImageEventBox.Name = "backgroundLargeImageEventBox";
			// Container child backgroundLargeImageEventBox.Gtk.Container+ContainerChild
			this.backgroundLargeImageVBox = new global::Gtk.VBox();
			this.backgroundLargeImageVBox.Name = "backgroundLargeImageVBox";
			this.backgroundLargeImageEventBox.Add(this.backgroundLargeImageVBox);
			this.mainHBox.Add(this.backgroundLargeImageEventBox);
			global::Gtk.Box.BoxChild w36 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.backgroundLargeImageEventBox]));
			w36.Position = 2;
			this.Add(this.mainHBox);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
