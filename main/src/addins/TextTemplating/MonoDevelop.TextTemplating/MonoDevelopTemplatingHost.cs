// 
// TextTemplatingFileGenerator.cs
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
using Mono.TextTemplating;
using MonoDevelop.Core;

namespace MonoDevelop.TextTemplating
{
	class MonoDevelopTemplatingHost : TemplateGenerator, IDisposable
	{
		TemplatingAppDomainRecycler.Handle domainHandle;
		
		public override AppDomain ProvideTemplatingAppDomain (string content)
		{
			if (domainHandle == null) {
				domainHandle = TextTemplatingService.GetTemplatingDomain ();
			}
			return domainHandle.Domain;
		}
		
		protected override string ResolveAssemblyReference (string assemblyReference)
		{
			//FIXME: resolve from addins
			var substituted = StringParserService.Parse (assemblyReference);
			return base.ResolveAssemblyReference (substituted);
		}
		
		protected override bool LoadIncludeText (string requestFileName, out string content, out string location)
		{
			//FIXME: resolve from addins
			var substituted = StringParserService.Parse (requestFileName);
			return base.LoadIncludeText (substituted, out content, out location);
		}
		
		protected override Type ResolveDirectiveProcessor (string processorName)
		{
			//FIXME: resolve from addins
			return base.ResolveDirectiveProcessor (processorName);
		}
		
		public void Dispose ()
		{
			if (domainHandle != null) {
				domainHandle.Dispose ();
				domainHandle = null;
			}
		}
	}
}
