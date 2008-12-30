/*
 * ConfigurationOptionsPanelWidget.cs.
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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Components;

namespace MonoDevelop.VBNetBinding
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ConfigurationOptionsPanelWidget : Gtk.Bin
	{
		DotNetProjectConfiguration config;
		
		public ConfigurationOptionsPanelWidget (Project project)
		{
			Gtk.CellRendererText cr;
		 	Gtk.ListStore store;
			
			this.Build();
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Yes");
			store.AppendValues ("No");
			cmbOptimize.Model = store;
			cmbOptimize.PackStart (cr, false);
			cmbOptimize.AddAttribute (cr, "text", 0);

			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Yes");
			store.AppendValues ("No");
			cmbDefineDEBUG.Model = store;
			cmbDefineDEBUG.PackStart (cr, false);
			cmbDefineDEBUG.AddAttribute (cr, "text", 0);

			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Yes");
			store.AppendValues ("No");
			cmbDefineTRACE.Model = store;
			cmbDefineTRACE.PackStart (cr, false);
			cmbDefineTRACE.AddAttribute (cr, "text", 0);
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("None");
			store.AppendValues ("pdbonly");
			store.AppendValues ("full");
			cmbDebugType.Model = store;
			cmbDebugType.PackStart (cr, false);
			cmbDebugType.AddAttribute (cr, "text", 0);
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Yes");
			store.AppendValues ("No");
			cmbEnableWarnings.Model = store;
			cmbEnableWarnings.PackStart (cr, false);
			cmbEnableWarnings.AddAttribute (cr, "text", 0);
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Yes");
			store.AppendValues ("No");
			cmbGenerateXmlDocumentation.Model = store;
			cmbGenerateXmlDocumentation.PackStart (cr, false);
			cmbGenerateXmlDocumentation.AddAttribute (cr, "text", 0);
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Yes");
			store.AppendValues ("No");
			cmbRemoveIntegerChecks.Model = store;
			cmbRemoveIntegerChecks.PackStart (cr, false);
			cmbRemoveIntegerChecks.AddAttribute (cr, "text", 0);
		}
		
		public void StorePanelContents ()
		{
			VBCompilerParameters c = (VBCompilerParameters) config.CompilationParameters;

			c.Optimize = cmbOptimize.Active == 0;
			c.DefineDebug = cmbDefineDEBUG.Active == 0;
			c.DefineTrace = cmbDefineTRACE.Active == 0;
			c.WarningsDisabled = cmbEnableWarnings.Active == 1;
			c.DocumentationFile = cmbGenerateXmlDocumentation.Active == 0 ? config.ParentItem.Name + ".xml" : string.Empty;
			c.RemoveIntegerChecks = cmbRemoveIntegerChecks.Active == 0;
			c.DebugType = cmbDebugType.ActiveText;
			c.NoWarn = txtDontWarnAbout.Text;
			c.DefineConstants = txtDefineConstants.Text;
			c.WarningsAsErrors = txtTreatAsError.Text;
			c.AdditionalParameters = txtAdditionalArguments.Text;
		}
		
		public void Load (DotNetProjectConfiguration config)
		{
			VBCompilerParameters c = (VBCompilerParameters) config.CompilationParameters;
			
			this.config = config;
			
			cmbOptimize.Active = c.Optimize ? 0 : 1;
			cmbDefineDEBUG.Active = c.DefineDebug ? 0 : 1;
			cmbDefineTRACE.Active = c.DefineTrace ? 0 : 1;
			cmbEnableWarnings.Active = c.WarningsDisabled ? 1 : 0;
			cmbGenerateXmlDocumentation.Active = string.IsNullOrEmpty (c.DocumentationFile) ? 1 : 0;
			cmbRemoveIntegerChecks.Active = c.RemoveIntegerChecks ? 0 : 1;

			switch (c.DebugType.ToLower ()) {
			case "none":
				cmbDebugType.Active = 0;
				break;
			case "pdbonly":
				cmbDebugType.Active = 1;
				break;
			case "full":
			default:
				cmbDebugType.Active = 2;
				break;
			}

			txtDontWarnAbout.Text = c.NoWarn;
			txtDefineConstants.Text = c.DefineConstants;
			txtTreatAsError.Text = c.WarningsAsErrors;
			txtAdditionalArguments.Text = c.AdditionalParameters;
		}
	}
}
