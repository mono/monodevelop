using System;
using MonoDevelop.Ide.Gui;
using GitHub.Issues.Views;

namespace GitHub.Issues
{
	/// <summary>
	/// Interface for the Labels view
	/// </summary>
	public interface ILabelsView : IAttachableViewContent
	{
	}

	/// <summary>
	/// Labels view which displays and manages the labels in the current repository
	/// </summary>
	public class LabelsView : BaseView, ILabelsView
	{
		private LabelsWidget widget;
		private String name;

		/// <summary>
		/// Gets the labels widget.
		/// </summary>
		/// <value>The labels widget.</value>
		public LabelsWidget LabelsWidget {
			get {
				return widget;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.LabelsView"/> class.
		/// </summary>
		/// <param name="name">Name.</param>
		public LabelsView (String name) : base (name)
		{
			this.name = name;
		}

		#region implemented abstract members of AbstractBaseViewContent

		/// <summary>
		/// Gets the control.
		/// </summary>
		/// <value>The control.</value>
		public override Gtk.Widget Control {
			get {
				if (widget == null) {
					CreateWidgetFromInfo ();
				}

				return widget; 
			}
		}

		#endregion

		/// <summary>
		/// Creates the widget from info.
		/// </summary>
		private void CreateWidgetFromInfo ()
		{
			this.widget = new LabelsWidget ();
		}

		/// <summary>
		/// Selected this instance.
		/// </summary>
		void IAttachableViewContent.Selected ()
		{
		}

		/// <summary>
		/// Deselected this instance.
		/// </summary>
		void IAttachableViewContent.Deselected ()
		{
		}

		/// <summary>
		/// Befores the save.
		/// </summary>
		void IAttachableViewContent.BeforeSave ()
		{
		}

		/// <summary>
		/// Bases the content changed.
		/// </summary>
		void IAttachableViewContent.BaseContentChanged ()
		{
		}
	}
}

