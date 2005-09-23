

namespace BooBinding.Properties

import System

import Gtk
import Gdk

import MonoDevelop.Gui.Widgets
import MonoDevelop.Core.Services
import MonoDevelop.Core.Properties
import MonoDevelop.Services

class BooShellProperties (ShellProperties):
	override PropertyName as string:
		get:
			return "BooBinding.BooShell.ShellProps"
