using System;
using System.IO; 
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl
{
	public abstract class BaseView : AbstractBaseViewContent, IViewContent
	{
		string name;
		
		protected BaseView (string name)
		{
			this.name = name;
		}
		
		protected virtual void SaveAs (string fileName)
		{
		}

		void IViewContent.Load (string fileName)
		{
			throw new InvalidOperationException();
		}
		
		void IViewContent.LoadNew (Stream stream, string mimeType)
		{
			throw new InvalidOperationException ();
		}
		
		void IViewContent.Save ()
		{
			throw new InvalidOperationException ();
		}
		
		void IViewContent.DiscardChanges ()
		{
		}
		
		void IViewContent.Save (string fileName)
		{
			SaveAs (fileName);
		}
		
		string IViewContent.ContentName {
			get { return name; }
			set { }
		}
		
		bool IViewContent.IsDirty {
			get { return false; }
			set { }
		}
		
		bool IViewContent.IsReadOnly {
			get { return true; }
		}

		bool IViewContent.IsUntitled {
			get { return false; }
		}

		bool IViewContent.IsViewOnly {
			get { return false; }
		}
		
		bool IViewContent.IsFile {
			get { return false; }
		}

		string IViewContent.PathRelativeToProject {
			get { return ""; }
		}
		
		MonoDevelop.Projects.Project IViewContent.Project {
			get { return null; }
			set { }
		}
		
		public override string TabPageLabel {
			get { return name; }
		}

		public virtual string StockIconId {
			get { return null; }
		}
		
		string IViewContent.UntitledName {
			get { return ""; }
			set { }
		}
		
		event EventHandler IViewContent.BeforeSave { add { } remove { } }
		event EventHandler IViewContent.ContentChanged { add { } remove { } }
		event EventHandler IViewContent.ContentNameChanged { add { } remove { } }
		event EventHandler IViewContent.DirtyChanged { add { } remove { } }
	}
}