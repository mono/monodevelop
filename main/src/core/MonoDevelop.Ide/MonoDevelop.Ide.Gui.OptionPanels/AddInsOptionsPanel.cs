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

using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Projects;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class AddInsOptionsPanel : OptionsPanel
	{
		AddInsPanelWidget widget;

		public override Widget CreatePanelWidget ()
		{
			return widget = new  AddInsPanelWidget ();
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
		
	internal partial class AddInsPanelWidget :  Gtk.Bin 
	{
		public  AddInsPanelWidget ()
		{
			Build ();
		
			bool checkForUpdates = PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.CkeckForUpdates", true);
			int updateSpan = PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.UpdateSpanValue", 1);
			string unit = PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", "D");
			
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
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", "D");
			else
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", "M");
			
			PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.UpdateSpanValue", (int) valueSpin.Value);
			PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.CkeckForUpdates", lookCheck.Active);
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
