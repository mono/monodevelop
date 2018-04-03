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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.LogReporting;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	public class LogAgentOptionsPanel : OptionsPanel
	{
		LogAgentPanelWidget widget;

		public override Control CreatePanelWidget ()
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
		bool? reportUsage;
		CheckButton chkUsage;
		VBox container;
		
		public LogAgentPanelWidget ()
		{
			BinContainer.Attach (this);

			var reportingLabel = GettextCatalog.GetString ("Report errors and usage information to help {0} improve my experience.", BrandingService.SuiteName);

			var value = LoggingService.ReportUsage;
			chkUsage = new CheckButton (reportingLabel);
			if (value.HasValue)
				chkUsage.Active = value.Value;
			chkUsage.Toggled += (sender, e) => reportUsage = chkUsage.Active;
			
			container = new Gtk.VBox ();
			container.PackStart (chkUsage, false, false, 0);

			var privacyStatement = BrandingService.PrivacyStatement;
			if (!string.IsNullOrEmpty (privacyStatement)) {
				var privacyLabel = new Xwt.Label { Markup = privacyStatement, Wrap = Xwt.WrapMode.Word };
				container.Add (new HBox ());
				container.PackEnd (privacyLabel.ToGtkWidget (), false, false, 30);
			}
			
			Add (container);
			ShowAll ();
		}
		
		public void Store ()
		{
			if (reportUsage.HasValue) {
				LoggingService.ReportCrashes = reportUsage.Value;
				LoggingService.ReportUsage = reportUsage.Value;
			}
		}
	}
}

