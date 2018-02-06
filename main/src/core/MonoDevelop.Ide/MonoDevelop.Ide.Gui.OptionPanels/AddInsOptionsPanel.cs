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

using MonoDevelop.Components;
using MonoDevelop.Core.Setup;
using MonoDevelop.Ide.Updater;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class AddInsOptionsPanel : OptionsPanel
	{
		AddInsPanelWidget widget;
		
		public override bool IsVisible ()
		{
			return !AddinManager.IsAddinLoaded ("MonoDevelop.Xamarin.Ide");
		}

		public override Control CreatePanelWidget ()
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
		public AddInsPanelWidget ()
		{
			Build ();
		
			if (!UpdateService.AutoCheckForUpdates)
				radioNever.Active = true;
			else if (UpdateService.UpdateSpanUnit == UpdateSpanUnit.Hour)
				radioHour.Active = true;
			else if (UpdateService.UpdateSpanUnit == UpdateSpanUnit.Day)
				radioDay.Active = true;
			else if (UpdateService.UpdateSpanUnit == UpdateSpanUnit.Month)
				radioMonth.Active = true;
			
			switch (UpdateService.UpdateLevel) {
			case UpdateLevel.Beta: radioBeta.Active = true; checkUnstable.Active = true; break;
			case UpdateLevel.Alpha: radioAlpha.Active = true; checkUnstable.Active = true; break;
			case UpdateLevel.Test: radioTest.Visible = true; radioTest.Active = true; checkUnstable.Active = true; break;
			default: checkUnstable.Active = false; break;
			}
			
			if (UpdateService.TestModeEnabled)
				radioTest.Visible = true;
		}
		
		public void Store ()
		{
			UpdateService.AutoCheckForUpdates = !radioNever.Active;
			UpdateService.UpdateSpanValue = 1;
			
			if (radioHour.Active)
				UpdateService.UpdateSpanUnit = UpdateSpanUnit.Hour;
			else if (radioDay.Active)
				UpdateService.UpdateSpanUnit = UpdateSpanUnit.Day;
			else if (radioMonth.Active)
				UpdateService.UpdateSpanUnit = UpdateSpanUnit.Month;

			if (checkUnstable.Active) {		
 				if (radioBeta.Active)		
					UpdateService.UpdateChannel = UpdateChannel.FromUpdateLevel (UpdateLevel.Beta);		
 				else if (radioAlpha.Active)		
					UpdateService.UpdateChannel = UpdateChannel.FromUpdateLevel (UpdateLevel.Alpha);		
 				else if (radioTest.Active)		
					UpdateService.UpdateChannel = UpdateChannel.FromUpdateLevel (UpdateLevel.Test);		
 			} else		
				UpdateService.UpdateChannel = UpdateChannel.FromUpdateLevel (UpdateLevel.Stable);
		}
		
		protected void OnCheckUnstableToggled (object sender, System.EventArgs e)
		{
			boxUnstable.Visible = checkUnstable.Active;
		}

		protected void OnButtonUpdateNowClicked (object sender, System.EventArgs e)
		{
			Store ();
			UpdateService.CheckForUpdates ();
		}
	}
}
