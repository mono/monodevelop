using System;
using System.IO;
using MonoDevelop.Ide.Gui;

namespace GitHub.Issues.Views
{
	/// <summary>
	/// Base view for all other views in the plugin
	/// </summary>
	public abstract class BaseView : AbstractBaseViewContent, IViewContent
	{
		/// <summary>
		/// The name.
		/// </summary>
		string name;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHub.Issues.Views.BaseView"/> class.
		/// </summary>
		/// <param name="name">Name.</param>
		protected BaseView (string name)
		{
			this.name = name;
		}

		/// <summary>
		/// Saves as.
		/// </summary>
		/// <param name="fileName">File name.</param>
		protected virtual void SaveAs (string fileName)
		{
		}

		/// <summary>
		/// Load the specified fileName.
		/// </summary>
		/// <param name="fileName">File name.</param>
		void IViewContent.Load (string fileName)
		{
			throw new InvalidOperationException ();
		}

		/// <summary>
		/// Loads the new.
		/// </summary>
		/// <param name="stream">Stream.</param>
		/// <param name="mimeType">MIME type.</param>
		void IViewContent.LoadNew (Stream stream, string mimeType)
		{
			throw new InvalidOperationException ();
		}

		/// <summary>
		/// Save this instance.
		/// </summary>
		void IViewContent.Save ()
		{
			throw new InvalidOperationException ();
		}

		/// <summary>
		/// Discards the changes.
		/// </summary>
		void IViewContent.DiscardChanges ()
		{
		}

		/// <summary>
		/// Save the specified fileName.
		/// </summary>
		/// <param name="fileName">File name.</param>
		void IViewContent.Save (string fileName)
		{
			SaveAs (fileName);
		}

		/// <summary>
		/// Gets or sets the name of the content.
		/// </summary>
		/// <value>The name of the content.</value>
		string IViewContent.ContentName {
			get { return name; }
			set { }
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is dirty.
		/// </summary>
		/// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
		bool IViewContent.IsDirty {
			get { return false; }
			set { }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is read only.
		/// </summary>
		/// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
		bool IViewContent.IsReadOnly {
			get { return true; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is untitled.
		/// </summary>
		/// <value><c>true</c> if this instance is untitled; otherwise, <c>false</c>.</value>
		bool IViewContent.IsUntitled {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is view only.
		/// </summary>
		/// <value><c>true</c> if this instance is view only; otherwise, <c>false</c>.</value>
		bool IViewContent.IsViewOnly {
			get { return false; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance is file.
		/// </summary>
		/// <value><c>true</c> if this instance is file; otherwise, <c>false</c>.</value>
		bool IViewContent.IsFile {
			get { return false; }
		}

		/// <summary>
		/// Gets the path relative to project.
		/// </summary>
		/// <value>The path relative to project.</value>
		string IViewContent.PathRelativeToProject {
			get { return ""; }
		}

		/// <summary>
		/// Gets or sets the project.
		/// </summary>
		/// <value>The project.</value>
		MonoDevelop.Projects.Project IViewContent.Project {
			get { return null; }
			set { }
		}

		/// <summary>
		/// The label used for the subview list.
		/// </summary>
		/// <value>The tab page label.</value>
		public override string TabPageLabel {
			get { return name; }
		}

		/// <summary>
		/// Gets the stock icon identifier.
		/// </summary>
		/// <value>The stock icon identifier.</value>
		public virtual string StockIconId {
			get { return null; }
		}

		/// <summary>
		/// Gets or sets the name of the untitled.
		/// </summary>
		/// <value>The name of the untitled.</value>
		string IViewContent.UntitledName {
			get { return ""; }
			set { }
		}

		/// <summary>
		/// Occurs when before save.
		/// </summary>
		event EventHandler IViewContent.BeforeSave { add { } remove { }
		}

		/// <summary>
		/// Occurs when content changed.
		/// </summary>
		event EventHandler IViewContent.ContentChanged { add { } remove { }
		}

		/// <summary>
		/// Occurs when content name changed.
		/// </summary>
		event EventHandler IViewContent.ContentNameChanged { add { } remove { }
		}

		/// <summary>
		/// Occurs when dirty changed.
		/// </summary>
		event EventHandler IViewContent.DirtyChanged { add { } remove { }
		}
	}
}