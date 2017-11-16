#pragma warning disable 436

namespace MonoDevelop.Ide.Gui.Components
{
	public partial class ExecutionModeComboBox
	{
		private global::Gtk.ComboBox comboMode;

		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Ide.Gui.Components.ExecutionModeComboBox
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Ide.Gui.Components.ExecutionModeComboBox";
			// Container child MonoDevelop.Ide.Gui.Components.ExecutionModeComboBox.Gtk.Container+ContainerChild
			this.comboMode = global::Gtk.ComboBox.NewText();
			this.comboMode.Name = "comboMode";
			this.Add(this.comboMode);
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
			this.comboMode.Changed += new global::System.EventHandler(this.OnComboModeChanged);
		}
	}
}
#pragma warning restore 436
