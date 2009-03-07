// 
// TemplatingHost.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	
	
	public class TemplateGenerator : MarshalByRefObject, ITextTemplatingEngineHost
	{
		//re-usable
		Engine engine;
		
		//per-run variables
		string inputFile, outputFile;
		Encoding encoding;
		
		//host fields
		CompilerErrorCollection errors = new CompilerErrorCollection ();
		List<string> refs = new List<string> ();
		List<string> imports = new List<string> ();
		List<string> includePaths = new List<string> ();
		List<string> referencePaths = new List<string> ();
		List<string> directiveProcessors = new List<string> ();
		Dictionary<string, string> processorValues = new Dictionary<string, string> ();
		
		//host properties for consumers to access
		public CompilerErrorCollection Errors { get { return errors; } }
		public List<string> Refs { get { return refs; } }
		public List<string> Imports { get { return imports; } }
		public List<string> IncludePaths { get { return includePaths; } }
		public List<string> ReferencePaths { get { return referencePaths; } }
		public List<string> DirectiveProcessors { get { return directiveProcessors; } }
		public IDictionary<string, string> ProcessorValues { get { return processorValues; } }
		
		public TemplateGenerator ()
		{
			Refs.Add (typeof (TextTransformation).Assembly.Location);
		}
		
		public bool ProcessTemplate (string inputFile, string outputFile)
		{
			if (String.IsNullOrEmpty (inputFile))
				throw new ArgumentNullException ("inputFile");
			if (String.IsNullOrEmpty (outputFile))
				throw new ArgumentNullException ("outputFile");
			
			errors.Clear ();
			encoding = Encoding.UTF8;
			this.outputFile = outputFile;
			this.inputFile = inputFile;
			
			return ProcessCurrent ();
		}
		
		public bool ProcessCurrent ()
		{
			if (engine == null)
				engine = new Engine ();
			
			string content;
			try {
				content = File.ReadAllText (inputFile);
			} catch (IOException ex) {
				AddError ("Could not read input file '" + inputFile + "'");
				return false;
			}
			
			string output = engine.ProcessTemplate (content, this);
			
			try {
				if (!errors.HasErrors)
					File.WriteAllText (outputFile, output, encoding);
			} catch (IOException ex) {
				AddError ("Could not read input file '" + inputFile + "'");
			}
			
			return !errors.HasErrors;
		}
		
		CompilerError AddError (string error)
		{
			CompilerError err = new CompilerError ();
			err.ErrorText = error;
			Errors.Add (err);
			return err;
		}
		
		#region Virtual members
		
		public virtual object GetHostOption (string optionName)
		{
			return null;
		}
		
		public virtual AppDomain ProvideTemplatingAppDomain (string content)
		{
			return null;
		}
		
		#endregion
		
		#region Explicit ITextTemplatingEngineHost implementation
		
		bool ITextTemplatingEngineHost.LoadIncludeText (string requestFileName, out string content, out string location)
		{
			content = "";
			location = null;
			
			if (Path.IsPathRooted (requestFileName)) {
				location = requestFileName;
			} else {
				foreach (string path in includePaths) {
					string f = Path.Combine (path, requestFileName);
					if (File.Exists (f)) {
						location = f;
						break;
					}
				}
			}
			
			if (location == null)
				return false;
			
			try {
				content = System.IO.File.ReadAllText (location);
				return true;
			} catch (IOException ex) {
				AddError ("Could not read included file '" + location + "'");
			}
			return false;
		}
		
		void ITextTemplatingEngineHost.LogErrors (CompilerErrorCollection errors)
		{
			this.errors.AddRange (errors);
		}
		
		string ITextTemplatingEngineHost.ResolveAssemblyReference (string assemblyReference)
		{
			//FIXME: implement
			return assemblyReference;
		}
		
		Type ITextTemplatingEngineHost.ResolveDirectiveProcessor (string processorName)
		{
			throw new NotImplementedException();
		}
		
		string ITextTemplatingEngineHost.ResolveParameterValue (string directiveId, string processorName, string parameterName)
		{
			throw new NotImplementedException();
		}
		
		string ITextTemplatingEngineHost.ResolvePath (string path)
		{
			throw new NotImplementedException();
		}
		
		void ITextTemplatingEngineHost.SetFileExtension (string extension)
		{
			extension = extension.TrimStart ('.');
			if (Path.HasExtension (outputFile)) {
				outputFile = Path.ChangeExtension (outputFile, extension);
			} else {
				outputFile = outputFile + "." + extension;
			}
		}
		
		void ITextTemplatingEngineHost.SetOutputEncoding (System.Text.Encoding encoding, bool fromOutputDirective)
		{
			this.encoding = encoding;
		}
		
		IList<string> ITextTemplatingEngineHost.StandardAssemblyReferences {
			get { return refs; }
		}
		
		IList<string> ITextTemplatingEngineHost.StandardImports {
			get { return imports; }
		}
		
		string ITextTemplatingEngineHost.TemplateFile {
			get { return inputFile; }
		}
		
		#endregion
	}
}
