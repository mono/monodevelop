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

namespace BooBinding.BooShell

import System
import System.Collections
import System.IO
import System.Threading
import Boo.Lang.Interpreter
import Boo.Lang.Compiler

import Gtk
import GLib

import MonoDevelop.Services

class BooShell (RemoteProcessObject):
	private _interpreter = InteractiveInterpreter(RememberLastValue: true, Print: print)

	private _commandQueue = Queue()
	private _outputQueue = Queue()

	private _thread as System.Threading.Thread

	private _processing as string = "true"

	override def InitializeLifetimeService ():
		return null

	def Reset() as bool:
		EnqueueCommand (ShellCommand (ShellCommandType.Reset, null))
		return true
	
	def LoadAssembly (assemblyPath as string) as bool:
		EnqueueCommand (ShellCommand (ShellCommandType.Load, assemblyPath))
		return true
	
	References as IList:
		get:
			list = []
			Monitor.Enter (_interpreter)
			for assembly as System.Reflection.Assembly in _interpreter.References:
				try:
					loc = assembly.Location
					list.Add (loc)
				except x:
					continue
			Monitor.Exit (_interpreter)
			return list
	
	def GetOutput() as (string):
		ret as (string)
		try:
			Monitor.Enter (_outputQueue)

			if _processing == "true":
				Monitor.Wait (_outputQueue)

			if _outputQueue.Count > 0:
				ret = array (string, _outputQueue.Count)
				_outputQueue.CopyTo (ret, 0)
				_outputQueue.Clear()
		ensure:
			Monitor.Pulse (_outputQueue)
			Monitor.Exit (_outputQueue)

		return ret
		
	def QueueInput (line as string):
		EnqueueCommand (ShellCommand (ShellCommandType.Eval, line))

	def ThreadRun():
		Application.Init()
		GLib.Idle.Add(ProcessCommands)
		Application.Run()

	private def ProcessCommands() as bool:
		com as ShellCommand
		try:
			Monitor.Enter (_commandQueue)
			if _commandQueue.Count == 0:
				Monitor.Exit (_commandQueue)
				System.Threading.Thread.Sleep (100)
				return  true

			com = _commandQueue.Dequeue()

			Monitor.Enter(_interpreter)
			if com.Type == ShellCommandType.Eval:
				if com.Data is not null:
					_interpreter.LoopEval(com.Data)
			elif com.Type == ShellCommandType.Reset:
				_interpreter.Reset()
			elif com.Type == ShellCommandType.Load:
				if com.Data is not null:
					_interpreter.load(com.Data)

			Monitor.Exit(_interpreter)

			com.Type = ShellCommandType.NoOp
	
			if _commandQueue.Count == 0:
				Monitor.Enter (_outputQueue)
				_processing = "false"
				Monitor.Pulse (_outputQueue)
				Monitor.Exit (_outputQueue)
		ensure:
			Monitor.Exit (_commandQueue)

		return true
	
	def Run():
		kickOffGuiThread()

	
	private def kickOffGuiThread():
		_start as ThreadStart = ThreadRun
		_thread = System.Threading.Thread (_start)
		_thread.Start ()
	
	private def print(obj):
		Monitor.Enter (_outputQueue)
		_outputQueue.Enqueue(obj)
		Monitor.Exit (_outputQueue)
	
	private def EnqueueCommand (command as ShellCommand):
		if not _thread.IsAlive:
			kickOffGuiThread()

		try:
			Monitor.Enter (_commandQueue)

			_commandQueue.Enqueue (command)

			Monitor.Enter (_outputQueue)
			_processing = "true"
			Monitor.Pulse (_outputQueue)
			Monitor.Exit (_outputQueue)
		ensure:
			Monitor.Pulse (_commandQueue)
			Monitor.Exit (_commandQueue)
	
	def Dispose ():
		_thread.Abort ()
		super ()

public enum ShellCommandType:
	NoOp
	Reset
	Load
	Eval

public struct ShellCommand:
	Type as ShellCommandType
	Data as string

	def constructor (type, data):
		self.Type = type
		self.Data = data
