#pragma warning disable 436

namespace MonoDevelop.Deployment.Gui
{
	internal partial class SshFuseFileCopyConfigurationEditorWidget
	{
		private global::Gtk.Table table1;

		private global::Gtk.Entry entryDirectory;

		private global::Gtk.Entry entryHostName;

		private global::Gtk.Entry entryUserName;

		private global::Gtk.Label label1;

		private global::Gtk.Label label2;

		private global::Gtk.Label label3;

		private global::Gtk.Label label4;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Deployment.Gui.SshFuseFileCopyConfigurationEditorWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Deployment.Gui.SshFuseFileCopyConfigurationEditorWidget";
			// Container child MonoDevelop.Deployment.Gui.SshFuseFileCopyConfigurationEditorWidget.Gtk.Container+ContainerChild
			this.table1 = new global::Gtk.Table(((uint)(4)), ((uint)(2)), false);
			this.table1.Name = "table1";
			this.table1.RowSpacing = ((uint)(6));
			this.table1.ColumnSpacing = ((uint)(6));
			// Container child table1.Gtk.Table+TableChild
			this.entryDirectory = new global::Gtk.Entry();
			this.entryDirectory.CanFocus = true;
			this.entryDirectory.Name = "entryDirectory";
			this.entryDirectory.IsEditable = true;
			this.entryDirectory.InvisibleChar = '●';
			this.table1.Add(this.entryDirectory);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.table1[this.entryDirectory]));
			w1.TopAttach = ((uint)(1));
			w1.BottomAttach = ((uint)(2));
			w1.LeftAttach = ((uint)(1));
			w1.RightAttach = ((uint)(2));
			w1.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryHostName = new global::Gtk.Entry();
			this.entryHostName.CanFocus = true;
			this.entryHostName.Name = "entryHostName";
			this.entryHostName.IsEditable = true;
			this.entryHostName.InvisibleChar = '●';
			this.table1.Add(this.entryHostName);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table1[this.entryHostName]));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.entryUserName = new global::Gtk.Entry();
			this.entryUserName.CanFocus = true;
			this.entryUserName.Name = "entryUserName";
			this.entryUserName.IsEditable = true;
			this.entryUserName.InvisibleChar = '●';
			this.table1.Add(this.entryUserName);
			global::Gtk.Table.TableChild w3 = ((global::Gtk.Table.TableChild)(this.table1[this.entryUserName]));
			w3.TopAttach = ((uint)(2));
			w3.BottomAttach = ((uint)(3));
			w3.LeftAttach = ((uint)(1));
			w3.RightAttach = ((uint)(2));
			w3.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Host name:");
			this.table1.Add(this.label1);
			global::Gtk.Table.TableChild w4 = ((global::Gtk.Table.TableChild)(this.table1[this.label1]));
			w4.XOptions = ((global::Gtk.AttachOptions)(4));
			w4.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.Xalign = 0F;
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Directory:");
			this.table1.Add(this.label2);
			global::Gtk.Table.TableChild w5 = ((global::Gtk.Table.TableChild)(this.table1[this.label2]));
			w5.TopAttach = ((uint)(1));
			w5.BottomAttach = ((uint)(2));
			w5.XOptions = ((global::Gtk.AttachOptions)(4));
			w5.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.Xalign = 0F;
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Username:");
			this.table1.Add(this.label3);
			global::Gtk.Table.TableChild w6 = ((global::Gtk.Table.TableChild)(this.table1[this.label3]));
			w6.TopAttach = ((uint)(2));
			w6.BottomAttach = ((uint)(3));
			w6.XOptions = ((global::Gtk.AttachOptions)(4));
			w6.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child table1.Gtk.Table+TableChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Note: the SSH key for this host must be installed on your system. If it is passwo" +
					"rd protected, the password must be loaded into a running SSH authentication daem" +
					"on, such as <i>ssh-agent</i> or <i>seahorse-agent</i>.");
			this.label4.UseMarkup = true;
			this.label4.Wrap = true;
			this.label4.Justify = ((global::Gtk.Justification)(3));
			this.table1.Add(this.label4);
			global::Gtk.Table.TableChild w7 = ((global::Gtk.Table.TableChild)(this.table1[this.label4]));
			w7.TopAttach = ((uint)(3));
			w7.BottomAttach = ((uint)(4));
			w7.RightAttach = ((uint)(2));
			w7.YOptions = ((global::Gtk.AttachOptions)(4));
			this.Add(this.table1);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
			this.entryUserName.Changed += new global::System.EventHandler(this.UserNameChanged);
			this.entryHostName.Changed += new global::System.EventHandler(this.HostnameChanged);
			this.entryDirectory.Changed += new global::System.EventHandler(this.DirectoryChanged);
		}
	}
}
#pragma warning restore 436
