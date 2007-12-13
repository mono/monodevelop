//  EditTemplateDialog.cs
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

using MonoDevelop.Core;
using MonoDevelop.Ide.CodeTemplates;

namespace MonoDevelop.Ide.CodeTemplates
{
	internal class EditTemplateDialog : Gtk.Dialog 
	{
		CodeTemplate codeTemplate;
		
		// Gtk members
		Gtk.Entry templateTextBox;
		Gtk.Entry descriptionTextBox;
		
		public CodeTemplate CodeTemplate {
			get {
				return codeTemplate;
			}
		}
		
		public EditTemplateDialog(CodeTemplate codeTemplate)
		{
			this.codeTemplate = codeTemplate;
			InitializeComponents();
			this.ShowAll();
		}
		
		void AcceptEvent(object sender, EventArgs e)
		{
			codeTemplate.Shortcut    = templateTextBox.Text;
			codeTemplate.Description = descriptionTextBox.Text;
			
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
			
			// set up the dialog fields and add them
			templateTextBox = new Gtk.Entry();
			descriptionTextBox = new Gtk.Entry();
			descriptionTextBox.ActivatesDefault = true;
			Gtk.Label label1 = new Gtk.Label(GettextCatalog.GetString ("_Description"));
			Gtk.Label label2 = new Gtk.Label(GettextCatalog.GetString ("_Template"));
			label1.Xalign = 0;
			label2.Xalign = 0;
			templateTextBox.Text    = codeTemplate.Shortcut;
			descriptionTextBox.Text = codeTemplate.Description;			
			Gtk.SizeGroup sizeGroup1 = new Gtk.SizeGroup(Gtk.SizeGroupMode.Horizontal);
			Gtk.SizeGroup sizeGroup2 = new Gtk.SizeGroup(Gtk.SizeGroupMode.Horizontal);			
			sizeGroup1.AddWidget(templateTextBox);
			sizeGroup1.AddWidget(descriptionTextBox);
			sizeGroup2.AddWidget(label1);
			sizeGroup2.AddWidget(label2);
			
			// FIXME: make the labels both part of the same sizing group so they have the same left and right rows.
			Gtk.HBox hBox1 = new Gtk.HBox(false, 6);
			hBox1.PackStart(label1, false, false, 6);
			hBox1.PackStart(descriptionTextBox, false, false, 6);
			
			Gtk.HBox hBox2 = new Gtk.HBox(false, 6);
			hBox2.PackStart(label2, false, false, 6);
			hBox2.PackStart(templateTextBox, false, false, 6);
			
			this.VBox.PackStart(hBox1, false, false, 6);
			this.VBox.PackStart(hBox2, false, false, 6);
			
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
