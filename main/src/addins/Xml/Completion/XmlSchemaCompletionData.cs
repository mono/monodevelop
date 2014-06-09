//
// MonoDevelop XML Editor
//
// Copyright (C) 2005 Matthew Ward
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
using System.Xml.Schema;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Xml.Editor;

namespace MonoDevelop.Xml.Completion
{
	/// <summary>
	/// Holds the completion (intellisense) data for an xml schema.
	/// </summary>
	public class XmlSchemaCompletionData : IXmlCompletionProvider
	{
		string namespaceUri = String.Empty;
		XmlSchema schema = null;
		string fileName = String.Empty;
		string baseUri = string.Empty;
		bool readOnly = false;
		bool loaded = false;
		
		/// <summary>
		/// Stores attributes that have been prohibited whilst the code
		/// generates the attribute completion data.
		/// </summary>
		XmlSchemaObjectCollection prohibitedAttributes = new XmlSchemaObjectCollection();
		
		#region Constructors
		
		public XmlSchemaCompletionData()
		{
		}
		
		/// <summary>
		/// Creates completion data from the schema passed in 
		/// via the reader object.
		/// </summary>
		public XmlSchemaCompletionData(TextReader reader)
		{
			ReadSchema(String.Empty, reader);
		}
		
		/// <summary>
		/// Creates completion data from the schema passed in 
		/// via the reader object.
		/// </summary>
		public XmlSchemaCompletionData(XmlTextReader reader)
		{
			reader.XmlResolver = null;
			ReadSchema(reader);
		}
		
		/// <summary>
		/// Creates the completion data from the specified schema file.
		/// </summary>
		public XmlSchemaCompletionData (string fileName) : this (String.Empty, fileName)
		{
		}
		
		/// <summary>
		/// Creates the completion data from the specified schema file and uses
		/// the specified baseUri to resolve any referenced schemas.
		/// </summary>
		public XmlSchemaCompletionData (string baseUri, string fileName) : this (baseUri, fileName, false)
		{
		}
		
		//lazyLoadFile should not be used when the namespace property needs to be read
		public XmlSchemaCompletionData (string baseUri, string fileName, bool lazyLoadFile)
		{
			this.fileName = fileName;
			this.baseUri = baseUri;
			
			if (!lazyLoadFile)
				using (StreamReader reader = new StreamReader (fileName, true))
					ReadSchema (baseUri, reader);
		}
		
		#endregion
		
		#region Properties
		
		public XmlSchema Schema {
			get {
				EnsureLoaded ();
				return schema;
			}
		}
		
		public bool ReadOnly {
			get { return readOnly; }
			set { readOnly = value; }
		}
		
		public string FileName {
			get { return fileName; }
			set { fileName = value; }
		}
		
		public string NamespaceUri {
			get { return namespaceUri; }
		}
		
		#endregion
		
		/// <summary>
		/// Converts the filename into a valid Uri.
		/// </summary>
		public static string GetUri(string fileName)
		{
			string uri = String.Empty;
			
			if (fileName != null) {
				if (fileName.Length > 0) {
					uri = String.Concat("file:///", fileName.Replace('\\', '/'));
				}
			}
			
			return uri;
		}
		
		#region ILazilyLoadedProvider implementation
		
		public bool IsLoaded {
			get { return loaded; }
		}
		
		public void EnsureLoaded ()
		{
			if (loaded)
				return;
			
			if (schema == null)
				using (StreamReader reader = new StreamReader (fileName, true))
					ReadSchema (baseUri, reader);
			
			//TODO: should we evaluate unresolved imports against other registered schemas?
			//will be messy because we'll have to re-evaluate if any schema is added, removed or changes
			//maybe we should just force users to use schemaLocation in their includes
			var sset = new XmlSchemaSet ();
			sset.XmlResolver = new LocalOnlyXmlResolver ();
			sset.Add (schema);
			sset.ValidationEventHandler += SchemaValidation;
			sset.Compile ();
			loaded = true;
		}
		#endregion
		
		#region Simplified API, useful for e.g. HTML
		
		public CompletionDataList GetChildElementCompletionData (string tagName)
		{
			EnsureLoaded ();
			
			var list = new XmlCompletionDataList ();
			var element = FindElement (tagName);
			if (element != null)
				GetChildElementCompletionData (list, element, "");
			return list;
		}
		
		public CompletionDataList GetAttributeCompletionData (string tagName)
		{
			EnsureLoaded ();
			
			var list = new XmlCompletionDataList ();
			var element = FindElement (tagName);
			if (element != null) {
				prohibitedAttributes.Clear();
				GetAttributeCompletionData (list, element);
			}
			return list;
		}
		
