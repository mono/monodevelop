// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ?Â¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Xml;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects;
using Ambience_ = MonoDevelop.Projects.Ambience.Ambience;

using Stock = MonoDevelop.Core.Gui.Stock;

using Gtk;

namespace MonoDevelop.Projects.Gui.Completion
{
	/// <summary>
	/// Data provider for code completion.
	/// </summary>
	public class CodeCompletionDataProvider : IMutableCompletionDataProvider, IDisposable
	{
//		static AmbienceService          ambienceService = (AmbienceService)ServiceManager.Services.GetService(typeof(AmbienceService));
		Hashtable insertedClasses = new Hashtable ();
		Hashtable insertedElements           = new Hashtable();
		Hashtable insertedPropertiesElements = new Hashtable();
		Hashtable insertedEventElements      = new Hashtable();
		
		Ambience_ ambience;
		int caretLineNumber;
		int caretColumn;
		bool ctrlspace;
		IParserContext parserContext;
		string fileName;
		EventHandler onStartedParsing;
		EventHandler onFinishedParsing;

		ArrayList completionData = null;
		
		public CodeCompletionDataProvider (IParserContext parserContext, Ambience_ ambience, string fileName) : this (parserContext, ambience, fileName, false)
		{
		}
			
		public CodeCompletionDataProvider (IParserContext parserContext, Ambience_ ambience, string fileName, bool ctrl) 
		{
			this.fileName = fileName;
			this.parserContext = parserContext;
			this.ctrlspace = ctrl;
			this.ambience = ambience;
			
			onStartedParsing = (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (OnStartedParsing));
			onFinishedParsing = (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (OnFinishedParsing));
			
			parserContext.ParserDatabase.ParseOperationStarted += onStartedParsing;
			parserContext.ParserDatabase.ParseOperationFinished += onFinishedParsing;
		}
		
		public virtual void Dispose ()
		{
			parserContext.ParserDatabase.ParseOperationStarted -= onStartedParsing;
			parserContext.ParserDatabase.ParseOperationFinished -= onFinishedParsing;
		}
		
		public ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			completionData = new ArrayList();
			insertedClasses.Clear ();
			insertedElements.Clear ();
			insertedPropertiesElements.Clear ();
			insertedEventElements.Clear ();
			
			// the parser works with 1 based coordinates
			caretLineNumber      = widget.TriggerLine + 1;
			caretColumn          = widget.TriggerLineOffset + 1;
			//string expression    = TextUtilities.GetExpressionBeforeOffset (textArea, insertIter.Offset);
			ResolveResult results;
			
			IExpressionFinder expressionFinder = parserContext.GetExpressionFinder(fileName);
			string expression    = expressionFinder == null ? TextUtilities.GetExpressionBeforeOffset(widget, widget.TriggerOffset) : expressionFinder.FindExpression(widget.GetText (0, widget.TriggerOffset), widget.TriggerOffset - 2).Expression;
			if (expression == null) return null;

			//FIXME: This chartyped check is a fucking *HACK*
			if (expression == "is" || expression == "as") {
				string expr = expressionFinder == null ? TextUtilities.GetExpressionBeforeOffset (widget, widget.TriggerOffset - 3) : expressionFinder.FindExpression (widget.GetText (0, widget.TriggerOffset), widget.TriggerOffset - 5).Expression;
				AddResolveResults (parserContext.IsAsResolve (expr, caretLineNumber, caretColumn, fileName, widget.GetText (0, widget.TextLength)));
				return (ICompletionData[])completionData.ToArray (typeof (ICompletionData));
			}
			if (ctrlspace && charTyped != '.') {
				AddResolveResults (parserContext.CtrlSpace (caretLineNumber, caretColumn, fileName));
				return (ICompletionData[])completionData.ToArray (typeof (ICompletionData));
			}
			if (charTyped == ' ') {
				if (expression == "using" || expression.EndsWith(" using") || expression.EndsWith("\tusing")|| expression.EndsWith("\nusing")|| expression.EndsWith("\rusing")) {
					string[] namespaces = parserContext.GetNamespaceList ("", true, true);
					AddResolveResults(new ResolveResult(namespaces));
				}
			} else {
				//FIXME: I added the null check, #D doesnt need it, why do we?
				if (fileName != null) {
					results = parserContext.Resolve (expression, caretLineNumber, caretColumn, fileName, widget.GetText (0, widget.TextLength));
					AddResolveResults(results);
				}
			}
			return (ICompletionData[]) completionData.ToArray (typeof (ICompletionData));
		}
		
		void AddResolveResults(LanguageItemCollection list) 
		{
			if (list == null) {
				return;
			}
			completionData.Capacity += list.Count;
			foreach (ILanguageItem o in list) {
				if (o is Namespace) {
					Namespace ns = (Namespace) o;
					completionData.Add(new CodeCompletionData(ns.Name, Stock.NameSpace));
				} else if (o is IClass) {
					IClass iclass = (IClass) o;
					if (iclass.Name != null && insertedClasses[iclass.Name] == null) {
						completionData.Add(new CodeCompletionData(iclass, ambience));
						insertedClasses[iclass.Name] = iclass;
					}
				} else if (o is IProperty) {
					IProperty property = (IProperty)o;
					if (property.Name != null && insertedPropertiesElements[property.Name] == null) {
						completionData.Add(new CodeCompletionData(property, ambience));
						insertedPropertiesElements[property.Name] = property;
					}
				} else if (o is IMethod) {
					IMethod method = (IMethod)o;
					
					if (method.Name != null &&!method.IsConstructor) {
						CodeCompletionData ccd = new CodeCompletionData(method, ambience);
						if (insertedElements[method.Name] == null) {
							completionData.Add(ccd);
							insertedElements[method.Name] = ccd;
						} else {
							CodeCompletionData firstMethod = (CodeCompletionData)insertedElements[method.Name];
//							++firstMethod.Overloads;
							firstMethod.AddOverload (ccd);
						}
					}
				} else if (o is IField) {
					completionData.Add(new CodeCompletionData((IField)o, ambience));
				} else if (o is IEvent) {
					IEvent e = (IEvent)o;
					if (e.Name != null && insertedEventElements[e.Name] == null) {
						completionData.Add(new CodeCompletionData(e, ambience));
						insertedEventElements[e.Name] = e;
					}
				} else if (o is IParameter) {
					completionData.Add (new CodeCompletionData((IParameter)o, ambience));
				}
			}
		}
			
		void AddResolveResults(ResolveResult results)
		{
			if (results != null) {
				AddResolveResults(results.Namespaces);
				AddResolveResults(results.Members);
			}
		}
		
		public bool IsChanging { 
			get { return parserContext.ParserDatabase.IsParsing; } 
		}
		
		void OnStartedParsing (object s, EventArgs args)
		{
			if (CompletionDataChanging != null)
				CompletionDataChanging (this, EventArgs.Empty);
		}
		
		void OnFinishedParsing (object s, EventArgs args)
		{
			if (CompletionDataChanged != null)
				CompletionDataChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler CompletionDataChanging;
		public event EventHandler CompletionDataChanged;
	}
}
