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
using MonoDevelop.Internal.Parser;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui.Completion;

using MonoDevelop.SourceEditor.Gui;

namespace MonoDevelop.SourceEditor.InsightWindow 
{
	public class MethodInsightDataProvider : IInsightDataProvider
	{
		AmbienceService          ambienceService = (AmbienceService)ServiceManager.GetService(typeof(AmbienceService));
		
		string              fileName = null;
		SourceEditorView    textArea  = null;
		MethodCollection    methods  = new MethodCollection();
		
		int caretLineNumber;
		int caretColumn;
		
		public int InsightDataCount {
			get {
				return methods.Count;
			}
		}
		
		public string GetInsightData(int number)
		{
			IMethod method = methods[number];
			IAmbience conv = ambienceService.CurrentAmbience;
			conv.ConversionFlags = ConversionFlags.StandardConversionFlags;
			return conv.Convert(method);
			//       "\n" + 
			//       CodeCompletionData.GetDocumentation(method.Documentation); // new (by G.B.)
		}
		
		int initialOffset;
		public void SetupDataProvider(Project project, string fileName, SourceEditorView textArea)
		{
			this.fileName = fileName;
			this.textArea = textArea;
			Gtk.TextIter initialIter = textArea.Buffer.GetIterAtMark (textArea.Buffer.InsertMark);
			initialOffset = initialIter.Offset;
			string text = textArea.Buffer.Text;
			
			string word         = TextUtilities.GetExpressionBeforeOffset(textArea, initialOffset);
			string methodObject = word;
			string methodName   =  null;
			int idx = methodObject.LastIndexOf('.');
			if (idx >= 0) {
				methodName   = methodObject.Substring(idx + 1);
				methodObject = methodObject.Substring(0, idx);
			} else {
				methodObject = "this";
				methodName   = word;
			}
			
			if (methodName.Length == 0 || methodObject.Length == 0) {
				return;
			}
			
			// the parser works with 1 based coordinates
			caretLineNumber      = initialIter.Line + 1;
			caretColumn          = initialIter.LineOffset + 1;
			
			string[] words = word.Split(' ');
			bool contructorInsight = false;
			if (words.Length > 1) {
				contructorInsight = words[words.Length - 2] == "new";
				if (contructorInsight) {
					methodObject = words[words.Length - 1];
				}
			}
			
			IParserContext parserContext;
			if (project != null)
				parserContext = Runtime.ProjectService.ParserDatabase.GetProjectParserContext (project);
			else
				parserContext = Runtime.ProjectService.ParserDatabase.GetFileParserContext (fileName);
			
			ResolveResult results = parserContext.Resolve (methodObject, caretLineNumber, caretColumn, fileName, text);
			
			if (results != null && results.Type != null) {
				if (contructorInsight) {
					AddConstructors(results.Type);
				} else {
					foreach (IClass c in parserContext.GetClassInheritanceTree (results.Type)) {
 						AddMethods(c, methodName, false);
					}
				}
			}
		}
		
		bool IsAlreadyIncluded(IMethod newMethod) 
		{
			foreach (IMethod method in methods) {
				if (method.Name == newMethod.Name) {
					if (newMethod.Parameters.Count != method.Parameters.Count) {
						return false;
					}
					
					for (int i = 0; i < newMethod.Parameters.Count; ++i) {
						if (newMethod.Parameters[i].ReturnType != method.Parameters[i].ReturnType) {
							return false;
						}
					}
					
					// take out old method, when it isn't documented.
					if (method.Documentation == null || method.Documentation.Length == 0) {
						methods.Remove(method);
						return false;
					}
					return true;
				}
			}
			return false;
		}
		
		void AddConstructors(IClass c)
		{
			foreach (IMethod method in c.Methods) {
				if (method.IsConstructor && !method.IsStatic) {
					methods.Add(method);
				}
			}
		}
		
		void AddMethods(IClass c, string methodName, bool discardPrivate)
		{
			foreach (IMethod method in c.Methods) {
				if (!(method.IsPrivate && discardPrivate) && 
				    method.Name == methodName &&
				    !IsAlreadyIncluded(method)) {
					methods.Add(method);
				}
			}
		}
		
		public bool CaretOffsetChanged()
		{
			Gtk.TextIter insertIter = textArea.Buffer.GetIterAtMark (textArea.Buffer.InsertMark);
			bool closeDataProvider = insertIter.Offset <= initialOffset;
			int brackets = 0;
			int curlyBrackets = 0;
			string text = textArea.Buffer.Text;
			if (!closeDataProvider) {
				bool insideChar   = false;
				bool insideString = false;
				for (int offset = initialOffset; offset < Math.Min(insertIter.Offset, text.Length); ++offset) {
					char ch = text[offset];
					switch (ch) {
						case '\'':
							insideChar = !insideChar;
							break;
						case '(':
							if (!(insideChar || insideString)) {
								++brackets;
							}
							break;
						case ')':
							if (!(insideChar || insideString)) {
								--brackets;
							}
							if (brackets <= 0) {
								return true;
							}
							break;
						case '"':
							insideString = !insideString;
							break;
						case '}':
							if (!(insideChar || insideString)) {
								--curlyBrackets;
							}
							if (curlyBrackets < 0) {
								return true;
							}
							break;
						case '{':
							if (!(insideChar || insideString)) {
								++curlyBrackets;
							}
							break;
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
				return textArea.Buffer.Text [offset] == ')';
			}
			return false;
		}
	}
}
