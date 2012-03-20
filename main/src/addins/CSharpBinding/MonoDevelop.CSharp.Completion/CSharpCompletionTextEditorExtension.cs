// 
// CSharpCompletionTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Ide.Gui.Content;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;
using System.Linq;
using MonoDevelop.CSharp.Formatting;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Text;
using MonoDevelop.Ide.CodeTemplates;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Completion
{
	
	public class CSharpCompletionTextEditorExtension : CompletionTextEditorExtension, ICompletionDataFactory, IParameterCompletionDataFactory, ITextEditorMemberPositionProvider
	{
		internal Mono.TextEditor.TextEditorData textEditorData;
		
		CompilationUnit unit;
		static readonly CompilationUnit emptyUnit = new CompilationUnit ();
		CompilationUnit Unit {
			get {
				return unit ?? emptyUnit;
			}
			set {
				unit = value;
			}
		}
		
		public new MonoDevelop.Ide.Gui.Document Document {
			get {
				return base.document;
			}
		}
		
		public CSharpParsedFile CSharpParsedFile {
			get;
			set;
		}
		
		public ParsedDocument ParsedDocument {
			get {
				return document.ParsedDocument;
			}
		}
		
		public ICompilation Compilation {
			get {
				return document.Compilation;
			}
		}
		
		public MonoDevelop.Projects.Project Project {
			get {
				return document.Project;
			}
		}
		
		CSharpFormattingPolicy policy;
		public CSharpFormattingPolicy FormattingPolicy {
			get {
				if (policy == null) {
					IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
					if (Document.Project != null && Document.Project.Policies != null) {
						policy = base.Document.Project.Policies.Get<CSharpFormattingPolicy> (types);
					} else {
						policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
					}
				}
				return policy;
			}
		}
		
		public CSharpCompletionTextEditorExtension ()
		{
		}
		
		/// <summary>
		/// Used in testing environment.
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public CSharpCompletionTextEditorExtension (MonoDevelop.Ide.Gui.Document doc) : this ()
		{
			Initialize (doc);
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			textEditorData = Document.Editor;
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument != null) {
				this.Unit = parsedDocument.GetAst<CompilationUnit> ();
				this.CSharpParsedFile = parsedDocument.ParsedFile as CSharpParsedFile;
			}
			
			Document.DocumentParsed += HandleDocumentParsed; 
		}
		
		public override void Dispose ()
		{
			unit = null;
			CSharpParsedFile = null;
			Document.DocumentParsed -= HandleDocumentParsed; 
			base.Dispose ();
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			var newDocument = Document.ParsedDocument;
			if (newDocument == null) 
				return;
			var newTree = TypeSystemSegmentTree.Create (Document);
			if (typeSystemSegmentTree != null)
				typeSystemSegmentTree.RemoveListener (document.Editor.Document);
			typeSystemSegmentTree = newTree;
			typeSystemSegmentTree.InstallListener (document.Editor.Document);
			
			this.Unit = newDocument.GetAst<CompilationUnit> ();
			this.CSharpParsedFile = newDocument.ParsedFile as CSharpParsedFile;
			var textEditor = Editor.Parent;
			if (textEditor != null) {
				Gtk.Application.Invoke (delegate {
					if (unit == null) // check, if we're disposed.
						return;
					textEditor.TextViewMargin.PurgeLayoutCache ();
					textEditor.RedrawMarginLines (textEditor.TextViewMargin, 1, Editor.LineCount);
				});
			}
			if (TypeSegmentTreeUpdated != null)
				TypeSegmentTreeUpdated (this, EventArgs.Empty);
		}
		public event EventHandler TypeSegmentTreeUpdated;
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool result = base.KeyPress (key, keyChar, modifier);
			
			if (EnableParameterInsight && (keyChar == ',' || keyChar == ')') && CanRunParameterCompletionCommand ())
				base.RunParameterCompletionCommand ();
			
//			if (IsInsideComment ())
//				ParameterInformationWindowManager.HideWindow (CompletionWidget);
			return result;
		}
		
		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			if (!EnableCodeCompletion)
				return null;
			if (!EnableAutoCodeCompletion && char.IsLetter (completionChar))
				return null;

			//	var timer = Counters.ResolveTime.BeginTiming ();
			try {
				if (char.IsLetterOrDigit (completionChar) || completionChar == '_') {
					if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit (document.Editor.GetCharAt (completionContext.TriggerOffset - 2)))
						return null;
					triggerWordLength = 1;
				}
				return InternalHandleCodeCompletion (completionContext, completionChar, false, ref triggerWordLength);
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected code completion exception." + Environment.NewLine + 
					"FileName: " + Document.FileName + Environment.NewLine + 
					"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
					"Line text: " + Document.Editor.GetLineText (completionContext.TriggerLine), 
					e);
				return null;
			} finally {
				//			if (timer != null)
				//				timer.Dispose ();
			}
		}
		
		ICompletionDataList InternalHandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, bool ctrlSpace, ref int triggerWordLength)
		{
			
			if (textEditorData.CurrentMode is CompletionTextLinkMode) {
				if (!((CompletionTextLinkMode)textEditorData.CurrentMode).TriggerCodeCompletion)
					return null;
			} else if (textEditorData.CurrentMode is Mono.TextEditor.TextLinkEditMode) {
				return null;
			}
			if (Unit == null || CSharpParsedFile == null)
				return null;
			var list = new CompletionDataList ();
			var engine = new CSharpCompletionEngine (
				textEditorData.Document,
				this,
				Document.GetProjectContext (),
				CSharpParsedFile.GetTypeResolveContext (Document.Compilation, document.Editor.Caret.Location) as CSharpTypeResolveContext,
				Unit,
				CSharpParsedFile
			);
			engine.MemberProvider = typeSystemSegmentTree;
			engine.FormattingPolicy = FormattingPolicy.CreateOptions ();
			engine.EolMarker = textEditorData.EolMarker;
			engine.IndentString = textEditorData.Options.IndentationString;
			list.AddRange (engine.GetCompletionData (completionContext.TriggerOffset, ctrlSpace));
			list.AutoCompleteEmptyMatch = engine.AutoCompleteEmptyMatch;
			list.AutoSelect = engine.AutoSelect;
			list.DefaultCompletionString = engine.DefaultCompletionString;
			list.CloseOnSquareBrackets = engine.CloseOnSquareBrackets;
			return list.Count > 0 ? list : null;
		}
		
		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			int triggerWordLength = 0;
			char ch = completionContext.TriggerOffset > 0 ? textEditorData.GetCharAt (completionContext.TriggerOffset - 1) : '\0';
			return InternalHandleCodeCompletion (completionContext, ch, true, ref triggerWordLength);
		}
		
		static bool ContainsPublicConstructors (ITypeDefinition t)
		{
			if (t.Methods.Count (m => m.IsConstructor) == 0)
				return true;
			return t.Methods.Any (m => m.IsConstructor && m.IsPublic);
		}


