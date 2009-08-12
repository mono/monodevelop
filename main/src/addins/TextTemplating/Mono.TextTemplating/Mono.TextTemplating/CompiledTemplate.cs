// 
// CompiledTemplate.cs
//  
// Author:
//       Nathan Baulch <nathan.baulch@gmail.com>
// 
// Copyright (c) 2009 Nathan Baulch
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
using System.Reflection;
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
{
	public class CompiledTemplate : MarshalByRefObject
	{
		readonly ITextTemplatingEngineHost host;
		readonly Assembly assembly;
		readonly TemplateSettings settings;
		readonly ParsedTemplate parsedTemplate;

		public CompiledTemplate (ParsedTemplate parsedTemplate, ITextTemplatingEngineHost host, Assembly assembly, TemplateSettings settings)
		{
			this.host = host;
			this.assembly = assembly;
			this.settings = settings;
			this.parsedTemplate = parsedTemplate;
		}

		public string Process ()
		{
			string output = "";

			try {
				output = TemplatingEngine.Run (assembly, settings.Namespace + "." + settings.Name, host, settings.Culture);
			} catch (Exception ex) {
				parsedTemplate.LogError ("Error running transform: " + ex);
			}

			host.LogErrors (parsedTemplate.Errors);
			return output;
		}
	}
}
