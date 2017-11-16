#pragma warning disable 436

namespace MonoDevelop.Packaging.Gui
{
	public partial class GtkCrossPlatformLibraryProjectTemplateWizardPageWidget
	{
		private global::Gtk.HBox mainHBox;

		private global::Gtk.EventBox leftBorderEventBox;

		private global::Gtk.VBox configurationVBox;

		private global::Gtk.EventBox configurationTopEventBox;

		private global::Gtk.EventBox configurationTableEventBox;

		private global::Gtk.Table configurationTable;

		private global::Gtk.Label descriptionLabel;

		private global::Gtk.Entry descriptionTextBox;

		private global::Gtk.Label implementationLabel;

		private global::Gtk.EventBox nameEventBox;

		private global::Gtk.Label nameLabel;

		private global::Gtk.Entry nameTextBox;

		private global::Gtk.EventBox organizationInfoEventBox;

		private global::Gtk.Label paddingLabelSharedCode;

		private global::Gtk.Label paddingLabelTargetPlatforms;

		private global::Gtk.Label paddingLabelXaml;

		private global::Gtk.DrawingArea sharedCodeSeparator;

		private global::Gtk.VBox sharedCodeVBox;

		private global::Gtk.HBox usePortableClassLibraryHBox;

		private global::Gtk.RadioButton sharedProjectRadioButton;

		private global::Gtk.VBox usePortableLibraryInfoVBox;

		private global::Gtk.Label usePortableLibraryInfoIconPaddingLabel;

		private global::Gtk.EventBox usePortableLibraryInfoEventBox;

		private global::Gtk.HBox useSharedLibraryHBox;

		private global::Gtk.RadioButton portableClassLibraryRadioButton;

		private global::Gtk.VBox useSharedLibraryInfoVBox;

		private global::Gtk.Label useSharedLibraryInfoIconPaddingLabel;

		private global::Gtk.EventBox useSharedLibraryInfoEventBox;

		private global::Gtk.Label targetPlatformsLabel;

		private global::Gtk.DrawingArea targetPlatformsSeparator;

		private global::Gtk.VBox targetPlatformsVBox;

		private global::Gtk.CheckButton androidCheckButton;

		private global::Gtk.CheckButton iOSCheckButton;

		private global::Gtk.EventBox configurationBottomEventBox;

		private global::Gtk.EventBox backgroundLargeImageEventBox;

