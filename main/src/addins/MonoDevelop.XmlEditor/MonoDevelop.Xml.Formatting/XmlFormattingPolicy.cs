// 
// XmlFormattingPolicy.cs
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
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Xml.Formatting
{
	public class XmlFormattingPolicy
	{
		List<XmlFormatingSettings> formats = new List<XmlFormatingSettings> ();
		XmlFormatingSettings defaultFormat = new XmlFormatingSettings ();
		
		public XmlFormattingPolicy ()
		{
		}
		
		public List<XmlFormatingSettings> Formats {
			get { return formats; }
		}
		
		public XmlFormatingSettings DefaultFormat {
			get { return defaultFormat; }
		}
	}
	
	public class XmlFormatingSettings
	{
		List<string> scope = new List<string> ();
		
		public XmlFormatingSettings ()
		{
			NewLineChars = "\n";
			OmitXmlDeclaration = false;
			IndentContent = true;
			ContentIndentString = "\t";
			
			AttributesInNewLine = false;
			MaxAttributesPerLine = 10;
			AttributesIndentString = "\t";
			AlignAttributes = false;
			AlignAttributeValues = false;
			SpacesBeforeAssignment = 0;
			SpacesAfterAssignment = 0;
			QuoteChar = '"';
			
			EmptyLinesBeforeStart = 0;
			EmptyLinesAfterStart = 0;
			EmptyLinesBeforeEnd = 0;
			EmptyLinesAfterEnd = 0;
		}
		
		public List<string> ScopeXPath {
			get { return scope; }
		}
		
		public bool OmitXmlDeclaration { get; set; }
		public string NewLineChars { get; set; }
		
		public bool IndentContent { get; set; }
		public string ContentIndentString { get; set; }
		
		public bool AttributesInNewLine { get; set; }
		public int MaxAttributesPerLine { get; set; }
		public string AttributesIndentString { get; set; }
		public bool AlignAttributes { get; set; }
		public bool AlignAttributeValues { get; set; }
		public char QuoteChar { get; set; }
		
		public int SpacesBeforeAssignment { get; set; }
		public int SpacesAfterAssignment { get; set; }
		
		public int EmptyLinesBeforeStart { get; set; }
		public int EmptyLinesAfterStart { get; set; }
		public int EmptyLinesBeforeEnd { get; set; }
		public int EmptyLinesAfterEnd { get; set; }
	}
}
