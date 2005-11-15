//
// AddInsOptionsPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class AddInsOptionsPanel : AbstractOptionPanel
	{
		AddInsPanelWidget widget;

		public override void LoadPanelContents()
		{
			Add (widget = new  AddInsPanelWidget ());
		}
		
		public override bool StorePanelContents()
		{
			widget.Store ();
			return true;
		}
		
		public class AddInsPanelWidget :  GladeWidgetExtract 
		{
			//
			// Gtk controls
			//

			[Glade.Widget] public Gtk.CheckButton lookCheck;
			[Glade.Widget] public Gtk.SpinButton valueSpin;   
			[Glade.Widget] public Gtk.ComboBox periodCombo;   

			public  AddInsPanelWidget () : base ("Base.glade", "AddInsOptionsPanel")
			{
				bool checkForUpdates = Runtime.Properties.GetProperty ("MonoDevelop.Ide.AddinUpdater.CkeckForUpdates", true);
				int updateSpan = Runtime.Properties.GetProperty ("MonoDevelop.Ide.AddinUpdater.UpdateSpanValue", 1);
				string unit = Runtime.Properties.GetProperty ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", "D");
				
				lookCheck.Active = checkForUpdates;
				valueSpin.Value = (double) updateSpan;
				
				if (unit == "D")
					periodCombo.Active = 0;
				else
					periodCombo.Active = 1;
				UpdateStatus ();
			}
			
			public void Store ()
			{
				if (periodCombo.Active == 0)
					Runtime.Properties.SetProperty ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", "D");
				else
					Runtime.Properties.SetProperty ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", "M");
				
				Runtime.Properties.SetProperty ("MonoDevelop.Ide.AddinUpdater.UpdateSpanValue", (int) valueSpin.Value);
				Runtime.Properties.SetProperty ("MonoDevelop.Ide.AddinUpdater.CkeckForUpdates", lookCheck.Active);
			}
			
			void UpdateStatus ()
			{
				valueSpin.Sensitive = periodCombo.Sensitive = lookCheck.Active;
			}
			
			public void OnManageClicked (object s, EventArgs a)
			{
				AddinUpdateHandler.ShowManager ();
			}
			
			public void OnCheckToggled (object s, EventArgs a)
			{
				UpdateStatus ();
			}
		}
	}
}
