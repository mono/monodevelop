// 
// XmlTextEditorExtension.cs
// 
// Authors:
//   Matt Ward
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright:
//   (C) 2007 Matt Ward
//   (C) 2008 Novell, Inc (http://www.novell.com)
// 
// License:
//   Derived from LGPL files (Matt Ward)
//   All code since then is MIT/X11

using System;
using System.Xml;
using System.Xml.Schema;

using MonoDevelop.Core;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.XmlEditor
{
	
	
	public class XmlTextEditorExtension : CompletionTextEditorExtension
	{
		const string TextXmlMimeType = "text/xml";
		const string ApplicationXmlMimeType = "application/xml";
		string stylesheetFileName;
		XmlSchemaCompletionData defaultSchemaCompletionData;
		string defaultNamespacePrefix;
		
		bool autoCompleteElements;
		bool showSchemaAnnotation;
		
		public XmlTextEditorExtension() : base ()
		{
		}
		
		public override bool ExtendsEditor (MonoDevelop.Ide.Gui.Document doc, IEditableTextBuffer editor)
		{
			if (doc == null)
				return false;
			return IsFileNameHandled (doc.FileName);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			XmlEditorAddInOptions.PropertyChanged += XmlEditorPropertyChanged;
			XmlSchemaManager.UserSchemaAdded += UserSchemaAdded;
			XmlSchemaManager.UserSchemaRemoved += UserSchemaRemoved;
			SetInitialValues();
		}
		
		bool disposed;
		public override void Dispose()
		{
			if (!disposed) {
				disposed = false;
				XmlEditorAddInOptions.PropertyChanged -= XmlEditorPropertyChanged;
				XmlSchemaManager.UserSchemaAdded -= UserSchemaAdded;
				XmlSchemaManager.UserSchemaRemoved -= UserSchemaRemoved;
				base.Dispose ();
			}
		}
		
		#region Code completion
		
		IEditableTextBuffer GetBuffer ()
		{
			IEditableTextBuffer buf = Document.GetContent<IEditableTextBuffer> ();
			System.Diagnostics.Debug.Assert (buf != null);
			return buf;
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			switch (completionChar) {
			case ' ':
			case '=':
			case '<':
			case '"':
			case '\'':
				IXmlSchemaCompletionDataCollection schemaCompletionDataItems = XmlSchemaManager.SchemaCompletionDataItems;
				return new XmlCompletionDataProvider (schemaCompletionDataItems, defaultSchemaCompletionData, defaultNamespacePrefix, completionContext);
			case '>':
				//this is "optional" autocompletion of elements, so disable if fully automatic completion enabled
				if (!autoCompleteElements)
					return new ClosingBracketCompletionDataProvider (GetBuffer ());
				return null;
			default:
				return null;
			}
		}
		/*
		/// <summary>
		/// Gets the XmlSchemaObject that defines the currently selected xml element or
		/// attribute.
		/// </summary>
		/// <param name="text">The complete xml text.</param>
		/// <param name="index">The current cursor index.</param>
		/// <param name="provider">The completion data provider</param>
		/// <param name="currentSchemaCompletionData">This is the schema completion data for the
		/// schema currently being displayed. This can be null if the document is
		/// not a schema.</param>
		public static XmlSchemaObject GetSchemaObjectSelected(string xml, int index, XmlCompletionDataProvider provider, XmlSchemaCompletionData currentSchemaCompletionData)
		{
			// Find element under cursor.
			XmlElementPath path = XmlParser.GetActiveElementStartPathAtIndex(xml, index);
			string attributeName = XmlParser.GetAttributeNameAtIndex(xml, index);
			
			// Find schema definition object.
			XmlSchemaCompletionData schemaCompletionData = provider.FindSchema(path);
			XmlSchemaObject schemaObject = null;
			if (schemaCompletionData != null) {
				XmlSchemaElement element = schemaCompletionData.FindElement(path);
				schemaObject = element;
				if (element != null) {
					if (attributeName.Length > 0) {
						XmlSchemaAttribute attribute = schemaCompletionData.FindAttribute(element, attributeName);
						if (attribute != null) {
							if (currentSchemaCompletionData != null) {
								schemaObject = GetSchemaObjectReferenced(xml, index, provider, currentSchemaCompletionData, element, attribute);
							} else {
								schemaObject = attribute;
							}
						}
					}
					return schemaObject;
				}
			}	
			return null;
		}
		
		/// <summary>
		/// Gets the XmlSchemaObject that defines the currently selected xml element or
		/// attribute.
		/// </summary>
		/// <param name="text">The complete xml text.</param>
		/// <param name="index">The current cursor index.</param>
		/// <param name="provider">The completion data provider</param>
		public static XmlSchemaObject GetSchemaObjectSelected(string xml, int index, XmlCompletionDataProvider provider)
		{
			return GetSchemaObjectSelected(xml, index, provider, null);
		}*/
		

		
		#endregion
		
		#region Settings handling
		
		void SetDefaultSchema (string fileName)
		{
			if (fileName == null) {
				return;
			}
			string extension = System.IO.Path.GetExtension (fileName).ToLower ();
			defaultSchemaCompletionData = XmlSchemaManager.GetSchemaCompletionData (extension);
			defaultNamespacePrefix = XmlSchemaManager.GetNamespacePrefix (extension);
		}
		
		/// Updates the default schema association since the schema may have been added.
		void UserSchemaAdded (object source, EventArgs e)
		{	
			SetDefaultSchema (Document.Title);
		}
		
		// Updates the default schema association since the schema may have been removed.
		void UserSchemaRemoved (object source, EventArgs e)
		{
			SetDefaultSchema (Document.Title);
		}
		
		void XmlEditorPropertyChanged (object sender, PropertyChangedEventArgs args)
 		{
			switch (args.Key) {
			case "AutoCompleteElements":
				autoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;
				break;
			case "ShowSchemaAnnotation":
				showSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
				break;
			default:
				string extension = System.IO.Path.GetExtension (Document.Title).ToLower ();
				if (args.Key == extension) {
					SetDefaultSchema (Document.Title);
				} else {
					LoggingService.LogError ("Unhandled property change in XmlTextEditorExtension: " + args.Key);
				}
				break;
			}
		}
		
		void SetInitialValues()
		{
			showSchemaAnnotation = XmlEditorAddInOptions.ShowSchemaAnnotation;
			autoCompleteElements = XmlEditorAddInOptions.AutoCompleteElements;
			SetDefaultSchema (Document.Title);
		}
		
		#endregion
		
		#region Stylesheet handling
		/*
		/// <summary>
		/// Gets or sets the stylesheet associated with this xml file.
		/// </summary>
		public string StylesheetFileName {
			get { return stylesheetFileName; }
			set { stylesheetFileName = value; }
		}*/
				
		#endregion
		
		#region Filetype/schema detection		
		
		public bool IsSchema {
			get {
				if (Document.FileName != null) {
					string extension = System.IO.Path.GetExtension (Document.FileName);
					if (extension != null)
						return String.Compare (extension, ".xsd", true) == 0;
				}
				return false;
			}
		}
		
		/// <summary>
		/// Determines whether the file can be displayed by
		/// the xml editor.
		/// </summary>
		public static bool IsFileNameHandled (string fileName)
		{			
			if (fileName == null)
				return false;
			
			string vfsname = fileName.Replace ("%", "%25").Replace ("#", "%23").Replace ("?", "%3F");
			string mimeType = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (vfsname);
			if (IsMimeTypeHandled (mimeType))
				return true;
			
			return XmlFileExtensions.IsXmlFileExtension (System.IO.Path.GetExtension (fileName));
		}
		
		public static bool IsMimeTypeHandled (string mimeType)
		{
			return (mimeType != null && (mimeType == TextXmlMimeType || mimeType == ApplicationXmlMimeType));
		}
			
		#endregion
		
		#region From XmlEditorView
		/*
		/// <summary>
		/// Check the key is a valid char for an xml attribute value.
		/// We cannot just pass the EventKey.KeyValue to the
		/// Char.IsLetterOrDigit method since special keys, for
		/// example the cursor keys, map to valid letters.
		/// </summary>
		static bool IsXmlAttributeValueChar (uint keyValue)
		{
			if (keyValue >= 0 && keyValue <= 0xFD00) {
				return XmlParser.IsAttributeValueChar ((char) keyValue);
			}
			return false;
		}
		
		/// <summary>
		/// Checks whether the element belongs to the XSD namespace.
		/// </summary>
		static bool IsXmlSchemaNamespace (XmlSchemaElement element)
		{
			XmlQualifiedName qualifiedName = element.QualifiedName;
			if (qualifiedName != null) {
				return XmlSchemaManager.IsXmlSchemaNamespace (qualifiedName.Namespace);
			}
			return false;
		}
		
		/// <summary>
		/// If the attribute value found references another item in the schema
		/// return this instead of the attribute schema object. For example, if the
		/// user can select the attribute value and the code will work out the schema object pointed to by the ref
		/// or type attribute:
		///
		/// xs:element ref="ref-name"
		/// xs:attribute type="type-name"
		/// </summary>
		/// <returns>
		/// The <paramref name="attribute"/> if no schema object was referenced.
		/// </returns>
		static XmlSchemaObject GetSchemaObjectReferenced (string xml, int index, XmlCompletionDataProvider provider, XmlSchemaCompletionData currentSchemaCompletionData, XmlSchemaElement element, XmlSchemaAttribute attribute)
		{
			XmlSchemaObject schemaObject = null;
			if (IsXmlSchemaNamespace(element)) {
				// Find attribute value.
				string attributeValue = XmlParser.GetAttributeValueAtIndex(xml, index);
				if (attributeValue.Length == 0) {
					return attribute;
				}
		
				if (attribute.Name == "ref") {
					schemaObject = FindSchemaObjectReference(attributeValue, provider, currentSchemaCompletionData, element.Name);
				} else if (attribute.Name == "type") {
					schemaObject = FindSchemaObjectType(attributeValue, provider, currentSchemaCompletionData, element.Name);
				}
			}
			
			if (schemaObject != null) {
				return schemaObject;
			}
			return attribute;
		}
		
		/// <summary>
		/// Attempts to locate the reference name in the specified schema.
		/// </summary>
		/// <param name="name">The reference to look up.</param>
		/// <param name="schemaCompletionData">The schema completion data to use to
		/// find the reference.</param>
		/// <param name="elementName">The element to determine what sort of reference it is
		/// (e.g. group, attribute, element).</param>
		/// <returns><see langword="null"/> if no match can be found.</returns>
		static XmlSchemaObject FindSchemaObjectReference(string name, XmlCompletionDataProvider provider, XmlSchemaCompletionData schemaCompletionData, string elementName)
		{
			QualifiedName qualifiedName = schemaCompletionData.CreateQualifiedName(name);
			XmlSchemaCompletionData qualifiedNameSchema = provider.FindSchema(qualifiedName.Namespace);
			if (qualifiedNameSchema != null) {
				schemaCompletionData = qualifiedNameSchema;
			}
			switch (elementName) {
				case "element":
					return schemaCompletionData.FindElement(qualifiedName);
				case "attribute":
					return schemaCompletionData.FindAttribute(qualifiedName.Name);
				case "group":
					return schemaCompletionData.FindGroup(qualifiedName.Name);
				case "attributeGroup":
					return schemaCompletionData.FindAttributeGroup(qualifiedName.Name);
			}
			return null;
		}
		
		/// <summary>
		/// Attempts to locate the type name in the specified schema.
		/// </summary>
		/// <param name="name">The type to look up.</param>
		/// <param name="schemaCompletionData">The schema completion data to use to
		/// find the type.</param>
		/// <param name="elementName">The element to determine what sort of type it is
		/// (e.g. group, attribute, element).</param>
		/// <returns><see langword="null"/> if no match can be found.</returns>
		static XmlSchemaObject FindSchemaObjectType(string name, XmlCompletionDataProvider provider, XmlSchemaCompletionData schemaCompletionData, string elementName)
		{
			QualifiedName qualifiedName = schemaCompletionData.CreateQualifiedName(name);
			XmlSchemaCompletionData qualifiedNameSchema = provider.FindSchema(qualifiedName.Namespace);
			if (qualifiedNameSchema != null) {
				schemaCompletionData = qualifiedNameSchema;
			}
			switch (elementName) {
				case "element":
					return schemaCompletionData.FindComplexType(qualifiedName);
				case "attribute":
					return schemaCompletionData.FindSimpleType(qualifiedName.Name);
			}
			return null;
		}
		
		/// <summary>
		/// Checks whether the caret is inside a set of quotes (" or ').
		/// </summary>
		bool IsInsideQuotes()
		{
			// Get character at cursor.
			TextIter iter = Buffer.GetIterAtMark(Buffer.InsertMark);			
			string charAfter = iter.Char;
			
			// Get character before cursor
			if (!iter.BackwardChar()) {
				return false;
			}
			string charBefore = iter.Char;
				
			if (((charBefore == "\'") && (charAfter == "\'")) ||
				((charBefore == "\"") && (charAfter == "\""))) {
				return true;
			}
			return false;
		}			
		*/
		#endregion
		
		#region Smart indent
		
		public override bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			bool result;
			
			if ((char)(uint)key == '>' && autoCompleteElements) {
				result = base.KeyPress (key, modifier);
				string autoClose = ClosingBracketCompletionDataProvider.GetAutoCloseElement (GetBuffer ());
				if (autoClose != null) {
					Editor.InsertText (Editor.CursorPosition, autoClose);
					Editor.CursorPosition -= autoClose.Length;
				}
				return result;
			}
			
			if (TextEditorProperties.IndentStyle == IndentStyle.Smart && key == Gdk.Key.Return) {
				result = base.KeyPress (key, modifier);
				SmartIndentLine (Editor.CursorLine);
				return result;
			}
			return base.KeyPress (key, modifier);
		}
		
		void SmartIndentLine (int line)
		{
			//FIXME: implement this
		}
		
		#endregion
		
	}
}
