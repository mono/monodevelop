// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃƒÂ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

using MonoDevelop.Core.Properties;
using MonoDevelop.Internal.Templates;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using SharpDevelop.Internal.Parser;
using MonoDevelop.TextEditor.Gui.CompletionWindow;
using MonoDevelop.Gui;

namespace MonoDevelop.DefaultEditor.Gui.Editor
{
	/// <summary>
	/// Data provider for code completion.
	/// </summary>
	public class CodeCompletionDataProvider : ICompletionDataProvider
	{
		static IconService classBrowserIconService = (IconService)ServiceManager.Services.GetService(typeof(IconService));
//		static AmbienceService          ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
		Hashtable insertedElements           = new Hashtable();
		Hashtable insertedPropertiesElements = new Hashtable();
		Hashtable insertedEventElements      = new Hashtable();
		
		public Gdk.Pixbuf[] ImageList {
			get {
				PixbufList list = classBrowserIconService.ImageList;
				return (Gdk.Pixbuf[])list.ToArray (typeof(Gdk.Pixbuf));
			}
		}
		
		int caretLineNumber;
		int caretColumn;
		string fileName;
		
		ArrayList completionData = null;
			
		public ICompletionData[] GenerateCompletionData(IProject project, string fileName, TextArea textArea, char charTyped)
		{
			IDocument document =  textArea.Document;
			Console.WriteLine ("resolve " + document.Language);
			Console.WriteLine ("nm " + fileName);
			completionData = new ArrayList();
			this.fileName = fileName;
			
			// the parser works with 1 based coordinates
			caretLineNumber      = document.GetLineNumberForOffset(textArea.Caret.Offset) + 1;
			caretColumn          = textArea.Caret.Offset - document.GetLineSegment(caretLineNumber - 1).Offset + 1;
			string expression    = TextUtilities.GetExpressionBeforeOffset(textArea, textArea.Caret.Offset);
			ResolveResult results;
			
			if (expression.Length == 0) {
				return null;
			}
			IParserService           parserService           = (IParserService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(IParserService));
			if (charTyped == ' ') {
				if (expression == "using" || expression.EndsWith(" using") || expression.EndsWith("\tusing")|| expression.EndsWith("\nusing")|| expression.EndsWith("\rusing")) {
					string[] namespaces = parserService.GetNamespaceList("");
//					AddResolveResults(new ResolveResult(namespaces, ShowMembers.Public));
					AddResolveResults(new ResolveResult(namespaces));
//					IParseInformation info = parserService.GetParseInformation(fileName);
//					ICompilationUnit unit = info.BestCompilationUnit as ICompilationUnit;
//					if (unit != null) {
//						foreach (IUsing u in unit.Usings) {
//							if (u.Region.IsInside(caretLineNumber, caretColumn)) {
//								foreach (string usingStr in u.Usings) {
//									results = parserService.Resolve(usingStr, caretLineNumber, caretColumn, fileName);
//									AddResolveResults(results);
//								}
//								if (u.Aliases[""] != null) {
//									results = parserService.Resolve(u.Aliases[""].ToString(), caretLineNumber, caretColumn, fileName);
//									AddResolveResults(results);
//								}
//							}
//						}
//					}
				}
			} else {
				//FIXME: I added the null check, #D doesnt need it, why do we?
				if (fileName != null) {
					Console.WriteLine ("resolve " + document.Language);
					results = parserService.Resolve(expression, 
				                                caretLineNumber,
				                                caretColumn,
				                                fileName,
				                                document.TextContent,
								document.Language);
				
					AddResolveResults(results);
				}
			}
			
			return (ICompletionData[])completionData.ToArray(typeof(ICompletionData));
		}
		
		void AddResolveResults(ResolveResult results)
		{
			if (results != null) {
				completionData.Capacity += results.Namespaces.Count +
					results.Members.Count;
				
				if (results.Namespaces != null && results.Namespaces.Count > 0) {
					foreach (string s in results.Namespaces) {
						completionData.Add(new CodeCompletionData(s, classBrowserIconService.NamespaceIndex));
					}
				}
				if (results.Members != null && results.Members.Count > 0) {
					foreach (object o in results.Members) {
						if (o is IClass) {
							completionData.Add(new CodeCompletionData((IClass)o));
						} else if (o is IProperty) {
							IProperty property = (IProperty)o;
							if (property.Name != null && insertedPropertiesElements[property.Name] == null) {
								completionData.Add(new CodeCompletionData(property));
								insertedPropertiesElements[property.Name] = property;
							}
						} else if (o is IMethod) {
							IMethod method = (IMethod)o;
							if (method.Name != null && insertedElements[method.Name] == null && !method.IsConstructor) {
								completionData.Add(new CodeCompletionData(method));
								insertedElements[method.Name] = method;
							}
						} else if (o is IField) {
							completionData.Add(new CodeCompletionData((IField)o));
						} else if (o is IEvent) {
							IEvent e = (IEvent)o;
							if (e.Name != null && insertedEventElements[e.Name] == null) {
								completionData.Add(new CodeCompletionData(e));
								insertedEventElements[e.Name] = e;
							}
						}
					}
				}
			}
		}
	}
}
