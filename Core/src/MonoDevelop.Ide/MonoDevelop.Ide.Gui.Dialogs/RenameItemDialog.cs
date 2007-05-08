using System;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Gui.Dialogs {
	public partial class RenameItemDialog : Gtk.Dialog {
		IParserContext ctx;
		ILanguageItem item;
		
		public RenameItemDialog (IParserContext ctx, ILanguageItem item)
		{
			this.ctx = ctx;
			this.item = item;
			
			this.Build ();
			
			if (item is IClass) {
				if (((IClass) item).ClassType == ClassType.Interface)
					this.Title = GettextCatalog.GetString ("Rename Interface");
				else
					this.Title = GettextCatalog.GetString ("Rename Class");
			} else if (item is IField) {
				this.Title = GettextCatalog.GetString ("Rename Field");
			} else if (item is IProperty) {
				this.Title = GettextCatalog.GetString ("Rename Property");
			} else if (item is IEvent) {
				this.Title = GettextCatalog.GetString ("Rename Event");
			} else if (item is IMethod) {
				this.Title = GettextCatalog.GetString ("Rename Method");
			} else if (item is IIndexer) {
				this.Title = GettextCatalog.GetString ("Rename Indexer");
			} else if (item is IParameter) {
				this.Title = GettextCatalog.GetString ("Rename Parameter");
			} else if (item is LocalVariable) {
				this.Title = GettextCatalog.GetString ("Rename Variable");
			} else {
				this.Title = GettextCatalog.GetString ("Rename Item");
			}
			
			entry.Text = item.Name;
			entry.SelectRegion (0, -1);
			
			buttonOk.Sensitive = false;
			entry.Changed += new EventHandler (OnEntryChanged);
			entry.Activated += new EventHandler (OnEntryActivated);
			
			buttonOk.Clicked += new EventHandler (OnOKClicked);
			buttonCancel.Clicked += new EventHandler (OnCancelClicked);
		}
		
		void OnEntryChanged (object sender, EventArgs e)
		{
			// Don't allow the user to click OK unless there is a new name
			buttonOk.Sensitive = entry.Text.Length > 0;
		}
		
		void OnEntryActivated (object sender, EventArgs e)
		{
			if (buttonOk.Sensitive)
				buttonOk.Click ();
		}
		
		void OnCancelClicked (object sender, EventArgs e)
		{
			((Widget) this).Destroy ();
		}
		
		void OnOKClicked (object sender, EventArgs e)
		{
			CodeRefactorer refactorer = IdeApp.ProjectOperations.CodeRefactorer;
			IProgressMonitor monitor = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor (this.Title, null);
			string name = entry.Text;
			
			if (item is IMember) {
				IMember member = (IMember) item;
				
				refactorer.RenameMember (monitor, member.DeclaringType, member, name, RefactoryScope.Solution);
			} else if (item is IClass) {
				refactorer.RenameClass (monitor, (IClass) item, name, RefactoryScope.Solution);
			} else if (item is LocalVariable) {
				refactorer.RenameVariable (monitor, (LocalVariable) item, name);
			} else if (item is IParameter) {
				refactorer.RenameParameter (monitor, (IParameter) item, name);
			}
			
			((Widget) this).Destroy ();
		}
	}
}
