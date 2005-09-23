#region license
// Copyright (c) 2004-2005, Daniel Grunwald (daniel@danielgrunwald.de)
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// All rights reserved.
//
// The BooBinding.Parser code is originally that of Daniel Grunwald
// (daniel@danielgrunwald.de) from the SharpDevelop BooBinding. The code has
// been imported here, and modified, including, but not limited to, changes
// to function with MonoDevelop, additions, refactorings, etc.
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

namespace BooBinding.Parser

import System
import System.Collections
import System.Diagnostics
import System.IO
import MonoDevelop.Services
import MonoDevelop.Internal.Project
import MonoDevelop.Internal.Parser
import Boo.Lang.Compiler
import Boo.Lang.Compiler.IO
import Boo.Lang.Compiler.Pipelines
import Boo.Lang.Compiler.Steps

class BooParser(IParser):
	private _lexerTags as (string)

	private cuCache = Hashtable()
	
	LexerTags as (string):
		get:
			return _lexerTags
		set:
			_lexerTags = value
	
	ExpressionFinder as IExpressionFinder:
		get:
			return BooBinding.Parser.ExpressionFinder()
	
	def CanParse(fileName as string):
		return Path.GetExtension(fileName).ToLower() == ".boo"
	
	def CanParse(project as Project):
		return project.ProjectType == BooBinding.BooLanguageBinding.LanguageName
	
	def Parse(fileName as string) as ICompilationUnitBase:
		content as string
		using r = StreamReader(fileName):
			content = r.ReadToEnd()
		return Parse(fileName, content)
	
	def Parse(fileName as string, fileContent as string) as ICompilationUnitBase:
		Log ("Parse ${fileName} with content")
		
		cr = char('\r')
		ln = char('\n')
		linecount = 1
		for c as Char in fileContent:
			linecount += 1 if c == ln
		lineLength = array(int, linecount)
		length = 0
		i = 0
		for c as Char in fileContent:
			if c == ln:
				lineLength[i] = length
				i += 1
				length = 0
			elif c != cr:
				length += 1
		lineLength[i] = length
		
		compiler = BooCompiler()
		compiler.Parameters.Input.Add(StringInput(fileName, fileContent))
		project as Project
		if MonoDevelop.Services.Runtime.ProjectService.CurrentOpenCombine is not null:
			for entry as Project in MonoDevelop.Services.Runtime.ProjectService.CurrentOpenCombine.GetAllProjects():
				if entry.IsFileInProject(fileName):
					project = entry
		
		if project is not null and project.ProjectReferences is not null:
			for projectRef as ProjectReference in project.ProjectReferences:
				compiler.Parameters.References.Add(System.Reflection.Assembly.LoadFile(projectRef.GetReferencedFileName()))
		
		return Parse(fileName, lineLength, compiler)
	
	private def Parse(fileName as string, lineLength as (int), compiler as BooCompiler):
		compiler.Parameters.OutputWriter = StringWriter()
		compiler.Parameters.TraceSwitch.Level = TraceLevel.Warning
		
		compilePipe = Compile()
		parsingStep as Boo.Lang.Parser.BooParsingStep = compilePipe[0]
		parsingStep.TabSize = 1
		//num = compilePipe.Find(typeof(ProcessMethodBodiesWithDuckTyping))
		// Include ProcessMethodBodies step now, as it solves issue
		// with [Propert(foo)] with an untyped 'foo'
		num = compilePipe.Find(typeof(StricterErrorChecking))
		visitor = Visitor(LineLength:lineLength)
		compilePipe[num] = visitor
		// Remove unneccessary compiler steps
		while compilePipe.Count > num + 1:
			compilePipe.RemoveAt(compilePipe.Count - 1)
		num = compilePipe.Find(typeof(TransformCallableDefinitions))
		compilePipe.RemoveAt(num)
		
		//for i in range(compilePipe.Count):
		//	print compilePipe[i].ToString()
		
		compilePipe.BreakOnErrors = false
		compiler.Parameters.Pipeline = compilePipe
		
		try:
			compiler.Run()
			// somehow the SD parser thread goes into an endless loop if this flag is not set
			visitor.Cu.ErrorsDuringCompile = true //context.Errors.Count > 0
		except e:
			Error (e.ToString ())

		for c as IClass in visitor.Cu.Classes:
			if c.Region is not null:
				c.Region.FileName = fileName

		// The following returns the "last known good" parse results
		// for a given file. Keeps our parse info from disappearing
		// when there is a parsing error in a file.
		if visitor.HadErrors:
			if cuCache[fileName] is null:
				return CompilationUnit()

			return cuCache[fileName] as ICompilationUnitBase
		
		cuCache[fileName] = visitor.Cu
		return visitor.Cu
	
	def CtrlSpace(parserContext as IParserContext, caretLine as int, caretColumn as int, fileName as string) as ArrayList:
		Log ("Ctrl-Space (${caretLine}/${caretColumn})")
		try:
			return Resolver(parserContext).CtrlSpace(caretLine, caretColumn, fileName)
		except e:
			//ShowException(e)
			return null
	
	def IsAsResolve (parserContext as IParserContext, expression as string , caretLineNumber as int , caretColumn as int , fileName as string , fileContent as string ) as ArrayList:
		return Resolver (parserContext).IsAsResolve (expression, caretLineNumber, caretColumn, fileName, fileContent)

	def Resolve(parserContext as IParserContext, expression as string, caretLineNumber as int, caretColumn as int, fileName as string, fileContent as string) as ResolveResult:
		Log ("Resolve ${expression} (${caretLineNumber}/${caretColumn})")
		try:
			return Resolver(parserContext).Resolve(expression, caretLineNumber, caretColumn, fileName, fileContent)
		except e:
			Error (e.ToString ())
			return null

	def MonodocResolver(parserContext as IParserContext, expression as string, caretLineNumber as int, caretColumn as int, fileName as string, fileContent as string) as string:
		Log ("MonodocResolver ${expression} (${caretLineNumber}/${caretColumn})")
		try:
			return Resolver(parserContext).MonodocResolver(expression, caretLineNumber, caretColumn, fileName, fileContent)
		except e:
			//ShowException(e)
			return null
	
	private def Log (message):
		Log (self.GetType(), message)

	private def Error (message):
		Error (self.GetType(), message)

	static def Log (type, message):
		MonoDevelop.Services.Runtime.LoggingService.Debug (type.ToString (), message)
	
	static def Error (type, message):
		MonoDevelop.Services.Runtime.LoggingService.Error (type.ToString (), message)
