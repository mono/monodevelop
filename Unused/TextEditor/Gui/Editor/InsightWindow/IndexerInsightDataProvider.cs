// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃƒÂ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Reflection;
using System.Collections;

using MonoDevelop.Core.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Services;
using MonoDevelop.TextEditor.Document;
using SharpDevelop.Internal.Parser;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Gui.InsightWindow;
using MonoDevelop.DefaultEditor.Gui.Editor;


namespace MonoDevelop.DefaultEditor.Gui.Editor
{
	public class IndexerInsightDataProvider : IInsightDataProvider
	{
		AmbienceService          ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
		
		string              fileName = null;
		IDocument document = null;
		TextArea textArea;
		IndexerCollection   methods  = new IndexerCollection();
		
		public int InsightDataCount {
			get {
				return methods.Count;
			}
		}
		
		public string GetInsightData(int number)
		{
			IIndexer method = methods[number];
			IAmbience conv = ambienceService.CurrentAmbience;
			conv.ConversionFlags = ConversionFlags.StandardConversionFlags;
			return conv.Convert(method) + 
			       "\n" + 
			       CodeCompletionData.GetDocumentation(method.Documentation); // new (by G.B.)
		}
		
		int initialOffset;
		public void SetupDataProvider(string fileName, TextArea textArea)
		{
			this.fileName = fileName;
			this.document = textArea.Document;
			this.textArea = textArea;
			initialOffset = textArea.Caret.Offset;
			
			string word         = TextUtilities.GetExpressionBeforeOffset(textArea, textArea.Caret.Offset);
			string methodObject = word;
			
			// the parser works with 1 based coordinates
			int caretLineNumber      = document.GetLineNumberForOffset(textArea.Caret.Offset) + 1;
			int caretColumn          = textArea.Caret.Offset - document.GetLineSegment(caretLineNumber - 1).Offset + 1;
			IParserService parserService = (IParserService)ServiceManager.Services.GetService(typeof(IParserService));
			ResolveResult results = parserService.Resolve(methodObject,
			                                              caretLineNumber,
			                                              caretColumn,
			                                              fileName,
			                                              document.TextContent);
			if (results != null && results.Type != null) {
				foreach (IClass c in results.Type.ClassInheritanceTree) {
					foreach (IIndexer indexer in c.Indexer) {
						methods.Add(indexer);
					}
				}
				foreach (object o in results.ResolveContents) {
					if (o is IClass) {
						foreach (IClass c in ((IClass)o).ClassInheritanceTree) {
							foreach (IIndexer indexer in c.Indexer) {
								methods.Add(indexer);
							}
						}
					}
				}
			}
		}
		
		public bool CaretOffsetChanged()
		{
			bool closeDataProvider = textArea.Caret.Offset <= initialOffset;
			
			if (!closeDataProvider) {
				bool insideChar   = false;
				bool insideString = false;
				for (int offset = initialOffset; offset < Math.Min(textArea.Caret.Offset, document.TextLength); ++offset) {
					char ch = document.GetCharAt(offset);
					switch (ch) {
						case '\'':
							insideChar = !insideChar;
							break;
						case '"':
							insideString = !insideString;
							break;
						case ']':
						case '}':
						case '{':
						case ';':
							if (!(insideChar || insideString)) {
								return true;
							}
							break;
					}
				}
			}
			
			return closeDataProvider;
		}
		
		public bool CharTyped()
		{
			int offset = textArea.Caret.Offset - 1;
			if (offset >= 0) {
				return document.GetCharAt(offset) == ']';
			}
			return false;
		}
	}
}
