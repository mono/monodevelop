// 
// BuildMessagePanel.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	
	internal class BuildMessagePanel : OptionsPanel
	{
		BuildMessagePanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			return widget = new BuildMessagePanelWidget ();
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class BuildMessagePanelWidget : Gtk.Bin
	{

		public BuildMessagePanelWidget ()
		{
			this.Build ();
			//comboboxJumpToFirst.AppendText (GettextCatalog.GetString ("Never"));
			comboboxJumpToFirst.AppendText (GettextCatalog.GetString ("Error"));
			comboboxJumpToFirst.AppendText (GettextCatalog.GetString ("Error or Warning"));
			comboboxJumpToFirst.Active = (int)IdeApp.Preferences.JumpToFirstErrorOrWarning.Value;
			
	/*		//comboboxBuildResultsDuring.AppendText (GettextCatalog.GetString ("Never"));
			comboboxBuildResultsDuring.AppendText (GettextCatalog.GetString ("Always"));
			comboboxBuildResultsDuring.AppendText (GettextCatalog.GetString ("On Errors"));
			comboboxBuildResultsDuring.AppendText (GettextCatalog.GetString ("On Errors or Warnings"));
			comboboxBuildResultsDuring.Active = (int)IdeApp.Preferences.ShowOutputPadDuringBuild;
			*/
			
		/*	//comboboxErrorPadDuring.AppendText (GettextCatalog.GetString ("Never"));
			comboboxErrorPadDuring.AppendText (GettextCatalog.GetString ("Always"));
			comboboxErrorPadDuring.AppendText (GettextCatalog.GetString ("On Errors"));
			comboboxErrorPadDuring.AppendText (GettextCatalog.GetString ("On Errors or Warnings"));
			comboboxErrorPadDuring.Active = (int)IdeApp.Preferences.ShowErrorPadDuringBuild;
			*/
			//comboboxErrorPadAfter.AppendText (GettextCatalog.GetString ("Never"));
			comboboxErrorPadAfter.AppendText (GettextCatalog.GetString ("Always"));
			comboboxErrorPadAfter.AppendText (GettextCatalog.GetString ("On Errors"));
			comboboxErrorPadAfter.AppendText (GettextCatalog.GetString ("On Errors or Warnings"));
			comboboxErrorPadAfter.Active = (int)IdeApp.Preferences.ShowErrorPadAfterBuild.Value;
			
			//comboboxMessageBubbles.AppendText (GettextCatalog.GetString ("Never"));
			comboboxMessageBubbles.AppendText (GettextCatalog.GetString ("For Errors"));
			comboboxMessageBubbles.AppendText (GettextCatalog.GetString ("For Errors and Warnings"));
			comboboxMessageBubbles.Active = (int)IdeApp.Preferences.ShowMessageBubbles.Value;
			this.QueueResize ();

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			comboboxJumpToFirst.SetCommonAccessibilityAttributes ("BuildMessagePanel.jumpToFirst", null,
			                                                      GettextCatalog.GetString ("Select which type of result to jump to after build completes"));
			comboboxJumpToFirst.SetAccessibilityLabelRelationship (label6);

			comboboxErrorPadAfter.SetCommonAccessibilityAttributes ("BuildMessagePanel.errorPadAfter", null,
			                                                        GettextCatalog.GetString ("Select when to show the Error Pad"));
			comboboxErrorPadAfter.SetAccessibilityLabelRelationship (label3);

			comboboxMessageBubbles.SetCommonAccessibilityAttributes ("BuildMessagePanel.messageBubbles", null,
			                                                         GettextCatalog.GetString ("Select when to show message bubbles"));
			comboboxMessageBubbles.SetAccessibilityLabelRelationship (label5);
		}
		public void Store ()
		{
			IdeApp.Preferences.JumpToFirstErrorOrWarning.Value = (JumpToFirst)comboboxJumpToFirst.Active;
//			IdeApp.Preferences.ShowErrorPadDuringBuild = (BuildResultStates)comboboxErrorPadDuring.Active;
//			IdeApp.Preferences.ShowOutputPadDuringBuild = (BuildResultStates)comboboxBuildResultsDuring.Active;
			
			IdeApp.Preferences.ShowErrorPadAfterBuild.Value = (BuildResultStates)comboboxErrorPadAfter.Active;
			IdeApp.Preferences.ShowMessageBubbles.Value = (ShowMessageBubbles)comboboxMessageBubbles.Active;
		}
	}
}
