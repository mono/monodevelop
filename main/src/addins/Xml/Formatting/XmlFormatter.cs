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
using System.Linq;
using System.Xml;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Xml.Formatting
{
	public class XmlFormatter: AbstractCodeFormatter
	{
		public static string FormatXml (TextStylePolicy textPolicy, XmlFormattingPolicy formattingPolicy, string input)
		{
			XmlDocument doc;
			try {
				doc = new XmlDocument ();
				doc.XmlResolver = null; // Prevent DTDs from being downloaded.
				doc.LoadXml (input);
			} catch (XmlException ex) {
				// handle xml files without root element (https://bugzilla.xamarin.com/show_bug.cgi?id=4748)
				if (ex.Message == "Root element is missing.")
					return input;
				MonoDevelop.Core.LoggingService.LogWarning ("Error formatting XML file", ex);
				IdeApp.Workbench.StatusBar.ShowError ("Error formatting file: " + ex.Message);
				return input;
			} catch (Exception ex) {
				// Ignore malformed xml
				MonoDevelop.Core.LoggingService.LogWarning ("Error formatting XML file", ex);
				IdeApp.Workbench.StatusBar.ShowError ("Error formatting file: " + ex.Message);
				return input;
			}
			
			var sw = new StringWriter ();
			var xmlWriter = new XmlFormatterWriter (sw);
			xmlWriter.WriteNode (doc, formattingPolicy, textPolicy);
			xmlWriter.Flush ();
			return sw.ToString ();
		}

		protected override Core.Text.ITextSource FormatImplementation (PolicyContainer policyParent, string mimeType, Core.Text.ITextSource input, int startOffset, int length)
		{
			if (policyParent == null)
				policyParent = PolicyService.DefaultPolicies;
			var mimeTypeInheritanceChain = DesktopService.GetMimeTypeInheritanceChain (mimeType).ToList ();
			var txtPol = policyParent.Get<TextStylePolicy> (mimeTypeInheritanceChain);
			var xmlPol = policyParent.Get<XmlFormattingPolicy> (mimeTypeInheritanceChain);
			return new StringTextSource(FormatXml (txtPol, xmlPol, input.Text));
		}
		
		public string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeInheritanceChain, string input, int fromOffest, int toOffset)
		{
			return null;
		}
	}
}
