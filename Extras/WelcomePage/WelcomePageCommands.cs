/*
Copyright (c) 2005 Scott Ellington

Permission is hereby granted, free of charge, to any person 
obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without 
restriction, including without limitation the rights to use, 
copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following 
conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
OTHER DEALINGS IN THE SOFTWARE. 
*/

using System;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;

using MonoDevelop.Ide.Gui;

namespace MonoDevelop.WelcomePage
{
	public enum WelcomePageCommands
	{
		ShowWelcomePage
	}
	
	public class ShowWelcomePageOnStartUpHandler : CommandHandler
	{
		protected override void Run()
		{
			if (!PropertyService.Get("WelcomePage.ShowOnStartup", true))
				return;
			
			WelcomePageView wpv = new WelcomePageView();
			IdeApp.Workbench.OpenDocument (wpv, true);

			// Html must be rendered after the widget has been drawn
			wpv.Initialize ();
		}
	}

	public class ShowWelcomePageHandler : CommandHandler
	{
		protected override void Run()
		{
			foreach (Document d in IdeApp.Workbench.Documents) {
				if (d.GetContent<WelcomePageView>() != null) {
					d.Select();
					return;
				}
			}
			WelcomePageView wpv = new WelcomePageView();
			IdeApp.Workbench.OpenDocument(wpv, true);
			
			// Html must be rendered after the widget has been drawn
			wpv.Initialize ();
		}
	}
}