		public CompletionDataList GetAttributeValueCompletionData (string tagName, string name)
		{
			EnsureLoaded ();
			
			var list = new XmlCompletionDataList ();
			var element = FindElement (tagName);
			if (element != null)
				GetAttributeValueCompletionData (list, element, name);
			return list;
		}
		
		#endregion

		/// <summary>
		/// Gets the possible root elements for an xml document using this schema.
		/// </summary>
		public CompletionDataList GetElementCompletionData ()
		{
			return GetElementCompletionData ("");
		}
		
		/// <summary>
		/// Gets the possible root elements for an xml document using this schema.
		/// </summary>
		public CompletionDataList GetElementCompletionData (string namespacePrefix)
		{
			EnsureLoaded ();
			
			var data = new XmlCompletionDataList ();
			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (element.Name != null) {
					data.AddElement (element.Name, namespacePrefix, element.Annotation);
				} else {
					// Do not add reference element.
				}
			}
			return data;
		}
		
		/// <summary>
		/// Gets the attribute completion data for the xml element that exists
		/// at the end of the specified path.
		/// </summary>
		public CompletionDataList GetAttributeCompletionData (XmlElementPath path)
		{
			EnsureLoaded ();
			
			var data = new XmlCompletionDataList (path.Namespaces);
			var element = FindElement (path);
			if (element != null) {
				prohibitedAttributes.Clear ();
				GetAttributeCompletionData (data, element);
			}
			return data;
		}
		
		/// <summary>
		/// Gets the child element completion data for the xml element that exists
		/// at the end of the specified path.
		/// </summary>
		public CompletionDataList GetChildElementCompletionData (XmlElementPath path)
		{
			EnsureLoaded ();
			
			var data = new XmlCompletionDataList (path.Namespaces);
			var element = FindElement (path);
			if (element != null) {
				var last = path.Elements.LastOrDefault ();
				GetChildElementCompletionData (data, element, last != null ? last.Prefix : "");
			}
			return data;
		}		
		
		/// <summary>
		/// Gets the autocomplete data for the specified attribute value.
		/// </summary>
		public CompletionDataList GetAttributeValueCompletionData (XmlElementPath path, string name)
		{
			EnsureLoaded ();
			
			var data = new XmlCompletionDataList (path.Namespaces);
			var element = FindElement (path);
			if (element != null)
				GetAttributeValueCompletionData (data, element, name);
			return data;
		}
		
 		/// <summary>
		/// Finds the element that exists at the specified path.
		/// </summary>
		/// <remarks>This method is not used when generating completion data,
		/// but is a useful method when locating an element so we can jump
		/// to its schema definition.</remarks>
		/// <returns><see langword="null"/> if no element can be found.</returns>
		public XmlSchemaElement FindElement (XmlElementPath path)
		{
			EnsureLoaded ();
			
			XmlSchemaElement element = null;
			for (int i = 0; i < path.Elements.Count; ++i) {
				QualifiedName name = path.Elements[i];
				if (i == 0) {
					// Look for root element.
					element = FindElement (name);
					if (element == null) {
						break;
					}
				} else {
					element = FindChildElement (element, name);
					if (element == null) {
						break;
					}
				}
			}
			return element;
		}
		
