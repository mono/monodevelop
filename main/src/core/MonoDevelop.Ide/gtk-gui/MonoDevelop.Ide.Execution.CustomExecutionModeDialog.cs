#pragma warning disable 436

namespace MonoDevelop.Ide.Execution
{
	internal partial class CustomExecutionModeDialog
	{
		private global::Gtk.VBox boxEditor;

		private global::Gtk.VBox boxModeSelector;

		private global::Gtk.HBox hbox5;

		private global::Gtk.Label label2;

		private global::MonoDevelop.Ide.Gui.Components.ExecutionModeComboBox comboTargetMode;

		private global::Gtk.HSeparator hseparator1;

		private global::Gtk.Notebook notebook;

		private global::Gtk.VBox boxSave;

		private global::Gtk.HSeparator hseparator;

		private global::Gtk.CheckButton checkSave;

		private global::Gtk.HBox boxName;

		private global::Gtk.Label label4;

		private global::Gtk.Entry entryModeName;

		private global::Gtk.Label label3;

		private global::Gtk.ComboBox comboStore;

		private global::Gtk.CheckButton checkPrompt;

		private global::Gtk.Button buttonCancel;

		private global::Gtk.Button buttonOk;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Execution.CustomExecutionModeDialog
			this.Name = "MonoDevelop.Ide.Execution.CustomExecutionModeDialog";
			this.Title = global::Mono.Unix.Catalog.GetString("Execution Arguments");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			// Internal child MonoDevelop.Ide.Execution.CustomExecutionModeDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.boxEditor = new global::Gtk.VBox();
			this.boxEditor.Name = "boxEditor";
			this.boxEditor.Spacing = 9;
			this.boxEditor.BorderWidth = ((uint)(6));
			// Container child boxEditor.Gtk.Box+BoxChild
			this.boxModeSelector = new global::Gtk.VBox();
			this.boxModeSelector.Name = "boxModeSelector";
			this.boxModeSelector.Spacing = 6;
			// Container child boxModeSelector.Gtk.Box+BoxChild
			this.hbox5 = new global::Gtk.HBox();
			this.hbox5.Name = "hbox5";
			this.hbox5.Spacing = 6;
			// Container child hbox5.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString("Execution Mode:");
			this.hbox5.Add(this.label2);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.label2]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox5.Gtk.Box+BoxChild
			this.comboTargetMode = new global::MonoDevelop.Ide.Gui.Components.ExecutionModeComboBox();
			this.comboTargetMode.Events = ((global::Gdk.EventMask)(256));
			this.comboTargetMode.Name = "comboTargetMode";
			this.hbox5.Add(this.comboTargetMode);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox5[this.comboTargetMode]));
			w3.Position = 1;
			this.boxModeSelector.Add(this.hbox5);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(this.boxModeSelector[this.hbox5]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Container child boxModeSelector.Gtk.Box+BoxChild
			this.hseparator1 = new global::Gtk.HSeparator();
			this.hseparator1.Name = "hseparator1";
			this.boxModeSelector.Add(this.hseparator1);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.boxModeSelector[this.hseparator1]));
			w5.Position = 1;
			w5.Expand = false;
			w5.Fill = false;
			this.boxEditor.Add(this.boxModeSelector);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.boxEditor[this.boxModeSelector]));
			w6.Position = 0;
			w6.Expand = false;
			w6.Fill = false;
			// Container child boxEditor.Gtk.Box+BoxChild
			this.notebook = new global::Gtk.Notebook();
			this.notebook.CanFocus = true;
			this.notebook.Name = "notebook";
			this.notebook.CurrentPage = -1;
			this.boxEditor.Add(this.notebook);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.boxEditor[this.notebook]));
			w7.Position = 1;
			// Container child boxEditor.Gtk.Box+BoxChild
			this.boxSave = new global::Gtk.VBox();
			this.boxSave.Name = "boxSave";
			this.boxSave.Spacing = 6;
			// Container child boxSave.Gtk.Box+BoxChild
			this.hseparator = new global::Gtk.HSeparator();
			this.hseparator.Name = "hseparator";
			this.boxSave.Add(this.hseparator);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.boxSave[this.hseparator]));
			w8.Position = 0;
			w8.Expand = false;
			w8.Fill = false;
			// Container child boxSave.Gtk.Box+BoxChild
			this.checkSave = new global::Gtk.CheckButton();
			this.checkSave.CanFocus = true;
			this.checkSave.Name = "checkSave";
			this.checkSave.Label = global::Mono.Unix.Catalog.GetString("Save this configuration as a custom execution mode");
			this.checkSave.DrawIndicator = true;
			this.checkSave.UseUnderline = true;
			this.boxSave.Add(this.checkSave);
			global::Gtk.Box.BoxChild w9 = ((global::Gtk.Box.BoxChild)(this.boxSave[this.checkSave]));
			w9.Position = 1;
			w9.Expand = false;
			w9.Fill = false;
			// Container child boxSave.Gtk.Box+BoxChild
			this.boxName = new global::Gtk.HBox();
			this.boxName.Name = "boxName";
			this.boxName.Spacing = 6;
			// Container child boxName.Gtk.Box+BoxChild
			this.label4 = new global::Gtk.Label();
			this.label4.Name = "label4";
			this.label4.LabelProp = global::Mono.Unix.Catalog.GetString("Custom Mode Name:");
			this.boxName.Add(this.label4);
			global::Gtk.Box.BoxChild w10 = ((global::Gtk.Box.BoxChild)(this.boxName[this.label4]));
			w10.Position = 0;
			w10.Expand = false;
			w10.Fill = false;
			// Container child boxName.Gtk.Box+BoxChild
			this.entryModeName = new global::Gtk.Entry();
			this.entryModeName.CanFocus = true;
			this.entryModeName.Name = "entryModeName";
			this.entryModeName.IsEditable = true;
			this.entryModeName.InvisibleChar = '●';
			this.boxName.Add(this.entryModeName);
			global::Gtk.Box.BoxChild w11 = ((global::Gtk.Box.BoxChild)(this.boxName[this.entryModeName]));
			w11.Position = 1;
			// Container child boxName.Gtk.Box+BoxChild
			this.label3 = new global::Gtk.Label();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString("Available for:");
			this.boxName.Add(this.label3);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.boxName[this.label3]));
			w12.Position = 2;
			w12.Expand = false;
			w12.Fill = false;
			// Container child boxName.Gtk.Box+BoxChild
			this.comboStore = global::Gtk.ComboBox.NewText();
			this.comboStore.AppendText(global::Mono.Unix.Catalog.GetString("Only this project"));
			this.comboStore.AppendText(global::Mono.Unix.Catalog.GetString("Only this solution"));
			this.comboStore.AppendText(global::Mono.Unix.Catalog.GetString("All solutions"));
			this.comboStore.Name = "comboStore";
			this.comboStore.Active = 0;
			this.boxName.Add(this.comboStore);
			global::Gtk.Box.BoxChild w13 = ((global::Gtk.Box.BoxChild)(this.boxName[this.comboStore]));
			w13.Position = 3;
			w13.Expand = false;
			w13.Fill = false;
			this.boxSave.Add(this.boxName);
			global::Gtk.Box.BoxChild w14 = ((global::Gtk.Box.BoxChild)(this.boxSave[this.boxName]));
			w14.Position = 2;
			w14.Expand = false;
			w14.Fill = false;
			// Container child boxSave.Gtk.Box+BoxChild
			this.checkPrompt = new global::Gtk.CheckButton();
			this.checkPrompt.CanFocus = true;
			this.checkPrompt.Name = "checkPrompt";
			this.checkPrompt.Label = global::Mono.Unix.Catalog.GetString("Always show the parameters window before running this custom mode");
			this.checkPrompt.DrawIndicator = true;
			this.checkPrompt.UseUnderline = true;
			this.boxSave.Add(this.checkPrompt);
			global::Gtk.Box.BoxChild w15 = ((global::Gtk.Box.BoxChild)(this.boxSave[this.checkPrompt]));
			w15.Position = 3;
			w15.Expand = false;
			w15.Fill = false;
			this.boxEditor.Add(this.boxSave);
			global::Gtk.Box.BoxChild w16 = ((global::Gtk.Box.BoxChild)(this.boxEditor[this.boxSave]));
			w16.PackType = ((global::Gtk.PackType)(1));
			w16.Position = 2;
			w16.Expand = false;
			w16.Fill = false;
			w1.Add(this.boxEditor);
			global::Gtk.Box.BoxChild w17 = ((global::Gtk.Box.BoxChild)(w1[this.boxEditor]));
			w17.Position = 0;
			// Internal child MonoDevelop.Ide.Execution.CustomExecutionModeDialog.ActionArea
			global::Gtk.HButtonBox w18 = this.ActionArea;
			w18.Name = "dialog1_ActionArea";
			w18.Spacing = 10;
			w18.BorderWidth = ((uint)(5));
			w18.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget(this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w19 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w18[this.buttonCancel]));
			w19.Expand = false;
			w19.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget(this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w20 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w18[this.buttonOk]));
			w20.Position = 1;
			w20.Expand = false;
			w20.Fill = false;
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.DefaultWidth = 655;
			this.DefaultHeight = 525;
			this.boxName.Hide();
			this.checkPrompt.Hide();
			this.Hide();
			this.comboTargetMode.SelectionChanged += new global::System.EventHandler(this.OnComboTargetModeSelectionChanged);
			this.checkSave.Toggled += new global::System.EventHandler(this.OnCheckSaveToggled);
			this.entryModeName.Changed += new global::System.EventHandler(this.OnEntryModeNameChanged);
		}
	}
}
#pragma warning restore 436
