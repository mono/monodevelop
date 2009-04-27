// 
// CompilerParametersPanelWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Gtk;

namespace ILAsmBinding
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CompilerParametersPanelWidget : Gtk.Bin
	{
		public CompilerParametersPanelWidget()
		{
			this.Build();
			
			ListStore store = new ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Executable"));
			store.AppendValues (GettextCatalog.GetString ("Library"));
			compileTargetCombo.Model = store;
			CellRendererText cr = new CellRendererText ();
			compileTargetCombo.PackStart (cr, true);
			compileTargetCombo.AddAttribute (cr, "text", 0);
		}
		
		DotNetProject project;
		DotNetProjectConfiguration configuration;
		public void Load (DotNetProject project, DotNetProjectConfiguration configuration)
		{
			this.project       = project;
			this.configuration = configuration;
			compileTargetCombo.Active          = configuration.CompileTarget == CompileTarget.Exe ? 0 : 1;
			checkbuttonIncludeDebugInfo.Active = configuration.DebugMode;
		}
		
		public void Store ()
		{
			if (compileTargetCombo.Active == 0) {
				project.CompileTarget = CompileTarget.Exe;
			} else {
				project.CompileTarget = CompileTarget.Library;
			}
			configuration.DebugMode = checkbuttonIncludeDebugInfo.Active;
		}
	}
	
	public class CompilerParametersPanel : MonoDevelop.Projects.Gui.Dialogs.MultiConfigItemOptionsPanel
	{
		CompilerParametersPanelWidget widget;
		
		public override Widget CreatePanelWidget()
		{
			return widget = new CompilerParametersPanelWidget ();
		}
		
		public override void LoadConfigData ()
		{
			widget.Load (ConfiguredProject as DotNetProject, (DotNetProjectConfiguration) CurrentConfiguration);
			widget.ShowAll ();
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
}