		private global::Gtk.VBox backgroundLargeImageVBox;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Packaging.Gui.GtkCrossPlatformLibraryProjectTemplateWizardPageWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Packaging.Gui.GtkCrossPlatformLibraryProjectTemplateWizardPageWidget";
			// Container child MonoDevelop.Packaging.Gui.GtkCrossPlatformLibraryProjectTemplateWizardPageWidget.Gtk.Container+ContainerChild
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
			this.configurationTable = new global::Gtk.Table(((uint)(9)), ((uint)(3)), false);
			this.configurationTable.Name = "configurationTable";
			this.configurationTable.RowSpacing = ((uint)(7));
			this.configurationTable.ColumnSpacing = ((uint)(6));
			// Container child configurationTable.Gtk.Table+TableChild
			this.descriptionLabel = new global::Gtk.Label();
			this.descriptionLabel.Name = "descriptionLabel";
			this.descriptionLabel.Xpad = 5;
			this.descriptionLabel.Xalign = 1F;
			this.descriptionLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Description:");
			this.descriptionLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.descriptionLabel);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.descriptionLabel]));
			w3.TopAttach = ((uint)(1));
			w3.BottomAttach = ((uint)(2));
			w3.XOptions = ((global::Gtk.AttachOptions)(4));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.descriptionTextBox = new global::Gtk.Entry();
			this.descriptionTextBox.CanFocus = true;
			this.descriptionTextBox.Name = "descriptionTextBox";
			this.descriptionTextBox.IsEditable = true;
			this.descriptionTextBox.InvisibleChar = '●';
			this.configurationTable.Add(this.descriptionTextBox);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.descriptionTextBox]));
			w4.TopAttach = ((uint)(1));
			w4.BottomAttach = ((uint)(2));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(2));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.implementationLabel = new global::Gtk.Label();
			this.implementationLabel.Name = "implementationLabel";
			this.implementationLabel.Xpad = 5;
			this.implementationLabel.Xalign = 1F;
			this.implementationLabel.Yalign = 0.8F;
			this.implementationLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Implementation:");
			this.implementationLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.implementationLabel);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.implementationLabel]));
			w5.TopAttach = ((uint)(6));
			w5.BottomAttach = ((uint)(7));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.nameEventBox = new global::Gtk.EventBox();
			this.nameEventBox.WidthRequest = 16;
			this.nameEventBox.HeightRequest = 16;
			this.nameEventBox.Name = "nameEventBox";
			this.nameEventBox.VisibleWindow = false;
			this.configurationTable.Add(this.nameEventBox);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.nameEventBox]));
			w6.LeftAttach = ((uint)(2));
			w6.RightAttach = ((uint)(3));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.nameLabel = new global::Gtk.Label();
			this.nameLabel.Name = "nameLabel";
			this.nameLabel.Xpad = 5;
			this.nameLabel.Xalign = 1F;
			this.nameLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Name:");
			this.nameLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.nameLabel);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.nameLabel]));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.nameTextBox = new global::Gtk.Entry();
			this.nameTextBox.CanFocus = true;
			this.nameTextBox.Name = "nameTextBox";
			this.nameTextBox.IsEditable = true;
			this.nameTextBox.InvisibleChar = '●';
			this.configurationTable.Add(this.nameTextBox);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.nameTextBox]));
			w8.LeftAttach = ((uint)(1));
			w8.RightAttach = ((uint)(2));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.organizationInfoEventBox = new global::Gtk.EventBox();
			this.organizationInfoEventBox.WidthRequest = 16;
			this.organizationInfoEventBox.HeightRequest = 16;
			this.organizationInfoEventBox.Name = "organizationInfoEventBox";
			this.organizationInfoEventBox.VisibleWindow = false;
			this.configurationTable.Add(this.organizationInfoEventBox);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.organizationInfoEventBox]));
			w9.TopAttach = ((uint)(1));
			w9.BottomAttach = ((uint)(2));
			w9.LeftAttach = ((uint)(2));
			w9.RightAttach = ((uint)(3));
			w9.XOptions = ((global::Gtk.AttachOptions)(2));
			w9.YOptions = ((global::Gtk.AttachOptions)(2));
			// Container child configurationTable.Gtk.Table+TableChild
			this.paddingLabelSharedCode = new global::Gtk.Label();
			this.paddingLabelSharedCode.WidthRequest = 132;
			this.paddingLabelSharedCode.Name = "paddingLabelSharedCode";
			this.paddingLabelSharedCode.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.paddingLabelSharedCode);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.paddingLabelSharedCode]));
			w10.TopAttach = ((uint)(7));
			w10.BottomAttach = ((uint)(8));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.paddingLabelTargetPlatforms = new global::Gtk.Label();
			this.paddingLabelTargetPlatforms.WidthRequest = 132;
			this.paddingLabelTargetPlatforms.Name = "paddingLabelTargetPlatforms";
			this.paddingLabelTargetPlatforms.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.paddingLabelTargetPlatforms);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.paddingLabelTargetPlatforms]));
			w11.TopAttach = ((uint)(4));
			w11.BottomAttach = ((uint)(5));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.paddingLabelXaml = new global::Gtk.Label();
			this.paddingLabelXaml.WidthRequest = 132;
			this.paddingLabelXaml.HeightRequest = 5;
			this.paddingLabelXaml.Name = "paddingLabelXaml";
			this.paddingLabelXaml.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.paddingLabelXaml);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.paddingLabelXaml]));
			w12.TopAttach = ((uint)(8));
			w12.BottomAttach = ((uint)(9));
			w12.XOptions = ((global::Gtk.AttachOptions)(4));
			w12.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.sharedCodeSeparator = new global::Gtk.DrawingArea();
			this.sharedCodeSeparator.HeightRequest = 1;
			this.sharedCodeSeparator.Name = "sharedCodeSeparator";
			this.configurationTable.Add(this.sharedCodeSeparator);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.sharedCodeSeparator]));
			w13.TopAttach = ((uint)(5));
			w13.BottomAttach = ((uint)(6));
			w13.RightAttach = ((uint)(3));
			w13.YPadding = ((uint)(10));
			w13.XOptions = ((global::Gtk.AttachOptions)(4));
			w13.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child configurationTable.Gtk.Table+TableChild
			this.sharedCodeVBox = new global::Gtk.VBox();
			this.sharedCodeVBox.Name = "sharedCodeVBox";
			// Container child sharedCodeVBox.Gtk.Box+BoxChild
			this.usePortableClassLibraryHBox = new global::Gtk.HBox();
			this.usePortableClassLibraryHBox.Name = "usePortableClassLibraryHBox";
			// Container child usePortableClassLibraryHBox.Gtk.Box+BoxChild
			this.sharedProjectRadioButton = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Platform specific"));
			this.sharedProjectRadioButton.CanFocus = true;
			this.sharedProjectRadioButton.Name = "sharedProjectRadioButton";
			this.sharedProjectRadioButton.Active = true;
			this.sharedProjectRadioButton.DrawIndicator = true;
			this.sharedProjectRadioButton.UseUnderline = true;
			this.sharedProjectRadioButton.Group = new global::GLib.SList(global::System.IntPtr.Zero);
			this.usePortableClassLibraryHBox.Add(this.sharedProjectRadioButton);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.usePortableClassLibraryHBox[this.sharedProjectRadioButton]));
			w14.Position = 0;
			// Container child usePortableClassLibraryHBox.Gtk.Box+BoxChild
			this.usePortableLibraryInfoVBox = new global::Gtk.VBox();
			this.usePortableLibraryInfoVBox.Name = "usePortableLibraryInfoVBox";
			// Container child usePortableLibraryInfoVBox.Gtk.Box+BoxChild
			this.usePortableLibraryInfoIconPaddingLabel = new global::Gtk.Label();
			this.usePortableLibraryInfoIconPaddingLabel.HeightRequest = 1;
			this.usePortableLibraryInfoIconPaddingLabel.Name = "usePortableLibraryInfoIconPaddingLabel";
			this.usePortableLibraryInfoVBox.Add(this.usePortableLibraryInfoIconPaddingLabel);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.usePortableLibraryInfoVBox[this.usePortableLibraryInfoIconPaddingLabel]));
			w15.Position = 0;
			w15.Expand = false;
			w15.Fill = false;
			// Container child usePortableLibraryInfoVBox.Gtk.Box+BoxChild
			this.usePortableLibraryInfoEventBox = new global::Gtk.EventBox();
			this.usePortableLibraryInfoEventBox.WidthRequest = 16;
			this.usePortableLibraryInfoEventBox.HeightRequest = 16;
			this.usePortableLibraryInfoEventBox.Name = "usePortableLibraryInfoEventBox";
			this.usePortableLibraryInfoEventBox.VisibleWindow = false;
			this.usePortableLibraryInfoVBox.Add(this.usePortableLibraryInfoEventBox);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.usePortableLibraryInfoVBox[this.usePortableLibraryInfoEventBox]));
			w16.Position = 1;
			this.usePortableClassLibraryHBox.Add(this.usePortableLibraryInfoVBox);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(this.usePortableClassLibraryHBox[this.usePortableLibraryInfoVBox]));
			w17.Position = 1;
			w17.Expand = false;
			this.sharedCodeVBox.Add(this.usePortableClassLibraryHBox);
			global::Gtk.Box.BoxChild w18 = ((global::Gtk.Box.BoxChild)(this.sharedCodeVBox[this.usePortableClassLibraryHBox]));
			w18.Position = 0;
			w18.Expand = false;
			w18.Fill = false;
			w18.Padding = ((uint)(1));
			// Container child sharedCodeVBox.Gtk.Box+BoxChild
			this.useSharedLibraryHBox = new global::Gtk.HBox();
			this.useSharedLibraryHBox.Name = "useSharedLibraryHBox";
			// Container child useSharedLibraryHBox.Gtk.Box+BoxChild
			this.portableClassLibraryRadioButton = new global::Gtk.RadioButton(global::Mono.Unix.Catalog.GetString("Single for all platforms"));
			this.portableClassLibraryRadioButton.CanFocus = true;
			this.portableClassLibraryRadioButton.Name = "portableClassLibraryRadioButton";
			this.portableClassLibraryRadioButton.DrawIndicator = true;
			this.portableClassLibraryRadioButton.UseUnderline = true;
			this.portableClassLibraryRadioButton.Group = this.sharedProjectRadioButton.Group;
			this.useSharedLibraryHBox.Add(this.portableClassLibraryRadioButton);
			global::Gtk.Box.BoxChild w19 = ((global::Gtk.Box.BoxChild)(this.useSharedLibraryHBox[this.portableClassLibraryRadioButton]));
			w19.Position = 0;
			// Container child useSharedLibraryHBox.Gtk.Box+BoxChild
			this.useSharedLibraryInfoVBox = new global::Gtk.VBox();
			this.useSharedLibraryInfoVBox.Name = "useSharedLibraryInfoVBox";
			// Container child useSharedLibraryInfoVBox.Gtk.Box+BoxChild
			this.useSharedLibraryInfoIconPaddingLabel = new global::Gtk.Label();
			this.useSharedLibraryInfoIconPaddingLabel.HeightRequest = 1;
			this.useSharedLibraryInfoIconPaddingLabel.Name = "useSharedLibraryInfoIconPaddingLabel";
			this.useSharedLibraryInfoVBox.Add(this.useSharedLibraryInfoIconPaddingLabel);
			global::Gtk.Box.BoxChild w20 = ((global::Gtk.Box.BoxChild)(this.useSharedLibraryInfoVBox[this.useSharedLibraryInfoIconPaddingLabel]));
			w20.Position = 0;
			w20.Expand = false;
			w20.Fill = false;
			// Container child useSharedLibraryInfoVBox.Gtk.Box+BoxChild
			this.useSharedLibraryInfoEventBox = new global::Gtk.EventBox();
			this.useSharedLibraryInfoEventBox.WidthRequest = 16;
			this.useSharedLibraryInfoEventBox.HeightRequest = 16;
			this.useSharedLibraryInfoEventBox.Name = "useSharedLibraryInfoEventBox";
			this.useSharedLibraryInfoEventBox.VisibleWindow = false;
			this.useSharedLibraryInfoVBox.Add(this.useSharedLibraryInfoEventBox);
			global::Gtk.Box.BoxChild w21 = ((global::Gtk.Box.BoxChild)(this.useSharedLibraryInfoVBox[this.useSharedLibraryInfoEventBox]));
			w21.Position = 1;
			this.useSharedLibraryHBox.Add(this.useSharedLibraryInfoVBox);
			global::Gtk.Box.BoxChild w22 = ((global::Gtk.Box.BoxChild)(this.useSharedLibraryHBox[this.useSharedLibraryInfoVBox]));
			w22.Position = 1;
			w22.Expand = false;
			this.sharedCodeVBox.Add(this.useSharedLibraryHBox);
			global::Gtk.Box.BoxChild w23 = ((global::Gtk.Box.BoxChild)(this.sharedCodeVBox[this.useSharedLibraryHBox]));
			w23.Position = 1;
			w23.Expand = false;
			w23.Fill = false;
			w23.Padding = ((uint)(1));
			this.configurationTable.Add(this.sharedCodeVBox);
			global::Gtk.Table.TableChild w24 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.sharedCodeVBox]));
			w24.TopAttach = ((uint)(6));
			w24.BottomAttach = ((uint)(8));
			w24.LeftAttach = ((uint)(1));
			w24.RightAttach = ((uint)(3));
			w24.XOptions = ((global::Gtk.AttachOptions)(4));
			w24.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child configurationTable.Gtk.Table+TableChild
			this.targetPlatformsLabel = new global::Gtk.Label();
			this.targetPlatformsLabel.Name = "targetPlatformsLabel";
			this.targetPlatformsLabel.Xpad = 5;
			this.targetPlatformsLabel.Xalign = 1F;
			this.targetPlatformsLabel.Yalign = 0.8F;
			this.targetPlatformsLabel.LabelProp = global::Mono.Unix.Catalog.GetString("Target Platforms:");
			this.targetPlatformsLabel.Justify = ((global::Gtk.Justification)(1));
			this.configurationTable.Add(this.targetPlatformsLabel);
			global::Gtk.Table.TableChild w25 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.targetPlatformsLabel]));
			w25.TopAttach = ((uint)(3));
			w25.BottomAttach = ((uint)(4));
			w25.XOptions = ((global::Gtk.AttachOptions)(4));
			w25.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child configurationTable.Gtk.Table+TableChild
			this.targetPlatformsSeparator = new global::Gtk.DrawingArea();
			this.targetPlatformsSeparator.HeightRequest = 1;
			this.targetPlatformsSeparator.Name = "targetPlatformsSeparator";
			this.configurationTable.Add(this.targetPlatformsSeparator);
			global::Gtk.Table.TableChild w26 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.targetPlatformsSeparator]));
			w26.TopAttach = ((uint)(2));
			w26.BottomAttach = ((uint)(3));
			w26.RightAttach = ((uint)(3));
			w26.YPadding = ((uint)(10));
			w26.XOptions = ((global::Gtk.AttachOptions)(4));
			w26.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child configurationTable.Gtk.Table+TableChild
			this.targetPlatformsVBox = new global::Gtk.VBox();
			this.targetPlatformsVBox.Name = "targetPlatformsVBox";
			// Container child targetPlatformsVBox.Gtk.Box+BoxChild
			this.androidCheckButton = new global::Gtk.CheckButton();
			this.androidCheckButton.CanFocus = true;
			this.androidCheckButton.Name = "androidCheckButton";
			this.androidCheckButton.Label = global::Mono.Unix.Catalog.GetString("Android");
			this.androidCheckButton.Active = true;
			this.androidCheckButton.DrawIndicator = true;
			this.androidCheckButton.UseUnderline = true;
			this.targetPlatformsVBox.Add(this.androidCheckButton);
			global::Gtk.Box.BoxChild w27 = ((global::Gtk.Box.BoxChild)(this.targetPlatformsVBox[this.androidCheckButton]));
			w27.Position = 0;
			w27.Expand = false;
			w27.Fill = false;
			w27.Padding = ((uint)(1));
			// Container child targetPlatformsVBox.Gtk.Box+BoxChild
			this.iOSCheckButton = new global::Gtk.CheckButton();
			this.iOSCheckButton.CanFocus = true;
			this.iOSCheckButton.Name = "iOSCheckButton";
			this.iOSCheckButton.Label = global::Mono.Unix.Catalog.GetString("iOS");
			this.iOSCheckButton.Active = true;
			this.iOSCheckButton.DrawIndicator = true;
			this.iOSCheckButton.UseUnderline = true;
			this.targetPlatformsVBox.Add(this.iOSCheckButton);
			global::Gtk.Box.BoxChild w28 = ((global::Gtk.Box.BoxChild)(this.targetPlatformsVBox[this.iOSCheckButton]));
			w28.Position = 1;
			w28.Expand = false;
			w28.Fill = false;
			w28.Padding = ((uint)(1));
			this.configurationTable.Add(this.targetPlatformsVBox);
			global::Gtk.Table.TableChild w29 = ((global::Gtk.Table.TableChild)(this.configurationTable[this.targetPlatformsVBox]));
			w29.TopAttach = ((uint)(3));
			w29.BottomAttach = ((uint)(5));
			w29.LeftAttach = ((uint)(1));
			w29.RightAttach = ((uint)(2));
			w29.XOptions = ((global::Gtk.AttachOptions)(4));
			w29.YOptions = ((global::Gtk.AttachOptions)(0));
			this.configurationTableEventBox.Add(this.configurationTable);
			this.configurationVBox.Add(this.configurationTableEventBox);
			global::Gtk.Box.BoxChild w31 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationTableEventBox]));
			w31.Position = 1;
			w31.Expand = false;
			w31.Fill = false;
			// Container child configurationVBox.Gtk.Box+BoxChild
			this.configurationBottomEventBox = new global::Gtk.EventBox();
			this.configurationBottomEventBox.Name = "configurationBottomEventBox";
			this.configurationVBox.Add(this.configurationBottomEventBox);
			global::Gtk.Box.BoxChild w32 = ((global::Gtk.Box.BoxChild)(this.configurationVBox[this.configurationBottomEventBox]));
			w32.Position = 2;
			this.mainHBox.Add(this.configurationVBox);
			global::Gtk.Box.BoxChild w33 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.configurationVBox]));
			w33.Position = 1;
			w33.Expand = false;
			w33.Fill = false;
			// Container child mainHBox.Gtk.Box+BoxChild
			this.backgroundLargeImageEventBox = new global::Gtk.EventBox();
			this.backgroundLargeImageEventBox.Name = "backgroundLargeImageEventBox";
			// Container child backgroundLargeImageEventBox.Gtk.Container+ContainerChild
			this.backgroundLargeImageVBox = new global::Gtk.VBox();
			this.backgroundLargeImageVBox.Name = "backgroundLargeImageVBox";
			this.backgroundLargeImageEventBox.Add(this.backgroundLargeImageVBox);
			this.mainHBox.Add(this.backgroundLargeImageEventBox);
			global::Gtk.Box.BoxChild w35 = ((global::Gtk.Box.BoxChild)(this.mainHBox[this.backgroundLargeImageEventBox]));
			w35.Position = 2;
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
