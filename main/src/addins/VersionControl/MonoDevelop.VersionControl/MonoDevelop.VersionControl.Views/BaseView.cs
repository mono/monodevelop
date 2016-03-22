using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl
{
	public abstract class BaseView : ViewContent
	{
		readonly string name;

		protected BaseView (string name)
		{
			ContentName = this.name = name;
		}

		public override string TabPageLabel {
			get { return name; }
		}
	}
}