// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.Internal.Templates;
using MonoDevelop.Core.Services;

using MonoDevelop.Services;

namespace MonoDevelop.EditorBindings.Gui.Dialogs
{
	public class EditTemplateGroupDialog : Gtk.Dialog 
	{
		CodeTemplateGroup codeTemplateGroup;
		string titlePrefix = string.Empty;
		
		// Gtk members
		Gtk.Entry templateExtensionsTextBox;		
		
		// Services
		StringParserService StringParserService = (StringParserService)ServiceManager.GetService (typeof (StringParserService));
		
		public CodeTemplateGroup CodeTemplateGroup {
			get {
				return codeTemplateGroup;
			}
		}
		
		public EditTemplateGroupDialog(CodeTemplateGroup codeTemplateGroup, string titlePrefix)
		{
			this.codeTemplateGroup = codeTemplateGroup;
			this.titlePrefix = titlePrefix;
			InitializeComponents();
			this.ShowAll();
		}
		
		void AcceptEvent(object sender, EventArgs e)
		{
			codeTemplateGroup.ExtensionStrings = templateExtensionsTextBox.Text.Split(';');
			
			// close the window
			CancelEvent(sender, EventArgs.Empty);
		}
		
		void CancelEvent(object sender, EventArgs e)
		{
			this.Destroy();
		}
		
		void InitializeComponents()
		{
			// set up this actual dialog
			this.Modal = true;
			// FIXME: make this a resource in the resource file
			this.Title = String.Format (GettextCatalog.GetString ("{0} Code Group"), titlePrefix);
			
			// set up the dialog fields and add them
			templateExtensionsTextBox = new Gtk.Entry();
			templateExtensionsTextBox.ActivatesDefault = true;
			// FIXME: make this a resource in the resource file
			Gtk.Label label1 = new Gtk.Label("Extensions (; seperated)");
			
			label1.Xalign = 0;			
			templateExtensionsTextBox.Text    = string.Join(";", codeTemplateGroup.ExtensionStrings);
			
			// FIXME: make the labels both part of the same sizing group so they have the same left and right rows.
			Gtk.HBox hBox1 = new Gtk.HBox(false, 6);
			hBox1.PackStart(label1, false, false, 6);
			hBox1.PackStart(templateExtensionsTextBox, false, false, 6);
			
			this.VBox.PackStart(hBox1, false, false, 6);
			
			// set up the buttons and add them
			this.DefaultResponse = Gtk.ResponseType.Ok;
			Gtk.Button cancelButton = new Gtk.Button(Gtk.Stock.Cancel);
			Gtk.Button okButton = new Gtk.Button(Gtk.Stock.Ok);
			okButton.Clicked += new EventHandler(AcceptEvent);
			cancelButton.Clicked += new EventHandler(CancelEvent);
			this.AddActionWidget (cancelButton, Gtk.ResponseType.Cancel);
			this.AddActionWidget (okButton, (int) Gtk.ResponseType.Ok);
		}
	}
}
