#pragma warning disable 436

namespace MonoDevelop.Autotools
{
	public partial class MakefileIntegrationFeatureWidget
	{
		protected virtual void Build()
		{
			global::Stetic.Gui.Initialize(this);
			// Widget MonoDevelop.Autotools.MakefileIntegrationFeatureWidget
			global::Stetic.BinContainer.Attach(this);
			this.Name = "MonoDevelop.Autotools.MakefileIntegrationFeatureWidget";
			if ((this.Child != null))
			{
				this.Child.ShowAll();
			}
			this.Show();
		}
	}
}
#pragma warning restore 436
