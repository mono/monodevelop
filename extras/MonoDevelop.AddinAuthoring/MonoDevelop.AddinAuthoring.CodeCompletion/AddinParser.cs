// AddinParser.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Xml;
using Mono.Addins.Description;

namespace MonoDevelop.AddinAuthoring.CodeCompletion
{
	public class AddinParser
	{
		XmlTextReader reader;
		char completionChar;
		AddinDescription adesc;
		
		public AddinParser (AddinDescription adesc, string text, char completionChar)
		{
			reader = new XmlTextReader (new StringReader (text));
			this.completionChar = completionChar;
			this.adesc = adesc;
		}

		public CompletionContext ParseElement (CompletionContext parentContext)
		{
		}
		
		public CompletionContext ParseElementContent (CompletionContext parentContext)
		{
			PositionCheckpoint ();
			
			do {
				if (!reader.Read ())
					return null;
				if (PositionFound ()) {
					if (OpeningElement)
						return new TopLevelCompletionContext ();
					else
						return null;
				}
			}
			while (reader.NodeType != XmlNodeType.Element);
				
			if (reader.Name == "Addin") {
				bool isEmpty = reader.IsEmptyElement;
				reader.Read ();
				if (PositionFound () && OpeningElement)
					return new InsideHeaderCompletionContext ();
				else if (!isEmpty)
					return ParseModule (adesc.MainModule);
			}
			return null;
		}

		CompletionContext ParseElement (CompletionContext parentContext)
		{
			do {
				if (reader.NodeType == XmlNodeType.Element) {
					bool isEmpty = reader.IsEmptyElement;
					ItemData data = parentContext.GetElementData (reader.LocalName);
					if (data != null) {
						CompletionContext childCtx = (CompletionContext) Activator.CreateInstance (data.ChildContextType);
						childCtx.Init (parentContext, reader);
						reader.Read ();
						if (PositionFound ())
							return FillContext (childCtx);
						if (!isEmpty) {
							CompletionContext ctx = ParseElement (childCtx);
							if (ctx != null)
								return ctx;
						}
					}
					else {
						reader.Skip ();
						if (PositionFound ())
							return null;
					}
				}
				else if (PositionFound ()) {
					if (OpeningElement)
						return ModuleExtensionContext (module);
					else
						return null;
				}
			}
			while (reader.Read ());
		}
	}
}
