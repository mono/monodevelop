//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using MonoDevelop.Projects.Gui.Completion;
using System;
using System.Collections;
using System.Xml;
using MonoDevelop.XmlEditor;

namespace MonoDevelop.XmlEditor.Completion
{
	/// <summary>
	/// Provides the autocomplete (intellisense) data for an
	/// xml document that specifies a known schema.
	/// </summary>
	public class XmlCompletionDataProvider : ICompletionDataProvider
	{
		IXmlSchemaCompletionDataCollection schemaCompletionDataItems;
		XmlSchemaCompletionData defaultSchemaCompletionData;
		ICodeCompletionContext completionContext;
		string defaultNamespacePrefix = String.Empty;
		string defaultCompletionString;
		
		public XmlCompletionDataProvider(IXmlSchemaCompletionDataCollection schemaCompletionDataItems, XmlSchemaCompletionData defaultSchemaCompletionData, string defaultNamespacePrefix, ICodeCompletionContext completionContext)
		{
			this.schemaCompletionDataItems = schemaCompletionDataItems;
			this.defaultSchemaCompletionData = defaultSchemaCompletionData;
			this.defaultNamespacePrefix = defaultNamespacePrefix;
			this.completionContext = completionContext;
		}
		
		public void Dispose ()
		{
		}
		
		public string DefaultCompletionString {
			get { return defaultCompletionString; }
			set { defaultCompletionString = value; }
		}

		public ICompletionData[] GenerateCompletionData(ICompletionWidget widget, char charTyped)
		{	
			string text = widget.GetText (0, completionContext.TriggerOffset);

			switch (charTyped) {
				case '=':
					// Namespace intellisense.
					if (XmlParser.IsNamespaceDeclaration(text, text.Length)) {
						return schemaCompletionDataItems.GetNamespaceCompletionData();;
					}
					break;
					
				case '<':
					// Child element intellisense.
					XmlElementPath parentPath = XmlParser.GetParentElementPath(text);
					if (parentPath.Elements.Count > 0) {
						return GetChildElementCompletionData(parentPath);
					} else if (defaultSchemaCompletionData != null) {
						return defaultSchemaCompletionData.GetElementCompletionData(defaultNamespacePrefix);
					}
					break;
					
				case ' ':
					// Attribute intellisense.
					if (!XmlParser.IsInsideAttributeValue(text, text.Length)) {
						XmlElementPath path = XmlParser.GetActiveElementStartPath(text, text.Length);
						if (path.Elements.Count > 0) {
							return GetAttributeCompletionData(path);
						}
					}
					break;
					
				default:
					
					// Attribute value intellisense.
					if (XmlParser.IsAttributeValueChar(charTyped)) {
						string attributeName = XmlParser.GetAttributeName(text, text.Length);
						if (attributeName.Length > 0) {
							XmlElementPath elementPath = XmlParser.GetActiveElementStartPath(text, text.Length);
							if (elementPath.Elements.Count > 0) {
								defaultCompletionString = charTyped.ToString();
								return GetAttributeValueCompletionData(elementPath, attributeName);
							}
						}
					}
					break;
			}
			           
			return null;
		}
		
		/// <summary>
		/// Finds the schema given the xml element path.
		/// </summary>
		public XmlSchemaCompletionData FindSchema(XmlElementPath path)
		{
			if (path.Elements.Count > 0) {
				string namespaceUri = path.Elements[0].Namespace;
				if (namespaceUri.Length > 0) {
					return schemaCompletionDataItems[namespaceUri];
				} else if (defaultSchemaCompletionData != null) {
					
					// Use the default schema namespace if none
					// specified in a xml element path, otherwise
					// we will not find any attribute or element matches
					// later.
					foreach (QualifiedName name in path.Elements) {
						if (name.Namespace.Length == 0) {
							name.Namespace = defaultSchemaCompletionData.NamespaceUri;
						}
					}
					return defaultSchemaCompletionData;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Finds the schema given a namespace URI.
		/// </summary>
		public XmlSchemaCompletionData FindSchema(string namespaceUri)
		{
			return schemaCompletionDataItems[namespaceUri];
		}
		
		/// <summary>
		/// Gets the schema completion data that was created from the specified 
		/// schema filename.
		/// </summary>
		public XmlSchemaCompletionData FindSchemaFromFileName(string fileName)
		{
			return schemaCompletionDataItems.GetSchemaFromFileName(fileName);
		}				
		
		ICompletionData[] GetChildElementCompletionData(XmlElementPath path)
		{
			ICompletionData[] completionData = null;
			
			XmlSchemaCompletionData schema = FindSchema(path);
			if (schema != null) {
				completionData = schema.GetChildElementCompletionData(path);
			}
			
			return completionData;
		}
		
		ICompletionData[] GetAttributeCompletionData(XmlElementPath path)
		{
			ICompletionData[] completionData = null;
			
			XmlSchemaCompletionData schema = FindSchema(path);
			if (schema != null) {
				completionData = schema.GetAttributeCompletionData(path);
			}
			
			return completionData;
		}
		
		ICompletionData[] GetAttributeValueCompletionData(XmlElementPath path, string name)
		{
			ICompletionData[] completionData = null;
			
			XmlSchemaCompletionData schema = FindSchema(path);
			if (schema != null) {
				completionData = schema.GetAttributeValueCompletionData(path, name);
			}
			
			return completionData;
		}		
	}
}
