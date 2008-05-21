//  CompilerParametersPanel.cs
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
using Gtk;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;

namespace ILAsmBinding
{
	public class CompilerParametersPanel : MultiConfigItemOptionsPanel
	{
		//ILAsmCompilerParameters compilerParameters = null;
		DotNetProjectConfiguration configuration;
		DotNetProject project;
		
		Entry outputPath = new Entry ();
		Entry assemblyName = new Entry ();
		RadioButton exeTarget = new RadioButton ("exe");
		RadioButton dllTarget;
		CheckButton debug = new CheckButton (GettextCatalog.GetString ("Include debug information"));
		Gtk.Widget mainBox;
		
		public override Widget CreatePanelWidget()
		{
			dllTarget = new RadioButton (exeTarget, "dll");
			SetupUI ();
			mainBox.ShowAll ();
			return mainBox;
		}
		
		public override void LoadConfigData ()
		{
			configuration = (DotNetProjectConfiguration) CurrentConfiguration;
			project = ConfiguredProject as DotNetProject;
			RestoreValues ();
		}
		
		public override void ApplyChanges()
		{
			configuration.OutputAssembly = assemblyName.Text;
			configuration.OutputDirectory = outputPath.Text;
			configuration.DebugMode = debug.Active;
			if (exeTarget.Active)
				project.CompileTarget = CompileTarget.Exe;
			else
				project.CompileTarget = CompileTarget.Library;
		}

		void SetupUI ()
		{
			VBox vbox = new VBox (false, 6);
			Label outputLabel = new Label ();
			outputLabel.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Output path"));
			vbox.PackStart (outputLabel, false, true, 0);
			vbox.PackStart (outputPath, false, true, 0);
			Label assemblyLabel = new Label ();
			assemblyLabel.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Assembly name"));
			vbox.PackStart (assemblyLabel, false, true, 0);
			vbox.PackStart (assemblyName, false, true, 0);
			Label targetLabel = new Label ();
			targetLabel.Markup = String.Format ("<b>{0}</b>", GettextCatalog.GetString ("Target options"));
			vbox.PackStart (targetLabel, false, true, 0);
			vbox.PackStart (exeTarget, false, true, 0);
			vbox.PackStart (dllTarget, false, true, 0);
			vbox.PackStart (debug, false, true, 0);
			mainBox = vbox;
		}

		void RestoreValues ()
		{
			assemblyName.Text = configuration.OutputAssembly;
			outputPath.Text = configuration.OutputDirectory;
			if (project.CompileTarget == CompileTarget.Exe)
				exeTarget.Active = true;
			else
				dllTarget.Active = true;
			debug.Active = configuration.DebugMode;
		}
	}
}
