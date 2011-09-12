// 
// WelcomePageOptionPanel.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageOptionPanel : OptionsPanel
	{
		CheckButton showOnStartCheckButton = new CheckButton ();
		CheckButton internetUpdateCheckButton = new CheckButton ();
		CheckButton closeOnOpenSlnCheckButton = new CheckButton ();
		
		public override Widget CreatePanelWidget ()
		{
			VBox vbox = new VBox();
			showOnStartCheckButton.Label = GettextCatalog.GetString ("Show welcome page on startup");
			showOnStartCheckButton.Active = WelcomePageOptions.ShowOnStartup;
			vbox.PackStart(showOnStartCheckButton, false, false, 0);
			
			internetUpdateCheckButton.Label = GettextCatalog.GetString ("Update welcome page from internet");
			internetUpdateCheckButton.Active = WelcomePageOptions.UpdateFromInternet;
			vbox.PackStart(internetUpdateCheckButton, false, false, 0);
			
			closeOnOpenSlnCheckButton.Label = GettextCatalog.GetString ("Close welcome page after opening a solution");
			closeOnOpenSlnCheckButton.Active = WelcomePageOptions.CloseWhenSolutionOpened;
			vbox.PackStart(closeOnOpenSlnCheckButton, false, false, 0);
			
			vbox.ShowAll ();
			return vbox;
		}
		
		public override void ApplyChanges ()
		{
			WelcomePageOptions.ShowOnStartup = showOnStartCheckButton.Active;
			WelcomePageOptions.UpdateFromInternet = internetUpdateCheckButton.Active;
			WelcomePageOptions.CloseWhenSolutionOpened = closeOnOpenSlnCheckButton.Active;
		}
	}

}