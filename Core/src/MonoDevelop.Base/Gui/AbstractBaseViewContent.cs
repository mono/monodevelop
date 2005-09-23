// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
//using System.Windows.Forms;

using MonoDevelop.Services;

namespace MonoDevelop.Gui
{
	public abstract class AbstractBaseViewContent : IBaseViewContent
	{
		IWorkbenchWindow workbenchWindow = null;
		
		public abstract Gtk.Widget Control {
			get;
		}
		
		public virtual IWorkbenchWindow WorkbenchWindow {
			get {
				return workbenchWindow;
			}
			set {
				workbenchWindow = value;
				OnWorkbenchWindowChanged(EventArgs.Empty);
			}
		}
		
		public virtual string TabPageLabel {
			get {
				return GettextCatalog.GetString ("Abstract Content");
			}
		}
		
		public virtual void RedrawContent()
		{
		}
		
		public virtual void Dispose()
		{
		}
		
		protected virtual void OnWorkbenchWindowChanged(EventArgs e)
		{
			if (WorkbenchWindowChanged != null) {
				WorkbenchWindowChanged(this, e);
			}
		}
		
		public event EventHandler WorkbenchWindowChanged;
	}
}
