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
import System.Diagnostics
import System.Collections
import System.IO
import System.Threading
import System.Runtime.Remoting
import System.Runtime.Remoting.Channels

import BooBinding.Properties
import BooBinding.BooShell

import MonoDevelop.Services
import MonoDevelop.Core.Services

class BooShellModel(IShellModel):
	private _props = BooShellProperties()

	private _commandQueue = Queue()
	private _outputQueue = Queue()

	private _outputHandler as callable
	
	private _thread as System.Threading.Thread

	private _booShell as BooShell

	MimeType as string:
		get:
			return "text/x-boo"

	LanguageName as string:
		get:
			return "Boo"

	MimeTypeExtension as string:
		get:
			return "boo"
	
	Properties as ShellProperties:
		get:
			return _props
	
	References as IList:
		get:	
			return _booShell.References
	
	def constructor():
		getRemoteShellObject()
		_booShell.Run ()

	private def getRemoteShellObject ():
		_procService as ProcessService = ServiceManager.GetService (typeof (ProcessService))
		_booShell = _procService.CreateExternalProcessObject ("../AddIns/BackendBindings/BooShell.dll", "BooBinding.BooShell.BooShell", false)
		if _booShell is null:
			raise Exception ("Unable to instantiate remote BooShell object")
	
	def Reset () as bool:
		_booShell.Reset()
		return true
	
	def LoadAssembly (assemblyPath as string) as bool:
		_booShell.LoadAssembly (assemblyPath)
		return true
	
	def GetOutput () as (string):
		ret as (string)
		lock _outputQueue:
			if _outputQueue.Count > 0:
				ret = array (string, _outputQueue.Count)
				_outputQueue.CopyTo (ret, 0)
				_outputQueue.Clear ()

		return ret

		
	def QueueInput (line as string):
		try:
			Monitor.Enter (_commandQueue)
			_commandQueue.Enqueue (line)
			Monitor.Pulse (_commandQueue)
		ensure:
			Monitor.Exit (_commandQueue)

	def ThreadRun ():
		while true:
			com as string
			try:
				Monitor.Enter (_commandQueue)
				if _commandQueue.Count == 0:
					Monitor.Wait (_commandQueue)

				com = _commandQueue.Dequeue ()


				if com is not null:
					_booShell.QueueInput (com)
					lines = _booShell.GetOutput ()
					if lines is not null:
						EnqueueOutput (lines)
					com = null
					lock _outputQueue:
						if _outputHandler is not null:
							_outputHandler ()

			ensure:
				Monitor.Exit (_commandQueue)

	
	def Run ():
		_thread = System.Threading.Thread (ThreadRun)
		_thread.Start ()
	
	def RegisterOutputHandler (handler as callable):
		_outputHandler = handler
	
	def EnqueueOutput (lines as (string)):
		lock _outputQueue:
			for line in lines:
				_outputQueue.Enqueue (line)
	
	def Dispose ():
		_thread.Abort ()
		_booShell.Dispose ()
		
	def print (obj):
		lock _outputQueue:
			_outputQueue.Enqueue (obj)
