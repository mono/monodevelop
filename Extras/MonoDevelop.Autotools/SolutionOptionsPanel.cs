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
		
		IExtendedDataItem item;
		
		public override void LoadPanelContents()
		{
			VBox vbox = new VBox();
			this.Add(vbox);
			
			item = (IExtendedDataItem) IdeApp.ProjectOperations.CurrentOpenCombine;

			object en_obj =  item.ExtendedProperties ["MakePkgConfig"];

			bool enabled = false;
			if (en_obj== null) 
			{
				item.ExtendedProperties["MakePkgConfig"] = false;
			}
			else enabled = (bool) en_obj;
			
			MakePkgConfigButton.Label = GettextCatalog.GetString ("Create 'pkg-config' file");
			MakePkgConfigButton.Active = enabled;
	
			vbox.PackStart (MakePkgConfigButton, false, false, 0);
		}
		
		public override bool StorePanelContents()
		{
			item.ExtendedProperties["MakePkgConfig"] = MakePkgConfigButton.Active;
			
			return true;
		}
	}

}
