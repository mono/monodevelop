

namespace BooBinding.Properties

import System

import Gtk
import Gdk

import MonoDevelop.Components
import MonoDevelop.Core.Properties
import MonoDevelop.Core

class BooShellProperties (ShellProperties):
	override PropertyName as string:
		get:
			return "BooBinding.BooShell.ShellProps"
