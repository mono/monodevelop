#pragma warning disable 436

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	public partial class RunOptionsPanelWidget
	{
		private global::Gtk.VBox vbox67;

		private global::Gtk.VBox vbox69;

		private global::Gtk.Table table10;

		private global::Gtk.Label label100;

		private global::Gtk.Entry parametersEntry;

		private global::Gtk.CheckButton externalConsoleCheckButton;

		private global::Gtk.CheckButton pauseConsoleOutputCheckButton;

		private global::Gtk.HSeparator hseparator1;

		private global::Gtk.Label label1;

		private global::MonoDevelop.Ide.Gui.Components.EnvVarList envVarList;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Projects.OptionPanels.RunOptionsPanelWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Projects.OptionPanels.RunOptionsPanelWidget";
			// Container child MonoDevelop.Ide.Projects.OptionPanels.RunOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox67 = new global::Gtk.VBox();
			this.vbox67.Name = "vbox67";
			this.vbox67.Spacing = 6;
			// Container child vbox67.Gtk.Box+BoxChild
			this.vbox69 = new global::Gtk.VBox();
			this.vbox69.Name = "vbox69";
			this.vbox69.Spacing = 6;
			// Container child vbox69.Gtk.Box+BoxChild
			this.table10 = new global::Gtk.Table(((uint)(1)), ((uint)(2)), false);
			this.table10.Name = "table10";
			this.table10.ColumnSpacing = ((uint)(6));
			// Container child table10.Gtk.Table+TableChild
			this.label100 = new global::Gtk.Label();
			this.label100.Name = "label100";
			this.label100.Xalign = 0F;
			this.label100.LabelProp = global::Mono.Unix.Catalog.GetString("Paramet_ers:");
			this.label100.UseUnderline = true;
			this.table10.Add(this.label100);
			global::Gtk.Table.TableChild w1 = ((global::Gtk.Table.TableChild)(this.table10[this.label100]));
			w1.XOptions = ((global::Gtk.AttachOptions)(4));
			w1.YOptions = ((global::Gtk.AttachOptions)(0));
			// Container child table10.Gtk.Table+TableChild
			this.parametersEntry = new global::Gtk.Entry();
			this.parametersEntry.Name = "parametersEntry";
			this.parametersEntry.IsEditable = true;
			this.parametersEntry.InvisibleChar = '●';
			this.table10.Add(this.parametersEntry);
			global::Gtk.Table.TableChild w2 = ((global::Gtk.Table.TableChild)(this.table10[this.parametersEntry]));
			w2.LeftAttach = ((uint)(1));
			w2.RightAttach = ((uint)(2));
			w2.YOptions = ((global::Gtk.AttachOptions)(0));
			this.vbox69.Add(this.table10);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox69[this.table10]));
			w3.Position = 0;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox69.Gtk.Box+BoxChild
			this.externalConsoleCheckButton = new global::Gtk.CheckButton();
			this.externalConsoleCheckButton.Name = "externalConsoleCheckButton";
			this.externalConsoleCheckButton.Label = global::Mono.Unix.Catalog.GetString("Run on e_xternal console");
			this.externalConsoleCheckButton.DrawIndicator = true;
			this.externalConsoleCheckButton.UseUnderline = true;
			this.vbox69.Add(this.externalConsoleCheckButton);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.vbox69[this.externalConsoleCheckButton]));
			w4.Position = 1;
			w4.Expand = false;
			w4.Fill = false;
			// Container child vbox69.Gtk.Box+BoxChild
			this.pauseConsoleOutputCheckButton = new global::Gtk.CheckButton();
			this.pauseConsoleOutputCheckButton.Name = "pauseConsoleOutputCheckButton";
			this.pauseConsoleOutputCheckButton.Label = global::Mono.Unix.Catalog.GetString("Pause _console output");
			this.pauseConsoleOutputCheckButton.DrawIndicator = true;
			this.pauseConsoleOutputCheckButton.UseUnderline = true;
			this.vbox69.Add(this.pauseConsoleOutputCheckButton);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox69[this.pauseConsoleOutputCheckButton]));
			w5.Position = 2;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox69.Gtk.Box+BoxChild
			this.hseparator1 = new global::Gtk.HSeparator();
			this.hseparator1.Name = "hseparator1";
			this.vbox69.Add(this.hseparator1);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox69[this.hseparator1]));
			w6.Position = 3;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox69.Gtk.Box+BoxChild
			this.label1 = new global::Gtk.Label();
			this.label1.Name = "label1";
			this.label1.Xalign = 0F;
			this.label1.LabelProp = global::Mono.Unix.Catalog.GetString("Environment Variables:");
			this.vbox69.Add(this.label1);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox69[this.label1]));
			w7.Position = 4;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox69.Gtk.Box+BoxChild
			this.envVarList = new global::MonoDevelop.Ide.Gui.Components.EnvVarList();
			this.envVarList.CanFocus = true;
			this.envVarList.Name = "envVarList";
			this.vbox69.Add(this.envVarList);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox69[this.envVarList]));
			w8.Position = 5;
			this.vbox67.Add(this.vbox69);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.vbox67[this.vbox69]));
			w9.Position = 0;
			this.Add(this.vbox67);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.label100.MnemonicWidget = this.parametersEntry;
			this.Show();
		}
	}
}
#pragma warning restore 436
