#pragma warning disable 436

namespace MonoDevelop.SourceEditor
{
	internal partial class PrintSettingsWidget
	{
		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.SourceEditor.PrintSettingsWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.SourceEditor.PrintSettingsWidget";
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Hide();
		}
	}
}
#pragma warning restore 436
