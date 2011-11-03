// 
// LogAgentOptionsPanel.cs
//  
// Author:
//       Alan McGovern <alan@xamarin.com>
// 
// Copyright (c) 2011, Xamarin Inc.
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
using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	public class LogAgentOptionsPanel : OptionsPanel
	{
		LogAgentPanelWidget widget;

		public override Widget CreatePanelWidget ()
		{
			return widget = new  LogAgentPanelWidget ();
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
	public class LogAgentPanelWidget : Gtk.Bin 
	{
		const string ReportCrashProperty = "MonoDevelop.LogAgent.ReportCrashes";
		const string ReportUsageProperty = "MonoDevelop.LogAgent.ReportUsage";
		
		bool? reportCrash;
		bool? reportUsage;
		
		CheckButton chkCrash;
		CheckButton chkUsage;
		VBox container;
		
		public LogAgentPanelWidget ()
		{
			global::Stetic.BinContainer.Attach (this);
			chkCrash = new CheckButton (GettextCatalog.GetString ("Automatically submit crash diagnostic information")) {
				Active = PropertyService.Get<bool> (ReportCrashProperty)
			};
			chkCrash.Toggled += (sender, e) => reportCrash = chkCrash.Active;
			
			chkUsage = new CheckButton (GettextCatalog.GetString ("Automatically submit usage information")) {
				Active = PropertyService.Get<bool> (ReportUsageProperty)
			};
			chkUsage.Toggled += (sender, e) => reportUsage = chkUsage.Active;
			
			container = new Gtk.VBox ();
			container.Add (chkCrash);
			container.Add (chkUsage);
			
			Add (container);
			ShowAll ();
		}
		
		public void Store ()
		{
			if (reportCrash.HasValue)
				PropertyService.Set (ReportCrashProperty, reportCrash.Value);
			if (reportUsage.HasValue)
				PropertyService.Set (ReportUsageProperty, reportUsage.Value);
		}
	}
}

