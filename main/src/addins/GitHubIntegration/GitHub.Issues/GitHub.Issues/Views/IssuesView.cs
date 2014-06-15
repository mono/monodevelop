using System;
using MonoDevelop.Ide.Gui;
using GitHub.Issues.UserInterface;

namespace GitHub.Issues.Views
{
	public interface IIssuesView : IAttachableViewContent
	{
	}

	public partial class IssuesView : BaseView, IIssuesView
	{
		private IssuesWidget widget;
		private String name;

		public IssuesWidget IssuesWidget {
			get {
				return widget;
			}
		}

		public IssuesView (String name) : base(name)
		{
			this.name = name;
		}

		#region implemented abstract members of AbstractBaseViewContent

		public override Gtk.Widget Control {
			get {
				if (widget == null) {
					CreateWidgetFromInfo ();
				}

				return widget; 
			}
		}

		#endregion

		private void CreateWidgetFromInfo ()
		{
			this.widget = new IssuesWidget ();
		}

		void IAttachableViewContent.Selected ()
		{
		}

		void IAttachableViewContent.Deselected ()
		{
		}

		void IAttachableViewContent.BeforeSave ()
		{
		}

		void IAttachableViewContent.BaseContentChanged ()
		{
		}
	}
}

