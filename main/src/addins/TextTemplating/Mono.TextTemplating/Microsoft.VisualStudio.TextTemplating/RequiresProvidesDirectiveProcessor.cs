// 
// RequiresProvidesDirectiveProcessor.cs
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
using System.Text;

namespace Microsoft.VisualStudio.TextTemplating
{
	
	
	public abstract class RequiresProvidesDirectiveProcessor : DirectiveProcessor
	{
		bool isInProcessingRun;
		ITextTemplatingEngineHost host;
		
		protected RequiresProvidesDirectiveProcessor ()
		{
		}
		
		public override void Initialize (ITextTemplatingEngineHost host)
		{
			base.Initialize (host);
			this.host = host;
			throw new NotImplementedException ();
		}
		
		protected abstract void InitializeProvidesDictionary (string directiveName, IDictionary<string, string> providesDictionary);
		protected abstract void InitializeRequiresDictionary (string directiveName, IDictionary<string, string> requiresDictionary);
		protected abstract string FriendlyName { get; }
		
		protected abstract void GenerateTransformCode (string directiveName, StringBuilder codeBuffer,
		                                               CodeDomProvider languageProvider, 
		                                               IDictionary<string, string> requiresArguments,
		                                               IDictionary<string, string> providesArguments);
		
		public override string GetClassCodeForProcessingRun ()
		{
			throw new System.NotImplementedException ();
		}
		
		public override string[] GetImportsForProcessingRun ()
		{
			throw new System.NotImplementedException ();
		}
		
		public override string GetPostInitializationCodeForProcessingRun ()
		{
			throw new System.NotImplementedException ();
		}
		
		public override string GetPreInitializationCodeForProcessingRun ()
		{
			throw new System.NotImplementedException ();
		}
		public override string[] GetReferencesForProcessingRun ()
		{
			throw new System.NotImplementedException ();
		}
		
		protected virtual void PostProcessArguments (string directiveName, IDictionary<string, string> requiresArguments,
		                                             IDictionary<string, string> providesArguments)
		{
		}
		
		public override void StartProcessingRun (System.CodeDom.Compiler.CodeDomProvider languageProvider, 
		                                         string templateContents,
		                                         System.CodeDom.Compiler.CompilerErrorCollection errors)
		{
			AssertNotProcessing ();
			isInProcessingRun = true;
			base.StartProcessingRun (languageProvider, templateContents, errors);
			
			throw new NotImplementedException ();
		}
		
		public override void FinishProcessingRun ()
		{
			isInProcessingRun = false;
			throw new NotImplementedException (); //reset the state machine
		}
		
		void AssertNotProcessing ()
		{
			if (isInProcessingRun)
				throw new InvalidOperationException ();
		}
		
		public override void ProcessDirective (string directiveName, IDictionary<string, string> arguments)
		{
			if (directiveName == null)
				throw new ArgumentNullException ("directiveName");
			if (arguments == null)
				throw new ArgumentNullException ("arguments");
			
			throw new NotImplementedException ();
		}
		
		protected virtual string ProvideUniqueId (string directiveName, IDictionary<string, string> arguments,
		                                          IDictionary<string, string> requiresArguments,
		                                          IDictionary<string, string> providesArguments)
		{
			throw new NotImplementedException ();
		}
		
		protected ITextTemplatingEngineHost Host {
			get { return host; }
		}
	}
}
