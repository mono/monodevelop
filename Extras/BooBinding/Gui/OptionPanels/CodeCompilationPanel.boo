#region license
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// All rights reserved.
//
// BooBinding is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// BooBinding is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with BooBinding; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion

namespace BooBinding.Gui.OptionPanels

import BooBinding

import System
import Gtk

import MonoDevelop.Internal.Project
import MonoDevelop.Gui.Dialogs
import MonoDevelop.Gui.Widgets
import MonoDevelop.Services
import MonoDevelop.Core.Services
import MonoDevelop.Core.Properties
import MonoDevelop.Core.AddIns.Codons

public class CodeGenerationPanel(AbstractOptionPanel):
	private codeGenerationLabel as Gtk.Label = Gtk.Label ()
	private labelWarnings as Gtk.Label = Gtk.Label ()
	private labelOutputDir as Gtk.Label = Gtk.Label ()
	private outputLabel as Gtk.Label = Gtk.Label ()
	private labelCompiler as Gtk.Label = Gtk.Label ()
	private labelCulture as Gtk.Label = Gtk.Label ()

	private labelCompileTarget as Gtk.Label = Gtk.Label ()
	private compileTargetCombo as Gtk.ComboBox = Gtk.ComboBox ()
	
	private checkDebug = CheckButton (GettextCatalog.GetString ("Enable debug"))
	private checkDucky = CheckButton (GettextCatalog.GetString ("Enable ducky mode"))

	private outputAssembly = Entry ()
	private outputDirectory = Entry()
	// Waiting on easy method for setting entry text before using
	//private outputDirectory as FolderEntry = FolderEntry ("Output Directory")
	private compilerPath = Entry ()
	private culture = Entry ()
	
	compilerParameters as BooCompilerParameters = null
	configuration as DotNetProjectConfiguration  = null
	
	public def constructor():

		InitializeComponent ()
		vbox = VBox ()
		hboxTmp = HBox ()
		hboxTmp.PackStart (codeGenerationLabel, false, false, 0)
		vbox.PackStart (hboxTmp, false, false, 12)
		
		hboxTmp = HBox()
		tableOutputOptions = Table (4, 2, false)
		tableOutputOptions.Attach (outputLabel, 0, 1, 0, 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0)
		tableOutputOptions.Attach (outputAssembly, 1, 2, 0, 1, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 3)
		tableOutputOptions.Attach (labelOutputDir, 0, 1, 1, 2, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0)
		tableOutputOptions.Attach (outputDirectory, 1, 2, 1, 2, AttachOptions.Fill | AttachOptions.Expand , AttachOptions.Fill, 0, 3)
		tableOutputOptions.Attach (labelCompileTarget, 0, 1, 2, 3, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0)
		tableOutputOptions.Attach (compileTargetCombo, 1, 2, 2, 3, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 3)
		tableOutputOptions.Attach (labelCulture, 0, 1, 3, 4, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0)
		tableOutputOptions.Attach (culture, 1, 2, 3, 4, AttachOptions.Fill | AttachOptions.Expand, AttachOptions.Fill, 0, 3)
		hboxTmp.PackStart (tableOutputOptions, true, true, 6)
		vbox.PackStart (hboxTmp, false, false, 0)
		
		hboxTmp = HBox ()
		hboxTmp.PackStart (labelWarnings, false, false, 0)
		vbox.PackStart (hboxTmp, false, false, 12)
		hboxTmp = HBox()
		hboxTmp.PackStart (checkDebug, false, false, 6)
		vbox.PackStart (hboxTmp, false, false, 0)
		hboxTmp = HBox()
		hboxTmp.PackStart (checkDucky, false, false, 6)
		vbox.PackStart (hboxTmp, false, false, 0)
		Add (vbox)

	private def InitializeComponent() as void:
		codeGenerationLabel.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Code Generation"))
		labelOutputDir.Markup = String.Format ("{0} :", GettextCatalog.GetString ("Output Path"))
		labelOutputDir.Layout.Alignment = Pango.Alignment.Right
		outputAssembly = Entry ()
		
		outputLabel.Markup = String.Format ("{0} :", GettextCatalog.GetString ("Output Assembly"))
		outputLabel.Layout.Alignment = Pango.Alignment.Right
		labelWarnings.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Warnings and Compiler Options"))
		
		labelCompiler.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Compiler"))
		labelCulture.Markup = String.Format ("{0} :", GettextCatalog.GetString ("Culture"))
		labelCulture.Layout.Alignment = Pango.Alignment.Right
		labelCompileTarget.Markup = String.Format ("{0} :", GettextCatalog.GetString ("Output Assembly"))
		

		store = ListStore (string)

		stringArray = array(System.String, 1)
		stringArray[0] = GettextCatalog.GetString ("Executable")
		store.AppendValues (stringArray)

		stringArray = array(System.String, 1)
		stringArray[0] = GettextCatalog.GetString ("Library")
		store.AppendValues (stringArray)

		/*
		stringArray = array(System.String, 1)
		stringArray[0] = GettextCatalog.GetString ("Windows Executable")
		store.AppendValues (stringArray)
		*/

		compileTargetCombo.Model = store
		cr = CellRendererText()
		compileTargetCombo.PackStart(cr, true)
		compileTargetCombo.AddAttribute(cr, "text", 0)

	
	public override def LoadPanelContents() as void:
		configuration = cast(DotNetProjectConfiguration,(cast(IProperties,CustomizationObject)).GetProperty("Config"))
		compilerParameters = cast (BooCompilerParameters, configuration.CompilationParameters)

		checkDebug.Active = configuration.DebugMode
		checkDucky.Active = compilerParameters.Ducky
		outputAssembly.Text = configuration.OutputAssembly
		//outputDirectory.DefaultPath = configuration.OutputDirectory
		outputDirectory.Text = configuration.OutputDirectory
		
		culture.Text = compilerParameters.Culture
		compileTargetCombo.Active = cast (int, configuration.CompileTarget)

	public override def StorePanelContents() as bool:
		if (compilerParameters is null):
			return true


		configuration.DebugMode = checkDebug.Active
		configuration.CompileTarget = cast (CompileTarget, compileTargetCombo.Active)
		configuration.OutputAssembly = outputAssembly.Text
		configuration.OutputDirectory = outputDirectory.Text
		//configuration.OutputDirectory = outputDirectory.Path

		compilerParameters.Ducky = checkDucky.Active
		compilerParameters.Culture = culture.Text

		return true
