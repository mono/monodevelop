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
import System.IO
import System.Runtime.InteropServices

import Gtk
import Gdk
import GLib
import Pango
import GtkSourceView

import MonoDevelop.Gui.Widgets
import MonoDevelop.Gui.Completion
import MonoDevelop.Core.Services
import MonoDevelop.Services
import MonoDevelop.Core.Properties
import MonoDevelop.Internal.Project

/*
 * TODO
 * 
 * 1) Don't record lines with errors in the _scriptLines buffer
 */

class ShellTextView (SourceView, ICompletionWidget):
	private static _promptRegular = ">>> "
	private static _promptMultiline = "... "
	
	[Getter(Model)]
	model as IShellModel

	private _scriptLines = ""
	
	private _commandHistoryPast as Stack = Stack()
	private _commandHistoryFuture as Stack = Stack()
	
	private _inBlock as bool = false
	private _blockText = ""

	private _reset_clears_history as bool
	private _reset_clears_scrollback as bool
	private _auto_indent as bool
	private _load_assembly_after_build as bool

	private _projService as ProjectService
	private _proj as Project

	private _assembliesLoaded as bool

	private _fakeProject as DotNetProject
	private _parserService as IParserDatabase
	private _fakeFileName as string
	private _fileInfo as FileStream
	private _parserContext as IParserContext;
	
	def constructor(model as IShellModel):
		_projService = ServiceManager.GetService(typeof(ProjectService))
		_parserService = _projService.ParserDatabase

		manager = SourceLanguagesManager()
		buf = SourceBuffer(manager.GetLanguageFromMimeType(model.MimeType))

		// This freaks out booc for some reason.
		//super(buf, Highlight: true)
		super(buf)
		buf.Highlight = true

		self.model = model
		self.WrapMode = Gtk.WrapMode.Word
		self.ModifyFont(Model.Properties.Font)

		# FIXME: Put the project file somewhere other than /tmp
		monodevelopConfigDir = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".config/MonoDevelop/")
		shellProjectFile = System.IO.Path.Combine (monodevelopConfigDir, "${Model.LanguageName}-shell-project.mdp")

		// 'touch' the file so the MD parsing foo sees it as existing.
		_fakeFileName = System.IO.Path.Combine (monodevelopConfigDir, "shell-dummy-file.${Model.MimeTypeExtension}")
		if not System.IO.File.Exists (_fakeFileName):
			_fileInfo  = System.IO.File.Create (_fakeFileName)
			_fileInfo.Close ()
		_fakeProject = DotNetProject(Model.LanguageName, Name: "___ShellProject", FileName: shellProjectFile)

		_parserService.Load(_fakeProject)
		_parserContext = _parserService.GetProjectParserContext (_fakeProject)

		Model.Properties.InternalProperties.PropertyChanged += OnPropertyChanged
		Model.RegisterOutputHandler (HandleOutput)

		_auto_indent = Model.Properties.AutoIndentBlocks
		_reset_clears_scrollback = Model.Properties.ResetClearsScrollback
		_reset_clears_history = Model.Properties.ResetClearsHistory
		_load_assembly_after_build = Model.Properties.LoadAssemblyAfterBuild


		// The 'Freezer' tag is used to keep everything except
		// the input line from being editable
		tag = TextTag ("Freezer")
		tag.Editable = false
		Buffer.TagTable.Add (tag)
		prompt (false)

		_projService.EndBuild += ProjectCompiled
		_projService.CurrentProjectChanged += ProjectChanged

		// Run our model. Needs to happen for models which may spawn threads,
		// processes, etc
		Model.Run()
	
	def ProjectChanged (sender, e as ProjectEventArgs):
		_proj = e.Project

	def ProjectCompiled (compiled as bool):
		if _load_assembly_after_build and compiled:
			Model.Reset()
			resetGui()
			loadProjectAssemblies ()

	def loadProjectAssemblies():
		for assembly in getProjectAssemblies ():
			if (System.IO.File.Exists(assembly)):
				Model.Reset()
				Model.LoadAssembly (assembly)
		_assembliesLoaded = true
					

	def getProjectAssemblies():
		_assemblies = []
		if (_proj is not null):
			assembly = _proj.GetOutputFileName()
			if assembly is not null:
				_assemblies.Add(assembly)
		else:
			_combine = _projService.CurrentOpenCombine
			if _combine is null:
				return _assemblies

			projects = _combine.GetAllProjects()
			if projects is null:
				return _assemblies
			for entry as Project in projects:
				if entry is null:
					continue
				assembly = entry.GetOutputFileName()
				if assembly is not null:
					_assemblies.Add(assembly)

		return _assemblies

	def HandleOutput():
		GLib.Idle.Add (outputIdleProcessor)
	
	def outputIdleProcessor() as bool:
		output = Model.GetOutput()
		if output is not null:
			for line as string in output:
				processOutput (line )
		prompt (true)
		for assembly in Model.References:
			_fakeProject.AddReference(assembly)

		GLib.Idle.Add( { _parserContext.ParseFile (_fakeFileName, _scriptLines) } )
		return false
			
	override def Dispose():
		Model.Dispose()

	#region Overrides of the standard methods for event handling
	override def OnPopulatePopup (menu as Gtk.Menu):
		_copyScriptInput = ImageMenuItem (GettextCatalog.GetString ("Copy Script"))
		_copyScriptInput.Activated += { Gtk.Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", true)).SetText(_scriptLines) }
		_copyScriptInput.Image = Gtk.Image (Stock.Copy, Gtk.IconSize.Menu)
		
		_saveScriptToFile = ImageMenuItem (GettextCatalog.GetString ("Save Script As ..."))
		_saveScriptToFile.Image = Gtk.Image (Stock.SaveAs, Gtk.IconSize.Menu)
		_saveScriptToFile.Activated += OnSaveScript
		
		_loadAssemblies = ImageMenuItem (GettextCatalog.GetString ("Load Project Assemblies (forces shell reset)"))
		_loadAssemblies.Image = Gtk.Image (Stock.Add, Gtk.IconSize.Menu)
		_loadAssemblies.Activated += def():
			if Model.Reset ():
				resetGui ()
				loadProjectAssemblies ()
		
		_reset = ImageMenuItem (GettextCatalog.GetString ("Reset Shell"))
		_reset.Image = Gtk.Image (Stock.Clear, Gtk.IconSize.Menu)
		_reset.Activated += def():
			if Model.Reset():
				resetGui()
				_assembliesLoaded = false

		if _scriptLines.Length <= 0:
			_copyScriptInput.Sensitive = false
			_saveScriptToFile.Sensitive = false
			_reset.Sensitive = false

		if (_assembliesLoaded == false) and (len (getProjectAssemblies ()) > 0):
			_loadAssemblies.Sensitive = true
		else:
			_loadAssemblies.Sensitive = false

		_sep = Gtk.SeparatorMenuItem()
		menu.Prepend(_sep)
		menu.Prepend(_copyScriptInput)
		menu.Prepend(_saveScriptToFile)
		menu.Prepend(_loadAssemblies)
		menu.Prepend(_reset)
		
		_sep.Show()
		_copyScriptInput.Show()
		_saveScriptToFile.Show()
		_loadAssemblies.Show()
		_reset.Show()
	
	override def OnKeyPressEvent (ev as Gdk.EventKey):
		if CompletionListWindow.ProcessKeyEvent (ev):
			return true
		
		// Short circuit to avoid getting moved back to the input line
		// when paging up and down in the shell output
		if ev.Key in Gdk.Key.Page_Up, Gdk.Key.Page_Down:
			return super (ev)
		
		// Needed so people can copy and paste, but always end up
		// typing in the prompt.
		if Cursor.Compare (InputLineBegin) < 0:
			Buffer.MoveMark (Buffer.SelectionBound, InputLineEnd)
			Buffer.MoveMark (Buffer.InsertMark, InputLineEnd)
		
		if (ev.State == Gdk.ModifierType.ControlMask) and ev.Key == Gdk.Key.space:
			TriggerCodeCompletion ()

		if ev.Key == Gdk.Key.Return:
			if _inBlock:
				if InputLine == "":
					processInput (_blockText)
					_blockText = ""
					_inBlock = false
				else:
					_blockText += "\n${InputLine}"
					if _auto_indent:
						_whiteSpace = /^(\s+).*/.Replace(InputLine, "$1")
						if InputLine.Trim()[-1:] == ":":
							_whiteSpace += "\t"
					prompt (true, true)
					if _auto_indent:
						InputLine += "${_whiteSpace}"
			else:
				// Special case for start of new code block
				if InputLine.Trim()[-1:] == ":":
					_inBlock = true
					_blockText = InputLine
					prompt (true, true)
					if _auto_indent:
						InputLine += "\t"
					return true

				// Bookkeeping
				if InputLine != "":
					// Everything but the last item (which was input),
					//in the future stack needs to get put back into the
					// past stack
					while _commandHistoryFuture.Count > 1:
						_commandHistoryPast.Push(cast(string,_commandHistoryFuture.Pop()))
					// Clear the pesky junk input line
					_commandHistoryFuture.Clear()

					// Record our input line
					_commandHistoryPast.Push(InputLine)
					if _scriptLines == "":
						_scriptLines += "${InputLine}"
					else:
						_scriptLines += "\n${InputLine}"
				
					processInput (InputLine)
			return true

		// The next two cases handle command history	
		elif ev.Key == Gdk.Key.Up:
			if (not _inBlock) and _commandHistoryPast.Count > 0:
				if _commandHistoryFuture.Count == 0:
					_commandHistoryFuture.Push(InputLine)
				else:
					if _commandHistoryPast.Count == 1:
						return true
					_commandHistoryFuture.Push(cast(string,_commandHistoryPast.Pop()))
				InputLine = cast (string, _commandHistoryPast.Peek())
			return true
			
		elif ev.Key == Gdk.Key.Down:
			if (not _inBlock) and _commandHistoryFuture.Count > 0:
				if _commandHistoryFuture.Count == 1:
					InputLine = cast(string, _commandHistoryFuture.Pop())
				else:
					_commandHistoryPast.Push (cast(string,_commandHistoryFuture.Pop()))
					InputLine = cast (string, _commandHistoryPast.Peek())
			return true
			
		elif ev.Key == Gdk.Key.Left:
			// Keep our cursor inside the prompt area
			if Cursor.Compare (InputLineBegin) <= 0:
				return true

		elif ev.Key == Gdk.Key.Home:
			Buffer.MoveMark (Buffer.InsertMark, InputLineBegin)
			// Move the selection mark too, if shift isn't held
			if (ev.State & Gdk.ModifierType.ShiftMask) == ev.State:
				Buffer.MoveMark (Buffer.SelectionBound, InputLineBegin)
			return true

		elif ev.Key == Gdk.Key.period:
			ret = super.OnKeyPressEvent(ev)
			prepareCompletionDetails (Buffer.GetIterAtMark (Buffer.InsertMark))
			CompletionListWindow.ShowWindow(char('.'), CodeCompletionDataProvider (_parserContext, _fakeFileName, true), self)
			return ret

		// Short circuit to avoid getting moved back to the input line
		// when paging up and down in the shell output
		elif ev.Key in Gdk.Key.Page_Up, Gdk.Key.Page_Down:
			return super (ev)
		
		return super (ev)
	
	protected override def OnFocusOutEvent (e as EventFocus):
		CompletionListWindow.HideWindow ()
		return super.OnFocusOutEvent(e)
	
	#endregion

	private def TriggerCodeCompletion():
		iter = Cursor
		triggerChar = char('\0')
		triggerIter = TextIter.Zero
		if (iter.Char != null and  iter.Char.Length > 0):
			if iter.Char[0] in (char(' '), char('\t'), char('.'), char('('), char('[')):
				triggerIter = iter
				triggerChar = iter.Char[0]

		while iter.LineOffset > 0 and triggerIter.Equals (TextIter.Zero):
			if (iter.Char == null or iter.Char.Length == 0):
				iter.BackwardChar ()
				continue

			if iter.Char[0] in (char(' '), char('\t'), char('.'), char('('), char('[')):
				triggerIter = iter
				triggerChar = iter.Char[0]
				break

			iter.BackwardChar ()
		
		if (triggerIter.Equals (TextIter.Zero)):
			return

		triggerIter.ForwardChar ()
		
		prepareCompletionDetails (triggerIter)
		CompletionListWindow.ShowWindow (triggerChar, CodeCompletionDataProvider (_parserContext, _fakeFileName, true), self)

	// Mark to find the beginning of our next input line
	private _endOfLastProcessing as TextMark

	#region Public getters for useful values
	public InputLineBegin as TextIter:
		get:
			endIter = Buffer.GetIterAtMark (_endOfLastProcessing)
			return endIter
	
	public InputLineEnd as TextIter:
		get:
			return Buffer.EndIter
	
	private Cursor as TextIter:
		get:
			return Buffer.GetIterAtMark (Buffer.InsertMark)
	#endregion
	
	// The current input line
	public InputLine as string:
		get:
			return Buffer.GetText (InputLineBegin, InputLineEnd, false)
		set:
			start = InputLineBegin
			end = InputLineEnd
			Buffer.Delete (start, end)
			start = InputLineBegin
			Buffer.Insert (start, value)
	
	#region local private methods
	private def processInput (line as string):
		Model.QueueInput (line)
	
	private def processOutput (line as string):
		end = Buffer.EndIter
		Buffer.Insert (end , "\n${line}")

	private def prompt (newLine as bool):
		prompt (newLine, false)

	private def prompt (newLine as bool, multiline as bool):
		end = Buffer.EndIter
		if newLine:
			Buffer.Insert (end , "\n")
		if multiline:
			Buffer.Insert (end , "${_promptMultiline}")
		else:
			Buffer.Insert (end , "${_promptRegular}")

		Buffer.PlaceCursor (Buffer.EndIter)
		ScrollMarkOnscreen(Buffer.InsertMark)
		

		// Record the end of where we processed, used to calculate start
		// of next input line
		_endOfLastProcessing = Buffer.CreateMark (null, Buffer.EndIter, true)

		// Freeze all the text except our input line
		Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin)
		
	private def resetGui():
		if _reset_clears_scrollback:
			Buffer.Text = ""
		if _reset_clears_history:
			_commandHistoryFuture.Clear()
			_commandHistoryPast.Clear()

		_scriptLines = ""
		prompt(not _reset_clears_scrollback)
		
	// FIXME: Make my FileChooser use suck less
	private def OnSaveScript():
		_sel = FileSelector("Save Script ...", FileChooserAction.Save)
		_sel.Run()
		if _sel.Filename:
			_sel.Hide()
			_path = _sel.Filename
			using writer = StreamWriter (_path):
				writer.Write (_scriptLines)
		else:
			_sel.Hide()
	
	def OnPropertyChanged (obj as object, e as PropertyEventArgs):
		if e.Key == "Font":
			self.ModifyFont(Model.Properties.Font)
		elif e.Key == "AutoIndentBlocks":
			_auto_indent = Model.Properties.AutoIndentBlocks
		elif e.Key == "ResetClearsScrollback":
			_reset_clears_scrollback = Model.Properties.ResetClearsScrollback
		elif e.Key == "ResetClearsHistory":
			_reset_clears_history = Model.Properties.ResetClearsHistory
		elif e.Key == "LoadAssemblyAfterBuild":
			_load_assembly_after_build = Model.Properties.LoadAssemblyAfterBuild

		return

	#endregion

	private def prepareCompletionDetails (triggerIter as TextIter):
		rect = GetIterLocation (Buffer.GetIterAtMark (Buffer.InsertMark))

		wx as int
		wy as int
		BufferToWindowCoords (Gtk.TextWindowType.Widget, rect.X, rect.Y + rect.Height, wx, wy)

		tx as int
		ty as int
		GdkWindow.GetOrigin (tx, ty)

		self.completionX = tx + wx
		self.completionY = ty + wy
		self.textHeight = rect.Height
		self.triggerMark = Buffer.CreateMark (null, triggerIter, true)

	#region ICompletionWidget

	[Getter(ICompletionWidget.TriggerXCoord)]
	private completionX

	[Getter(ICompletionWidget.TriggerYCoord)]
	private completionY
	
	[Getter(ICompletionWidget.TriggerTextHeight)]
	private textHeight as int
	
	ICompletionWidget.Text:
		get:
			return Buffer.Text
	
	ICompletionWidget.TextLength:
		get:
			return Buffer.EndIter.Offset
	
	def ICompletionWidget.GetChar (offset as int) as System.Char:
		return Buffer.GetIterAtLine (offset).Char[0]
	
	def ICompletionWidget.GetText (startOffset as int, endOffset as int) as string:
		return Buffer.GetText(Buffer.GetIterAtOffset (startOffset), Buffer.GetIterAtOffset(endOffset), true)
	
	ICompletionWidget.CompletionText:
		get:
			return Buffer.GetText (Buffer.GetIterAtMark (triggerMark), Buffer.GetIterAtMark (Buffer.InsertMark), false)
	
	def ICompletionWidget.SetCompletionText (partial_word as string, complete_word as string):
		offsetIter = Buffer.GetIterAtMark(triggerMark)
		endIter = Buffer.GetIterAtOffset (offsetIter.Offset + partial_word.Length)
		Buffer.MoveMark (Buffer.InsertMark, offsetIter)
		Buffer.Delete (offsetIter, endIter)
		Buffer.InsertAtCursor (complete_word)
	
	def ICompletionWidget.InsertAtCursor (text as string):
		Buffer.InsertAtCursor (text)
	
	private triggerMark as TextMark
	ICompletionWidget.TriggerOffset:
		get:
			return Buffer.GetIterAtMark (triggerMark).Offset

	ICompletionWidget.TriggerLine:
		get:
			return Buffer.GetIterAtMark (triggerMark).Line

	ICompletionWidget.TriggerLineOffset:
		get:
			return Buffer.GetIterAtMark (triggerMark).LineOffset
	
	ICompletionWidget.GtkStyle:
		get:
			return self.Style.Copy();
	#endregion