//			CompletionDataList result = new ProjectDomCompletionDataList ();
//			// "var o = new " needs special treatment.
//			if (returnType == null && returnTypeUnresolved != null && returnTypeUnresolved.FullName == "var")
//				returnType = returnTypeUnresolved = DomReturnType.Object;
//
//			//	ExpressionContext.TypeExpressionContext tce = context as ExpressionContext.TypeExpressionContext;
//
//			CompletionDataCollector col = new CompletionDataCollector (this, dom, result, Document.CompilationUnit, callingType, location);
//			IType type = null;
//			if (returnType != null)
//				type = dom.GetType (returnType);
//			if (type == null)
//				type = dom.SearchType (Document.CompilationUnit, callingType, location, returnTypeUnresolved);
//			
//			// special handling for nullable types: Bug 674516 - new completion for nullables should not include "Nullable"
//			if (type is InstantiatedType && ((InstantiatedType)type).UninstantiatedType.FullName == "System.Nullable" && ((InstantiatedType)type).GenericParameters.Count == 1) {
//				var genericParameter = ((InstantiatedType)type).GenericParameters [0];
//				returnType = returnTypeUnresolved = Document.CompilationUnit.ShortenTypeName (genericParameter, location);
//				type = dom.SearchType (Document.CompilationUnit, callingType, location, genericParameter);
//			}
//			
//			if (type == null || !(type.IsAbstract || type.ClassType == ClassType.Interface)) {
//				if (type == null || type.ConstructorCount == 0 || type.Methods.Any (c => c.IsConstructor && c.IsAccessibleFrom (dom, callingType, type, callingType != null && dom.GetInheritanceTree (callingType).Any (x => x.FullName == type.FullName)))) {
//					if (returnTypeUnresolved != null) {
//						col.FullyQualify = true;
//						CompletionData unresovedCompletionData = col.Add (returnTypeUnresolved);
//						col.FullyQualify = false;
//						// don't set default completion string for arrays, since it interferes with: 
//						// string[] arr = new string[] vs new { "a"}
//						if (returnTypeUnresolved.ArrayDimensions == 0)
//							result.DefaultCompletionString = StripGenerics (unresovedCompletionData.CompletionText);
//					} else {
//						CompletionData unresovedCompletionData = col.Add (returnType);
//						if (returnType.ArrayDimensions == 0)
//							result.DefaultCompletionString = StripGenerics (unresovedCompletionData.CompletionText);
//					}
//				}
//			}
//			
//			//				if (tce != null && tce.Type != null) {
//			//					result.DefaultCompletionString = StripGenerics (col.AddCompletionData (result, tce.Type).CompletionString);
//			//				} 
//			//			else {
//			//			}
//			if (type == null)
//				return result;
//			HashSet<string > usedNamespaces = new HashSet<string> (GetUsedNamespaces ());
//			if (type.FullName == DomReturnType.Object.FullName) 
//				AddPrimitiveTypes (col);
//			
//			foreach (IType curType in dom.GetSubclasses (type)) {
//				if (context != null && context.FilterEntry (curType))
//					continue;
//				if ((curType.TypeModifier & TypeModifier.HasOnlyHiddenConstructors) == TypeModifier.HasOnlyHiddenConstructors)
//					continue;
//				if (usedNamespaces.Contains (curType.Namespace)) {
//					if (curType.ConstructorCount > 0) {
//						if (!(curType.Methods.Any (c => c.IsConstructor && c.IsAccessibleFrom (dom, curType, callingType, callingType != null && dom.GetInheritanceTree (callingType).Any (x => x.FullName == curType.FullName)))))
//							continue;
//					}
//					col.Add (curType);
//				} else {
//					string nsName = curType.Namespace;
//					int idx = nsName.IndexOf ('.');
//					if (idx >= 0)
//						nsName = nsName.Substring (0, idx);
//					col.Add (new Namespace (nsName));
//				}
//			}
//			
//			// add aliases
//			if (returnType != null) {
//				foreach (IUsing u in Document.CompilationUnit.Usings) {
//					foreach (KeyValuePair<string, IReturnType> alias in u.Aliases) {
//						if (alias.Value.ToInvariantString () == returnType.ToInvariantString ())
//							result.Add (alias.Key, "md-class");
//					}
//				}
//			}
//			
//			return result;
//		}
		
