//  ChooseRuntimePanel.cs
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
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Core.Gui.Dialogs;

using Gtk;

namespace CSharpBinding
{
	public class ChooseRuntimePanel : AbstractOptionPanel
	{
		CSharpCompilerParameters parameters;
		DotNetProjectConfiguration config;

		// FIXME: set the right rb groups
		RadioButton msnetRadioButton = new RadioButton ("Msnet");
		RadioButton monoRadioButton = new RadioButton ("Mono");
		RadioButton mintRadioButton = new RadioButton ("Mint");
		RadioButton cscRadioButton = new RadioButton ("CSC");
		RadioButton mcsRadioButton = new RadioButton ("MCS");
		
		public override void LoadPanelContents()
		{
			config = ((Properties)CustomizationObject).Get<DotNetProjectConfiguration> ("Config");
			parameters = (CSharpCompilerParameters) config.CompilationParameters;
			
			msnetRadioButton.Active = config.NetRuntime == NetRuntime.MsNet;
			monoRadioButton.Active  = config.NetRuntime == NetRuntime.Mono;
			mintRadioButton.Active  = config.NetRuntime == NetRuntime.MonoInterpreter;
			
			cscRadioButton.Active = parameters.CsharpCompiler == CsharpCompiler.Csc;
			mcsRadioButton.Active = parameters.CsharpCompiler == CsharpCompiler.Mcs;
		}
		
		public override bool StorePanelContents()
		{
			if (msnetRadioButton.Active) {
				config.NetRuntime =  NetRuntime.MsNet;
			} else if (monoRadioButton.Active) {
				config.NetRuntime =  NetRuntime.Mono;
			} else {
				config.NetRuntime =  NetRuntime.MonoInterpreter;
			}
			parameters.CsharpCompiler = cscRadioButton.Active ? CsharpCompiler.Csc : CsharpCompiler.Mcs;
			
			return true;
		}
	}
}