		/// <summary>
		/// Finds an element in the schema.
		/// </summary>
		/// <remarks>
		/// Only looks at the elements that are defined in the 
		/// root of the schema so it will not find any elements
		/// that are defined inside any complex types.
		/// </remarks>
		public XmlSchemaElement FindElement (QualifiedName name)
		{
			EnsureLoaded();

			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (name.Equals (element.QualifiedName)) {
					return element;
				}
			}
			MonoDevelop.Core.LoggingService.LogDebug ("XmlSchemaDataObject did not find element '{0}' in the schema", name.Name);
			return null;
		}
		
		public XmlSchemaElement FindElement (string name)
		{
			EnsureLoaded();

			foreach (XmlSchemaElement element in schema.Elements.Values)
				if (element.QualifiedName.Name == name)
					return element;
			
			MonoDevelop.Core.LoggingService.LogDebug ("XmlSchemaDataObject did not find element '{0}' in the schema", name);
			return null;
		}
		
		/// <summary>
		/// Finds the complex type with the specified name.
		/// </summary>
		public XmlSchemaComplexType FindComplexType (QualifiedName name)
		{
			EnsureLoaded ();

			var qualifiedName = new XmlQualifiedName (name.Name, name.Namespace);
			return FindNamedType (schema, qualifiedName);
		}
		
		/// <summary>
		/// Finds the specified attribute name given the element.
		/// </summary>
		/// <remarks>This method is not used when generating completion data,
		/// but is a useful method when locating an attribute so we can jump
		/// to its schema definition.</remarks>
		/// <returns><see langword="null"/> if no attribute can be found.</returns>
		public XmlSchemaAttribute FindAttribute (XmlSchemaElement element, string name)
		{
			EnsureLoaded ();

			XmlSchemaAttribute attribute = null;
			var complexType = GetElementAsComplexType(element);
			if (complexType != null) {
				attribute = FindAttribute(complexType, name);
			}
			return attribute;
		}
		
		/// <summary>
		/// Finds the attribute group with the specified name.
		/// </summary>
		public XmlSchemaAttributeGroup FindAttributeGroup (string name)
		{
			EnsureLoaded ();
			return FindAttributeGroup (schema, name);
		}
		
		/// <summary>
		/// Finds the simple type with the specified name.
		/// </summary>
		public XmlSchemaSimpleType FindSimpleType(string name)
		{
			EnsureLoaded ();
			var qualifiedName = new XmlQualifiedName (name, namespaceUri);
			return FindSimpleType (qualifiedName);
		}
		
		/// <summary>
		/// Finds the specified attribute in the schema. This method only checks
		/// the attributes defined in the root of the schema.
		/// </summary>
		public XmlSchemaAttribute FindAttribute (string name)
		{
			EnsureLoaded ();
			foreach (XmlSchemaAttribute attribute in schema.Attributes.Values)
				if (attribute.Name == name)
					return attribute;
			return null;
		}
		
		/// <summary>
		/// Finds the schema group with the specified name.
		/// </summary>
		public XmlSchemaGroup FindGroup(string name)
		{
			EnsureLoaded();
			if (name != null) {
				foreach (XmlSchemaObject schemaObject in schema.Groups.Values) {
					var group = schemaObject as XmlSchemaGroup;
					if (group != null && group.Name == name)
							return group;
				}
			}
			return null;
		}	
		
		/// <summary>
		/// Takes the name and creates a qualified name using the namespace of this
		/// schema.
		/// </summary>
		/// <remarks>If the name is of the form myprefix:mytype then the correct 
		/// namespace is determined from the prefix. If the name is not of this
		/// form then no prefix is added.</remarks>
		public QualifiedName CreateQualifiedName (string name)
		{
			int index = name.IndexOf (":");
			if (index >= 0) {
				string prefix = name.Substring (0, index);
				name = name.Substring (index + 1);
				EnsureLoaded ();
				//FIXME: look these up from the document's namespaces
				foreach (XmlQualifiedName xmlQualifiedName in schema.Namespaces.ToArray ()) {
					if (xmlQualifiedName.Name == prefix) {
						return new QualifiedName (name, xmlQualifiedName.Namespace, prefix);
					}
				}
			}
			
			// Default behaviour just return the name with the namespace uri.
			return new QualifiedName (name, namespaceUri);
		}		

		/// <summary>
		/// Handler for schema validation errors.
		/// </summary>
		void SchemaValidation (object source, ValidationEventArgs e)
		{
			MonoDevelop.Core.LoggingService.LogWarning ("Validation error loading schema '{0}': {1}", this.fileName, e.Message);
		}
		
		/// <summary>
		/// Loads the schema.
		/// </summary>
		void ReadSchema (XmlReader reader)
		{
			try {
				schema = XmlSchema.Read (reader, new ValidationEventHandler(SchemaValidation));			
				namespaceUri = schema.TargetNamespace;
			} finally {
				reader.Close ();
			}
		}
		
		void ReadSchema (string baseUri, TextReader reader)
		{
			XmlTextReader xmlReader = new XmlTextReader(baseUri, reader);
			
			// The default resolve can cause exceptions loading 
			// xhtml1-strict.xsd because of the referenced dtds. It also has the
			// possibility of blocking on referenced remote URIs.
			// Instead we only resolve local xsds.
			xmlReader.XmlResolver = new LocalOnlyXmlResolver ();
			ReadSchema (xmlReader);
		}			
			
			
		/// <summary>
		/// Finds an element in the schema.
		/// </summary>
		/// <remarks>
		/// Only looks at the elements that are defined in the 
		/// root of the schema so it will not find any elements
		/// that are defined inside any complex types.
		/// </remarks>
		XmlSchemaElement FindElement (XmlQualifiedName name)
		{
			XmlSchemaElement matchedElement = null;
			foreach (XmlSchemaElement element in schema.Elements.Values) {
				if (name.Equals(element.QualifiedName)) {
					matchedElement = element;
					break;
				}
			}
			
			return matchedElement;
		}		
		
		void GetChildElementCompletionData (XmlCompletionDataList data, XmlSchemaElement element, string prefix)
		{
			var complexType = GetElementAsComplexType (element);
			if (complexType != null)
				GetChildElementCompletionData (data, complexType, prefix);
		}
		
		void GetChildElementCompletionData (XmlCompletionDataList data, XmlSchemaComplexType complexType, string prefix)
		{
			var sequence = complexType.Particle as XmlSchemaSequence;
			if (sequence != null) {
				GetChildElementCompletionData (data, sequence.Items, prefix);
				return;
			}
			var choice = complexType.Particle as XmlSchemaChoice;
			if (choice != null) {
				GetChildElementCompletionData (data, choice.Items, prefix);
				return;
			}
			var complexContent = complexType.ContentModel as XmlSchemaComplexContent;
			if (complexContent != null) {
				GetChildElementCompletionData (data, complexContent, prefix);
				return;
			}
			var groupRef = complexType.Particle as XmlSchemaGroupRef;
			if (groupRef != null) {
				GetChildElementCompletionData (data, groupRef, prefix);
				return;
			}
			var all = complexType.Particle as XmlSchemaAll;
			if (all != null) {
				GetChildElementCompletionData (data, all.Items, prefix);
				return;
			}
		}
		
		void GetChildElementCompletionData (XmlCompletionDataList data, XmlSchemaObjectCollection items, string prefix)
		{
			foreach (XmlSchemaObject schemaObject in items) {
				var childElement = schemaObject as XmlSchemaElement;
				if (childElement != null) {
					string name = childElement.Name;
					if (name == null) {
						name = childElement.RefName.Name;
						var element = FindElement (childElement.RefName);
						if (element != null) {
							if (element.IsAbstract) {
								AddSubstitionGroupElements (data, element.QualifiedName, prefix);
							} else {
								data.AddElement (name, prefix, element.Annotation);
							}
						} else {
							data.AddElement (name, prefix, childElement.Annotation);						
						}
					} else {
						data.AddElement (name, prefix, childElement.Annotation);
					}
					continue;
				}
				var childSequence = schemaObject as XmlSchemaSequence;
				if (childSequence != null) {
					GetChildElementCompletionData (data, childSequence.Items, prefix);
					continue;
				}
				var childChoice = schemaObject as XmlSchemaChoice;
				if (childChoice != null) {
					GetChildElementCompletionData (data, childChoice.Items, prefix);
					continue;
				}
				var groupRef = schemaObject as XmlSchemaGroupRef;
				if (groupRef != null) {
					GetChildElementCompletionData (data, groupRef, prefix);
					continue;
				}
			}
		}
		
		void GetChildElementCompletionData (XmlCompletionDataList data, XmlSchemaComplexContent complexContent, string prefix)
		{
			var extension = complexContent.Content as XmlSchemaComplexContentExtension;
			if (extension != null) {
				GetChildElementCompletionData (data, extension, prefix);
				return;
			}
			var restriction = complexContent.Content as XmlSchemaComplexContentRestriction;
			if (restriction != null) {
				GetChildElementCompletionData (data, restriction, prefix);
				return;
			}
		}
		
		void GetChildElementCompletionData (XmlCompletionDataList data, XmlSchemaComplexContentExtension extension, string prefix)
		{
			var complexType = FindNamedType (schema, extension.BaseTypeName);
			if (complexType != null)
				GetChildElementCompletionData (data, complexType, prefix);
			
			if (extension.Particle == null)
				return;
			
			var sequence = extension.Particle as XmlSchemaSequence;
			if (sequence != null) {
				GetChildElementCompletionData (data, sequence.Items, prefix);
				return;
			}
			var choice = extension.Particle as XmlSchemaChoice;
			if (choice != null) {
				GetChildElementCompletionData (data, choice.Items, prefix);
				return;
			}
			var groupRef = extension.Particle as XmlSchemaGroupRef;
			if (groupRef != null) {
				GetChildElementCompletionData (data, groupRef, prefix);
				return;
			}
		}		
		
		void GetChildElementCompletionData (XmlCompletionDataList data, XmlSchemaGroupRef groupRef, string prefix)
		{
			var group = FindGroup (groupRef.RefName.Name);
			if (group == null)
				return;
			var sequence = group.Particle as XmlSchemaSequence;
			if (sequence != null) {
				GetChildElementCompletionData (data, sequence.Items, prefix);
				return;
			}
			var choice = group.Particle as XmlSchemaChoice;
			if (choice != null) {
				GetChildElementCompletionData (data, choice.Items, prefix);
				return;
			}
		}		
		
		void GetChildElementCompletionData (XmlCompletionDataList data, XmlSchemaComplexContentRestriction restriction, string prefix)
		{
			if (restriction.Particle == null)
				return;
			var sequence = restriction.Particle as XmlSchemaSequence;
			if (sequence != null) {
				GetChildElementCompletionData (data, sequence.Items, prefix);
				return;
			}
			var choice = restriction.Particle as XmlSchemaChoice;
			if (choice != null) {
				GetChildElementCompletionData (data, choice.Items, prefix);
				return;
			}
			var groupRef = restriction.Particle as XmlSchemaGroupRef;
			if (groupRef != null) {
				GetChildElementCompletionData (data, groupRef, prefix);
				return;
			}
		}
		
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaElement element)
		{
			var complexType = GetElementAsComplexType (element);
			if (complexType != null)
				GetAttributeCompletionData (data, complexType);
		}	
		
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaComplexContentRestriction restriction)
		{						
			GetAttributeCompletionData (data, restriction.Attributes);
			
			var baseComplexType = FindNamedType(schema, restriction.BaseTypeName);
			if (baseComplexType != null) {
				GetAttributeCompletionData (data, baseComplexType);
			}
		}
		
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaComplexType complexType)
		{
			GetAttributeCompletionData (data, complexType.Attributes);

			// Add any complex content attributes.
			var complexContent = complexType.ContentModel as XmlSchemaComplexContent;
			if (complexContent != null) {
				var extension = complexContent.Content as XmlSchemaComplexContentExtension;
				var restriction = complexContent.Content as XmlSchemaComplexContentRestriction;
				if (extension != null)
					GetAttributeCompletionData (data, extension);
				else if (restriction != null)
					GetAttributeCompletionData (data, restriction);
			} else {
				var simpleContent = complexType.ContentModel as XmlSchemaSimpleContent;
				if (simpleContent != null)
					GetAttributeCompletionData (data, simpleContent);
			}
		}
		
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaComplexContentExtension extension)
		{
			GetAttributeCompletionData (data, extension.Attributes);
			var baseComplexType = FindNamedType (schema, extension.BaseTypeName);
			if (baseComplexType != null)
				GetAttributeCompletionData (data, baseComplexType);
		}		
		
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaSimpleContent simpleContent)
		{
			var extension = simpleContent.Content as XmlSchemaSimpleContentExtension;
			if (extension != null)
				GetAttributeCompletionData (data, extension);
		}
		
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaSimpleContentExtension extension)
		{
			GetAttributeCompletionData (data, extension.Attributes);
		}		
		
		/// <summary>
		/// Converts the element to a complex type if possible.
		/// </summary>
		XmlSchemaComplexType GetElementAsComplexType (XmlSchemaElement element)
		{
			return (element.SchemaType as XmlSchemaComplexType)
				?? FindNamedType (schema, element.SchemaTypeName);
		}
		
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaObjectCollection attributes)
		{
			foreach (XmlSchemaObject schemaObject in attributes) {
				var attribute = schemaObject as XmlSchemaAttribute;
				if (attribute != null) {
					if (!IsProhibitedAttribute(attribute)) {
						data.AddAttribute (attribute);
					} else {
						prohibitedAttributes.Add (attribute);
					}
				} else {
					var attributeGroupRef = schemaObject as XmlSchemaAttributeGroupRef;
					if (attributeGroupRef != null)
						GetAttributeCompletionData (data, attributeGroupRef);
				}
			}
		}
		
		/// <summary>
		/// Checks that the attribute is prohibited or has been flagged
		/// as prohibited previously. 
		/// </summary>
		bool IsProhibitedAttribute (XmlSchemaAttribute attribute)
		{
			bool prohibited = false;
			if (attribute.Use == XmlSchemaUse.Prohibited) {
				prohibited = true;
			} else {
				foreach (XmlSchemaAttribute prohibitedAttribute in prohibitedAttributes) {
					if (prohibitedAttribute.QualifiedName == attribute.QualifiedName) {
						prohibited = true;
						break;
					}
				}
			}
		
			return prohibited;
		}
		
		/// <summary>
		/// Gets attribute completion data from a group ref.
		/// </summary>
		void GetAttributeCompletionData (XmlCompletionDataList data, XmlSchemaAttributeGroupRef groupRef)
		{
			var group = FindAttributeGroup (schema, groupRef.RefName.Name);
			if (group != null)
				GetAttributeCompletionData (data, group.Attributes);
		}
		
		static XmlSchemaComplexType FindNamedType (XmlSchema schema, XmlQualifiedName name)
		{
			if (name == null)
				return null;
			
			foreach (XmlSchemaObject schemaObject in schema.Items) {
				var complexType = schemaObject as XmlSchemaComplexType;
				if (complexType != null && complexType.QualifiedName == name)
						return complexType;
			}
			
			// Try included schemas.
			foreach (XmlSchemaExternal external in schema.Includes) {
				var include = external as XmlSchemaInclude;
				if (include != null && include.Schema != null) {
					var matchedComplexType = FindNamedType (include.Schema, name);
					if (matchedComplexType != null)
						return matchedComplexType;
				}
			}
			
			return null;
		}	
		
		/// <summary>
		/// Finds an element that matches the specified <paramref name="name"/>
		/// from the children of the given <paramref name="element"/>.
		/// </summary>
		XmlSchemaElement FindChildElement (XmlSchemaElement element, QualifiedName name)
		{
			var complexType = GetElementAsComplexType (element);
			if (complexType != null)
				return FindChildElement (complexType, name);
			return null;
		}
		
		XmlSchemaElement FindChildElement (XmlSchemaComplexType complexType, QualifiedName name)
		{
			var sequence = complexType.Particle as XmlSchemaSequence;
			if (sequence != null)
				return FindElement (sequence.Items, name);
			
			var choice = complexType.Particle as XmlSchemaChoice;
			if (choice != null)
				return FindElement (choice.Items, name);
			
			var complexContent = complexType.ContentModel as XmlSchemaComplexContent;
			if (complexContent != null) {
				var extension = complexContent.Content as XmlSchemaComplexContentExtension;
				if (extension != null)
					return FindChildElement (extension, name);
				var restriction = complexContent.Content as XmlSchemaComplexContentRestriction;
				if (restriction != null)
					return FindChildElement (restriction, name);
			}
			
			var groupRef = complexType.Particle as XmlSchemaGroupRef;
			if (groupRef != null)
				return FindElement(groupRef, name);
			
			var all = complexType.Particle as XmlSchemaAll;
			if (all != null)
				return FindElement(all.Items, name);
			
			return null;
		}
		
		/// <summary>
		/// Finds the named child element contained in the extension element.
		/// </summary>
		XmlSchemaElement FindChildElement (XmlSchemaComplexContentExtension extension, QualifiedName name)
		{
			var complexType = FindNamedType (schema, extension.BaseTypeName);
			if (complexType == null)
				return null;
			
			var matchedElement = FindChildElement (complexType, name);
			if (matchedElement != null)
				return matchedElement;
			
			var sequence = extension.Particle as XmlSchemaSequence;
			if (sequence != null)
				return FindElement (sequence.Items, name);
			
			var choice = extension.Particle as XmlSchemaChoice;
			if (choice != null)
				return FindElement (choice.Items, name);
			
			var groupRef = extension.Particle as XmlSchemaGroupRef;
			if (groupRef != null)
				return FindElement (groupRef, name);
			
			return null;
		}
		
		/// <summary>
		/// Finds the named child element contained in the restriction element.
		/// </summary>
		XmlSchemaElement FindChildElement (XmlSchemaComplexContentRestriction restriction, QualifiedName name)
		{
			var sequence = restriction.Particle as XmlSchemaSequence;
			if (sequence != null)
				return FindElement (sequence.Items, name);
			
			var groupRef = restriction.Particle as XmlSchemaGroupRef;
			if (groupRef != null)
				return FindElement (groupRef, name);

			return null;
		}		
		
		/// <summary>
		/// Finds the element in the collection of schema objects.
		/// </summary>
		XmlSchemaElement FindElement (XmlSchemaObjectCollection items, QualifiedName name)
		{
			XmlSchemaElement matchedElement = null;
			
			foreach (XmlSchemaObject schemaObject in items) {
				var element = schemaObject as XmlSchemaElement;
				var sequence = schemaObject as XmlSchemaSequence;
				var choice = schemaObject as XmlSchemaChoice;
				var groupRef = schemaObject as XmlSchemaGroupRef;
				
				if (element != null) {
					if (element.Name != null) {
						if (name.Name == element.Name) {
							return element;
						}
					} else if (element.RefName != null) {
						if (name.Name == element.RefName.Name) {
							matchedElement = FindElement (element.RefName);
						} else {
							var abstractElement = FindElement (element.RefName);
							if (abstractElement != null && abstractElement.IsAbstract) {
								matchedElement = FindSubstitutionGroupElement (abstractElement.QualifiedName, name);
							}
						}
					}
				} else if (sequence != null) {
					matchedElement = FindElement (sequence.Items, name);
				} else if (choice != null) {
					matchedElement = FindElement (choice.Items, name);
				} else if (groupRef != null) {
					matchedElement = FindElement (groupRef, name);
				}
				
				if (matchedElement != null)
					return matchedElement;
			}
			
			return null;
		}
		
		XmlSchemaElement FindElement (XmlSchemaGroupRef groupRef, QualifiedName name)
		{
			var group = FindGroup (groupRef.RefName.Name);
			if (group == null)
				return null;
			
			var sequence = group.Particle as XmlSchemaSequence;
			if (sequence != null)
				return FindElement (sequence.Items, name);
			var choice = group.Particle as XmlSchemaChoice;
			if (choice != null)
				return FindElement (choice.Items, name);
			
			return null;
		}
		
		static XmlSchemaAttributeGroup FindAttributeGroup (XmlSchema schema, string name)
		{
			if (name == null)
				return null;
			
			foreach (XmlSchemaObject schemaObject in schema.Items) {
				var group = schemaObject as XmlSchemaAttributeGroup;
				if (group != null && group.Name == name)
					return group;
			}
			
			// Try included schemas.
			foreach (XmlSchemaExternal external in schema.Includes) {
				var include = external as XmlSchemaInclude;
				if (include != null && include.Schema != null) {
					var found = FindAttributeGroup (include.Schema, name);
					if (found != null)
						return found;
				}
			}
			return null;
		}
		
		void GetAttributeValueCompletionData (XmlCompletionDataList data, XmlSchemaElement element, string name)
		{
			var complexType = GetElementAsComplexType (element);
			if (complexType != null) {
				var attribute = FindAttribute (complexType, name);
				if (attribute != null)
					GetAttributeValueCompletionData (data, attribute);
			}
		}
		
		void GetAttributeValueCompletionData (XmlCompletionDataList data, XmlSchemaAttribute attribute)
		{			
			if (attribute.SchemaType != null) {
				var simpleTypeRestriction = attribute.SchemaType.Content as XmlSchemaSimpleTypeRestriction;
				if (simpleTypeRestriction != null) {
					GetAttributeValueCompletionData (data, simpleTypeRestriction);
				}
			} else if (attribute.AttributeSchemaType != null) {
				if (attribute.AttributeSchemaType.TypeCode == XmlTypeCode.Boolean)
					GetBooleanAttributeValueCompletionData (data);
				else
					GetAttributeValueCompletionData (data, attribute.AttributeSchemaType);
			}
		}
		
		void GetAttributeValueCompletionData (XmlCompletionDataList data, XmlSchemaSimpleTypeRestriction simpleTypeRestriction)
		{
			foreach (XmlSchemaObject schemaObject in simpleTypeRestriction.Facets) {
				var enumFacet = schemaObject as XmlSchemaEnumerationFacet;
				if (enumFacet != null)
					data.AddAttributeValue (enumFacet.Value, enumFacet.Annotation);
			}
		}
		
		void GetAttributeValueCompletionData (XmlCompletionDataList data, XmlSchemaSimpleTypeUnion union)
		{
			foreach (XmlSchemaObject schemaObject in union.BaseTypes) {
				var simpleType = schemaObject as XmlSchemaSimpleType;
				if (simpleType != null)
					GetAttributeValueCompletionData (data, simpleType);
			}
		}		
		
		void GetAttributeValueCompletionData (XmlCompletionDataList data, XmlSchemaSimpleType simpleType)
		{
			var xsstr = simpleType.Content as XmlSchemaSimpleTypeRestriction;
			if (xsstr != null) {
				GetAttributeValueCompletionData (data, xsstr);
				return;
			}
			var xsstu = simpleType.Content as XmlSchemaSimpleTypeUnion;
			if (xsstu != null) {
				GetAttributeValueCompletionData (data, xsstu);
				return;
			}
			var xsstl = simpleType.Content as XmlSchemaSimpleTypeList;
			if (xsstl != null) {
				GetAttributeValueCompletionData (data, xsstl);
				return;
			}
		}		
			
		void GetAttributeValueCompletionData (XmlCompletionDataList data, XmlSchemaSimpleTypeList list)
		{
			if (list.ItemType != null) {
				GetAttributeValueCompletionData (data, list.ItemType);
			} else if (list.ItemTypeName != null) {
				var simpleType = FindSimpleType (list.ItemTypeName);
				if (simpleType != null)
					GetAttributeValueCompletionData (data, simpleType);
			}
		}	
		
		/// <summary>
		/// Gets the set of attribute values for an xs:boolean type.
		/// </summary>
		void GetBooleanAttributeValueCompletionData (XmlCompletionDataList data)
		{
			data.AddAttributeValue ("0");
			data.AddAttributeValue ("1");
			data.AddAttributeValue ("true");
			data.AddAttributeValue ("false");
		}
		
		XmlSchemaAttribute FindAttribute (XmlSchemaComplexType complexType, string name)
		{
			var matchedAttribute = FindAttribute (complexType.Attributes, name);
			if (matchedAttribute != null)
				return matchedAttribute;
			
			var complexContent = complexType.ContentModel as XmlSchemaComplexContent;
			if (complexContent != null)
				return FindAttribute (complexContent, name);
			
			return null;
		}
		
		XmlSchemaAttribute FindAttribute (XmlSchemaObjectCollection schemaObjects, string name)
		{
			foreach (XmlSchemaObject schemaObject in schemaObjects) {
				var attribute = schemaObject as XmlSchemaAttribute;
				if (attribute != null && attribute.Name == name)
					return attribute;
				
				var groupRef = schemaObject as XmlSchemaAttributeGroupRef;
				if (groupRef != null) {
					var matchedAttribute = FindAttribute (groupRef, name);
					if (matchedAttribute != null)
						return matchedAttribute;
				}
			}
			return null;
		}
		
		XmlSchemaAttribute FindAttribute (XmlSchemaAttributeGroupRef groupRef, string name)
		{
			if (groupRef.RefName != null) {
				var group = FindAttributeGroup (schema, groupRef.RefName.Name);
				if (group != null) {
					return FindAttribute (group.Attributes, name);
				}
			}
			return null;
		}
		
		XmlSchemaAttribute FindAttribute (XmlSchemaComplexContent complexContent, string name)
		{
			var extension = complexContent.Content as XmlSchemaComplexContentExtension;
			if (extension != null)
				return FindAttribute (extension, name);
			
			var restriction = complexContent.Content as XmlSchemaComplexContentRestriction;
			if (restriction != null)
				return FindAttribute (restriction, name);
			
			return null;
		}		
		
		XmlSchemaAttribute FindAttribute (XmlSchemaComplexContentExtension extension, string name)
		{
			return FindAttribute (extension.Attributes, name);
		}			
		
		XmlSchemaAttribute FindAttribute (XmlSchemaComplexContentRestriction restriction, string name)
		{
			var matchedAttribute = FindAttribute (restriction.Attributes, name);
			if (matchedAttribute != null)
				return matchedAttribute;
			
			var complexType = FindNamedType (schema, restriction.BaseTypeName);
			if (complexType != null)
				return FindAttribute (complexType, name);
			
			return null;
		}
		
		XmlSchemaSimpleType FindSimpleType (XmlQualifiedName name)
		{
			foreach (XmlSchemaObject schemaObject in schema.SchemaTypes.Values) {
				var simpleType = schemaObject as XmlSchemaSimpleType;
				if (simpleType != null && simpleType.QualifiedName == name)
					return simpleType;
			}
			return null;
		}
		
		/// <summary>
		/// Adds any elements that have the specified substitution group.
		/// </summary>
		void AddSubstitionGroupElements (XmlCompletionDataList data, XmlQualifiedName group, string prefix)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values)
				if (element.SubstitutionGroup == group)
					data.AddElement (element.Name, prefix, element.Annotation);
		}
		
		/// <summary>
		/// Looks for the substitution group element of the specified name.
		/// </summary>
		XmlSchemaElement FindSubstitutionGroupElement (XmlQualifiedName group, QualifiedName name)
		{
			foreach (XmlSchemaElement element in schema.Elements.Values)
				if (element.SubstitutionGroup == group && element.Name != null && element.Name == name.Name)
					return element;
			
			return null;
		}
	}
}
