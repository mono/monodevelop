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
import System.IO
import System.CodeDom.Compiler
import System.Text

import MonoDevelop.Core
import MonoDevelop.Core.ProgressMonitoring
import MonoDevelop.Projects

[extension] #FIXME: workaround BOO-1167
def GetAllReferences (this as ProjectItemCollection) as ProjectReference*:
	for item in this:
		yield item if item isa ProjectReference

[extension] #FIXME: workaround BOO-1167
def GetAllFiles (this as ProjectItemCollection) as ProjectFile*:
	for item in this:
		yield item if item isa ProjectFile

public class BooBindingCompilerServices:
	
	public def CanCompile (fileName as string):
		return Path.GetExtension (fileName).ToUpper () == ".BOO"
	
	def Compile (projectItems as ProjectItemCollection, configuration as DotNetProjectConfiguration, configurationSelector as ConfigurationSelector, monitor as IProgressMonitor) as BuildResult:
		compilerparameters = cast (BooCompilerParameters, configuration.CompilationParameters)
		if compilerparameters is null:
			compilerparameters = BooCompilerParameters ()
		#we get the compiler target to be used
		compilerTarget as string = "exe"
		
		if configuration.CompileTarget == CompileTarget.Exe:
			compilerTarget = "exe"
		elif configuration.CompileTarget == CompileTarget.Library:
			compilerTarget = "library"
		elif configuration.CompileTarget == CompileTarget.WinExe:
			compilerTarget = "winexe"
		
		parameters as StringBuilder = StringBuilder (
			"-o:${configuration.CompiledOutputName} -t:${compilerTarget}")
		
		#we decide if we want to use ducks
		if 	compilerparameters.Ducky:
			parameters.Append (" -ducky ")
			
		#we decide if we are going to define the debug var
		if configuration.DebugMode:
			parameters.Append (" -debug+ ")
		else:
			parameters.Append (" -debug- ")
			
		if configuration.EnvironmentVariables.Keys.Count > 0:
			parameters.Append (" -define: ")
			
		#we loop through the defines and add them to the list
		for currentDefine in configuration.EnvironmentVariables.Keys:
			defineValue = ""
			if configuration.EnvironmentVariables.TryGetValue (currentDefine, defineValue):
				if not defineValue == String.Empty:
					parameters.Append ("${currentDefine}=${defineValue},")
				else:
					parameters.Append ("${currentDefine},")
				
		#we need to remove the last comma added in the loop
		if configuration.EnvironmentVariables.Keys.Count > 0:
			parameters.Remove (parameters.ToString ().LastIndexOf (","),1)
		
		#we add the different references
		for lib as ProjectReference in projectItems.GetAllReferences ():
			for fileName as string in lib.GetReferencedFileNames (configurationSelector):
				parameters.Append (" -reference:${fileName} ")
				
		for finfo as ProjectFile in projectItems.GetAllFiles ():
			if finfo.Subtype != Subtype.Directory:
				if finfo.BuildAction == BuildAction.Compile:
					parameters.Append (" ${finfo.FilePath} ")
				elif finfo.BuildAction == BuildAction.EmbeddedResource:
					parameters.Append ("  -embedres:${finfo.FilePath},${finfo.ResourceId}")

		# if the assembly is signed we point to the file
		if configuration.SignAssembly:
			parameters.Append (" -keyfile: ${configuration.AssemblyKeyFile} ")
			
		# we check if the project is going to be using a strong signature and let the 
		# compiler know where to find it
		
		tf = TempFileCollection ()
		compilationOutput = DoCompilation (monitor, parameters.ToString (), configuration.OutputDirectory )
		return ParseOutput (tf, compilationOutput)
		

	private def DoCompilation (monitor as IProgressMonitor, parameters as string, outputDir as string):
		try:
			
			swError = StringWriter ()
			chainedError as LogTextWriter = LogTextWriter ()
			chainedError.ChainWriter (monitor.Log)
			chainedError.ChainWriter (swError);
			
			swLog =  StringWriter ()
			chainedLogs as LogTextWriter = LogTextWriter ()
			chainedLogs.ChainWriter (swLog);
			
			operationMonitor = AggregatedOperationMonitor (monitor)
			monitor.Log.WriteLine (GettextCatalog.GetString ("Starting Boo compilation"))
			monitor.Log.WriteLine ("booc ${parameters}")
			
			#we create a new process that will be used to execute the command line of the compiler
			wrapper =  MonoDevelop.Core.Runtime.ProcessService.StartProcess ("booc",parameters ,
				Path.GetDirectoryName (outputDir),chainedError , chainedLogs, null)
			
			#we take care of cancelation
			operationMonitor.AddOperation (wrapper);
			wrapper.WaitForOutput ();
			exitCode = wrapper.ExitCode
			
			if monitor.IsCancelRequested:
				monitor.Log.WriteLine (GettextCatalog.GetString ("Build cancelled"))
				monitor.ReportError (GettextCatalog.GetString ("Build cancelled"), null)
				if exitCode == 0:
					exitCode = -1
					
			error = swLog.ToString ()
			return error
		ensure:
			#we get rid of this guys
			wrapper.Dispose ()
			swError.Close ()
			chainedError.Close ()
			operationMonitor.Dispose ()
			monitor.EndTask ()
			swLog.Close ()
		
	def ParseOutput (tf as TempFileCollection , errors as string):
		cr = CompilerResults (tf)
		# we read the errors line by line to get them in the monodevelop list
		reader = StringReader (errors);
		nextError as string
		
		while (nextError = reader. ReadLine()) != null:
			error = ParseErrorLine(nextError)
			if not error is null:
				cr.Errors.Insert (0,error)
			
		reader.Close ();
		return BuildResult (cr, null)
	
	private def ParseErrorLine(errorLine as string) as System.CodeDom.Compiler.CompilerError:
		error = System.CodeDom.Compiler.CompilerError()
		#errors are of the form "file(row, column):ErrorNum:Type:message"
		data = @/(?<file>.*\.boo)\s*\((?<row>\d+),\s?(?<column>\d+)\):\s*(?<message>.*)/.Matches (errorLine)
		if data.Count > 0:
			error.ErrorText = data[0].Groups["message"].Value
			error.FileName = data[0].Groups["file"].Value
			error.Line = int.Parse (data[0].Groups["row"].Value)
			error.Column = int.Parse (data[0].Groups["column"].Value)
			if error.ErrorText.Contains ("WARNING"):
				error.IsWarning = true
			return error
		else:
			return null
	