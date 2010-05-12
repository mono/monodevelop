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

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Linq;

namespace MonoDevelop.WelcomePage
{
	public enum WelcomePageCommands
	{
		ShowWelcomePage
	}
	
	class ShowWelcomePageOnStartUpHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.Workspace.FirstWorkspaceItemOpened += delegate {
				if (WelcomePageOptions.CloseWhenSolutionOpened) {
					var doc = ShowWelcomePageHandler.GetWelcomePageDoc ();
					if (doc != null)
						doc.Close ();
				}
			};
			IdeApp.Workspace.LastWorkspaceItemClosed += delegate {
				if (WelcomePageOptions.CloseWhenSolutionOpened && WelcomePageOptions.ShowOnStartup)
					ShowWelcomePageHandler.Show ();
			};
			
			if (WelcomePageOptions.ShowOnStartup)
				IdeApp.Workbench.OpenDocument (new WelcomePageView (), true);
		}
	}

	class ShowWelcomePageHandler : CommandHandler
	{
		public static Document GetWelcomePageDoc ()
		{
			foreach (var d in IdeApp.Workbench.Documents)
				if (d.GetContent<WelcomePageView>() != null)
					return d;
			return null;
		}
		
		public static void Show ()
		{
			var wp = GetWelcomePageDoc ();
			if (wp != null) {
				wp.Select();
			} else {
				IdeApp.Workbench.OpenDocument (new WelcomePageView (), true);
			}
		}
		
		protected override void Run()
		{
			Show ();
		}
		
		protected override void Update (CommandInfo info)
		{
			base.Update (info);
		}
	}
	
	static class WelcomePageOptions
	{
		public static bool ShowOnStartup {
			get { return PropertyService.Get ("WelcomePage.ShowOnStartup", true); }
			set { PropertyService.Set ("WelcomePage.ShowOnStartup", value); }
		}
		
		public static bool CloseWhenSolutionOpened {
			get { return PropertyService.Get ("WelcomePage.CloseWhenSolutionOpened", true); }
			set { PropertyService.Set ("WelcomePage.CloseWhenSolutionOpened", value); }
		}
		
		public static bool UpdateFromInternet {
			get { return PropertyService.Get ("WelcomePage.UpdateFromInternet", true); }
			set { PropertyService.Set ("WelcomePage.UpdateFromInternet", value); }
		}
	}
}
