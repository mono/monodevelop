// 
// XmlFormatter.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.XmlEditor
{
	public class XmlFormatter: IPrettyPrinter
	{
		public bool CanFormat (string mimeType)
		{
			return DesktopService.GetMimeTypeIsSubtype (mimeType, "application/xml");
		}
		
		public string FormatText (MonoDevelop.Projects.SolutionItem policyParent, string mimeType, string input)
		{
			TextStylePolicy policy;
			if (policyParent != null)
				policy = policyParent.Policies.Get <TextStylePolicy> (DesktopService.GetMimeTypeInheritanceChain (mimeType));
			else
				policy = PolicyService.GetDefaultPolicy <TextStylePolicy> (DesktopService.GetMimeTypeInheritanceChain (mimeType));
			
			XmlTextReader reader = new XmlTextReader (new StringReader (input));
			reader.WhitespaceHandling = WhitespaceHandling.None;
			
			StringWriter indentedXmlWriter = new StringWriter ();
			XmlWriterSettings settings = new XmlWriterSettings ();
			settings.Indent = true;
			if (policy.TabsToSpaces) {
				settings.IndentChars = new string (' ', policy.TabWidth);
			} else {
				settings.IndentChars = "\t";
			}
			settings.NewLineChars = policy.GetEolMarker ();
			
			XmlWriter xmlWriter = XmlTextWriter.Create (indentedXmlWriter, settings);
			xmlWriter.WriteNode (reader, false);
			xmlWriter.Flush ();

			return indentedXmlWriter.ToString ();
		}
		
		public string FormatText (MonoDevelop.Projects.SolutionItem policyParent, string mimeType, string input, int fromOffest, int toOffset)
		{
			return input;
		}
	}
}
