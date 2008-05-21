//
// RuntimeOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;

using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class RuntimeOptionsPanel : ItemOptionsPanel
	{
		RuntimeOptionsPanelWidget widget;
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is DotNetProject;
		}
		
		public override Widget CreatePanelWidget()
		{
			return (widget = new RuntimeOptionsPanelWidget ((DotNetProject)ConfiguredProject));
		}
		
		public override void ApplyChanges()
		{
			widget.Store ();
		}
	}

	partial class RuntimeOptionsPanelWidget : Gtk.Bin 
	{
		DotNetProject project;
		ArrayList supportedVersions = new ArrayList (); 

		public RuntimeOptionsPanelWidget (DotNetProject project)
		{
			Build ();
			
			this.project = project;
			if (project != null) {
				// Get the list of available versions, and add only those supported by the target language.
				ClrVersion[] langSupported = project.LanguageBinding.GetSupportedClrVersions ();
				foreach (ClrVersion ver in Runtime.SystemAssemblyService.GetSupportedClrVersions ()) {
					if (Array.IndexOf (langSupported, ver) == -1)
						continue;
					string desc;
					switch (ver) {
					case ClrVersion.Net_1_1:
						desc = "Mono/.NET 1.1 Profile";
						break;
					case ClrVersion.Net_2_0:
						desc = "Mono/.NET 2.0 Profile";
						break;
					case ClrVersion.Clr_2_1:
						desc = "Moonlight/Silverlight 1.1";
						break;
					default:
						throw new Exception ("Unknown ClrVersion '" + ver.ToString () + "'");
					}
					runtimeVersionCombo.AppendText (desc);
					if (project.ClrVersion == ver)
		 				runtimeVersionCombo.Active = supportedVersions.Count;
					supportedVersions.Add (ver);
				}
				if (supportedVersions.Count <= 1)
					Sensitive = false;
 			}
 			else
 				Sensitive = false;
		}

		public void Store ()
		{	
			if (project == null || runtimeVersionCombo.Active == -1)
				return;
			project.ClrVersion = (ClrVersion) supportedVersions [runtimeVersionCombo.Active];
		}
	}
}
