//  HelpCommands.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Diagnostics;
using System.IO;
using System.Collections;
using Gtk;

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
		Help,
		TipOfTheDay,
		About
	}
	
	internal class HelpHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.HelpOperations.ShowHelp ("root:");
		}
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
