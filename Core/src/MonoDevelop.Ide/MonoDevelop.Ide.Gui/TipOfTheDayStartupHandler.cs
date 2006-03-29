/*
Copyright (C) 2006  Jacob Ilsø Christensen

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// The responsibility of this CommandHandler is
	/// to show the Tip of the Day window at startup
	/// </summary>
	class TipOfTheDayStartupHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (Runtime.Properties.GetProperty("MonoDevelop.Core.Gui.Dialog.TipOfTheDayView.ShowTipsAtStartup", false))
			{
				new TipOfTheDayWindow().Show();
			}
		}		
	}
}
