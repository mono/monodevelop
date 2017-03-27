using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl
{
	public abstract class BaseView : ViewContent
	{
		readonly string name;
		readonly string accessibilityDescription;

		protected BaseView (string name) :this (name, "")
		{
		}

		protected BaseView (string name, string description)
		{
			ContentName = this.name = name;
			accessibilityDescription = description;
		}

		public override string TabPageLabel {
			get { return name; }
		}

		public override string TabAccessibilityDescription {
			get {
				return accessibilityDescription;
			}
		}
	}
}