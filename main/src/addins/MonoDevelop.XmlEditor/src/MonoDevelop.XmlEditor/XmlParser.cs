//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
//

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Utility class that contains xml parsing routines used to determine
	/// the currently selected element so we can provide intellisense.
	/// </summary>
	/// <remarks>
	/// All of the routines return <see cref="XmlElementPath"/> objects
	/// since we are interested in the complete path or tree to the 
	/// currently active element. 
	/// </remarks>
	public class XmlParser
	{
		/// <summary>
		/// Helper class.  Holds the namespace URI and the prefix currently
		/// in use for this namespace.
		/// </summary>
		class NamespaceURI
		{
			string namespaceURI = String.Empty;
			string prefix = String.Empty;
			
			public NamespaceURI()
			{	
			}
			
			public NamespaceURI(string namespaceURI, string prefix)
			{
				this.namespaceURI = namespaceURI;
				this.prefix = prefix;
			}
			
			public string Namespace {
				get {
					return namespaceURI;
				}
				set {
					namespaceURI = value;
				}
			}
			
			public string Prefix {
				get {
					return prefix;
				}
				set {
					prefix = value;
					if (prefix == null) {
						prefix = String.Empty;
					}
				}
			}
		}
		
		XmlParser()
		{
		}
		
		/// <summary>
		/// Gets path of the xml element start tag that the specified 
		/// <paramref name="index"/> is currently inside.
		/// </summary>
		/// <remarks>If the index outside the start tag then an empty path
		/// is returned.</remarks>
		public static XmlElementPath GetActiveElementStartPath(string xml, int index)
		{
			XmlElementPath path = new XmlElementPath();
			
			string elementText = GetActiveElementStartText(xml, index);
			
			if (elementText != null) {
				QualifiedName elementName = GetElementName(elementText);
				NamespaceURI elementNamespace = GetElementNamespace(elementText);
							
				path = GetParentElementPath(xml, index);
				if (elementNamespace.Namespace.Length == 0) {
					if (path.Elements.Count > 0) {
						QualifiedName parentName = path.Elements[path.Elements.Count - 1];
						elementNamespace.Namespace = parentName.Namespace;
						elementNamespace.Prefix = parentName.Prefix;
					}
				}
				
				path.Elements.Add(new QualifiedName(elementName.Name, elementNamespace.Namespace, elementNamespace.Prefix));
			}
			
			path.Compact();
			
			return path;
		}				
		
		/// <summary>
		/// Gets the parent element path based on the index position.
		/// </summary>
		public static XmlElementPath GetParentElementPath(string xml, int index)
		{
			XmlElementPath path = new XmlElementPath();
			
			try {
				StringReader reader = new StringReader(xml);
				XmlTextReader xmlReader = new XmlTextReader(reader);
				while (xmlReader.Read()) {
					switch (xmlReader.NodeType) {
						case XmlNodeType.Element:
							if (!xmlReader.IsEmptyElement) {
								QualifiedName elementName = new QualifiedName(xmlReader.LocalName, xmlReader.NamespaceURI, xmlReader.Prefix);
								path.Elements.Add(elementName);
							}
							break;
						case XmlNodeType.EndElement:
							path.Elements.RemoveLast();
							break;
					}
				}
			} catch (XmlException) { 
				// Do nothing.
			}
			
			path.Compact();
			
			return path;
		}			
		
		/// <summary>
		/// Checks whether the attribute at the end of the string is a 
		/// namespace declaration.
		/// </summary>
		public static bool IsNamespaceDeclaration(string xml, int index)
		{
			if (xml.Length == 0) {
				return false;	
			}
			
			if (index >= xml.Length) {
				index = xml.Length - 1;
			}
		
			// Move back one character if the last character is an '='
			if (xml[index] == '=') {
				xml = xml.Substring(0, xml.Length - 1);
				--index;
			}
									
			// From the end of the string work backwards until we have
			// picked out the last attribute and reached some whitespace.
			StringBuilder reversedAttributeName = new StringBuilder();
			
			bool ignoreWhitespace = true;
			int currentIndex = index;
			for (int i = 0; i < index; ++i) {
				
				char currentChar = xml[currentIndex];
				
				if (Char.IsWhiteSpace(currentChar)) {
					if (ignoreWhitespace == false) {
						// Reached the start of the attribute name.
						break;
					}
				} else if (Char.IsLetterOrDigit(currentChar) || (currentChar == ':')) {
					ignoreWhitespace = false;
					reversedAttributeName.Append(currentChar);
				} else {
					// Invalid string.
					break;
				}
				
				--currentIndex;
			}
			
			// Did we get a namespace?
			
			bool isNamespace = false;

			if ((reversedAttributeName.ToString() == "snlmx") || (reversedAttributeName.ToString().EndsWith(":snlmx"))) {
			    isNamespace = true;
			}
			
			return isNamespace;
		}
		
		/// <summary>
		/// Gets the name of the attribute inside but before the specified 
		/// index.
		/// </summary>
		public static string GetAttributeName(string xml, int index)
		{
			if (xml.Length == 0) {
				return String.Empty;
			}
			
			if (index >= xml.Length) {
				index = xml.Length - 1;
			}
			
			string name = String.Empty;
			
			// From the end of the string work backwards until we have
			// picked out the attribute name.
			StringBuilder reversedAttributeName = new StringBuilder();
			
			bool ignoreWhitespace = true;
			bool ignoreQuote = true;
			bool ignoreEqualsSign = true;
			int currentIndex = index;
			bool invalidString = true;
			
			for (int i = 0; i <= index; ++i) {
				
				char currentChar = xml[currentIndex];
				
				if (Char.IsLetterOrDigit(currentChar)) {
					if (!ignoreEqualsSign) {
						ignoreWhitespace = false;
						reversedAttributeName.Append(currentChar);
					} 
				} else if (Char.IsWhiteSpace(currentChar)) {
					if (ignoreWhitespace == false) {
						// Reached the start of the attribute name.
						invalidString = false;
						break;
					}
				} else if ((currentChar == '\'') || (currentChar == '\"')) {
					if (ignoreQuote) {
						ignoreQuote = false;
					} else {
						break;
					}
				} else if (currentChar == '='){
					if (ignoreEqualsSign) {
						ignoreEqualsSign = false;
					} else {
						break;
					}
				} else if (IsAttributeValueChar(currentChar)) {
					if (!ignoreQuote) {
						break;
					}
				} else {
					break;
				}
				
				--currentIndex;
			}

			if (!invalidString) {
				name = ReverseString(reversedAttributeName.ToString());
			}
			
			return name;
		}
		
		/// <summary>
		/// Checks for valid xml attribute value character
		/// </summary>
		public static bool IsAttributeValueChar(char ch)
		{
			if (Char.IsLetterOrDigit(ch) || 
			    (ch == ':') || 
			    (ch == '/') || 
			    (ch == '_') ||
			    (ch == '.') || 
			    (ch == '-') ||
			    (ch == '#'))
			{
				return true;
			} 
			
			return false;
		}
	
		/// <summary>
		/// Checks for valid xml element or attribute name character.
		/// </summary>
		public static bool IsXmlNameChar(char ch)
		{
			if (Char.IsLetterOrDigit(ch) || 
			    (ch == ':') || 
			    (ch == '/') || 
			    (ch == '_') ||
			    (ch == '.') || 
			    (ch == '-'))
			{
				return true;
			} 
			
			return false;
		}
		/// <summary>
		/// Gets the text of the xml element start tag that the index is 
		/// currently inside.
		/// </summary>
		/// <returns>
		/// Returns the text up to and including the start tag &lt; character.
		/// </returns>
		static string GetActiveElementStartText(string xml, int index)
		{
			int elementStartIndex = GetActiveElementStartIndex(xml, index);
			
			if (elementStartIndex >= 0) {
				if (elementStartIndex < index) {
					int elementEndIndex = GetActiveElementEndIndex(xml, index);
					
					if (elementEndIndex >= index) {
						return xml.Substring(elementStartIndex, elementEndIndex - elementStartIndex);
					}
				}
			}
			
			return null;
		}
		
		/// <summary>
		/// Locates the index of the start tag &lt; character.
		/// </summary>
		/// <returns>
		/// Returns the index of the start tag character; otherwise
		/// -1 if no start tag character is found or a end tag
		/// &gt; character is found first.
		/// </returns>
		static int GetActiveElementStartIndex(string xml, int index)
		{
			int elementStartIndex = -1;
			
			int currentIndex = index - 1;
			for (int i = 0; i < index; ++i) {
				
				char currentChar = xml[currentIndex];
				if (currentChar == '<') {
					elementStartIndex = currentIndex;
					break;
				} else if (currentChar == '>') {
					break;
				}
				
				--currentIndex;
			}
			
			return elementStartIndex;
		}
		
		/// <summary>
		/// Locates the index of the end tag character.
		/// </summary>
		/// <returns>
		/// Returns the index of the end tag character; otherwise
		/// -1 if no end tag character is found or a start tag
		/// character is found first.
		/// </returns>
		static int GetActiveElementEndIndex(string xml, int index)
		{
			int elementEndIndex = index;
			
			for (int i = index; i < xml.Length; ++i) {
				
				char currentChar = xml[i];
				if (currentChar == '>') {
					elementEndIndex = i;
					break;
				} else if (currentChar == '<'){
					elementEndIndex = -1;
					break;
				}
			}
			
			return elementEndIndex;
		}		
		
		/// <summary>
		/// Gets the element name from the element start tag string.
		/// </summary>
		/// <param name="xml">This string must start at the 
		/// element we are interested in.</param>
		static QualifiedName GetElementName(string xml)
		{
			string name = String.Empty;
			
			// Find the end of the element name.
			xml = xml.Replace("\r\n", " ");
			int index = xml.IndexOf(' ');
			if (index > 0) {
				name = xml.Substring(1, index - 1);
			}
			
			QualifiedName qualifiedName = new QualifiedName();
			
			int prefixIndex = name.IndexOf(':');
			if (prefixIndex > 0) {
				qualifiedName.Prefix = name.Substring(0, prefixIndex);
				qualifiedName.Name = name.Substring(prefixIndex + 1);
			} else {
				qualifiedName.Name = name;
			}
			
			return qualifiedName;
		}		
		
		/// <summary>
		/// Gets the element namespace from the element start tag
		/// string.
		/// </summary>
		/// <param name="xml">This string must start at the 
		/// element we are interested in.</param>
		static NamespaceURI GetElementNamespace(string xml)
		{
			NamespaceURI namespaceURI = new NamespaceURI();
			
			Match match = Regex.Match(xml, ".*?(xmlns\\s*?|xmlns:.*?)=\\s*?['\\\"](.*?)['\\\"]");
			if (match.Success) {
				namespaceURI.Namespace = match.Groups[2].Value;
				
				string xmlns = match.Groups[1].Value.Trim();
				int prefixIndex = xmlns.IndexOf(':');
				if (prefixIndex > 0) {
					namespaceURI.Prefix = xmlns.Substring(prefixIndex + 1);
				}
			}
			
			return namespaceURI;
		}			
		
		static string ReverseString(string text)
		{
			StringBuilder reversedString = new StringBuilder(text);
			
			int index = text.Length;
			foreach (char ch in text) {
				--index;
				reversedString[index] = ch;
			}
			
			return reversedString.ToString();
		}
	}
}
