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
using MonoDevelop.Components.Commands;
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
			return IsFileNameHandled (string.IsNullOrEmpty (doc.FileName)? doc.Title : doc.FileName );
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			XmlEditorAddInOptions.PropertyChanged += XmlEditorPropertyChanged;
			XmlSchemaManager.UserSchemaAdded += UserSchemaAdded;
			XmlSchemaManager.UserSchemaRemoved += UserSchemaRemoved;
			SetInitialValues();
			
/* FIXME: this causes build breakage
			MonoDevelop.SourceEditor.SourceEditorView view = 
				Document.GetContent<MonoDevelop.SourceEditor.SourceEditorView> ();
			if (view != null && view.Document.SyntaxMode == null) {
				Mono.TextEditor.Highlighting.SyntaxMode mode = Mono.TextEditor.Highlighting.SyntaxModeService.GetSyntaxMode (ApplicationXmlMimeType);
				if (mode != null)
					view.Document.SyntaxMode = mode;
				else
					LoggingService.LogWarning ("XmlTextEditorExtension could not get SyntaxMode for mimetype '" + ApplicationXmlMimeType + "'.");
			}
*/
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
		
		#endregion
		
		#region Schema resolution
		
		/// <summary>
		/// Gets the XmlSchemaObject that defines the currently selected xml element or attribute.
		/// </summary>
		/// <param name="text">The complete xml text.</param>
		/// <param name="index">The current cursor index.</param>
		/// <param name="provider">The completion data provider</param>
		/// <param name="currentSchemaCompletionData">This is the schema completion data for the schema currently being 
		/// displayed. This can be null if the document is not a schema.</param>
		public static XmlSchemaObject GetSchemaObjectSelected (string xml, int index, 
		    XmlCompletionDataProvider provider, XmlSchemaCompletionData currentSchemaCompletionData)
		{
			// Find element under cursor.
			XmlElementPath path = XmlParser.GetActiveElementStartPathAtIndex (xml, index);
			string attributeName = XmlParser.GetAttributeNameAtIndex (xml, index);
			
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
		
		/// <summary>
		/// Gets or sets the stylesheet associated with this xml file.
		/// </summary>
		public string StylesheetFileName {
			get { return stylesheetFileName; }
			set { stylesheetFileName = value; }
		}
						
		#endregion
		
		#region Filetype/schema detection		
		
		public bool IsSchema {
			get {
				string extension = System.IO.Path.GetExtension (FileName);
				if (extension != null)
					return String.Compare (extension, ".xsd", true) == 0;
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
			
			if (System.IO.Path.IsPathRooted (fileName)) {
				string vfsname = fileName.Replace ("%", "%25").Replace ("#", "%23").Replace ("?", "%3F");
				string mimeType = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (vfsname);
				if (IsMimeTypeHandled (mimeType))
					return true;
			}
			
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
		/// Gets the XmlSchemaObject that defines the currently selected xml element or
		/// attribute.
		/// </summary>
		/// <param name="text">The complete xml text.</param>
		/// <param name="index">The current cursor index.</param>
		/// <param name="provider">The completion data provider</param>
		public static XmlSchemaObject GetSchemaObjectSelected(string xml, int index, XmlCompletionDataProvider provider)
		{
			return GetSchemaObjectSelected(xml, index, provider, null);
		}
		
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
		
		string GetLineIndent (int line)
		{
			string indent = string.Empty;
			int start = Editor.GetPositionFromLineColumn (line, 1);
			int i = start;
			while (i < Editor.TextLength) {
				char c = Editor.GetCharAt (i);
				if (c == '\n' || c == '\r')
					break;
				if (!char.IsWhiteSpace (c))
					break;
				i++;
			}
			if (i > 0)
				indent = Editor.GetText (start, i);
			return indent;
		}
		
		//gets the indent of the line containing this position, up to the position index
		string GetPositionIndent (int position)
		{
			int indentEnd = position;
			int i = position - 1;
			while (i > 0) {
				char c = Editor.GetCharAt (i);
				if (c == '\n' || c == '\r')
					return Editor.GetText (i + 1, indentEnd);
				if (!char.IsWhiteSpace (c))
					indentEnd--;
				i--;
			}
			return null;
		}
		
		#endregion
		
		#region Command handlers
		
		[CommandUpdateHandler (MonoDevelop.Ide.Commands.EditCommands.ToggleCodeComment)]
		protected void ToggleCodeCommentCommandUpdate (CommandInfo info)
		{
			info.Enabled = false;
		}
		
		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.ToggleCodeComment)]
		public void ToggleCodeCommentCommand ()
		{
			//FIXME: implement
		}
		
		[CommandHandler (Commands.Format)]
		public void FormatCommand ()
		{
			MonoDevelop.Ide.Gui.IdeApp.Services.TaskService.ClearExceptCommentTasks ();
			
			using (IProgressMonitor monitor = XmlEditorService.GetMonitor ()) {
				bool selection = (Editor.SelectionEndPosition - Editor.SelectionStartPosition) > 0;
				string xml = selection? Editor.SelectedText : Editor.Text;
				XmlDocument doc = XmlEditorService.ValidateWellFormedness (monitor, xml, FileName);
				if (doc == null)
					return;
				
				//if there's a line indent at the current location, prepend that to all new lines
				string extraIndent = null;
				if (selection)
					extraIndent = GetPositionIndent (Editor.SelectionStartPosition);
				
				string formattedXml = XmlEditorService.IndentedFormat (xml);
				
				//convert newlines and prepend extra indents to each line if needed
				bool nonNativeNewline = (Editor.NewLine != Environment.NewLine);
				bool hasExtraIndent = !string.IsNullOrEmpty (extraIndent);
				if (hasExtraIndent || nonNativeNewline) {
					System.Text.StringBuilder builder = new System.Text.StringBuilder (formattedXml);
					
					if (nonNativeNewline)
						builder.Replace (Environment.NewLine, Editor.NewLine);
					
					if (hasExtraIndent) {
						builder.Replace (Editor.NewLine, Editor.NewLine + extraIndent);
						if (formattedXml.EndsWith (Environment.NewLine))
							builder.Remove (builder.Length - 1 - extraIndent.Length, extraIndent.Length);
					}
					formattedXml = builder.ToString ();
				}
				
				Editor.BeginAtomicUndo ();
				if (selection) {
					Editor.SelectedText = formattedXml;
				} else {
					Editor.DeleteText (0, Editor.TextLength);
					Editor.InsertText (0, formattedXml);
				}
				Editor.EndAtomicUndo ();
			}
		}
		
		[CommandHandler (Commands.CreateSchema)]
		public void CreateSchemaCommand ()
		{
			try {
				MonoDevelop.Ide.Gui.IdeApp.Services.TaskService.ClearExceptCommentTasks ();
				string xml = Editor.Text;
				using (IProgressMonitor monitor = XmlEditorService.GetMonitor ()) {
					XmlDocument doc = XmlEditorService.ValidateWellFormedness (monitor, xml, FileName);
					if (doc == null)
						return;
					monitor.BeginTask (GettextCatalog.GetString ("Creating schema..."), 0);
					try {
						string schema = XmlEditorService.CreateSchema (xml);
						string fileName = XmlEditorService.GenerateFileName (FileName, "{0}.xsd");
						MonoDevelop.Ide.Gui.IdeApp.Workbench.NewDocument (fileName, "application/xml", schema);
						monitor.ReportSuccess (GettextCatalog.GetString ("Schema created."));
					} catch (Exception ex) {
						string msg = GettextCatalog.GetString ("Error creating XML schema.");
						LoggingService.LogError (msg, ex);
						monitor.ReportError (msg, ex);
					}
				}
			} catch (Exception ex) {
				MonoDevelop.Core.Gui.MessageService.ShowError(ex.Message);
			}
		}
		
		[CommandHandler (Commands.OpenStylesheet)]
		public void OpenStylesheetCommand ()
		{
			if (!string.IsNullOrEmpty (stylesheetFileName)) {
				try {
					MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (stylesheetFileName);
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError ("Could not open document.", ex);
					MonoDevelop.Core.Gui.MessageService.ShowException (ex, "Could not open document.");
				}
			}
		}
		
		[CommandUpdateHandler (Commands.OpenStylesheet)]
		public void UpdateOpenStylesheetCommand (CommandInfo info)
		{
			info.Enabled = !string.IsNullOrEmpty (stylesheetFileName);
		}
		
		[CommandHandler (Commands.GoToSchemaDefinition)]
		public void GoToSchemaDefinitionCommand ()
		{
			try {
				//try to resolve the schema
				ICompletionWidget completionWidget = Document.GetContent <ICompletionWidget> ();
				XmlCompletionDataProvider provider = new XmlCompletionDataProvider (
				    XmlSchemaManager.SchemaCompletionDataItems,
				    defaultSchemaCompletionData,
				    defaultNamespacePrefix,
				    completionWidget.CreateCodeCompletionContext (Editor.CursorPosition));
				XmlSchemaCompletionData currentSchemaCompletionData = provider.FindSchemaFromFileName (FileName);						
				XmlSchemaObject schemaObject = GetSchemaObjectSelected (
				    Editor.Text, Editor.CursorPosition, provider, currentSchemaCompletionData);
				
				// Open schema if resolved
				if (schemaObject != null && schemaObject.SourceUri != null && schemaObject.SourceUri.Length > 0) {
					string schemaFileName = schemaObject.SourceUri.Replace ("file:/", String.Empty);
					MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (
					    schemaFileName,
					    Math.Max (1, schemaObject.LineNumber),
					    Math.Max (1, schemaObject.LinePosition), true);
				}
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not open document.", ex);
				MonoDevelop.Core.Gui.MessageService.ShowException (ex, "Could not open document.");
			}
		}
		
		[CommandHandler (Commands.Validate)]
		public void ValidateCommand ()
		{
			MonoDevelop.Ide.Gui.IdeApp.Services.TaskService.ClearExceptCommentTasks ();
			using (IProgressMonitor monitor = XmlEditorService.GetMonitor()) {
				if (IsSchema)
					XmlEditorService.ValidateSchema (monitor, Editor.Text, FileName);
				else
					XmlEditorService.ValidateXml (monitor, Editor.Text, FileName);
			}
		}
		
		[CommandHandler (Commands.AssignStylesheet)]
		public void AssignStylesheetCommand ()
		{
			// Prompt user for filename.
			string fileName = XmlEditorService.BrowseForStylesheetFile ();
			if (!string.IsNullOrEmpty (stylesheetFileName))
				stylesheetFileName = fileName;
		}
		
		[CommandHandler (Commands.RunXslTransform)]
		public void RunXslTransformCommand ()
		{
			if (string.IsNullOrEmpty (stylesheetFileName)) {
				stylesheetFileName = XmlEditorService.BrowseForStylesheetFile ();
				if (string.IsNullOrEmpty (stylesheetFileName))
					return;
			}
			
			using (IProgressMonitor monitor = XmlEditorService.GetMonitor()) {
				try {
					string xsltContent;
					try {
						xsltContent = GetFileContent (stylesheetFileName);	
					} catch (System.IO.IOException) {
						monitor.ReportError (
						    GettextCatalog.GetString ("Error reading file '{0}'.", stylesheetFileName), null);
						return;
					}
					System.Xml.Xsl.XslTransform xslt = 
						XmlEditorService.ValidateStylesheet (monitor, xsltContent, stylesheetFileName);
					if (xslt == null)
						return;
					
					XmlDocument doc = XmlEditorService.ValidateXml (monitor, Editor.Text, FileName);
					if (doc == null)
						return;
					
					string newFileName = XmlEditorService.GenerateFileName (FileName, "-transformed{0}.xml");
					
					monitor.BeginTask (GettextCatalog.GetString ("Executing transform..."), 1);
					using (XmlTextWriter output = XmlEditorService.CreateXmlTextWriter()) {
						xslt.Transform (doc, null, output);
						MonoDevelop.Ide.Gui.IdeApp.Workbench.NewDocument (
						    newFileName, "application/xml", output.ToString ());
					}
					monitor.ReportSuccess (GettextCatalog.GetString ("Transform completed."));
					monitor.EndTask ();
				} catch (Exception ex) {
					string msg = GettextCatalog.GetString ("Could not run transform.");
					monitor.ReportError (msg, ex);
					monitor.EndTask ();
				}
			}
		}
		
		string GetFileContent (string fileName)
		{
			MonoDevelop.Projects.Text.IEditableTextFile tf =
				MonoDevelop.DesignerSupport.OpenDocumentFileProvider.Instance.GetEditableTextFile (fileName);
			if (tf != null)
				return tf.Text;
			System.IO.StreamReader reader = new System.IO.StreamReader (fileName, true);
			return reader.ReadToEnd();
		}
		
		
		
		#endregion
		
	}
}
