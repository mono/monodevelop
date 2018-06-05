//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Xsl;

using MonoDevelop.Components;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Projects;
using MonoDevelop.Xml.Completion;

namespace MonoDevelop.Xml.Editor
{
	static class XmlEditorService
	{
		public static ProgressMonitor GetMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ("XML", "md-xml-file-icon", false, true);
		}
		
		/// <summary>
		/// Creates a XmlTextWriter using the current text editor
		/// properties for indentation.
		/// </summary>
		public static XmlTextWriter CreateXmlTextWriter (TextEditor doc, TextWriter textWriter)
		{
			var xmlWriter = new XmlTextWriter (textWriter) {
				Formatting = System.Xml.Formatting.Indented
			};
			if (doc.Options.TabsToSpaces) {
				xmlWriter.Indentation = doc.Options.TabSize;
				xmlWriter.IndentChar = ' ';
			} else {
				xmlWriter.Indentation = 1;
				xmlWriter.IndentChar = '\t';
			}
			return xmlWriter;
		}
		
		public static string CreateSchema (TextEditor doc, string xml)
		{
			using (var dataSet = new System.Data.DataSet()) {
				dataSet.ReadXml(new StringReader (xml), System.Data.XmlReadMode.InferSchema);
				using (var writer = new EncodedStringWriter (Encoding.UTF8)) {
					using (var xmlWriter = CreateXmlTextWriter (doc, writer)) {
						dataSet.WriteXmlSchema(xmlWriter);
						return writer.ToString();
					}
				}
			}
		}
		
		public static string GenerateFileName (string sourceName, string extensionFormat)
		{
			return GenerateFileName (
			    Path.Combine (Path.GetDirectoryName (sourceName), Path.GetFileNameWithoutExtension (sourceName)) + 
			    extensionFormat);
		}
		
		// newNameFormat should be a string format for the new filename such as 
		// "/some/path/oldname{0}.xsd", where {0} is the index that will be incremented until a
		// non-existing file is found.
		public static string GenerateFileName (string newNameFormat)
		{
			string generatedFilename = string.Format (newNameFormat, "");
			int count = 1;
			while (File.Exists (generatedFilename)) {
				generatedFilename = string.Format (newNameFormat, count);
				++count;
			}
			return generatedFilename;
		}

		#region Validation

		public static Task<List<BuildError>> Validate (string xml, string fileName, CancellationToken token)
		{
			switch (Path.GetExtension (fileName).ToUpperInvariant ()) {
			case ".XSD":
				return ValidateSchema (xml, fileName, token);
			default:
				return ValidateXml (xml, fileName, token);
			}
		}

		static XmlReaderSettings CreateValidationSettings (List<BuildError> errors, string filename)
		{
			void validationCallback (object _, ValidationEventArgs args)
			{
				errors.Add (CreateBuildError (args, filename));
			}

			var settings = new XmlReaderSettings {
				ValidationFlags =
					XmlSchemaValidationFlags.ProcessIdentityConstraints
					| XmlSchemaValidationFlags.ProcessInlineSchema
					| XmlSchemaValidationFlags.ProcessSchemaLocation
					| XmlSchemaValidationFlags.ReportValidationWarnings,
				ValidationType = ValidationType.Schema,
				DtdProcessing = DtdProcessing.Parse,
				XmlResolver = new LocalOnlyXmlResolver ()
			};

			settings.ValidationEventHandler += validationCallback;

			return settings;
		}
		
		/// <summary>
		/// Validates the xml against known schemas.
		/// </summary>		
		public static async Task<List<BuildError>> ValidateXml (string xml, string filename, CancellationToken token)
		{
			var errors = new List<BuildError> ();
			var settings = CreateValidationSettings (errors, filename);

			try {
				settings.Schemas = await XmlSchemaManager.GetSchemaSet (token);
				using (var reader = XmlReader.Create (new StringReader (xml), settings)) {
					while (!reader.EOF)
						reader.Read ();
				}
			} catch (XmlSchemaException ex) {
				errors.Add (CreateBuildError (ex, filename));
			}
			catch (XmlException ex) {
				errors.Add (CreateBuildError (ex, filename));
			}

			return errors;
		}
		
