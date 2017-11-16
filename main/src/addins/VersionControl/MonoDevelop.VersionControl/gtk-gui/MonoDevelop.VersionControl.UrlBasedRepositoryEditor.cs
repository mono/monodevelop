#pragma warning disable 436

namespace MonoDevelop.VersionControl
{
	public partial class UrlBasedRepositoryEditor
	{
		private global::Gtk.Table table1;

		private global::Gtk.HBox hbox1;

		private global::Gtk.ComboBox comboProtocol;

		private global::Gtk.HBox hbox2;

		private global::Gtk.SpinButton repositoryPortSpin;

		private global::Gtk.HSeparator hseparator2;

		private global::Gtk.Label label11;

		private global::Gtk.Label label4;

		private global::Gtk.Label label5;

		private global::Gtk.Label label6;

		private global::Gtk.Label label7;

		private global::Gtk.Label label8;

		private global::Gtk.Label labelError;

		private global::Gtk.Entry repositoryPathEntry;

		private global::Gtk.Entry repositoryServerEntry;

		private global::Gtk.Entry repositoryUrlEntry;

		private global::Gtk.Entry repositoryUserEntry;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.VersionControl.UrlBasedRepositoryEditor
			global::Stetic.BinContainer.Attach(this);
			this.Events = ((global::Gdk.EventMask)(256));
			this.Name = "MonoDevelop.VersionControl.UrlBasedRepositoryEditor";
			// Container child MonoDevelop.VersionControl.UrlBasedRepositoryEditor.Gtk.Container+ContainerChild
			this.table1 = new global::Gtk.Table(((uint)(8)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			this.table1.BorderWidth = ((uint)(12));
			// Container child table1.Gtk.Table+TableChild
			this.hbox1 = new global::Gtk.HBox();
			this.hbox1.Name = "hbox1";
			// Container child hbox1.Gtk.Box+BoxChild
			this.comboProtocol = global::Gtk.ComboBox.NewText();
			this.comboProtocol.Name = "comboProtocol";
			this.hbox1.Add(this.comboProtocol);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.hbox1[this.comboProtocol]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			this.table1.Add(this.hbox1);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table1[this.hbox1]));
			w2.TopAttach = ((uint)(3));
			w2.BottomAttach = ((uint)(4));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.XOptions = ((global::Gtk.AttachOptions)(4));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.hbox2 = new global::Gtk.HBox();
			this.hbox2.Name = "hbox2";
			// Container child hbox2.Gtk.Box+BoxChild
			this.repositoryPortSpin = new global::Gtk.SpinButton(0D, 99999D, 1D);
			this.repositoryPortSpin.CanFocus = true;
			this.repositoryPortSpin.Name = "repositoryPortSpin";
			this.repositoryPortSpin.Adjustment.PageIncrement = 10D;
			this.repositoryPortSpin.ClimbRate = 1D;
			this.repositoryPortSpin.Numeric = true;
			this.repositoryPortSpin.Value = 1D;
			this.hbox2.Add(this.repositoryPortSpin);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox2[this.repositoryPortSpin]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			this.table1.Add(this.hbox2);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.hbox2]));
			w4.TopAttach = ((uint)(5));
			w4.BottomAttach = ((uint)(6));
			w4.LeftAttach = ((uint)(1));
			w4.RightAttach = ((uint)(2));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.hseparator2 = new global::Gtk.HSeparator();
			this.hseparator2.Name = "hseparator2";
			this.table1.Add(this.hseparator2);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.hseparator2]));
			w5.TopAttach = ((uint)(2));
			w5.BottomAttach = ((uint)(3));
			w5.RightAttach = ((uint)(2));
			w5.YPadding = ((uint)(6));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label11 = new global::Gtk.Label();
			this.label11.Name = "label11";
			this.label11.Xalign = 0F;
			this.label11.LabelProp = global::Mono.Unix.Catalog.GetString("Server:");
			this.table1.Add(this.label11);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.label11]));
			w6.TopAttach = ((uint)(4));
			w6.BottomAttach = ((uint)(5));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.Xalign = 0F;
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Url:");
			this.table1.Add(this.label4);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table1[this.label4]));
			w7.XOptions = ((global::Gtk.AttachOptions)(4));
			w7.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.label5 = new global::Gtk.Label();
			this.label5.Name = "label5";
			this.label5.Xalign = 0F;
			this.label5.LabelProp = global::Mono.Unix.Catalog.GetString("Protocol:");
			this.table1.Add(this.label5);
			global::Gtk.Table.TableChild w8 = ((global::Gtk.Table.TableChild)(this.table1[this.label5]));
			w8.TopAttach = ((uint)(3));
			w8.BottomAttach = ((uint)(4));
			w8.XOptions = ((global::Gtk.AttachOptions)(4));
			w8.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label6 = new global::Gtk.Label();
			this.label6.Name = "label6";
			this.label6.Xalign = 0F;
			this.label6.LabelProp = global::Mono.Unix.Catalog.GetString("Port:");
			this.table1.Add(this.label6);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.table1[this.label6]));
			w9.TopAttach = ((uint)(5));
			w9.BottomAttach = ((uint)(6));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.label7 = new global::Gtk.Label();
			this.label7.Name = "label7";
			this.label7.Xalign = 0F;
			this.label7.LabelProp = global::Mono.Unix.Catalog.GetString("Path:");
			this.table1.Add(this.label7);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.table1[this.label7]));
			w10.TopAttach = ((uint)(6));
			w10.BottomAttach = ((uint)(7));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.label8 = new global::Gtk.Label();
			this.label8.Name = "label8";
			this.label8.Xalign = 0F;
			this.label8.LabelProp = global::Mono.Unix.Catalog.GetString("User:");
			this.table1.Add(this.label8);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.table1[this.label8]));
			w11.TopAttach = ((uint)(7));
			w11.BottomAttach = ((uint)(8));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.labelError = new global::Gtk.Label();
			this.labelError.Name = "labelError";
			this.labelError.Xalign = 0F;
			this.labelError.UseMarkup = true;
			this.table1.Add(this.labelError);
			global::Gtk.Table.TableChild w12 = ((global::Gtk.Table.TableChild)(this.table1[this.labelError]));
			w12.TopAttach = ((uint)(1));
			w12.BottomAttach = ((uint)(2));
			w12.LeftAttach = ((uint)(1));
			w12.RightAttach = ((uint)(2));
			w12.XOptions = ((global::Gtk.AttachOptions)(4));
			w12.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.repositoryPathEntry = new global::Gtk.Entry();
			this.repositoryPathEntry.CanFocus = true;
			this.repositoryPathEntry.Name = "repositoryPathEntry";
			this.repositoryPathEntry.IsEditable = true;
			this.repositoryPathEntry.InvisibleChar = '●';
			this.table1.Add(this.repositoryPathEntry);
			global::Gtk.Table.TableChild w13 = ((global::Gtk.Table.TableChild)(this.table1[this.repositoryPathEntry]));
			w13.TopAttach = ((uint)(6));
			w13.BottomAttach = ((uint)(7));
			w13.LeftAttach = ((uint)(1));
			w13.RightAttach = ((uint)(2));
			w13.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.repositoryServerEntry = new global::Gtk.Entry();
			this.repositoryServerEntry.CanFocus = true;
			this.repositoryServerEntry.Name = "repositoryServerEntry";
			this.repositoryServerEntry.IsEditable = true;
			this.repositoryServerEntry.InvisibleChar = '●';
			this.table1.Add(this.repositoryServerEntry);
			global::Gtk.Table.TableChild w14 = ((global::Gtk.Table.TableChild)(this.table1[this.repositoryServerEntry]));
			w14.TopAttach = ((uint)(4));
			w14.BottomAttach = ((uint)(5));
			w14.LeftAttach = ((uint)(1));
			w14.RightAttach = ((uint)(2));
			w14.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.repositoryUrlEntry = new global::Gtk.Entry();
			this.repositoryUrlEntry.CanFocus = true;
			this.repositoryUrlEntry.Name = "repositoryUrlEntry";
			this.repositoryUrlEntry.IsEditable = true;
			this.repositoryUrlEntry.InvisibleChar = '●';
			this.table1.Add(this.repositoryUrlEntry);
			global::Gtk.Table.TableChild w15 = ((global::Gtk.Table.TableChild)(this.table1[this.repositoryUrlEntry]));
			w15.LeftAttach = ((uint)(1));
			w15.RightAttach = ((uint)(2));
			w15.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table1.Gtk.Table+TableChild
			this.repositoryUserEntry = new global::Gtk.Entry();
			this.repositoryUserEntry.CanFocus = true;
			this.repositoryUserEntry.Name = "repositoryUserEntry";
			this.repositoryUserEntry.IsEditable = true;
			this.repositoryUserEntry.InvisibleChar = '●';
			this.table1.Add(this.repositoryUserEntry);
			global::Gtk.Table.TableChild w16 = ((global::Gtk.Table.TableChild)(this.table1[this.repositoryUserEntry]));
			w16.TopAttach = ((uint)(7));
			w16.BottomAttach = ((uint)(8));
			w16.LeftAttach = ((uint)(1));
			w16.RightAttach = ((uint)(2));
			w16.YOptions = ((global::Gtk.AttachOptions)(0));
			this.Add(this.table1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.labelError.Hide();
			this.Show();
			this.repositoryUserEntry.Changed += new global::System.EventHandler(this.OnRepositoryUserEntryChanged);
			this.repositoryUrlEntry.Changed += new global::System.EventHandler(this.OnRepositoryUrlEntryChanged);
			this.repositoryUrlEntry.ClipboardPasted += new global::System.EventHandler(this.OnRepositoryUrlEntryClipboardPasted);
			this.repositoryServerEntry.Changed += new global::System.EventHandler(this.OnRepositoryServerEntryChanged);
			this.repositoryPathEntry.Changed += new global::System.EventHandler(this.OnRepositoryPathEntryChanged);
			this.repositoryPortSpin.ValueChanged += new global::System.EventHandler(this.OnRepositoryPortSpinValueChanged);
			this.comboProtocol.Changed += new global::System.EventHandler(this.OnComboProtocolChanged);
		}
	}
}
#pragma warning restore 436
