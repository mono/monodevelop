//
//TextStylePolicyPanelWidget.cs
// 
//Author:
//      Michael Hutchinson <mhutchinson@novell.com>
//
//Copyright (c) 2009 Michael Hutchinson
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

using System;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	
	class TextStylePolicyPanel : MimeTypePolicyOptionsPanel<TextStylePolicy>
	{
		TextStylePolicyPanelWidget widget;
		
		public override Control CreatePanelWidget ()
		{
			widget = new TextStylePolicyPanelWidget (this);
			widget.Show ();
			return widget;
		}
		
		protected override void LoadFrom (TextStylePolicy policy)
		{
			widget.LoadFrom (policy);
		}
		
		protected override TextStylePolicy GetPolicy ()
		{
			return widget.GetPolicy ();
		}
	}
	
	partial class TextStylePolicyPanelWidget : Gtk.Bin
	{
		TextStylePolicyPanel panel;
		
		public TextStylePolicyPanelWidget (TextStylePolicyPanel panel)
		{
			this.Build();
			this.panel = panel;
			
			//NOTE: order corresponds to EolMarker enum values
			lineEndingCombo.AppendText (GettextCatalog.GetString ("Native"));
			lineEndingCombo.AppendText (GettextCatalog.GetString ("Mac Classic"));
			lineEndingCombo.AppendText (GettextCatalog.GetString ("Unix / Mac"));
			lineEndingCombo.AppendText (GettextCatalog.GetString ("Microsoft Windows")); // Using "Windows" is too short, otherwise the translation get's confused. Mike

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			columnWidthSpin.SetCommonAccessibilityAttributes ("Textpolicy.WidthSpinner", label1,
			                                                  GettextCatalog.GetString ("The desired width of the file in columns"));

			lineEndingCombo.SetCommonAccessibilityAttributes ("Textpolicy.LineEndings", label6,
			                                                  GettextCatalog.GetString ("Select the type of line endings the file should have"));

			tabWidthSpin.SetCommonAccessibilityAttributes ("Textpolicy.TabWidth", label7,
			                                               GettextCatalog.GetString ("Select the width of tab stops"));

			indentWidthSpin.SetCommonAccessibilityAttributes ("Textpolicy.IndentWidth", label9,
			                                                  GettextCatalog.GetString ("Select the width of indents"));

			tabsToSpaceCheck.SetCommonAccessibilityAttributes ("Textpolicy.TabsToSpaces", "",
			                                                   GettextCatalog.GetString ("Check to automatically convert tabs to spaces"));
			tabsAfterNonTabsCheck.SetCommonAccessibilityAttributes ("Textpolicy.TabsAfterSpaces", "",
			                                                        GettextCatalog.GetString ("Check to allow tabs after non-tabs"));
			removeTrailingWhitespaceCheck.SetCommonAccessibilityAttributes ("Textpolicy.TrailingWhitespace", "",
			                                                                GettextCatalog.GetString ("Check to automatically remove trailing whitespace from a line"));
		}

		protected virtual void UpdateState (object sender, System.EventArgs e)
		{
			panel.UpdateSelectedNamedPolicy ();
		}
		
		public void LoadFrom (TextStylePolicy policy)
		{
			tabWidthSpin.Value = policy.TabWidth;
			indentWidthSpin.Value = policy.IndentWidth;
			tabsAfterNonTabsCheck.Active = !policy.NoTabsAfterNonTabs;
			tabsToSpaceCheck.Active = policy.TabsToSpaces;
			removeTrailingWhitespaceCheck.Active = policy.RemoveTrailingWhitespace;
			columnWidthSpin.Value = policy.FileWidth;
			lineEndingCombo.Active = (int) policy.EolMarker;
		}
		
		public TextStylePolicy GetPolicy ()
		{
			return new TextStylePolicy (
				(int)columnWidthSpin.Value,
				(int)tabWidthSpin.Value,
				(int)indentWidthSpin.Value,
				tabsToSpaceCheck.Active,
				!tabsAfterNonTabsCheck.Active,
				removeTrailingWhitespaceCheck.Active,
				(EolMarker) lineEndingCombo.Active);
		}
	}
}
