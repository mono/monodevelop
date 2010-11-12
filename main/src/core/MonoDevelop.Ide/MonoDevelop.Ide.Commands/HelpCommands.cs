// HelpCommands.cs
//
// Author:
//   Carlo Kok (ck@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//


using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
{
	/// <summary>
	/// Copied from MonoDevelop.Ide.addin.xml
	/// </summary>
	public enum HelpCommands {
		Help,
		TipOfTheDay,
		About
	}

	// MonoDevelop.Ide.Commands.HelpCommands.Help
	public class HelpHandler: CommandHandler 
	{
		protected override void Run ()
		{
			IdeApp.HelpOperations.ShowHelp ("root:");
		}
		
		protected override void Update (CommandInfo info)
		{
			if (!IdeApp.HelpOperations.CanShowHelp ("root:"))
				info.Visible = false;
		}
	}

	// MonoDevelop.Ide.Commands.HelpCommands.TipOfTheDay
	public class TipOfTheDayHandler : CommandHandler
	{
		protected override void Run ()
		{
			TipOfTheDayWindow dlg = new TipOfTheDayWindow ();
			dlg.Show ();
		}
	}

	// MonoDevelop.Ide.Commands.HelpCommands.About
	public class AboutHandler : CommandHandler
	{
		protected override void Run ()
		{
			var dlg = new CommonAboutDialog ();
			try {
				MessageService.PlaceDialog (dlg, null);
				dlg.Run ();
			}
			finally {
				dlg.Destroy ();
			}
		}
	}
}
