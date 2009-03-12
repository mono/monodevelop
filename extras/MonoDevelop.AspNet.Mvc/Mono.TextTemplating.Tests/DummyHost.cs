// 
// IncludeFileProviderHost.cs
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
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating.Tests
{
	
	public class DummyHost : ITextTemplatingEngineHost
	{
		public virtual object GetHostOption (string optionName)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual void LogErrors (System.CodeDom.Compiler.CompilerErrorCollection errors)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual AppDomain ProvideTemplatingAppDomain (string content)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual string ResolveAssemblyReference (string assemblyReference)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual Type ResolveDirectiveProcessor (string processorName)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual string ResolveParameterValue (string directiveId, string processorName, string parameterName)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual string ResolvePath (string path)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual void SetFileExtension (string extension)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual void SetOutputEncoding (System.Text.Encoding encoding, bool fromOutputDirective)
		{
			throw new System.NotImplementedException();
		}
		
		public virtual System.Collections.Generic.IList<string> StandardAssemblyReferences {
			get {
				throw new System.NotImplementedException();
			}
		}
		
		public virtual System.Collections.Generic.IList<string> StandardImports {
			get {
				throw new System.NotImplementedException();
			}
		}
		
		public virtual string TemplateFile {
			get {
				throw new System.NotImplementedException();
			}
		}
	}
	
	public class IncludeFileHost : DummyHost
	{
		public readonly Dictionary<string, string> Locations = new Dictionary<string, string> ();
		public readonly Dictionary<string, string> Contents = new Dictionary<string, string> ();
		
		public override bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			return Locations.TryGetValue (requestFileName, out location)
				&& Contents.TryGetValue (requestFileName, out content);
		}
	}
}
