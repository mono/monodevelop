//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using MonoDevelop.Core.Properties;
using System;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Represents an association between an xml schema and a file extension.
	/// </summary>
	public class XmlSchemaAssociation : IXmlConvertable
	{
		string namespaceUri = String.Empty;
		string extension = String.Empty;
		string namespacePrefix = String.Empty;
		
		static readonly string schemaAssociationElementName = "SchemaAssociation";
		static readonly string extensionAttributeName = "extension";
		static readonly string namespaceAttributeName = "namespace";
		static readonly string prefixAttributeName = "prefix";
		
		public XmlSchemaAssociation(string extension)
			: this(extension, String.Empty, String.Empty)
		{
		}
		
		public XmlSchemaAssociation(string extension, string namespaceUri)
			: this(extension, namespaceUri, String.Empty)
		{
		}
		
		public XmlSchemaAssociation(string extension, string namespaceUri, string namespacePrefix)
		{
			this.extension = extension;
			this.namespaceUri = namespaceUri;
			this.namespacePrefix = namespacePrefix;
		}
		
		public string NamespaceUri {
			get {
				return namespaceUri;
			}
			
			set {
				namespaceUri = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the file extension (e.g. '.xml').
		/// </summary>
		public string Extension {
			get {
				return extension;
			}
			
			set {
				extension = value;
			}
		}
		
		/// <summary>
		/// Gets or sets the default namespace prefix that will be added
		/// to the xml elements.
		/// </summary>
		public string NamespacePrefix {
			get {
				return namespacePrefix;
			}
			
			set {
				namespacePrefix = value;
			}
		}		
		
		/// <summary>
		/// Gets the default schema association for the file extension. 
		/// </summary>
		/// <remarks>
		/// These defaults are hard coded.
		/// </remarks>
		public static XmlSchemaAssociation GetDefaultAssociation(string extension)
		{
			XmlSchemaAssociation association = null;
			
			switch (extension.ToLower()) {
				case ".config":
					association = new XmlSchemaAssociation(extension, @"urn:app-config");
					break;
				case ".build":
					association = new XmlSchemaAssociation(extension, @"http://nant.sf.net/release/0.85-rc3/nant.xsd");
					break;
				case ".xsl":
				case ".xslt":
					association = new XmlSchemaAssociation(extension, @"http://www.w3.org/1999/XSL/Transform", "xsl");
					break;
				case ".xsd":
					association = new XmlSchemaAssociation(extension, @"http://www.w3.org/2001/XMLSchema", "xs");
					break;
				default:
					association = new XmlSchemaAssociation(extension);
					break;
			}
			return association;
		}
		
		/// <summary>
		/// Two schema associations are considered equal if their file extension,
		/// prefix and namespaceUri are the same.
		/// </summary>
		public override bool Equals(object obj)
		{
			bool equals = false;
			
			XmlSchemaAssociation rhs = obj as XmlSchemaAssociation;
			if (rhs != null) {
				if ((this.namespacePrefix == rhs.namespacePrefix) &&
				    (this.extension == rhs.extension) &&
				    (this.namespaceUri == rhs.namespaceUri)) {
					equals = true;
				}
			}
						
			return equals;
		}
		
		public override int GetHashCode()
		{
			return (namespaceUri != null ? namespaceUri.GetHashCode() : 0) ^ (extension != null ? extension.GetHashCode() : 0) ^ (namespacePrefix != null ? namespacePrefix.GetHashCode() : 0);
		}
		
		/// <summary>
		/// Creates an XmlSchemaAssociation from the saved xml.
		/// </summary>
		public object FromXmlElement(XmlElement element)
		{
			return XmlSchemaAssociation.ConvertFromXmlElement(element);
		}
		
		/// <summary>
		/// Creates an XmlSchemaAssociation from the saved xml.
		/// </summary>
		public static object ConvertFromXmlElement(XmlElement element)
		{
			XmlSchemaAssociation association = null;
			
			if (element.ChildNodes.Count == 1) {
				XmlElement childElement = element.ChildNodes[0] as XmlElement;
				if (childElement != null) {
					if (childElement.Name == schemaAssociationElementName) {
						association = new XmlSchemaAssociation(childElement.GetAttribute(extensionAttributeName), childElement.GetAttribute(namespaceAttributeName), childElement.GetAttribute(prefixAttributeName));
					} else {
						throw new UnknownPropertyNodeException(childElement.Name);
					}					
				}
			}
			return association;
		}
		
		/// <summary>
		/// Creates an xml element from an XmlSchemaAssociation.
		/// </summary>
		public XmlElement ToXmlElement(XmlDocument doc)
		{	
			XmlElement element = doc.CreateElement(schemaAssociationElementName);
			
			element.SetAttribute(extensionAttributeName, extension);
			element.SetAttribute(namespaceAttributeName, namespaceUri);
			element.SetAttribute(prefixAttributeName, namespacePrefix);
			
			return element;	
		}
	}
}
