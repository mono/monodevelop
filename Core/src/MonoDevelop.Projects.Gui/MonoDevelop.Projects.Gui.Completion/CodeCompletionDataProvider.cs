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
		Hashtable insertedClasses = new Hashtable ();
		Hashtable insertedElements           = new Hashtable();
		Hashtable insertedPropertiesElements = new Hashtable();
		Hashtable insertedEventElements      = new Hashtable();
		
		Ambience_ ambience;
		IParserContext parserContext;
		EventHandler onStartedParsing;
		EventHandler onFinishedParsing;

		string defaultCompletionString;
		ArrayList completionData = new ArrayList ();
		
		public CodeCompletionDataProvider (IParserContext parserContext, Ambience_ ambience) 
		{
			this.parserContext = parserContext;
			this.ambience = ambience;
			
			onStartedParsing = (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (OnStartedParsing));
			onFinishedParsing = (EventHandler) Services.DispatchService.GuiDispatch (new EventHandler (OnFinishedParsing));
			
			if (parserContext != null) {
				parserContext.ParserDatabase.ParseOperationStarted += onStartedParsing;
				parserContext.ParserDatabase.ParseOperationFinished += onFinishedParsing;
			}
		}
		
		public virtual void Dispose ()
		{
			if (parserContext != null) {
				parserContext.ParserDatabase.ParseOperationStarted -= onStartedParsing;
				parserContext.ParserDatabase.ParseOperationFinished -= onFinishedParsing;
			}
		}
		
		public void Clear ()
		{
			completionData.Clear ();
			insertedClasses.Clear ();
			insertedElements.Clear ();
			insertedPropertiesElements.Clear ();
			insertedEventElements.Clear ();
		}
		
		public bool IsEmpty {
			get { return completionData.Count == 0; }
		}
		
		public string DefaultCompletionString {
			get { return defaultCompletionString; }
			set { defaultCompletionString = value; }
		}
		
		public virtual ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			return (ICompletionData[]) completionData.ToArray (typeof (ICompletionData));
		}
		
		public void AddCompletionData (ICompletionData data)
		{
			completionData.Add (data);
		}
		
		public void AddResolveResults (LanguageItemCollection list) 
		{
			if (list == null) {
				return;
			}
			completionData.Capacity += list.Count;
			foreach (ILanguageItem o in list)
				AddResolveResult (o);
		}
		
		public void AddResolveResult (ILanguageItem o) 
		{
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
//						++firstMethod.Overloads;
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
			
		public void AddResolveResults (ResolveResult results)
		{
			if (results != null) {
				AddResolveResults(results.Namespaces);
				AddResolveResults(results.Members);
			}
		}
		
		public bool IsChanging { 
			get { return parserContext != null && parserContext.ParserDatabase.IsParsing; } 
		}
		
		void OnStartedParsing (object s, EventArgs args)
		{
			OnCompletionDataChanging ();
		}
		
		void OnFinishedParsing (object s, EventArgs args)
		{
			OnCompletionDataChanged ();
		}
		
		protected virtual void OnCompletionDataChanging ()
		{
			if (CompletionDataChanging != null)
				CompletionDataChanging (this, EventArgs.Empty);
		}
		
		protected virtual void OnCompletionDataChanged ()
		{
			if (CompletionDataChanged != null)
				CompletionDataChanged (this, EventArgs.Empty);
		}
		
		public event EventHandler CompletionDataChanging;
		public event EventHandler CompletionDataChanged;
	}
}
