using System;
using MonoDevelop.Ide.Gui;
using GitHub.Issues.Views;

namespace GitHub.Issues
{
	public interface ILabelsView : IAttachableViewContent
	{
	}

	public class LabelsView : BaseView, ILabelsView
	{
		private LabelsWidget widget;
		private String name;

		public LabelsWidget LabelsWidget {
			get {
				return widget;
			}
		}

		public LabelsView (String name) : base (name)
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
			this.widget = new LabelsWidget ();
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