		/// <summary>
		/// Validates the schema.
		/// </summary>		
		public static async Task<List<BuildError>> ValidateSchema (string xml, string filename, CancellationToken token)
		{
			var errors = new List<BuildError> ();
			var settings = new XmlReaderSettings {
				XmlResolver = new LocalOnlyXmlResolver ()
			};

			void validationCallback (object _, ValidationEventArgs args)
			{
				errors.Add (CreateBuildError (args, filename));
			}

			try {
				XmlSchema schema;
				using (var xmlReader = XmlReader.Create (new StringReader (xml))) {
					schema = XmlSchema.Read (xmlReader, validationCallback);
				}

				var sset = new XmlSchemaSet ();
				sset.ValidationEventHandler += validationCallback;

				foreach (XmlSchema s in (await XmlSchemaManager.GetSchemaSet (token)).Schemas ()) {
					if (s.TargetNamespace != schema.TargetNamespace) {
						sset.Add (schema);
					}
				}
				sset.Compile ();
			} 
			catch (XmlSchemaException ex) {
				errors.Add (CreateBuildError (ex, filename));
			}
			catch (XmlException ex) {
				errors.Add (CreateBuildError (ex, filename));
			}

			return errors;
		}
		
		public static (XslCompiledTransform transform, BuildError error) CompileStylesheet (string xml, string fileName)
		{
			try {
				using (var reader = XmlReader.Create (new StringReader (xml))) {
					var xslt = new XslCompiledTransform ();
					xslt.Load (reader, null, new LocalOnlyXmlResolver ());
					return (xslt, null);
				}
			}
			catch (XsltException ex) {
				return (null, CreateBuildError (ex, fileName));
			}
			catch (XmlException ex) {
				return (null, CreateBuildError (ex, fileName));
			}
		}
		
		#endregion
		
		#region File browsing utilities
		
		/// <summary>Allows the user to browse the file system for a stylesheet.</summary>
		/// <returns>The stylesheet filename the user selected; otherwise null.</returns>
		public static string BrowseForStylesheetFile ()
		{
			var dlg = new SelectFileDialog (GettextCatalog.GetString ("Select XSLT Stylesheet")) {
				TransientFor = IdeApp.Workbench.RootWindow,
			};
			dlg.AddFilter (new SelectFileDialogFilter (
				GettextCatalog.GetString ("XML Files"),
				new string[] { "*.xml" },
				new string[] { "text/xml", "application/xml" }
			));
			dlg.AddFilter (new SelectFileDialogFilter(
				GettextCatalog.GetString ("XSL Files"),
				new string[] { "*.xslt", "*.xsl" },
				new string[] { "text/x-xslt" }
			));
			dlg.AddAllFilesFilter ();
			
			if (dlg.Run ())
				return dlg.SelectedFile;
			return null;
		}
		
		/// <summary>Allows the user to browse the file system for a schema.</summary>
		/// <returns>The schema filename the user selected; otherwise null.</returns>
		public static string BrowseForSchemaFile ()
		{
			var dlg = new SelectFileDialog (GettextCatalog.GetString ("Select XML Schema"));
			dlg.AddFilter (new SelectFileDialogFilter (
				GettextCatalog.GetString ("XML Files"),
				new string[] { "*.xsd" },
				new string[] { "text/xml", "application/xml" }
			));
			dlg.AddAllFilesFilter ();
			
			if (dlg.Run ())
				return dlg.SelectedFile;
			return null;
		}
		
		#endregion

		static BuildError CreateBuildError (XmlException ex, string filename)
		{
			return new BuildError {
				Column = ex.LinePosition,
				Line = ex.LineNumber,
				ErrorText = ex.Message,
				FileName = GetLocation (ex.SourceUri, filename)
			};
		}

		static BuildError CreateBuildError (XsltException ex, string filename)
		{
			return new BuildError {
				Column = ex.LinePosition,
				Line = ex.LineNumber,
				ErrorText = ex.Message,
				FileName = GetLocation (ex.SourceUri, filename)
			};
		}

		static BuildError CreateBuildError (XmlSchemaException ex, string filename)
		{
			return new BuildError {
				Column = ex.LinePosition,
				Line = ex.LineNumber,
				ErrorText = ex.Message,
				FileName = GetLocation (ex.SourceUri, filename)
			};
		}

		static BuildError CreateBuildError (ValidationEventArgs args, string filename)
		{
			return new BuildError {
				Column = args.Exception.LinePosition,
				Line = args.Exception.LineNumber,
				ErrorText = args.Message,
				FileName = GetLocation (args.Exception.SourceUri, filename),
				IsWarning = args.Severity == XmlSeverityType.Warning
			};
		}

		static string GetLocation (string reportedUri, string fallbackFilename)
		{
			if (!string.IsNullOrEmpty (reportedUri)) {
				return new System.Uri (reportedUri).LocalPath;
			}
			return fallbackFilename;
		}
	}
}