#region license
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// All rights reserved.
//
// BooBinding is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// BooBinding is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with BooBinding; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion


namespace BooBinding.Pads

import System

import MonoDevelop.Core.AddIns
import MonoDevelop.Services
import MonoDevelop.Core.Services
import MonoDevelop.Gui
import BooBinding.Gui


public class BooShellPadContent (AbstractPadContent):
	private static _scroller as Gtk.ScrolledWindow
	private static _shellView as ShellTextView

	override Control:
		get:
			if _scroller is null:
				CreateBooShell()
			return _scroller
	
	def constructor():
		super( "Boo Shell", "md-boo-binding-base" )
	
	def CreateBooShell():
		_scroller = Gtk.ScrolledWindow()
		_model = BooShellModel ()
		_shellView = ShellTextView (_model)
		_scroller.Add(_shellView)
		_scroller.ShowAll()

	override def RedrawContent():
		OnTitleChanged(null)
		OnIconChanged(null)

	override def Dispose():
		_shellView.Dispose()
		_scroller.Dispose()
		
	override DefaultPlacement:
		get:
			return "Bottom"
