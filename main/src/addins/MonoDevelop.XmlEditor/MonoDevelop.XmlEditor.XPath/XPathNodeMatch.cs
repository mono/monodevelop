//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
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

using System;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Stores an XmlNode and its associated line number and position after an 
	/// XPath query has been evaluated.
	/// </summary>
	public class XPathNodeMatch : IXmlLineInfo
	{
		int? lineNumber;
		int linePosition;
		string value;
		string displayValue;
		XPathNodeType nodeType;
		
		/// <summary>
		/// Creates an XPathNodeMatch from the navigator which should be position on the
		/// node.
		/// </summary>
		/// <remarks>
		/// We deliberately use the OuterXml when we find a Namespace since the
		/// navigator location returned starts from the xmlns attribute.
		/// </remarks>
		public XPathNodeMatch(XPathNavigator currentNavigator)
		{
			SetLineNumbers(currentNavigator as IXmlLineInfo);
			nodeType = currentNavigator.NodeType;
			switch (nodeType) {
				case XPathNodeType.Text:
					SetTextValue(currentNavigator);
					break;
				case XPathNodeType.Comment:
					SetCommentValue(currentNavigator);
					break;
				case XPathNodeType.Namespace:
					SetNamespaceValue(currentNavigator);
					break;
				case XPathNodeType.Element:
					SetElementValue(currentNavigator);
					break;
				case XPathNodeType.ProcessingInstruction:
					SetProcessingInstructionValue(currentNavigator);
					break;
				case XPathNodeType.Attribute:
					SetAttributeValue(currentNavigator);
					break;
				default:
					value = currentNavigator.LocalName;
					displayValue = value;
					break;
			}
		}
		
		/// <summary>
		/// Line numbers are zero based.
		/// </summary>
		public int LineNumber {
			get {
				return lineNumber.GetValueOrDefault(0);
			}
		}
		
		/// <summary>
		/// Line positions are zero based.
		/// </summary>
		public int LinePosition {
			get {
				return linePosition;
			}
		}
		
		public bool HasLineInfo()
		{
			return lineNumber.HasValue;
		}
		
		/// <summary>
		/// Gets the text value of the node.
		/// </summary>
		public string Value {
			get {
				return value;
			}
		}
		
		/// <summary>
		/// Gets the node display value. This includes the angle brackets if it is
		/// an element, for example.
		/// </summary>
		public string DisplayValue {
			get {
				return displayValue;
			}
		}
		
		public XPathNodeType NodeType {
			get {
				return nodeType;
			}
		}
		
		void SetElementValue(XPathNavigator navigator)
		{
			value = navigator.Name;
			if (navigator.IsEmptyElement) {
				displayValue = String.Concat("<", value, "/>");
			} else {
				displayValue = String.Concat("<", value, ">");
			}
		}
		
		void SetTextValue(XPathNavigator navigator)
		{
			value = navigator.Value;
			displayValue = value;
		}
		
		void SetCommentValue(XPathNavigator navigator)
		{
			value = navigator.Value;
			displayValue = navigator.OuterXml;
		}
		
		void SetNamespaceValue(XPathNavigator navigator)
		{
			value = navigator.OuterXml;
			displayValue = value;
		}
		
		void SetProcessingInstructionValue(XPathNavigator navigator)
		{
			value = navigator.Name;
			displayValue = navigator.OuterXml;
		}
		
		void SetAttributeValue(XPathNavigator navigator)
		{
			value = navigator.Name;
			displayValue = String.Concat("@", value);
		}
		
		/// <summary>
		/// Takes one of the xml line number so the numbers are now zero
		/// based instead of one based.
		/// </summary>
		/// <remarks>A namespace query (e.g. //namespace::*) will return
		/// a line info of -1, -1 for the xml namespace. Which looks like
		/// a bug in the XPathDocument class.</remarks>
		void SetLineNumbers(IXmlLineInfo lineInfo)
		{
			if (lineInfo.HasLineInfo() && lineInfo.LineNumber > 0) {
				lineNumber = lineInfo.LineNumber - 1;
				linePosition = lineInfo.LinePosition - 1;
			}
		}
	}
}
