
namespace GitHub.Issues
{
	public partial class LabelsWidget
	{
		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget GitHub.Issues.IssueWidget
			global::Stetic.BinContainer.Attach (this);
			this.Name = "GitHub.Issues.LabelsWidget";
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.Hide ();
		}
	}
}
