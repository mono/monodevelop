/*
Copyright (c) 2006 Scott Ellington

Permission is hereby granted, free of charge, to any person 
obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without 
restriction, including without limitation the rights to use, 
copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following 
conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
OTHER DEALINGS IN THE SOFTWARE. 
*/

using System;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Serialization;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AutoTools
{
	public class SolutionOptionsPanel : AbstractOptionPanel
	{
		CheckButton MakePkgConfigButton = new CheckButton();
		CheckButton MakeLibPCButton = new CheckButton();
		
		IExtendedDataItem item;
		
		public override void LoadPanelContents()
		{
			VBox vbox = new VBox();
			vbox.Spacing = 6;
			this.Add(vbox);
			
			item = (IExtendedDataItem) IdeApp.ProjectOperations.CurrentOpenCombine;

			MakeLibPCButton.Label = GettextCatalog.GetString ("Create pkg-config files for libraries");
			MakeLibPCButton.Active = GetProperty ( item, "MakeLibPC", true );
			vbox.PackStart (MakeLibPCButton, false, false, 0);
			
			MakePkgConfigButton.Label = GettextCatalog.GetString ("Create pkg-config file for entire solution");
			MakePkgConfigButton.Active = GetProperty ( item, "MakePkgConfig", false );
			vbox.PackStart (MakePkgConfigButton, false, false, 0);
		}

		static bool GetProperty ( IExtendedDataItem itm, string ke, bool initial )
		{
			object en_obj =  itm.ExtendedProperties [ke];

			if (en_obj== null)
			{	
				itm.ExtendedProperties[ke] = initial;
				return initial;
			}
			else return (bool) en_obj;
		}
		
		public override bool StorePanelContents()
		{
			item.ExtendedProperties["MakePkgConfig"] = MakePkgConfigButton.Active;
			item.ExtendedProperties["MakeLibPC"] = MakeLibPCButton.Active;
			
			return true;
		}
	}

}
