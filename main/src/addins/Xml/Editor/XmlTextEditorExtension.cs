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
using System.Linq;
using System.Xml;
using System.Xml.Schema;

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Xml.Completion;
using MonoDevelop.Xml.Dom;
using MonoDevelop.Xml.Parser;
using MonoDevelop.Ide.Editor.Extension;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Xml.Editor
{
	public class XmlTextEditorExtension : BaseXmlEditorExtension
	{
		const string TextXmlMimeType = "text/xml";
		const string ApplicationXmlMimeType = "application/xml";
		string stylesheetFileName;
		XmlSchemaCompletionData defaultSchemaCompletionData;
		string defaultNamespacePrefix;
		InferredXmlCompletionProvider inferredCompletionData;
		bool inferenceQueued;

		public override bool IsValidInContext (DocumentContext context)
		{
			return IsFileNameHandled (context.Name) && base.IsValidInContext (context);
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();
			XmlEditorOptions.XmlFileAssociationChanged += HandleXmlFileAssociationChanged;
			XmlSchemaManager.UserSchemaAdded += UserSchemaAdded;
			XmlSchemaManager.UserSchemaRemoved += UserSchemaRemoved;
			SetDefaultSchema ();

			if (string.IsNullOrEmpty (Editor.MimeType) || Editor.MimeType == "text/plain" || Editor.MimeType == "application/octet-stream") {
				Editor.MimeType = ApplicationXmlMimeType;
				DocumentContext.ReparseDocument ();
			}
		}

		void HandleXmlFileAssociationChanged (object sender, XmlFileAssociationChangedEventArgs e)
		{
			var filename = DocumentContext.Name;
			if (filename != null && filename.ToString ().EndsWith (e.Extension, StringComparison.Ordinal))
				SetDefaultSchema ();
		}
		
		bool disposed;
		public override void Dispose()
		{
			if (!disposed) {
				disposed = false;
				XmlEditorOptions.XmlFileAssociationChanged -= HandleXmlFileAssociationChanged;
				XmlSchemaManager.UserSchemaAdded -= UserSchemaAdded;

				XmlSchemaManager.UserSchemaRemoved -= UserSchemaRemoved;
				ClearTasksForStandaloneXmlFile ();
				base.Dispose ();
			}
		}

		void ClearTasksForStandaloneXmlFile ()
		{
			var tasks = TaskService.Errors
				.GetOwnerTasks (this)
				.Where (t => t.WorkspaceObject == null)
				.ToList ();

			if (tasks.Any ())
				TaskService.Errors.RemoveRange (tasks);
		}

		#region Code completion

		XmlElementPath GetElementPath ()
		{
			return XmlElementPath.Resolve (
				GetCurrentPath (),
				defaultSchemaCompletionData != null? defaultSchemaCompletionData.NamespaceUri : null,
				defaultNamespacePrefix ?? ""
			);
		}
		
		protected override async Task<CompletionDataList> GetElementCompletions (CancellationToken token)
		{
			CompletionDataList list = null;
			var path = GetElementPath ();

			if (path.Elements.Count > 0) {
				IXmlCompletionProvider schema = FindSchema (path);
				if (schema == null)
					schema = inferredCompletionData;

				if (schema != null) {

					var completionData = await schema.GetChildElementCompletionData (path, token);
					if (completionData != null)
						list = completionData;
				}

			} else if (defaultSchemaCompletionData != null) {
				list = await defaultSchemaCompletionData.GetElementCompletionData (defaultNamespacePrefix, token);

			} else if (inferredCompletionData != null) {
				list = await inferredCompletionData.GetElementCompletionData (token);
			}
			if (list == null) {
				list = new CompletionDataList ();
			}
			AddMiscBeginTags (list);
			return list;
		}
		
		protected override Task<CompletionDataList> GetAttributeCompletions (IAttributedXObject attributedOb,
		                                                               Dictionary<string, string> existingAtts, CancellationToken token)
		{
			var path = GetElementPath ();

			if (path.Elements.Count > 0) {
				IXmlCompletionProvider schema = FindSchema (path);
				if (schema == null)
					schema = inferredCompletionData;

				if (schema != null)
					return schema.GetAttributeCompletionData (path, token);
			}
			return Task.FromResult (new CompletionDataList ());
		}
		
		protected override Task<CompletionDataList> GetAttributeValueCompletions (IAttributedXObject attributedOb, XAttribute att, CancellationToken token)
		{
			var path = GetElementPath ();

			if (path.Elements.Count > 0) {
				var schema = FindSchema (path);

				if (schema != null)
					return schema.GetAttributeValueCompletionData (path, att.Name.FullName, token);
			}
			return Task.FromResult (new CompletionDataList ());
		}
		
		#endregion
		
		#region From XmlCompletionDataProvider.cs
		
		public XmlSchemaCompletionData FindSchemaFromFileName (string fileName)
		{
			return XmlSchemaManager.SchemaCompletionDataItems.GetSchemaFromFileName (fileName);
		}
		
		public XmlSchemaCompletionData FindSchema (string namespaceUri)
		{
			return XmlSchemaManager.SchemaCompletionDataItems[namespaceUri];
		}
		
		public XmlSchemaCompletionData FindSchema (XmlElementPath path)
		{
			return FindSchema (XmlSchemaManager.SchemaCompletionDataItems, path);
		}
		
		/// <summary>

		/// Finds the schema given the xml element path.

		/// </summary>

		public XmlSchemaCompletionData FindSchema (IXmlSchemaCompletionDataCollection schemaCompletionDataItems, XmlElementPath path)

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
		
		#endregion
		
		#region Schema resolution
		
		/// <summary>

		/// Gets the XmlSchemaObject that defines the currently selected xml element or attribute.

		/// </summary>

		/// <param name="currentSchemaCompletionData">This is the schema completion data for the schema currently being 
		/// displayed. This can be null if the document is not a schema.</param>

		public XmlSchemaObject GetSchemaObjectSelected (XmlSchemaCompletionData currentSchemaCompletionData)

		{

			// Find element under cursor.

			XmlElementPath path = GetElementPath ();
			
			//attribute name under cursor, if valid

			string attributeName = null;
			XAttribute xatt = Tracker.Engine.Nodes.Peek (0) as XAttribute;
			if (xatt != null) {
				XName xattName = xatt.Name;
				if (Tracker.Engine.CurrentState is XmlNameState) {
					xattName = GetCompleteName ();
				}
				attributeName = xattName.FullName;
			}

			
			// Find schema definition object.

			XmlSchemaCompletionData schemaCompletionData = FindSchema (path);

			XmlSchemaObject schemaObject = null;

			if (schemaCompletionData != null) {
				XmlSchemaElement element = schemaCompletionData.FindElement(path);

				schemaObject = element;

				if (element != null) {
					if (!string.IsNullOrEmpty (attributeName)) {
						XmlSchemaAttribute attribute = schemaCompletionData.FindAttribute(element, attributeName);

						if (attribute != null) {
							if (currentSchemaCompletionData != null) {

								schemaObject = GetSchemaObjectReferenced (currentSchemaCompletionData, element, attribute);

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

		XmlSchemaObject GetSchemaObjectReferenced (XmlSchemaCompletionData currentSchemaCompletionData, XmlSchemaElement element, XmlSchemaAttribute attribute)

		{

			XmlSchemaObject schemaObject = null;

			if (IsXmlSchemaNamespace(element)) {

				// Find attribute value.
				//fixme implement

				string attributeValue = "";// XmlParser.GetAttributeValueAtIndex(xml, index);

				if (attributeValue.Length == 0) {

					return attribute;

				}

		

				if (attribute.Name == "ref") {

					schemaObject = FindSchemaObjectReference(attributeValue, currentSchemaCompletionData, element.Name);

				} else if (attribute.Name == "type") {

					schemaObject = FindSchemaObjectType(attributeValue, currentSchemaCompletionData, element.Name);

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

		XmlSchemaObject FindSchemaObjectReference(string name, XmlSchemaCompletionData schemaCompletionData, string elementName)

		{

			QualifiedName qualifiedName = schemaCompletionData.CreateQualifiedName(name);

			XmlSchemaCompletionData qualifiedNameSchema = FindSchema(qualifiedName.Namespace);

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

		XmlSchemaObject FindSchemaObjectType(string name, XmlSchemaCompletionData schemaCompletionData, string elementName)

		{

			QualifiedName qualifiedName = schemaCompletionData.CreateQualifiedName(name);

			XmlSchemaCompletionData qualifiedNameSchema = FindSchema(qualifiedName.Namespace);

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
		
		void SetDefaultSchema ()

		{
			var filename = DocumentContext.Name;
			if (filename == null)
				return;
			

			defaultSchemaCompletionData = XmlSchemaManager.GetSchemaCompletionDataForFileName (filename);
			if (defaultSchemaCompletionData != null)
				inferredCompletionData = null;
			else
				QueueInference ();

			defaultNamespacePrefix = XmlSchemaManager.GetNamespacePrefixForFileName (filename);

		}

		

		/// Updates the default schema association since the schema may have been added.

		void UserSchemaAdded (object source, EventArgs e)

		{	
			SetDefaultSchema ();
		}

		

		// Updates the default schema association since the schema may have been removed.

		void UserSchemaRemoved (object source, EventArgs e)

		{

			SetDefaultSchema ();

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
					return String.Compare (extension, ".xsd", StringComparison.OrdinalIgnoreCase) == 0;
				return false;
			}
		}
		
		/// <summary>
		/// Determines whether the file can be displayed by
		/// the xml editor.
		/// </summary>
		static bool IsFileNameHandled (string fileName)
		{
			if (string.IsNullOrEmpty (fileName))
				return false;

			string mimeType = DesktopService.GetMimeTypeForUri (fileName);
			if (IsMimeTypeHandled (mimeType))
				return true;
			
			return XmlFileAssociationManager.IsXmlFileName (fileName);
		}
		
		static bool IsMimeTypeHandled (string mimeType)
		{
			foreach (var m in DesktopService.GetMimeTypeInheritanceChain (mimeType)) {
				if (m == TextXmlMimeType || m == ApplicationXmlMimeType)
					return true;
			}
			return false;
		}
			
		#endregion
		
		#region Smart indent
		
		public override bool KeyPress (KeyDescriptor descriptor)
		{
			bool result;
			

			if (Editor.Options.IndentStyle == IndentStyle.Smart && descriptor.SpecialKey == SpecialKey.Return) {
				result = base.KeyPress (descriptor);
				SmartIndentLine (Editor.CaretLine);
				return result;
			}
			return base.KeyPress (descriptor);
		}
		
		void SmartIndentLine (int line)
		{
			//FIXME: implement this
		}
		
//		string GetLineIndent (int line)
//		{
//			string indent = string.Empty;
//			int start = Editor.GetPositionFromLineColumn (line, 1);
//			int i = start;
//			while (i < Editor.TextLength) {
//				char c = Editor.GetCharAt (i);
//				if (c == '\n' || c == '\r')
//					break;
//				if (!char.IsWhiteSpace (c))
//					break;
//				i++;
//			}
//			if (i > 0)
//				indent = Editor.GetText (start, i);
//			return indent;
//		}
		
		//gets the indent of the line containing this position, up to the position index
		string GetPositionIndent (int position)
		{
			int indentEnd = position;
			int i = position - 1;
			while (i > 0) {
				char c = Editor.GetCharAt (i);
				if (c == '\n' || c == '\r')
					return Editor.GetTextBetween (i + 1, indentEnd);
				if (!char.IsWhiteSpace (c))
					indentEnd--;
				i--;
			}
			return null;
		}
		
		#endregion
		
		#region Command handlers

		[CommandHandler (XmlCommands.CreateSchema)]
		public void CreateSchemaCommand ()
		{
			try {

				TaskService.Errors.Clear ();

				string xml = Editor.Text;
				using (ProgressMonitor monitor = XmlEditorService.GetMonitor ()) {
					XmlDocument doc = XmlEditorService.ValidateWellFormedness (monitor, xml, FileName, DocumentContext.Project);
					if (doc == null)
						return;
					monitor.BeginTask (GettextCatalog.GetString ("Creating schema..."), 0);
					try {
						string schema = XmlEditorService.CreateSchema (Editor, xml);

						string fileName = XmlEditorService.GenerateFileName (FileName, "{0}.xsd");
						IdeApp.Workbench.NewDocument (fileName, "application/xml", schema);
						monitor.ReportSuccess (GettextCatalog.GetString ("Schema created."));
					} catch (Exception ex) {
						string msg = GettextCatalog.GetString ("Error creating XML schema.");
						LoggingService.LogError (msg, ex);
						monitor.ReportError (msg, ex);
					}
				}

			} catch (Exception ex) {

				MessageService.ShowError (ex.Message);

			}
		}
		
		[CommandHandler (XmlCommands.OpenStylesheet)]
		public void OpenStylesheetCommand ()
		{

			if (!string.IsNullOrEmpty (stylesheetFileName)) {

				try {

					IdeApp.Workbench.OpenDocument (stylesheetFileName, DocumentContext.Project);

				} catch (Exception ex) {
					LoggingService.LogError ("Could not open document.", ex);
					MessageService.ShowError ("Could not open document.", ex);
				}
			}
		}
		
		[CommandUpdateHandler (XmlCommands.OpenStylesheet)]
		public void UpdateOpenStylesheetCommand (CommandInfo info)
		{
			info.Enabled = !string.IsNullOrEmpty (stylesheetFileName);
		}
		
		[CommandHandler (XmlCommands.GoToSchemaDefinition)]
		public void GoToSchemaDefinitionCommand ()
		{
			try {
				//try to resolve the schema

				XmlSchemaCompletionData currentSchemaCompletionData = FindSchemaFromFileName (FileName);						

				XmlSchemaObject schemaObject = GetSchemaObjectSelected (currentSchemaCompletionData);

				

				// Open schema if resolved

				if (schemaObject != null && schemaObject.SourceUri != null && schemaObject.SourceUri.Length > 0) {

					string schemaFileName = schemaObject.SourceUri.Replace ("file:/", String.Empty);
					IdeApp.Workbench.OpenDocument (
					    schemaFileName,
						DocumentContext.Project,
					    Math.Max (1, schemaObject.LineNumber),
					    Math.Max (1, schemaObject.LinePosition));
				}
			} catch (Exception ex) {
				MonoDevelop.Core.LoggingService.LogError ("Could not open document.", ex);
				MessageService.ShowError ("Could not open document.", ex);
			}
		}
		
		[CommandHandler (XmlCommands.Validate)]
		public void ValidateCommand ()
		{
			TaskService.Errors.Clear ();
			using (ProgressMonitor monitor = XmlEditorService.GetMonitor()) {
				if (IsSchema)
					XmlEditorService.ValidateSchema (monitor, Editor.Text, FileName, DocumentContext.Project);
				else
					XmlEditorService.ValidateXml (monitor, Editor.Text, FileName, DocumentContext.Project);
			}
		}
		
		[CommandHandler (XmlCommands.AssignStylesheet)]
		public void AssignStylesheetCommand ()
		{
			// Prompt user for filename.

			string fileName = XmlEditorService.BrowseForStylesheetFile ();

			if (!string.IsNullOrEmpty (stylesheetFileName))

				stylesheetFileName = fileName;
		}
		
		[CommandHandler (XmlCommands.RunXslTransform)]
		public void RunXslTransformCommand ()
		{
			if (string.IsNullOrEmpty (stylesheetFileName)) {

				stylesheetFileName = XmlEditorService.BrowseForStylesheetFile ();

				if (string.IsNullOrEmpty (stylesheetFileName))
					return;
			}
			
			using (ProgressMonitor monitor = XmlEditorService.GetMonitor()) {
				try {
					string xsltContent;
					try {
						xsltContent = GetFileContent (stylesheetFileName);	
					} catch (System.IO.IOException) {
						monitor.ReportError (
						    GettextCatalog.GetString ("Error reading file '{0}'.", stylesheetFileName), null);
						return;
					}
					System.Xml.Xsl.XslCompiledTransform xslt = 
						XmlEditorService.ValidateStylesheet (monitor, xsltContent, stylesheetFileName, DocumentContext.Project);
					if (xslt == null)
						return;
					
					XmlDocument doc = XmlEditorService.ValidateXml (monitor, Editor.Text, FileName, DocumentContext.Project);
					if (doc == null)
						return;
					
					string newFileName = XmlEditorService.GenerateFileName (FileName, "-transformed{0}.xml");
					
					monitor.BeginTask (GettextCatalog.GetString ("Executing transform..."), 1);
					using (XmlTextWriter output = XmlEditorService.CreateXmlTextWriter(Editor)) {
						xslt.Transform (doc, null, output);
						IdeApp.Workbench.NewDocument (
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
			var tf =
				MonoDevelop.Ide.TextFileProvider.Instance.GetReadOnlyTextEditorData (fileName);
 			if (tf != null)
				return tf.Text;

			System.IO.StreamReader reader = new System.IO.StreamReader (fileName, true);

			return reader.ReadToEnd();

		}
		
		#endregion

		void QueueInference ()
		{
			var doc = CU as XmlParsedDocument;
			if (defaultSchemaCompletionData != null || doc == null || doc.XDocument == null || inferenceQueued)
				return;
			if (inferredCompletionData == null
			    || (doc.LastWriteTimeUtc - inferredCompletionData.TimeStampUtc).TotalSeconds >= 5
					&& doc.GetErrorsAsync(false).Result.Count <= inferredCompletionData.ErrorCount)
			{
				inferenceQueued = true;
				System.Threading.ThreadPool.QueueUserWorkItem (delegate {
					try {
						InferredXmlCompletionProvider newData = new InferredXmlCompletionProvider ();
						newData.Populate (doc.XDocument);
						newData.TimeStampUtc = DateTime.UtcNow;
						newData.ErrorCount = doc.GetErrorsAsync(false).Result.Count;
						this.inferenceQueued = false;
						this.inferredCompletionData = newData;
					} catch (Exception ex) {
						LoggingService.LogInternalError ("Unhandled error in XML inference", ex);
					}
				});
			}	
		}
		
		protected override void OnParsedDocumentUpdated ()
		{
			QueueInference ();
			base.OnParsedDocumentUpdated ();
		}
	}
}