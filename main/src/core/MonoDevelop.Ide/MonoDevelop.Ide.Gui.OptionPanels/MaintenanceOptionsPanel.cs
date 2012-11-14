// 
// MaintenanceOptionsPanelWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Ide.Gui.Dialogs;
using Gtk;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	internal class MaintenanceOptionsPanel : OptionsPanel
	{
		MaintenanceOptionsPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new MaintenanceOptionsPanelWidget ();
		}
		
		public override void ApplyChanges ()
		{
			widget.ApplyChanges ();
		}
		
		public override bool IsVisible ()
		{
			return IsMaintenanceMode;
		}
		
		static bool IsMaintenanceMode {
			get {
				return IdeApp.Preferences.EnableInstrumentation ||
					IdeApp.Preferences.EnableAutomatedTesting ||
					!string.IsNullOrEmpty (Environment.GetEnvironmentVariable ("MONODEVELOP_MAINTENANCE"));
			}
		}
	}
	
	internal partial class MaintenanceOptionsPanelWidget : Gtk.Bin
	{

		public MaintenanceOptionsPanelWidget ()
		{
			this.Build ();
			checkInstr.Active = IdeApp.Preferences.EnableInstrumentation;
			checkAutoTest.Active = IdeApp.Preferences.EnableAutomatedTesting;
			checkInstr.Label = MonoDevelop.Core.BrandingService.BrandApplicationName (checkInstr.Label);
		}
		
		public void ApplyChanges ()
		{
			if (IdeApp.Preferences.EnableInstrumentation != checkInstr.Active)
				IdeApp.Preferences.EnableInstrumentation = checkInstr.Active;
			if (IdeApp.Preferences.EnableAutomatedTesting != checkAutoTest.Active)
				IdeApp.Preferences.EnableAutomatedTesting = checkAutoTest.Active;
		}
	}
}
