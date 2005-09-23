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

namespace BooBinding.Gui

import System
import System.Collections
import BooBinding.Properties

interface IShellModel:
	def Reset() as bool:
		pass

	def LoadAssembly (assemblyPath as string) as bool:
		pass

	def RegisterOutputHandler (handler as callable):
		pass

	def Run():
		pass
	
	def GetOutput() as (string):
		pass
	
	def QueueInput (line as string):
		pass
	
	def Dispose():
		pass

	Properties as ShellProperties:
		get

	LanguageName as string:
		get

	MimeType as string:
		get
	
	MimeTypeExtension as string:
		get
	
	References as IList:
		get
