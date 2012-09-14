// 
// AnalysisOptionsPanel.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.AnalysisCore.Gui
{
	public class AnalysisOptionsPanel : OptionsPanel
	{
		AnalysisOptionsWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			return widget = new AnalysisOptionsWidget () {
				AnalysisEnabled = AnalysisOptions.AnalysisEnabled
			};
		}
		
		public override void ApplyChanges ()
		{
			AnalysisOptions.AnalysisEnabled.Set (widget.AnalysisEnabled);
		}
	}
	
	class AnalysisOptionsWidget : VBox
	{
		CheckButton enabledCheck;
		
		public AnalysisOptionsWidget ()
		{
			enabledCheck = new CheckButton (GettextCatalog.GetString ("Enable source analysis of open files"));
			PackStart (enabledCheck, false, false, 0);
			if (GC.MaxGeneration == 0) {
				HBox hb = new HBox ();
				hb.Spacing = 6;
				hb.PackStart (ImageService.GetImage (Stock.DialogWarning, IconSize.Dialog), false, false, 0);
				hb.PackStart (new Label (GettextCatalog.GetString ("Note: Source analysis may be slow with the current garbage collector.\nUse a generational GC like sgen to get best performance.")), true, true, 0);
				PackStart (hb, false, false, 32);
			}

			ShowAll ();
		}
		
		public bool AnalysisEnabled {
			get { return enabledCheck.Active; }
			set { enabledCheck.Active = value; }
		}
	}
}

