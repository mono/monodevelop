
// This file has been generated by the GUI designer. Do not modify.
namespace MonoDevelop.Debugger
{
	public partial class DebuggerOptionsPanelWidget
	{
		private global::Gtk.VBox vbox3;
		
		private global::Gtk.CheckButton checkProjectCodeOnly;
		
		private global::Gtk.CheckButton checkStepOverPropertiesAndOperators;
		
		private global::Gtk.CheckButton checkAllowEval;
		
		private global::Gtk.Alignment alignmentAllowToString;
		
		private global::Gtk.CheckButton checkAllowToString;
		
		private global::Gtk.CheckButton checkShowBaseGroup;
		
		private global::Gtk.CheckButton checkGroupPrivate;
		
		private global::Gtk.CheckButton checkGroupStatic;
		
		private global::Gtk.Table tableEval;
		
		private global::Gtk.Label label3;
		
		private global::Gtk.Label labelEvalTimeout;
		
		private global::Gtk.SpinButton spinTimeout;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget MonoDevelop.Debugger.DebuggerOptionsPanelWidget
			global::Stetic.BinContainer.Attach (this);
			this.Name = "MonoDevelop.Debugger.DebuggerOptionsPanelWidget";
			// Container child MonoDevelop.Debugger.DebuggerOptionsPanelWidget.Gtk.Container+ContainerChild
			this.vbox3 = new global::Gtk.VBox ();
			this.vbox3.Name = "vbox3";
			this.vbox3.Spacing = 6;
			this.vbox3.BorderWidth = ((uint)(9));
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkProjectCodeOnly = new global::Gtk.CheckButton ();
			this.checkProjectCodeOnly.CanFocus = true;
			this.checkProjectCodeOnly.Name = "checkProjectCodeOnly";
			this.checkProjectCodeOnly.Label = global::Mono.Unix.Catalog.GetString ("Debug project code only; do not step into framework code.");
			this.checkProjectCodeOnly.Active = true;
			this.checkProjectCodeOnly.DrawIndicator = true;
			this.checkProjectCodeOnly.UseUnderline = true;
			this.vbox3.Add (this.checkProjectCodeOnly);
			global::Gtk.Box.BoxChild w1 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.checkProjectCodeOnly]));
			w1.Position = 0;
			w1.Expand = false;
			w1.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkStepOverPropertiesAndOperators = new global::Gtk.CheckButton ();
			this.checkStepOverPropertiesAndOperators.CanFocus = true;
			this.checkStepOverPropertiesAndOperators.Name = "checkStepOverPropertiesAndOperators";
			this.checkStepOverPropertiesAndOperators.Label = global::Mono.Unix.Catalog.GetString ("Step over properties and operators");
			this.checkStepOverPropertiesAndOperators.Active = true;
			this.checkStepOverPropertiesAndOperators.DrawIndicator = true;
			this.checkStepOverPropertiesAndOperators.UseUnderline = true;
			this.vbox3.Add (this.checkStepOverPropertiesAndOperators);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.checkStepOverPropertiesAndOperators]));
			w2.Position = 1;
			w2.Expand = false;
			w2.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkAllowEval = new global::Gtk.CheckButton ();
			this.checkAllowEval.CanFocus = true;
			this.checkAllowEval.Name = "checkAllowEval";
			this.checkAllowEval.Label = global::Mono.Unix.Catalog.GetString ("Allow implicit property evaluation and method invocation");
			this.checkAllowEval.Active = true;
			this.checkAllowEval.DrawIndicator = true;
			this.checkAllowEval.UseUnderline = true;
			this.vbox3.Add (this.checkAllowEval);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.checkAllowEval]));
			w3.Position = 2;
			w3.Expand = false;
			w3.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.alignmentAllowToString = new global::Gtk.Alignment (0F, 0.5F, 1F, 1F);
			this.alignmentAllowToString.Name = "alignmentAllowToString";
			this.alignmentAllowToString.LeftPadding = ((uint)(18));
			// Container child alignmentAllowToString.Gtk.Container+ContainerChild
			this.checkAllowToString = new global::Gtk.CheckButton ();
			this.checkAllowToString.CanFocus = true;
			this.checkAllowToString.Name = "checkAllowToString";
			this.checkAllowToString.Label = global::Mono.Unix.Catalog.GetString ("Call string-conversion function on objects in variables windows");
			this.checkAllowToString.Active = true;
			this.checkAllowToString.DrawIndicator = true;
			this.checkAllowToString.UseUnderline = true;
			this.alignmentAllowToString.Add (this.checkAllowToString);
			this.vbox3.Add (this.alignmentAllowToString);
			global::Gtk.Box.BoxChild w5 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.alignmentAllowToString]));
			w5.Position = 3;
			w5.Expand = false;
			w5.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkShowBaseGroup = new global::Gtk.CheckButton ();
			this.checkShowBaseGroup.CanFocus = true;
			this.checkShowBaseGroup.Name = "checkShowBaseGroup";
			this.checkShowBaseGroup.Label = global::Mono.Unix.Catalog.GetString ("Show inherited class members in a base class group");
			this.checkShowBaseGroup.DrawIndicator = true;
			this.checkShowBaseGroup.UseUnderline = true;
			this.vbox3.Add (this.checkShowBaseGroup);
			global::Gtk.Box.BoxChild w6 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.checkShowBaseGroup]));
			w6.Position = 4;
			w6.Expand = false;
			w6.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkGroupPrivate = new global::Gtk.CheckButton ();
			this.checkGroupPrivate.CanFocus = true;
			this.checkGroupPrivate.Name = "checkGroupPrivate";
			this.checkGroupPrivate.Label = global::Mono.Unix.Catalog.GetString ("Group non-public members");
			this.checkGroupPrivate.DrawIndicator = true;
			this.checkGroupPrivate.UseUnderline = true;
			this.vbox3.Add (this.checkGroupPrivate);
			global::Gtk.Box.BoxChild w7 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.checkGroupPrivate]));
			w7.Position = 5;
			w7.Expand = false;
			w7.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.checkGroupStatic = new global::Gtk.CheckButton ();
			this.checkGroupStatic.CanFocus = true;
			this.checkGroupStatic.Name = "checkGroupStatic";
			this.checkGroupStatic.Label = global::Mono.Unix.Catalog.GetString ("Group static members");
			this.checkGroupStatic.DrawIndicator = true;
			this.checkGroupStatic.UseUnderline = true;
			this.vbox3.Add (this.checkGroupStatic);
			global::Gtk.Box.BoxChild w8 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.checkGroupStatic]));
			w8.Position = 6;
			w8.Expand = false;
			w8.Fill = false;
			// Container child vbox3.Gtk.Box+BoxChild
			this.tableEval = new global::Gtk.Table (((uint)(1)), ((uint)(3)), false);
			this.tableEval.Name = "tableEval";
			this.tableEval.RowSpacing = ((uint)(6));
			this.tableEval.ColumnSpacing = ((uint)(6));
			// Container child tableEval.Gtk.Table+TableChild
			this.label3 = new global::Gtk.Label ();
			this.label3.Name = "label3";
			this.label3.LabelProp = global::Mono.Unix.Catalog.GetString ("ms");
			this.tableEval.Add (this.label3);
			global::Gtk.Table.TableChild w9 = ((global::Gtk.Table.TableChild)(this.tableEval [this.label3]));
			w9.LeftAttach = ((uint)(2));
			w9.RightAttach = ((uint)(3));
			w9.XOptions = ((global::Gtk.AttachOptions)(4));
			w9.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableEval.Gtk.Table+TableChild
			this.labelEvalTimeout = new global::Gtk.Label ();
			this.labelEvalTimeout.Name = "labelEvalTimeout";
			this.labelEvalTimeout.Xalign = 0F;
			this.labelEvalTimeout.LabelProp = global::Mono.Unix.Catalog.GetString ("Evaluation Timeout:");
			this.tableEval.Add (this.labelEvalTimeout);
			global::Gtk.Table.TableChild w10 = ((global::Gtk.Table.TableChild)(this.tableEval [this.labelEvalTimeout]));
			w10.XOptions = ((global::Gtk.AttachOptions)(4));
			w10.YOptions = ((global::Gtk.AttachOptions)(4));
			// Container child tableEval.Gtk.Table+TableChild
			this.spinTimeout = new global::Gtk.SpinButton (0, 1000000, 100);
			this.spinTimeout.CanFocus = true;
			this.spinTimeout.Name = "spinTimeout";
			this.spinTimeout.Adjustment.PageIncrement = 10;
			this.spinTimeout.ClimbRate = 100;
			this.spinTimeout.Numeric = true;
			this.tableEval.Add (this.spinTimeout);
			global::Gtk.Table.TableChild w11 = ((global::Gtk.Table.TableChild)(this.tableEval [this.spinTimeout]));
			w11.LeftAttach = ((uint)(1));
			w11.RightAttach = ((uint)(2));
			w11.XOptions = ((global::Gtk.AttachOptions)(4));
			w11.YOptions = ((global::Gtk.AttachOptions)(4));
			this.vbox3.Add (this.tableEval);
			global::Gtk.Box.BoxChild w12 = ((global::Gtk.Box.BoxChild)(this.vbox3 [this.tableEval]));
			w12.Position = 7;
			w12.Expand = false;
			w12.Fill = false;
			this.Add (this.vbox3);
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
			this.checkAllowEval.Toggled += new global::System.EventHandler (this.OnCheckAllowEvalToggled);
		}
	}
}
