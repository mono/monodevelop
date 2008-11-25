//  EditTemplateGroupDialog.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;

using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Core;

namespace MonoDevelop.SourceEditor.Gui.Dialogs
{
	public class EditTemplateGroupDialog : Gtk.Dialog 
	{
		CodeTemplateGroup codeTemplateGroup;
		string titlePrefix = string.Empty;
		
		// Gtk members
		Gtk.Entry templateExtensionsTextBox;		
		
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
