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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Xml.Formatting
{
	public class XmlFormatter
	{
		public bool CanFormat (string mimeType)
		{
			return DesktopService.GetMimeTypeIsSubtype (mimeType, "application/xml");
		}
		
		public string FormatXml (TextStylePolicy textPolicy, XmlFormattingPolicy formattingPolicy, string input)
		{
			StringWriter sw = new StringWriter ();
			
			List<XmlFormatingSettings> formats = new List<XmlFormatingSettings> ();
			XmlFormatingSettings f = new XmlFormatingSettings ();
			f.AttributesInNewLine = true;
			f.AlignAttributeValues = true;
			f.EmptyLinesAfterStart = 1;
			f.EmptyLinesBeforeEnd = 1;
			f.SpacesAfterAssignment = f.SpacesBeforeAssignment = 1;
			f.ScopeXPath.Add ("Addin");
			f.ScopeXPath.Add ("Addin/Extension/Condition");
			formats.Add (f);
			
			f = new XmlFormatingSettings ();
			f.EmptyLinesBeforeStart = 1;
			f.EmptyLinesAfterEnd = 1;
			f.ScopeXPath.Add ("Addin/*");
			formats.Add (f);
			
			try {
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml (input);
				
				XmlFormatterWriter xmlWriter = new XmlFormatterWriter (sw);
				xmlWriter.WriteNode (doc, formattingPolicy);
				xmlWriter.Flush ();
			} catch (Exception ex) {
				// Ignore malfored xml
				Console.WriteLine ("pp:" + ex);
				return input;
			}

			return sw.ToString ();
		}
	}
}
