// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Gtk;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
{
	public enum HelpCommands
	{
		TipOfTheDay,
		About
	}
	
	internal class TipOfTheDayHandler: CommandHandler
	{
		protected override void Run ()
		{
			TipOfTheDayWindow totdw = new TipOfTheDayWindow ();
			totdw.Show ();
		}
	}
		
	internal class AboutHandler: CommandHandler
	{
		protected override void Run ()
		{
			CommonAboutDialog ad = new CommonAboutDialog ();
			try {
				ad.Run ();
			} finally {
				ad.Destroy ();
			}
		}
	}
}
