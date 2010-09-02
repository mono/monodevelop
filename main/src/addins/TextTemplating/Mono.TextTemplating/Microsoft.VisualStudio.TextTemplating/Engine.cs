// 
// Engine.cs
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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using Mono.TextTemplating;

namespace Microsoft.VisualStudio.TextTemplating
{
	public class Engine : ITextTemplatingEngine
	{
		public Engine ()
		{
		}
		
		public string ProcessTemplate (string content, ITextTemplatingEngineHost host)
		{
			if (content == null)
				throw new ArgumentNullException ("content");
			if (host == null)
				throw new ArgumentNullException ("host");
			
			return GetEngine (host, content).ProcessTemplate (content, host);
		}
		
		ITextTemplatingEngine GetEngine (ITextTemplatingEngineHost host, string content)
		{
			var appdomain = host.ProvideTemplatingAppDomain (content);
			if (appdomain == null) {
				return new TemplatingEngine ();
			} else {
				var type = typeof (TemplatingEngine);
				return (ITextTemplatingEngine) appdomain.CreateInstanceAndUnwrap (type.Assembly.FullName, type.FullName);
			}
		}
		
		public string PreprocessTemplate (string content, ITextTemplatingEngineHost host, string className, 
			string classNamespace, out string language, out string[] references)
		{
			if (content == null)
				throw new ArgumentNullException ("content");
			if (host == null)
				throw new ArgumentNullException ("host");
			if (className == null)
				throw new ArgumentNullException ("className");
			if (classNamespace == null)
				throw new ArgumentNullException ("classNamespace");
			
			return GetEngine (host, content).PreprocessTemplate (content, host, className, classNamespace, out language, out references);
		}
		
		public const string CacheAssembliesOptionString = "CacheAssemblies";
	}
}
