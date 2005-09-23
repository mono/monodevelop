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

namespace BooBinding

import System
import System.Diagnostics
import System.IO
import System.CodeDom.Compiler
import System.Text
import System.Reflection

import MonoDevelop.Gui.Components
import MonoDevelop.Services
import MonoDevelop.Core.Services
import MonoDevelop.Internal.Project

import Boo.Lang.Compiler
import Boo.Lang.Compiler.Resources

public class BooBindingCompilerServices:
	public def CanCompile (fileName as string):
		return Path.GetExtension(fileName).ToUpper() == ".BOO"
	
	def Compile (projectFiles as ProjectFileCollection, references as ProjectReferenceCollection, configuration as DotNetProjectConfiguration, monitor as IProgressMonitor) as ICompilerResult:
		compilerparameters = cast(BooCompilerParameters, configuration.CompilationParameters)
		if compilerparameters is null:
			compilerparameters = BooCompilerParameters()
		
		// FIXME: Use outdir 'configuration.OutputDirectory'
		compiler = Boo.Lang.Compiler.BooCompiler()
		compiler.Parameters.Pipeline = Pipelines.CompileToFile()

		compiler.Parameters.Debug = configuration.DebugMode
		compiler.Parameters.OutputAssembly = configuration.CompiledOutputName
		compiler.Parameters.Ducky = compilerparameters.Ducky

		# Make sure we don't load the generated assembly at all
		compiler.Parameters.GenerateInMemory = false

		if references is not null:
			for lib as ProjectReference in references:
				fileName = lib.GetReferencedFileName()
				compiler.Parameters.References.Add(Assembly.LoadFile(fileName))

		for finfo as ProjectFile in projectFiles:
			if finfo.Subtype != Subtype.Directory:
				if finfo.BuildAction == BuildAction.Compile:
					compiler.Parameters.Input.Add(Boo.Lang.Compiler.IO.FileInput(finfo.Name))
				elif finfo.BuildAction == BuildAction.EmbedAsResource:
					compiler.Parameters.Resources.Add (EmbeddedFileResource (finfo.Name))

		
		if configuration.CompileTarget == CompileTarget.Exe:
			compiler.Parameters.OutputType = CompilerOutputType.ConsoleApplication
		elif configuration.CompileTarget == CompileTarget.Library:
			compiler.Parameters.OutputType = CompilerOutputType.Library
		elif configuration.CompileTarget == CompileTarget.WinExe:
			compiler.Parameters.OutputType = CompilerOutputType.WindowsApplication

		tf = TempFileCollection ()
		context = DoCompilation (monitor, compiler)
		cr = ParseOutput (tf, context)
		return cr

	private def DoCompilation (monitor as IProgressMonitor, compiler as Boo.Lang.Compiler.BooCompiler):
		try:
			monitor.BeginTask (null, 2)
			monitor.Log.WriteLine ("Compiling Boo source code ...")
			context = compiler.Run()
			monitor.Step (1)
			return context
		ensure:
			monitor.EndTask()
		
	def ParseOutput (tf as TempFileCollection , context as CompilerContext) as ICompilerResult:
		cr = CompilerResults (tf)
		
		for err as Boo.Lang.Compiler.CompilerError in context.Errors:
			cerror = System.CodeDom.Compiler.CompilerError ()
			cerror.ErrorText = err.Code + ": " + err.Message

			if err.LexicalInfo is not null:
				SetErrorLexicalInfo (cerror, err.LexicalInfo)

			cr.Errors.Add(cerror)

		for warning as CompilerWarning in context.Warnings:
			cerror = System.CodeDom.Compiler.CompilerError ()
			cerror.ErrorText = warning.Code + ": " + warning.Message

			if warning.LexicalInfo is not null:
				SetErrorLexicalInfo (cerror, warning.LexicalInfo)

			cerror.IsWarning = true
			cr.Errors.Add(cerror)

		return DefaultCompilerResult (cr, null)
	
	def SetErrorLexicalInfo (error as System.CodeDom.Compiler.CompilerError, lexicalInfo as Boo.Lang.Compiler.Ast.LexicalInfo):
		error.FileName = lexicalInfo.FileName 
		error.Column = lexicalInfo.Column
		error.Line = lexicalInfo.Line
