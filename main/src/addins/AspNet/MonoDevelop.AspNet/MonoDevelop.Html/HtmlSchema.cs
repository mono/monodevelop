// 
// HtmlSchema.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.Collections.Generic;
using MonoDevelop.XmlEditor.Completion;

namespace MonoDevelop.Html
{
	
	public class HtmlSchema
	{
		string docType;
		string name;
		string substituteProvider;
		IXmlCompletionProvider provider;
		
		public HtmlSchema (string name, string docType, IXmlCompletionProvider provider)
		{
			this.docType = docType;
			this.name = name;
			this.provider = provider;
		}
		
		public HtmlSchema (string name, string docType, string substituteProvider)
			: this (name, docType, (IXmlCompletionProvider) null)
		{
			this.substituteProvider = string.IsNullOrEmpty (substituteProvider)? null : substituteProvider;
		}
		
		public string DocType {
			get { return docType; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public IXmlCompletionProvider CompletionProvider {
			get {
				if (substituteProvider != null)
					ResolveProvider ();
				return provider;
			}
		}
		
		void ResolveProvider ()
		{
			try {
				HtmlSchema hs = HtmlSchemaService.GetSchema (substituteProvider);
				if (hs != null)
					provider = hs.CompletionProvider;
			} catch (StackOverflowException) {
				MonoDevelop.Core.LoggingService.LogWarning (
				    "HTML doctype '{0}' contains a substitute schema reference that is either cyclical or too deep, and hence cannot be resolved.'", 
				    name);
			}
			if (provider == null) {
				provider = new EmptyXmlCompletionProvider ();
				MonoDevelop.Core.LoggingService.LogWarning (
				    "HTML doctype '{0}' cannot find substitute schema '{1}'", name, substituteProvider);
			}
			substituteProvider = null;
		}
	}
}
