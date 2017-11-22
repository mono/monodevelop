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
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;
using System.Text;
using System.ComponentModel;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Xml.Formatting
{
	[PolicyType ("XML formatting")]
	public class XmlFormattingPolicy : IEquatable<XmlFormattingPolicy>
	{
		List<XmlFormattingSettings> formats = new List<XmlFormattingSettings> ();
		XmlFormattingSettings defaultFormat = new XmlFormattingSettings ();
		
		public XmlFormattingPolicy ()
		{
		}
		
		[ItemProperty]
		public List<XmlFormattingSettings> Formats {
			get { return formats; }
		}
		
		[ItemProperty]
		public XmlFormattingSettings DefaultFormat {
			get { return defaultFormat; }
		}
		
		public bool Equals (XmlFormattingPolicy other)
		{
			if (!defaultFormat.Equals (other.defaultFormat))
				return false;
			
			if (formats.Count != other.formats.Count)
				return false;
			
			List<XmlFormattingSettings> list = new List<XmlFormattingSettings> (other.formats);
			foreach (XmlFormattingSettings fs in formats) {
				bool found = false;
				for (int n=0; n<list.Count; n++) {
					if (fs.Equals (list [n])) {
						list.RemoveAt (n);
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}
		
		public XmlFormattingPolicy Clone ()
		{
			XmlFormattingPolicy clone = new XmlFormattingPolicy ();
			clone.defaultFormat = defaultFormat.Clone ();
			foreach (var f in formats)
				clone.formats.Add (f.Clone ());
			return clone;
		}
	}
	
	public class XmlFormattingSettings
	{
		List<string> scope = new List<string> ();
		
		public XmlFormattingSettings ()
		{
			OmitXmlDeclaration = false;
			IndentContent = true;
			
			AttributesInNewLine = false;
			MaxAttributesPerLine = 10;
			AlignAttributes = false;
			AlignAttributeValues = false;
			WrapAttributes = false;
			SpacesBeforeAssignment = 0;
			SpacesAfterAssignment = 0;
			QuoteChar = '"';
			
			EmptyLinesBeforeStart = 0;
			EmptyLinesAfterStart = 0;
			EmptyLinesBeforeEnd = 0;
			EmptyLinesAfterEnd = 0;
		}
		
		public bool Equals (XmlFormattingSettings other)
		{
			if (scope.Count != other.scope.Count)
				return false;
			List<string> list = new List<string> (other.scope.Count);
			foreach (string s in scope) {
				int n = list.IndexOf (s);
				if (n == -1)
					return false;
				list.RemoveAt (n);
			}
			
			return OmitXmlDeclaration == other.OmitXmlDeclaration &&
				IndentContent == other.IndentContent &&
				AttributesInNewLine == other.AttributesInNewLine &&
				MaxAttributesPerLine == other.MaxAttributesPerLine &&
				AlignAttributes == other.AlignAttributes &&
				WrapAttributes == other.WrapAttributes &&
				AlignAttributeValues == other.AlignAttributeValues &&
				SpacesBeforeAssignment == other.SpacesBeforeAssignment &&
				SpacesAfterAssignment == other.SpacesAfterAssignment &&
				QuoteChar == other.QuoteChar &&
				EmptyLinesBeforeStart == other.EmptyLinesBeforeStart &&
				EmptyLinesAfterStart == other.EmptyLinesAfterStart &&
				EmptyLinesBeforeEnd == other.EmptyLinesBeforeEnd &&
				EmptyLinesAfterEnd == other.EmptyLinesAfterEnd;
		}
		
		public XmlFormattingSettings Clone ()
		{
			XmlFormattingSettings clone = (XmlFormattingSettings) MemberwiseClone ();
			clone.scope = new List<string> (scope);
			return clone;
		}
		
		[ItemProperty]
		[System.ComponentModel.Browsable (false)]
		public List<string> ScopeXPath {
			get { return scope; }
		}
		
		[ItemProperty]
		[LocalizedCategory ("Document")]
		[LocalizedDisplayName ("Omit XML declaration")]
		public bool OmitXmlDeclaration { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Document")]
		[LocalizedDisplayName ("Indent content")]
		public bool IndentContent { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Attributes in new line")]
		public bool AttributesInNewLine { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Max attributes per line")]
		public int MaxAttributesPerLine { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Wrap attributes")]
		public bool WrapAttributes { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Align attributes")]
		public bool AlignAttributes { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Align attribute values")]
		public bool AlignAttributeValues { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Quote char")]
		public char QuoteChar { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Spaces before assignment")]
		public int SpacesBeforeAssignment { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Attributes")]
		[LocalizedDisplayName ("Spaces after assignment")]
		public int SpacesAfterAssignment { get; set; }
		
		
		[ItemProperty]
		[LocalizedCategory ("Elements")]
		[LocalizedDisplayName ("Empty lines before start")]
		public int EmptyLinesBeforeStart { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Elements")]
		[LocalizedDisplayName ("Empty lines after start")]
		public int EmptyLinesAfterStart { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Elements")]
		[LocalizedDisplayName ("Empty lines before end")]
		public int EmptyLinesBeforeEnd { get; set; }
		
		[ItemProperty]
		[LocalizedCategory ("Elements")]
		[LocalizedDisplayName ("Empty lines after end")]
		public int EmptyLinesAfterEnd { get; set; }
	}

	class CStringsConverter : TypeConverter
	{
		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof (string);
		}
	
		public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof (string);
		}
	
		public override object ConvertFrom (ITypeDescriptorContext context, 
		                                    System.Globalization.CultureInfo culture, object value)
		{
			return UnescapeString ((string) value);
		}
	
		public override object ConvertTo (ITypeDescriptorContext context, System.Globalization.CultureInfo culture,
		                                  object value, Type destinationType)
		{
			return EscapeString ((string)value);
		}
	
		public static string EscapeString (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				string txt;
				switch (c) {
					case '"': txt = "\\\""; break;
					case '\0': txt = @"\0"; break;
					case '\\': txt = @"\\"; break;
					case '\a': txt = @"\a"; break;
					case '\b': txt = @"\b"; break;
					case '\f': txt = @"\f"; break;
					case '\v': txt = @"\v"; break;
					case '\n': txt = @"\n"; break;
					case '\r': txt = @"\r"; break;
					case '\t': txt = @"\t"; break;
					default:
						sb.Append (c);
						continue;
				}
				sb.Append (txt);
			}
			return sb.ToString ();
		}
		
		public static string UnescapeString (string text)
		{
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < text.Length; i++) {
				char c = text[i];
				if (c == '\\') {
					if (++i >= text.Length)
						break;
					c = text [i];
					char txt;
					switch (c) {
						case '"': txt = '"'; break;
						case '0': txt = '\0'; break;
						case '\\': txt = '\\'; break;
						case 'a': txt = '\a'; break;
						case 'b': txt = '\b'; break;
						case 'f': txt = '\f'; break;
						case 'v': txt = '\v'; break;
						case 'n': txt = '\n'; break;
						case 'r': txt = '\r'; break;
						case 't': txt = '\t'; break;
						default: txt = c; break;
					}
					sb.Append (txt);
				} else
					sb.Append (c);
			}
			return sb.ToString ();
		}
	}
}
