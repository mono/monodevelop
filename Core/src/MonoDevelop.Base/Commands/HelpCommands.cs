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

using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;

namespace MonoDevelop.Commands
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
			using (CommonAboutDialog ad = new CommonAboutDialog ()) {
				ad.Run ();
				ad.Hide ();
			}
		}
	}
}
