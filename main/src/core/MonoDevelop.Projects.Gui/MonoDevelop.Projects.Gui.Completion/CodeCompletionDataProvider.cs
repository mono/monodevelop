//  CodeCompletionDataProvider.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects;
using Ambience_ = MonoDevelop.Projects.Dom.Output.Ambience;

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
			
			onStartedParsing = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnStartedParsing));
			onFinishedParsing = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnFinishedParsing));
//TODO:
//			if (parserContext != null) {
//				parserContext.ParserDatabase.ParseOperationStarted += onStartedParsing;
//				parserContext.ParserDatabase.ParseOperationFinished += onFinishedParsing;
//			}
		}
		
		public virtual void Dispose ()
		{
//TODO:
//			if (parserContext != null) {
//				parserContext.ParserDatabase.ParseOperationStarted -= onStartedParsing;
//				parserContext.ParserDatabase.ParseOperationFinished -= onFinishedParsing;
//			}
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
	/*	
		public void AddResolveResults (IEnumerable<IMember> list) 
		{
			AddResolveResults (list, true, null);
		}
		
		public void AddResolveResults (IEnumerable<IMember>list, bool allowInstrinsicNames) 
		{
			AddResolveResults (list, allowInstrinsicNames, null);
		}*/
		
	/*	public void AddResolveResults (IEnumerable<IMember> list, bool allowInstrinsicNames, ITypeNameResolver typeNameResolver) 
		{
			if (list == null) {
				return;
			}
			completionData.Capacity += list.Count;
			foreach (IMember o in list)
				AddResolveResult (o, allowInstrinsicNames, typeNameResolver);
		}
		*/
		CodeCompletionData SearchData (string text)
		{
			foreach (CodeCompletionData ccd in completionData) {
				if (ccd.Text != null && ccd.Text.Length == 1 && ccd.Text[0] == text) 
					return ccd;
			}
			return null;
		}
		/*
		public void AddResolveResult (IMember o, bool allowInstrinsicNames, ITypeNameResolver typeNameResolver) 
		{
			if (o is Namespace) {
				Namespace ns = (Namespace) o;
				if (SearchData (ns.Name) == null)
					completionData.Add(new CodeCompletionData(ns.Name, Stock.NameSpace));
			} else if (o is IType) {
				IType iclass = (IType) o;
				if (iclass.Name != null && insertedClasses[iclass.Name] == null) {
					completionData.Add(new CodeCompletionData(iclass, ambience, allowInstrinsicNames, typeNameResolver));
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
		}*/
			
	/*	public void AddResolveResults (ResolveResult results)
		{
			AddResolveResults (results, true, null);
		}
		
		public void AddResolveResults (ResolveResult results, bool allowInstrinsicNames)
		{
			AddResolveResults (results, allowInstrinsicNames, null);
		}*/
		/*
		public void AddResolveResults (ResolveResult results, bool allowInstrinsicNames, ITypeNameResolver typeNameResolver)
		{
			if (results != null) {
				AddResolveResults (results.Namespaces, allowInstrinsicNames, typeNameResolver);
				AddResolveResults (results.Members, allowInstrinsicNames, typeNameResolver);
			}
		}*/
		
		public bool IsChanging { 
			get { 
				return false;
			//return parserContext != null && parserContext.ParserDatabase.IsParsing; 
			} 
		}

		public ICompletionData GetCompletionData (string completionString)
		{
			foreach (ICompletionData data in completionData) {
				if (data.CompletionString == completionString)
					return data;
			}
			return null;
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
