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
using MonoDevelop.Core;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	
	class TextStylePolicyPanel : MimeTypePolicyOptionsPanel<TextStylePolicy>
	{
		TextStylePolicyPanelWidget widget;
		
		public override Gtk.Widget CreatePanelWidget ()
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
			lineEndingCombo.AppendText (GettextCatalog.GetString ("Mac"));
			lineEndingCombo.AppendText (GettextCatalog.GetString ("Unix"));
			lineEndingCombo.AppendText (GettextCatalog.GetString ("Microsoft Windows")); // Using "Windows" is too short, otherwise the translation get's confused. Mike
		}
		
		protected virtual void UpdateState (object sender, System.EventArgs e)
		{
			panel.UpdateSelectedNamedPolicy ();
		}
		
		public void LoadFrom (TextStylePolicy policy)
		{
			tabWidthSpin.Value = policy.TabWidth;
			tabsAfterNonTabsCheck.Active = !policy.NoTabsAfterNonTabs;
			tabsToSpaceCheck.Active = policy.TabsToSpaces;
			removeTrailingWhitespaceCheck.Active = policy.RemoveTrailingWhitespace;
			columnWidthSpin.Value = policy.FileWidth;
			lineEndingCombo.Active = (int) policy.EolMarker;
		}
		
		public TextStylePolicy GetPolicy ()
		{
			return new TextStylePolicy (
				(int) columnWidthSpin.Value,
				(int) tabWidthSpin.Value,
				tabsToSpaceCheck.Active,
				!tabsAfterNonTabsCheck.Active,
				removeTrailingWhitespaceCheck.Active,
				(EolMarker) lineEndingCombo.Active);
		}
	}
}
