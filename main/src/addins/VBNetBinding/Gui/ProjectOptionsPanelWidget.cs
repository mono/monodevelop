/*
 * ProjectOptionsPanelWidget.cs.
 *
 * Author:
 *   Rolf Bjarne Kvinge <RKvinge@novell.com>
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;
using System.Collections.Generic;
using System.IO;

using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Components;

namespace MonoDevelop.VBNetBinding
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ProjectOptionsPanelWidget : Gtk.Bin
	{
		DotNetProject project;
		VBProjectParameters parameters;
		
		public ProjectOptionsPanelWidget (MonoDevelop.Projects.Project project)
		{
			Gtk.ListStore store;
			Gtk.CellRendererText cr;

			this.Build();

			this.project = (DotNetProject) project;
			parameters = (VBProjectParameters) this.project.LanguageParameters;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Executable"));
			store.AppendValues (GettextCatalog.GetString ("Library"));
			store.AppendValues (GettextCatalog.GetString ("Executable with GUI"));
			store.AppendValues (GettextCatalog.GetString ("Module")); 
			compileTargetCombo.Model = store;
			compileTargetCombo.PackStart (cr, true);
			compileTargetCombo.AddAttribute (cr, "text", 0);
			compileTargetCombo.Active = (int) this.project.CompileTarget;
			compileTargetCombo.Changed += delegate(object sender, EventArgs e) {
				entryMainClass.Sensitive = compileTargetCombo.Active != (int) CompileTarget.Library	&& compileTargetCombo.Active != (int) CompileTarget.Module;
			};

			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("WindowsForms");
			store.AppendValues ("Windows");
			store.AppendValues ("Console");
			txtMyType.Model = store;
			txtMyType.TextColumn = 0;
			switch (parameters.MyType) {
			case "WindowsForms":
				txtMyType.Active = 0;
				break;
			case "Windows":
				txtMyType.Active = 1;
				break;
			case "Console":
				txtMyType.Active = 2;
				break;
			case null:
			case "":
				break;
			default:
				txtMyType.AppendText (parameters.MyType);
				txtMyType.Active = 3;
				break;
			}

			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Binary");
			store.AppendValues ("Text");
			cmbOptionCompare.Model = store;
			cmbOptionCompare.PackStart (cr, true);
			cmbOptionCompare.AddAttribute (cr, "text", 0);
			cmbOptionCompare.Active = parameters.BinaryOptionCompare ? 0 : 1;
				
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionExplicit.Model = store;
			cmbOptionExplicit.PackStart (cr, true);
			cmbOptionExplicit.AddAttribute (cr, "text", 0);
			cmbOptionExplicit.Active = parameters.OptionExplicit ? 0 : 1;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionInfer.Model = store;
			cmbOptionInfer.PackStart (cr, true);
			cmbOptionInfer.AddAttribute (cr, "text", 0);
			cmbOptionInfer.Active = parameters.OptionInfer ? 0 : 1;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionStrict.Model = store;
			cmbOptionStrict.PackStart (cr, true);
			cmbOptionStrict.AddAttribute (cr, "text", 0);
			cmbOptionStrict.Active = parameters.OptionStrict ? 0 : 1;

			// Codepage
			string foundEncoding = null;
			string currentCodepage = parameters.CodePage;
			foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
				if (e.CodePage == -1)
					continue;
				if (e.Id == currentCodepage)
					foundEncoding = e.Id;
				cmbCodePage.AppendText (e.Id);
			}
			if (foundEncoding != null)
				cmbCodePage.Entry.Text = foundEncoding;
			else if (!string.IsNullOrEmpty (currentCodepage))
				cmbCodePage.Entry.Text = currentCodepage;
			
			entryMainClass.Entry.Text = parameters.StartupObject;
			iconEntry.Path = parameters.ApplicationIcon;
		}
		
		public void StorePanelContents ()
		{
			parameters.BinaryOptionCompare = cmbOptionCompare.ActiveText == "Binary";
			parameters.OptionExplicit = cmbOptionExplicit.ActiveText == "On";
			parameters.OptionInfer = cmbOptionInfer.ActiveText == "On";
			parameters.OptionStrict = cmbOptionStrict.ActiveText == "On";
			parameters.MyType = txtMyType.ActiveText;
			parameters.StartupObject = entryMainClass.ActiveText;
			parameters.CodePage = cmbCodePage.Entry.Text;
			parameters.ApplicationIcon = iconEntry.Path;
			this.project.CompileTarget = (CompileTarget) compileTargetCombo.Active;
		}
	}
}
