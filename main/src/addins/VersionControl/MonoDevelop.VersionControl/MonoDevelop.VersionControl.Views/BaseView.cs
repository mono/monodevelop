using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.VersionControl
{
	public abstract class BaseView : DocumentController
	{
		readonly string name;
		readonly string accessibilityDescription;

		protected BaseView (string name) :this (name, "")
		{
		}

		protected BaseView (string name, string description)
		{
			DocumentTitle = this.name = name;
			accessibilityDescription = description;
		}
	}
}