//		IEnumerable<ICompletionData> GetDefineCompletionData ()
//		{
//			if (Document.Project == null)
//				yield break;
//			
//			var symbols = new Dictionary<string, string> ();
//			var cp = new ProjectDomCompletionDataList ();
//			foreach (DotNetProjectConfiguration conf in Document.Project.Configurations) {
//				var cparams = conf.CompilationParameters as CSharpCompilerParameters;
//				if (cparams != null) {
//					string[] syms = cparams.DefineSymbols.Split (';');
//					foreach (string s in syms) {
//						string ss = s.Trim ();
//						if (ss.Length > 0 && !symbols.ContainsKey (ss)) {
//							symbols [ss] = ss;
//							yield return factory.CreateLiteralCompletionData (ss);
//						}
//					}
//				}
//			}
//		}
		
		public override IParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			if (!EnableCodeCompletion)
				return null;
			if (Unit == null || CSharpParsedFile == null)
				return null;
			try {
				var engine = new CSharpParameterCompletionEngine (
					textEditorData.Document,
					this,
					Document.GetProjectContext (),
					CSharpParsedFile.GetTypeResolveContext (Document.Compilation, document.Editor.Caret.Location) as CSharpTypeResolveContext,
					Unit,
					CSharpParsedFile
				);
				engine.MemberProvider = typeSystemSegmentTree;
				return engine.GetParameterDataProvider (completionContext.TriggerOffset, completionChar);
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected parameter completion exception." + Environment.NewLine + 
					"FileName: " + Document.FileName + Environment.NewLine + 
					"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
					"Line text: " + Document.Editor.GetLineText (completionContext.TriggerLine), 
					e);
				return null;
			} finally {
				//			if (timer != null)
				//				timer.Dispose ();
			}
		}
		
		List<string> GetUsedNamespaces ()
		{
			var scope = CSharpParsedFile.GetUsingScope (new TextLocation (document.Editor.Caret.Line, document.Editor.Caret.Column));
			var result = new List<string> ();
			while (scope != null) {
				result.Add (scope.NamespaceName);
				var ctx = CSharpParsedFile.GetResolver (Document.Compilation, scope.Region.Begin);
				foreach (var u in scope.Usings) {
					var ns = u.ResolveNamespace (ctx);
					if (ns == null)
						continue;
					result.Add (ns.FullName);
				}
				scope = scope.Parent;
			}
			return result;
		}
		/*
		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			// Start calculating the parameter offset from the beginning of the
			// current member, instead of the beginning of the file. 
			cpos = textEditorData.Caret.Offset - 1;
			var parsedDocument = Document.ParsedDocument;
			if (parsedDocument == null)
				return false;
			IMember mem = currentMember;
			if (mem == null || (mem is IType))
				return false;
			int startPos = textEditorData.LocationToOffset (mem.Region.BeginLine, mem.Region.BeginColumn);
			int parenDepth = 0;
			int chevronDepth = 0;
			while (cpos > startPos) {
				char c = textEditorData.GetCharAt (cpos);
				if (c == ')')
					parenDepth++;
				if (c == '>')
					chevronDepth++;
				if (parenDepth == 0 && c == '(' || chevronDepth == 0 && c == '<') {
					int p = MethodParameterDataProvider.GetCurrentParameterIndex (CompletionWidget, cpos + 1, startPos);
					if (p != -1) {
						cpos++;
						return true;
					} else {
						return false;
					}
				}
				if (c == '(')
					parenDepth--;
				if (c == '<')
					chevronDepth--;
				cpos--;
			}
			return false;
		}*/

		public override int GetCurrentParameterIndex (int startOffset)
		{
			var engine = new CSharpParameterCompletionEngine (
				textEditorData.Document,
				this,
				Document.GetProjectContext (),
				CSharpParsedFile.GetTypeResolveContext (Document.Compilation, document.Editor.Caret.Location) as CSharpTypeResolveContext,
				Unit,
				CSharpParsedFile
			);
			engine.MemberProvider = typeSystemSegmentTree;
			return engine.GetCurrentParameterIndex (startOffset, document.Editor.Caret.Offset);
		}
		/*
		internal int GetCurrentParameterIndex (ICompletionWidget widget, int offset, int memberStart)
		{
			int cursor = widget.CurrentCodeCompletionContext.TriggerOffset;
			int i = offset;
			if (i > cursor)
				return -1;
			if (i == cursor) 
				return 1; // parameters are 1 based
			var types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (CSharpFormatter.MimeType);
			var engine = new CSharpIndentEngine (MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types));
			int index = memberStart + 1;
			int parentheses = 0;
			int bracket = 0;
			do {
				char c = widget.GetChar (i - 1);
				engine.Push (c);
				switch (c) {
				case '{':
					if (!engine.IsInsideOrdinaryCommentOrString)
						bracket++;
					break;
				case '}':
					if (!engine.IsInsideOrdinaryCommentOrString)
						bracket--;
					break;
				case '(':
					if (!engine.IsInsideOrdinaryCommentOrString)
						parentheses++;
					break;
				case ')':
					if (!engine.IsInsideOrdinaryCommentOrString)
						parentheses--;
					break;
				case ',':
					if (!engine.IsInsideOrdinaryCommentOrString && parentheses == 1 && bracket == 0)
						index++;
					break;
				}
				i++;
			} while (i <= cursor && parentheses >= 0);
			
			return parentheses != 1 || bracket > 0 ? -1 : index;
		}*/
		
		#region ICompletionDataFactory implementation
		ICompletionData ICompletionDataFactory.CreateEntityCompletionData (IEntity entity)
		{
			return new MemberCompletionData (this, entity, OutputFlags.IncludeGenerics | OutputFlags.HideArrayBrackets | OutputFlags.IncludeParameterName) {
				HideExtensionParameter = true
			};
		}

		ICompletionData ICompletionDataFactory.CreateEntityCompletionData (IEntity entity, string text)
		{
			return new CompletionData (text, entity.GetStockIcon (), null, text);
		}
		
		ICompletionData ICompletionDataFactory.CreateEntityCompletionData (IUnresolvedEntity entity)
		{
			var context = CSharpParsedFile.GetTypeResolveContext (Document.Compilation, document.Editor.Caret.Location);
			var resolvedEntity = ((IUnresolvedMember)entity).CreateResolved (context);
			
			return new MemberCompletionData (this, resolvedEntity, OutputFlags.IncludeGenerics | OutputFlags.HideArrayBrackets | OutputFlags.IncludeParameterName) {
				HideExtensionParameter = true
			};
		}

		ICompletionData ICompletionDataFactory.CreateEntityCompletionData (IUnresolvedEntity entity, string text)
		{
			var context = CSharpParsedFile.GetTypeResolveContext (Document.Compilation, document.Editor.Caret.Location);
			var resolvedEntity = ((IUnresolvedMember)entity).CreateResolved (context);
			return new MemberCompletionData (this, resolvedEntity, OutputFlags.IncludeGenerics | OutputFlags.HideArrayBrackets | OutputFlags.IncludeParameterName) {
				HideExtensionParameter = true
			};
		}

		ICompletionData ICompletionDataFactory.CreateTypeCompletionData (IType type, string shortType)
		{
			var result = new CompletionData (shortType, type.GetStockIcon ());
			if (!(type is ParameterizedType) && type.TypeParameterCount > 0) {
				var sb = new StringBuilder (shortType);
				sb.Append ("<");
				for (int i = 0; i < type.TypeParameterCount; i++) {
					if (i > 0)
						sb.Append (", ");
					sb.Append (GetAmbience ().GetString (type.GetDefinition ().TypeParameters[i], OutputFlags.ClassBrowserEntries));
				}
				sb.Append (">");
				result.DisplayText = sb.ToString ();
			}
			return result;
		}
		
		ICompletionData ICompletionDataFactory.CreateTypeCompletionData (IUnresolvedTypeDefinition type, string shortType)
		{
			return new CompletionData (shortType, type.GetStockIcon ());
		}
		
		ICompletionData ICompletionDataFactory.CreateLiteralCompletionData (string title, string description, string insertText)
		{
			return new CompletionData (title, "md-keyword", description, insertText ?? title);
		}

		ICompletionData ICompletionDataFactory.CreateNamespaceCompletionData (string name)
		{
			return new CompletionData (name, Stock.Namespace);
		}

		ICompletionData ICompletionDataFactory.CreateVariableCompletionData (IVariable variable)
		{
			return new VariableCompletionData (variable);
		}

		ICompletionData ICompletionDataFactory.CreateVariableCompletionData (IUnresolvedTypeParameter parameter)
		{
			return new CompletionData (parameter.Name, parameter.GetStockIcon ());
		}

		ICompletionData ICompletionDataFactory.CreateEventCreationCompletionData (string varName, IType delegateType, IEvent evt, string parameterDefinition, IUnresolvedMember currentMember, IUnresolvedTypeDefinition currentType)
		{
			return new EventCreationCompletionData (this, varName, delegateType, evt, parameterDefinition, currentMember, currentType);
		}
		
		ICompletionData ICompletionDataFactory.CreateNewOverrideCompletionData (int declarationBegin, IUnresolvedTypeDefinition type, IMember m)
		{
			return new NewOverrideCompletionData (this, declarationBegin, type, m);
		}
		ICompletionData ICompletionDataFactory.CreateNewPartialCompletionData (int declarationBegin, IUnresolvedTypeDefinition type, IUnresolvedMember m)
		{
			var ctx = CSharpParsedFile.GetTypeResolveContext (Compilation, document.Editor.Caret.Location);
			return new NewOverrideCompletionData (this, declarationBegin, type, m.CreateResolved (ctx));
		}
		IEnumerable<ICompletionData> ICompletionDataFactory.CreateCodeTemplateCompletionData ()
		{
			var result = new CompletionDataList ();
			CodeTemplateService.AddCompletionDataForMime ("text/x-csharp", result);
			return result;
		}
		
		IEnumerable<ICompletionData> ICompletionDataFactory.CreatePreProcessorDefinesCompletionData ()
		{
			var project = document.Project;
			if (project == null)
				yield break;
			var configuration = project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			var par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
			if (par == null)
				yield break;
			foreach (var define in par.DefineSymbols.Split (';', ',', ' ', '\t').Where (s => !string.IsNullOrWhiteSpace (s)))
				yield return new CompletionData (define, "md-keyword");
				
		}
		#endregion

		#region IParameterCompletionDataFactory implementation
		IParameterDataProvider IParameterCompletionDataFactory.CreateConstructorProvider (int startOffset, IType type)
		{
			return new ConstructorParameterDataProvider (startOffset, this, type);
		}

		IParameterDataProvider IParameterCompletionDataFactory.CreateMethodDataProvider (int startOffset, IEnumerable<IMethod> methods)
		{
			return new MethodParameterDataProvider (startOffset, this, methods);
		}
		
		IParameterDataProvider IParameterCompletionDataFactory.CreateDelegateDataProvider (int startOffset, IType type)
		{
			return new DelegateDataProvider (startOffset, this, type);
		}
		
		IParameterDataProvider IParameterCompletionDataFactory.CreateIndexerParameterDataProvider (int startOffset, IType type, AstNode resolvedNode)
		{
			return new IndexerParameterDataProvider (startOffset, this, type, resolvedNode);
		}
		
		IParameterDataProvider IParameterCompletionDataFactory.CreateTypeParameterDataProvider (int startOffset, IEnumerable<IType> types)
		{
			return new TemplateParameterDataProvider (startOffset, this, types);
		}
		#endregion
		
		#region TypeSystemSegmentTree
		
		internal TypeSystemSegmentTree typeSystemSegmentTree;
		
		internal class TypeSystemTreeSegment : TreeSegment
		{
			public IUnresolvedEntity Entity {
				get;
				private set;
			}
			
			public TypeSystemTreeSegment (int offset, int length, IUnresolvedEntity entity) : base (offset, length)
			{
				this.Entity = entity;
			}
		}
		
		internal class TypeSystemSegmentTree : SegmentTree<TypeSystemTreeSegment>, IMemberProvider
		{
			public IUnresolvedTypeDefinition GetTypeAt (int offset)
			{
				IUnresolvedTypeDefinition result = null;
				foreach (var seg in GetSegmentsAt (offset).Where (s => s.Entity is IUnresolvedTypeDefinition)) {
					if (result == null || result.Region.IsInside (seg.Entity.Region.Begin))
						result = (IUnresolvedTypeDefinition)seg.Entity;
				}
				return result;
			}
			
			public IUnresolvedMember GetMemberAt (int offset)
			{
				// Members don't overlap
				var seg = GetSegmentsAt (offset).FirstOrDefault (s => s.Entity is IUnresolvedMember);
				if (seg == null)
					return null;
				return (IUnresolvedMember)seg.Entity;
			}
			
			public TypeSystemTreeSegment GetMemberSegmentAt (int offset)
			{
				// Members don't overlap
				var seg = GetSegmentsAt (offset).FirstOrDefault (s => s.Entity is IUnresolvedMember);
				if (seg == null)
					return null;
				return seg;
			}
			
			
			internal static TypeSystemSegmentTree Create (MonoDevelop.Ide.Gui.Document document)
			{
				TypeSystemSegmentTree result = new TypeSystemSegmentTree ();
				
				foreach (var type in document.ParsedDocument.TopLevelTypeDefinitions)
					AddType (document, result, type);
				
				return result;
			}
			
			static void AddType (MonoDevelop.Ide.Gui.Document document, TypeSystemSegmentTree result, IUnresolvedTypeDefinition type)
			{
				int offset = document.Editor.LocationToOffset (type.Region.Begin);
				int endOffset = document.Editor.LocationToOffset (type.Region.End);
				result.Add (new TypeSystemTreeSegment (offset, endOffset - offset, type));
				foreach (var entity in type.Members) {
					offset = document.Editor.LocationToOffset (entity.Region.Begin);
					endOffset = document.Editor.LocationToOffset (entity.Region.End);
					result.Add (new TypeSystemTreeSegment (offset, endOffset - offset, entity));
				}
				
				foreach (var nested in type.NestedTypes)
					AddType (document, result, nested);
			}

			void IMemberProvider.GetCurrentMembers (int offset, out IUnresolvedTypeDefinition currentType, out IUnresolvedMember currentMember)
			{
				currentType = GetTypeAt (offset);
				currentMember = GetMemberAt (offset);
			}
		}
		
		public IUnresolvedTypeDefinition GetTypeAt (int offset)
		{
			if (typeSystemSegmentTree == null)
				return null;
			return typeSystemSegmentTree.GetTypeAt (offset);
		}
			
		public IUnresolvedMember GetMemberAt (int offset)
		{
			if (typeSystemSegmentTree == null)
				return null;
			return typeSystemSegmentTree.GetMemberAt (offset);
		}
		#endregion
	}
}
