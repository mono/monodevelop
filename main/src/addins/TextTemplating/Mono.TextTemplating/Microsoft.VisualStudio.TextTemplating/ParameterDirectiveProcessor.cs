// 
// ParameterDirectiveProcessor.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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

namespace Microsoft.VisualStudio.TextTemplating
{
	public sealed class ParameterDirectiveProcessor : DirectiveProcessor, IRecognizeHostSpecific
	{
		CodeDomProvider languageProvider;
		
		public ParameterDirectiveProcessor ()
		{
		}
		
		public override void StartProcessingRun (CodeDomProvider languageProvider, string templateContents, CompilerErrorCollection errors)
		{
			base.StartProcessingRun (languageProvider, templateContents, errors);
			this.languageProvider = languageProvider;
		}
		
		public override void FinishProcessingRun ()
		{
			languageProvider = null;
		}
		
		public override string GetClassCodeForProcessingRun ()
		{
			throw new NotImplementedException ();
		}
		
		public override string[] GetImportsForProcessingRun ()
		{
			throw new NotImplementedException ();
		}
		
		public override string GetPostInitializationCodeForProcessingRun ()
		{
			throw new NotImplementedException ();
		}
		
		public override string GetPreInitializationCodeForProcessingRun ()
		{
			throw new NotImplementedException ();
		}
		
		public override string[] GetReferencesForProcessingRun ()
		{
			throw new NotImplementedException ();
		}
		
		public override bool IsDirectiveSupported (string directiveName)
		{
			throw new NotImplementedException ();
		}
		
		public override void ProcessDirective (string directiveName, IDictionary<string, string> arguments)
		{
			throw new NotImplementedException ();
		}
		
		void IRecognizeHostSpecific.SetProcessingRunIsHostSpecific (bool hostSpecific)
		{
		}

		public bool RequiresProcessingRunIsHostSpecific {
			get { return false; }
		}
	}
}

