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

using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Ambience;

using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.SourceEditor.Gui;

namespace MonoDevelop.SourceEditor.InsightWindow
{
	public class IndexerInsightDataProvider : IInsightDataProvider
	{
		Ambience			ambience = null;
		
		SourceEditorView    textArea;
		IndexerCollection   methods  = new IndexerCollection (null);
		
		public int InsightDataCount {
			get {
				return methods.Count;
			}
		}
		
		public string GetInsightData(int number)
		{
			IIndexer method = methods[number];
			return ambience.Convert(method);
			//IAmbience conv = ambienceService.GenericAmbience;
			//return conv.Convert(method);// + 
			       //"\n" + 
			       //CodeCompletionData.GetDocumentation(method.Documentation); // new (by G.B.)
		}
		
		int initialOffset;
		public void SetupDataProvider(Project project, string fileName, SourceEditorView textArea)
		{
			this.textArea = textArea;
			Gtk.TextIter initialIter = textArea.Buffer.GetIterAtMark (textArea.Buffer.InsertMark);
			initialOffset = initialIter.Offset;
			
			string word         = TextUtilities.GetExpressionBeforeOffset(textArea, initialOffset);
			string methodObject = word;
			
			// the parser works with 1 based coordinates
			int caretLineNumber      = initialIter.Line + 1;
			int caretColumn          = initialIter.LineOffset + 1;
			
			IParserContext parserContext;
			if (project != null) {
				parserContext = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
				ambience = project.Ambience;
			} else {
				parserContext = IdeApp.ProjectOperations.ParserDatabase.GetFileParserContext (fileName);
				ambience = MonoDevelop.Projects.Services.Ambience.GenericAmbience;
			}
			
			ResolveResult results = parserContext.Resolve (methodObject, caretLineNumber, caretColumn, fileName, textArea.Buffer.Text);
			
			if (results != null && results.Type != null) {
				foreach (IClass c in parserContext.GetClassInheritanceTree (results.Type)) {
					foreach (IIndexer indexer in c.Indexer) {
						methods.Add(indexer);
					}
				}

				//FIXME: This shouldnt be commented out, but i replaced the parser and cant figure this out
				/*foreach (object o in results.ResolveContents) {
					if (o is IClass) {
						foreach (IClass c in ((IClass)o).ClassInheritanceTree) {
							foreach (IIndexer indexer in c.Indexer) {
								methods.Add(indexer);
							}
						}
					}
				}*/
			}
		}
		
		public bool CaretOffsetChanged()
		{
			Gtk.TextIter caret = textArea.Buffer.GetIterAtMark (textArea.Buffer.InsertMark);
			bool closeDataProvider = caret.Offset <= initialOffset;
			string text = textArea.Buffer.Text;
			
			if (!closeDataProvider) {
				bool insideChar   = false;
				bool insideString = false;
				for (int offset = initialOffset; offset < Math.Min(caret.Offset, text.Length); ++offset) {
					char ch = text [offset];
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
			int offset = textArea.Buffer.GetIterAtMark (textArea.Buffer.InsertMark).Offset - 1;
			if (offset >= 0) {
				return textArea.Buffer.Text [offset] == ']';
			}
			return false;
		}
	}
}
