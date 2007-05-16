// /cvs/monodevelop/Core/src/MonoDevelop.Ide.Gui.Dialogs/EncapsulateFieldDialog.cs created with MonoDevelop
// User: fejj at 3:50 PM 5/15/2007
//

using System;
using System.Text;
using System.CodeDom;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Gui.Dialogs {
	public partial class EncapsulateFieldDialog : Gtk.Dialog {
		IParserContext ctx;
		IField field;
		
		public EncapsulateFieldDialog (IParserContext ctx, IField field)
		{
			this.field = field;
			this.ctx = ctx;
			
			this.Build ();
			
			this.Title = GettextCatalog.GetString ("Encapsulate '{0}'", field.Name);
			
			entryFieldName.Text = field.Name;
			
			buttonOk.Sensitive = false;
			entryPropertyName.Changed += new EventHandler (OnEntryChanged);
			entryPropertyName.Activated += new EventHandler (OnEntryActivated);
			
			entryPropertyName.Text = GeneratePropertyName ();
			entryPropertyName.SelectRegion (0, -1);
			
			buttonOk.Clicked += new EventHandler (OnOKClicked);
			buttonCancel.Clicked += new EventHandler (OnCancelClicked);
		}
		
		string GeneratePropertyName ()
		{
			StringBuilder builder = new StringBuilder (field.Name.Length);
			string fieldName = field.Name;
			bool upper = true;
			int i = 0;
			
			// Field names are commonly prefixed with "m" or "m_" by devs from c++ land.
			if (fieldName[0] == 'm' && fieldName.Length > 1 &&
			    (fieldName[1] == '_' || Char.IsUpper (fieldName[1])))
				i++;
			
			while (i < fieldName.Length) {
				if (fieldName[i] == '_') {
					// strip _'s and uppercase the next letter
					upper = true;
				} else if (Char.IsLetter (fieldName[i])) {
					builder.Append (upper ? Char.ToUpper (fieldName[i]) : fieldName[i]);
					upper = false;
				} else {
					builder.Append (fieldName[i]);
				}
				
				i++;
			}
			
			return builder.ToString ();
		}
		
		void OnEntryChanged (object sender, EventArgs e)
		{
			string name = entryPropertyName.Text;
			IMember member;
			int i;
			
			// Don't allow the user to click OK unless there is a new name
			if (name.Length == 0) {
				buttonOk.Sensitive = false;
				return;
			}
			
			// Check that this member name isn't already used by properties
			for (i = 0; i < field.DeclaringType.Properties.Count; i++) {
				member = field.DeclaringType.Properties[i];
				if (member.Name == name) {
					buttonOk.Sensitive = false;
					return;
				}
			}
			
			// Check that this member name isn't already used by fields
			for (i = 0; i < field.DeclaringType.Fields.Count; i++) {
				member = field.DeclaringType.Fields[i];
				if (member.Name == name) {
					buttonOk.Sensitive = false;
					return;
				}
			}
			
			// Check that this member name isn't already used by methods
			for (i = 0; i < field.DeclaringType.Methods.Count; i++) {
				member = field.DeclaringType.Methods[i];
				if (member.Name == name) {
					buttonOk.Sensitive = false;
					return;
				}
			}
			
			// Check that this member name isn't already used by events
			for (i = 0; i < field.DeclaringType.Events.Count; i++) {
				member = field.DeclaringType.Events[i];
				if (member.Name == name) {
					buttonOk.Sensitive = false;
					return;
				}
			}
			
			buttonOk.Sensitive = true;
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
			string name = entryPropertyName.Text;
			CodeMemberProperty member;
			
			member = new CodeMemberProperty ();
			member.Name = name;
			
			member.Type = new CodeTypeReference (field.ReturnType.Name);
			member.Attributes = MemberAttributes.Public;
			
			member.HasGet = true;
			member.HasSet = true;
			
			refactorer.EncapsulateField (monitor, field.DeclaringType, field, member);
			
			((Widget) this).Destroy ();
		}
	}
}
