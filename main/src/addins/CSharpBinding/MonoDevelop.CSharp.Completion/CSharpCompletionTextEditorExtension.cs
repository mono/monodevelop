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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Mono.TextEditor;

using MonoDevelop.Core;
using MonoDevelop.Debugger;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CodeGeneration;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components.Commands;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;

using MonoDevelop.CSharp.Project;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Refactoring.CodeActions;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CSharp.NRefactoryWrapper;

namespace MonoDevelop.CSharp.Completion
{
	public class CSharpCompletionTextEditorExtension : CompletionTextEditorExtension, IParameterCompletionDataFactory, ITextEditorMemberPositionProvider, IDebuggerExpressionResolver
	{
		SyntaxTree unit;
		static readonly SyntaxTree emptyUnit = new SyntaxTree ();
		SyntaxTree Unit {
			get {
				return unit ?? emptyUnit;
			}
			set {
				unit = value;
			}
		}


		public ICompilation UnresolvedFileCompilation {
			get;
			set;
		}
		
		public CSharpUnresolvedFile CSharpUnresolvedFile {
			get;
			set;
		}
		
		public ParsedDocument ParsedDocument {
			get {
				return DocumentContext.ParsedDocument;
			}
		}
		
		public ICompilation Compilation {
			get {
				return DocumentContext.Compilation;
			}
		}
		
		public MonoDevelop.Projects.Project Project {
			get {
				return DocumentContext.Project;
			}
		}
		
		CSharpFormattingPolicy policy;
		public CSharpFormattingPolicy FormattingPolicy {
			get {
				if (policy == null) {
					IEnumerable<string> types = MonoDevelop.Ide.DesktopService.GetMimeTypeInheritanceChain (MonoDevelop.CSharp.Formatting.CSharpFormatter.MimeType);
					if (DocumentContext.Project != null && DocumentContext.Project.Policies != null) {
						policy = base.DocumentContext.Project.Policies.Get<CSharpFormattingPolicy> (types);
					} else {
						policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<CSharpFormattingPolicy> (types);
					}
				}
				return policy;
			}
		}

		public override string CompletionLanguage {
			get {
				return "C#";
			}
		}

		internal MDRefactoringContext MDRefactoringCtx {
			get;
			private set;
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
			Initialize (doc.Editor, doc);
		}
		
		protected override void Initialize ()
		{
			base.Initialize ();
			var parsedDocument = DocumentContext.ParsedDocument;
			if (parsedDocument != null) {
				this.Unit = parsedDocument.GetAst<SyntaxTree> ();
				this.UnresolvedFileCompilation = DocumentContext.Compilation;
				this.CSharpUnresolvedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
				Editor.CaretPositionChanged += HandlePositionChanged;
			}
			
			DocumentContext.DocumentParsed += HandleDocumentParsed; 
		}

		CancellationTokenSource src = new CancellationTokenSource ();

		void StopPositionChangedTask ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		void HandlePositionChanged (object sender, EventArgs e)
		{
			StopPositionChangedTask ();

			var doc = DocumentContext;
			if (doc == null || Editor == null)
				return;
			MDRefactoringContext.Create (Editor, doc, Editor.CaretLocation, src.Token).ContinueWith (t => {
				MDRefactoringCtx = t.Result;
			}, TaskContinuationOptions.ExecuteSynchronously);
		}
		
		[CommandUpdateHandler (CodeGenerationCommands.ShowCodeGenerationWindow)]
		public void CheckShowCodeGenerationWindow (CommandInfo info)
		{
			info.Enabled = Editor != null && DocumentContext.GetContent<ICompletionWidget> () != null;
		}

		[CommandHandler (CodeGenerationCommands.ShowCodeGenerationWindow)]
		public void ShowCodeGenerationWindow ()
		{
			var completionWidget = DocumentContext.GetContent<ICompletionWidget> ();
			if (completionWidget == null)
				return;
			CodeCompletionContext completionContext = completionWidget.CreateCodeCompletionContext (Editor.CaretOffset);
			GenerateCodeWindow.ShowIfValid (Editor, DocumentContext, completionContext);
		}

		public override void Dispose ()
		{
			StopPositionChangedTask ();
			unit = null;
			CSharpUnresolvedFile = null;
			UnresolvedFileCompilation = null;
			DocumentContext.DocumentParsed -= HandleDocumentParsed;
			if (unstableTypeSystemSegmentTree != null) {
				unstableTypeSystemSegmentTree.RemoveListener ();
				unstableTypeSystemSegmentTree = null;
			}

			if (validTypeSystemSegmentTree != null) {
				validTypeSystemSegmentTree.RemoveListener ();
				validTypeSystemSegmentTree = null;
			}

			base.Dispose ();
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			var newDocument = DocumentContext.ParsedDocument;
			if (newDocument == null) 
				return;
			var newTree = TypeSystemSegmentTree.Create (Editor, DocumentContext);

			if (unstableTypeSystemSegmentTree != null)
				unstableTypeSystemSegmentTree.RemoveListener ();

			if (!newDocument.HasErrors) {
				if (validTypeSystemSegmentTree != null)
					validTypeSystemSegmentTree.RemoveListener ();
				validTypeSystemSegmentTree = newTree;
				unstableTypeSystemSegmentTree = null;
			} else {
				unstableTypeSystemSegmentTree = newTree;
			}
			newTree.InstallListener (Editor);

			this.Unit = newDocument.GetAst<SyntaxTree> ();
			this.CSharpUnresolvedFile = newDocument.ParsedFile as CSharpUnresolvedFile;
			this.UnresolvedFileCompilation = DocumentContext.Compilation;
			if (TypeSegmentTreeUpdated != null)
				TypeSegmentTreeUpdated (this, EventArgs.Empty);
		}
		public event EventHandler TypeSegmentTreeUpdated;

		public void UpdateParsedDocument ()
		{
			HandleDocumentParsed (null, null);
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool result = base.KeyPress (key, keyChar, modifier);
			
			if (/*EnableParameterInsight &&*/ (keyChar == ',' || keyChar == ')') && CanRunParameterCompletionCommand ())
				base.RunParameterCompletionCommand ();
			
//			if (IsInsideComment ())
//				ParameterInformationWindowManager.HideWindow (CompletionWidget);
			return result;
		}
		
		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
//			if (!EnableCodeCompletion)
//				return null;
			if (!EnableAutoCodeCompletion && char.IsLetter (completionChar))
				return null;

			//	var timer = Counters.ResolveTime.BeginTiming ();
			try {
				if (char.IsLetterOrDigit (completionChar) || completionChar == '_') {
					if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit (Editor.GetCharAt (completionContext.TriggerOffset - 2)))
						return null;
					triggerWordLength = 1;
				}
				return InternalHandleCodeCompletion (completionContext, completionChar, false, ref triggerWordLength);
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected code completion exception." + Environment.NewLine + 
					"FileName: " + DocumentContext.Name + Environment.NewLine + 
					"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
					"Line text: " + Editor.GetLineText (completionContext.TriggerLine), 
					e);
				return null;
			} finally {
				//			if (timer != null)
				//				timer.Dispose ();
			}
		}

		class CSharpCompletionDataList : CompletionDataList
		{
			public CSharpResolver Resolver {
				get;
				set;
			}
		}

		interface IListData
		{
			CSharpCompletionDataList List { get; set; }
		}

		ICompletionContextProvider CreateContextProvider ()
		{
			return new CompletionContextProvider (Editor, DocumentContext, validTypeSystemSegmentTree, unstableTypeSystemSegmentTree);
		}

		CSharpTypeResolveContext CreateTypeResolveContext ()
		{
			var compilation = UnresolvedFileCompilation;
			if (compilation == null)
				return null;
			var rctx = new CSharpTypeResolveContext (compilation.MainAssembly);
			var loc = Editor.CaretLocation;
			rctx = rctx.WithUsingScope (CSharpUnresolvedFile.GetUsingScope (loc).Resolve (compilation));
			int offset = Editor.CaretOffset;
			var curDef = GetTypeAt (offset);
			if (curDef != null) {
				var resolvedDef = curDef.Resolve (rctx).GetDefinition ();
				if (resolvedDef == null)
					return rctx;
				rctx = rctx.WithCurrentTypeDefinition (resolvedDef);
				var foundMember = GetMemberAt (offset);
				if (foundMember != null) {
					var curMember = resolvedDef.Members.FirstOrDefault (m => m.Region.FileName == foundMember.Region.FileName && m.Region.Begin == foundMember.Region.Begin);
					if (curMember != null)
						rctx = rctx.WithCurrentMember (curMember);
				}
			}

			return rctx;
		}
		CompletionEngineCache cache = new CompletionEngineCache ();
		ICompletionDataList InternalHandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, bool ctrlSpace, ref int triggerWordLength)
		{
			var data = Editor;
			if (data.EditMode != MonoDevelop.Ide.Editor.EditMode.Edit)
				return null;
//			if (data.CurrentMode is TextLinkEditMode) {
//				if (((TextLinkEditMode)data.CurrentMode).TextLinkMode == TextLinkMode.EditIdentifier)
//					return null;
//			}
			if (Unit == null || CSharpUnresolvedFile == null)
				return null;
			if(unstableTypeSystemSegmentTree == null && validTypeSystemSegmentTree == null)
				return null;

			var list = new CSharpCompletionDataList ();
			list.Resolver = CSharpUnresolvedFile != null ? CSharpUnresolvedFile.GetResolver (UnresolvedFileCompilation, Editor.CaretLocation) : new CSharpResolver (Compilation);
			var ctx = CreateTypeResolveContext ();
			if (ctx == null)
				return null;
			var completionDataFactory = new CompletionDataFactory (this, new CSharpResolver (ctx));
			if (MDRefactoringCtx == null) {
				src.Cancel ();
				MDRefactoringCtx = MDRefactoringContext.Create (Editor, DocumentContext, Editor.CaretLocation).Result;
			}

			var engine = new MonoCSharpCompletionEngine (
				this,
				new DocumentWrapper (data),
				CreateContextProvider (),
				completionDataFactory,
				DocumentContext.ProjectContent,
				ctx
			);
			completionDataFactory.Engine = engine;
			engine.AutomaticallyAddImports = AddImportedItemsToCompletionList.Value;
			engine.IncludeKeywordsInCompletionList = EnableAutoCodeCompletion || IncludeKeywordsInCompletionList.Value;
			engine.CompletionEngineCache = cache;
			if (FilterCompletionListByEditorBrowsable) {
				engine.EditorBrowsableBehavior = IncludeEditorBrowsableAdvancedMembers ? EditorBrowsableBehavior.IncludeAdvanced : EditorBrowsableBehavior.Normal;
			} else {
				engine.EditorBrowsableBehavior = EditorBrowsableBehavior.Ignore;
			}
			if (DocumentContext.HasProject && MonoDevelop.Ide.IdeApp.IsInitialized) {
				var configuration = DocumentContext.Project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
				var par = configuration != null ? configuration.CompilationParameters as CSharpCompilerParameters : null;
				if (par != null)
					engine.LanguageVersion = MonoDevelop.CSharp.Parser.TypeSystemParser.ConvertLanguageVersion (par.LangVersion);
			}

			engine.FormattingPolicy = FormattingPolicy.CreateOptions ();
			engine.EolMarker = data.EolMarker;
			engine.IndentString = data.Options.IndentationString;
			try {
				foreach (var cd in engine.GetCompletionData (completionContext.TriggerOffset, ctrlSpace)) {
					list.Add (cd);
					if (cd is IListData)
						((IListData)cd).List = list;
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while getting completion data.", e);
			}
			list.AutoCompleteEmptyMatch = engine.AutoCompleteEmptyMatch;
			list.AutoCompleteEmptyMatchOnCurlyBrace = engine.AutoCompleteEmptyMatchOnCurlyBracket;
			list.AutoSelect = engine.AutoSelect;
			list.DefaultCompletionString = engine.DefaultCompletionString;
			list.CloseOnSquareBrackets = engine.CloseOnSquareBrackets;
			if (ctrlSpace)
				list.AutoCompleteUniqueMatch = true;
			return list.Count > 0 ? list : null;
		}
		
		public override ICompletionDataList CodeCompletionCommand (CodeCompletionContext completionContext)
		{
			int triggerWordLength = 0;
			char ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt (completionContext.TriggerOffset - 1) : '\0';
			return InternalHandleCodeCompletion (completionContext, ch, true, ref triggerWordLength);
		}

		static bool HasAllUsedParameters (IParameterDataProvider provider, List<string> list, int n)
		{
			int pc = provider.GetParameterCount (n);
			foreach (var usedParam in list) {
				bool found = false;
				for (int m = 0; m < pc; m++) {
					if (usedParam == provider.GetParameterName (n, m)){
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}
		public override int GuessBestMethodOverload (IParameterDataProvider provider, int currentOverload)
		{
			var ctx = CreateTypeResolveContext ();
			if (ctx == null)
				return -1;
			var engine = new CSharpParameterCompletionEngine (
				new DocumentWrapper (Editor),
				CreateContextProvider (),
				this,
				DocumentContext.ProjectContent,
				ctx
				);
			List<string> list;
			int cparam = engine.GetCurrentParameterIndex (provider.StartOffset, Editor.CaretOffset, out list);
			if (cparam > provider.GetParameterCount (currentOverload) && !provider.AllowParameterList (currentOverload) || !HasAllUsedParameters (provider, list, currentOverload)) {
				// Look for an overload which has more parameters
				int bestOverload = -1;
				int bestParamCount = int.MaxValue;
				for (int n = 0; n < provider.Count; n++) {
					int pc = provider.GetParameterCount (n);
					if (pc < bestParamCount && pc >= cparam) {

						if (HasAllUsedParameters (provider, list, n)) {
							bestOverload = n;
							bestParamCount = pc;
						}
					}


				}
				if (bestOverload == -1) {
					for (int n=0; n<provider.Count; n++) {
						if (provider.AllowParameterList (n) && HasAllUsedParameters (provider, list, n)) {
							bestOverload = n;
							break;
						}
					}
				}
				return bestOverload;
			}
			return -1;
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
		
		public override ParameterDataProvider HandleParameterCompletion (CodeCompletionContext completionContext, char completionChar)
		{
//			if (!EnableCodeCompletion)
//				return null;
			if (Unit == null || CSharpUnresolvedFile == null)
				return null;
			var ctx = CreateTypeResolveContext ();
			if (ctx == null)
				return null;

			if (completionChar != '(' && completionChar != ',')
				return null;

			try {
				var engine = new CSharpParameterCompletionEngine (
					new DocumentWrapper (Editor),
					CreateContextProvider (),
					this,
					DocumentContext.ProjectContent,
					ctx
				);
				return engine.GetParameterDataProvider (completionContext.TriggerOffset, completionChar) as ParameterDataProvider;
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected parameter completion exception." + Environment.NewLine + 
					"FileName: " + DocumentContext.Name + Environment.NewLine + 
					"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine + 
					"Line text: " + Editor.GetLineText (completionContext.TriggerLine), 
					e);
				return null;
			} finally {
				//			if (timer != null)
				//				timer.Dispose ();
			}
		}
		
		List<string> GetUsedNamespaces ()
		{
			var scope = CSharpUnresolvedFile.GetUsingScope (Editor.CaretLocation);
			var result = new List<string> ();
			while (scope != null) {
				result.Add (scope.NamespaceName);
				var ctx = CSharpUnresolvedFile.GetResolver (DocumentContext.Compilation, scope.Region.Begin);
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

		public override bool GetParameterCompletionCommandOffset (out int cpos)
		{
			var ctx = CreateTypeResolveContext ();
			if (ctx == null) {
				cpos = -1;
				return false;
			}

			var engine = new CSharpParameterCompletionEngine (
				new DocumentWrapper (Editor),
				CreateContextProvider (),
				this,
				DocumentContext.ProjectContent,
				ctx
			);
			engine.SetOffset (Editor.CaretOffset);
			return engine.GetParameterCompletionCommandOffset (out cpos);
		}

		public override int GetCurrentParameterIndex (int startOffset)
		{
			var ctx = CreateTypeResolveContext ();
			if (ctx == null)
				return -1;

			var engine = new CSharpParameterCompletionEngine (
				new DocumentWrapper (Editor),
				CreateContextProvider (),
				this,
				DocumentContext.ProjectContent,
				ctx
			);
			List<string> list;
			return engine.GetCurrentParameterIndex (startOffset, Editor.CaretOffset, out list);
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
		internal class CompletionDataFactory : ICompletionDataFactory
		{
			internal readonly CSharpCompletionTextEditorExtension ext;
//			readonly CSharpResolver state;
			readonly TypeSystemAstBuilder builder;

			public CSharpCompletionEngine Engine {
				get;
				set;
			}

			public CompletionDataFactory (CSharpCompletionTextEditorExtension ext, CSharpResolver state)
			{
//				this.state = state;
				if (state != null)
					builder = new TypeSystemAstBuilder(state);
				this.ext = ext;
			}
			
			ICompletionData ICompletionDataFactory.CreateEntityCompletionData (IEntity entity)
			{
				return new MemberCompletionData (this, entity, OutputFlags.IncludeGenerics | OutputFlags.HideArrayBrackets | OutputFlags.IncludeParameterName) {
					HideExtensionParameter = true
				};
			}

			class GenericTooltipCompletionData : CompletionData, IListData
			{
				readonly Func<CSharpCompletionDataList, bool, TooltipInformation> tooltipFunc;

				#region IListData implementation

				CSharpCompletionDataList list;
				public CSharpCompletionDataList List {
					get {
						return list;
					}
					set {
						list = value;
						if (overloads != null) {
							foreach (var overload in overloads.Skip (1)) {
								var ld = overload as IListData;
								if (ld != null)
									ld.List = list;
							}
						}
					}
				}

				#endregion

				public GenericTooltipCompletionData (Func<CSharpCompletionDataList, bool, TooltipInformation> tooltipFunc, string text, string icon) : base (text, icon)
				{
					this.tooltipFunc = tooltipFunc;
				}

				public GenericTooltipCompletionData (Func<CSharpCompletionDataList, bool, TooltipInformation> tooltipFunc, string text, string icon, string description, string completionText) : base (text, icon, description, completionText)
				{
					this.tooltipFunc = tooltipFunc;
				}

				public override TooltipInformation CreateTooltipInformation (bool smartWrap)
				{
					return tooltipFunc != null ? tooltipFunc (List, smartWrap) : new TooltipInformation ();
				}

				protected List<ICompletionData> overloads;
				public override bool HasOverloads {
					get { return overloads != null && overloads.Count > 0; }
				}

				public override IEnumerable<ICompletionData> OverloadedData {
					get {
						return overloads;
					}
				}

				public override void AddOverload (ICSharpCode.NRefactory.Completion.ICompletionData data)
				{
					if (overloads == null) {
						overloads = new List<ICompletionData> ();
						overloads.Add (this);
					}
					overloads.Add (data);
				}

				public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
				{
					var currentWord = GetCurrentWord (window);
					if (CompletionText == "new()" && keyChar == '(') {
						window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, "new");
					} else {
						window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, CompletionText);
					}
				}

			}

			class LazyGenericTooltipCompletionData : GenericTooltipCompletionData
			{
				Lazy<string> displayText;
				public override string DisplayText {
					get {
						return displayText.Value;
					}
				}

				public override string CompletionText {
					get {
						return displayText.Value;
					}
				}

				public LazyGenericTooltipCompletionData (Func<CSharpCompletionDataList, bool, TooltipInformation> tooltipFunc, Lazy<string> displayText, string icon) : base (tooltipFunc, null, icon)
				{
					this.displayText = displayText;
				}
			}

			class TypeCompletionData : LazyGenericTooltipCompletionData, IListData
			{
				IType type;
				CSharpCompletionTextEditorExtension ext;
				CSharpUnresolvedFile file;
				ICompilation compilation;
//				CSharpResolver resolver;

				string IdString {
					get {
						return DisplayText + type.TypeParameterCount;
					}
				}

				public override string CompletionText {
					get {
						if (type.TypeParameterCount > 0 && !type.IsParameterized)
							return type.Name;
						return base.CompletionText;
					}
				}

				public override TooltipInformation CreateTooltipInformation (bool smartWrap)
				{
					var def = type.GetDefinition ();
					var result = def != null ? MemberCompletionData.CreateTooltipInformation (compilation, file, List.Resolver, ext.Editor, ext.FormattingPolicy, def, smartWrap)  : new TooltipInformation ();
					if (ConflictingTypes != null) {
						var conflicts = new StringBuilder ();
						var sig = new SignatureMarkupCreator (List.Resolver, ext.FormattingPolicy.CreateOptions ());
						for (int i = 0; i < ConflictingTypes.Count; i++) {
							var ct = ConflictingTypes[i];
							if (i > 0)
								conflicts.AppendLine (",");
//							if ((i + 1) % 5 == 0)
//								conflicts.Append (Environment.NewLine + "\t");
							conflicts.Append (sig.GetTypeReferenceString (((TypeCompletionData)ct).type));
						}
						result.AddCategory ("Type Conflicts", conflicts.ToString ());
					}
					return result;
				}

				public TypeCompletionData (IType type, CSharpCompletionTextEditorExtension ext, Lazy<string> displayText, string icon, bool addConstructors) : base (null, displayText, icon)
				{
					this.type = type;
					this.ext = ext;
					this.file = ext.CSharpUnresolvedFile;
					this.compilation = ext.UnresolvedFileCompilation;

				}

				Dictionary<string, ICSharpCode.NRefactory.Completion.ICompletionData> addedDatas = new Dictionary<string, ICSharpCode.NRefactory.Completion.ICompletionData> ();

				List<ICompletionData> ConflictingTypes = null;

				public override void AddOverload (ICSharpCode.NRefactory.Completion.ICompletionData data)
				{
					if (overloads == null)
						addedDatas [IdString] = this;

					if (data is TypeCompletionData) {
						string id = ((TypeCompletionData)data).IdString;
						ICompletionData oldData;
						if (addedDatas.TryGetValue (id, out oldData)) {
							var old = (TypeCompletionData)oldData;
							if (old.ConflictingTypes == null)
								old.ConflictingTypes = new List<ICompletionData> ();
							old.ConflictingTypes.Add (data);
							return;
						}
						addedDatas [id] = data;
					}

					base.AddOverload (data);
				}

			}

			ICompletionData ICompletionDataFactory.CreateEntityCompletionData (IEntity entity, string text)
			{
				return new GenericTooltipCompletionData ((list, sw) => MemberCompletionData.CreateTooltipInformation (ext, list.Resolver, entity, sw), text, entity.GetStockIcon ());
			}

			ICompletionData ICompletionDataFactory.CreateTypeCompletionData (IType type, bool showFullName, bool isInAttributeContext, bool addConstructors)
			{
				if (addConstructors) {
					ICompletionData constructorResult = null;
					foreach (var ctor in type.GetConstructors ()) {
						if (constructorResult != null) {
							constructorResult.AddOverload (((ICompletionDataFactory)this).CreateEntityCompletionData (ctor));
						} else {
							constructorResult = ((ICompletionDataFactory)this).CreateEntityCompletionData (ctor);
						}
					}
					return constructorResult;
				}

				Lazy<string> displayText = new Lazy<string> (delegate {
					string name = showFullName ? builder.ConvertType(type).ToString() : type.Name; 
					if (isInAttributeContext && name.EndsWith("Attribute") && name.Length > "Attribute".Length) {
						name = name.Substring(0, name.Length - "Attribute".Length);
					}
					return name;
				});

				var result = new TypeCompletionData (type, ext,
					displayText, 
					type.GetStockIcon (),
					addConstructors);
				return result;
			}

			ICompletionData ICompletionDataFactory.CreateMemberCompletionData(IType type, IEntity member)
			{
				Lazy<string> displayText = new Lazy<string> (delegate {
					string name = builder.ConvertType(type).ToString(); 
					return name + "."+ member.Name;
				});

				var result = new LazyGenericTooltipCompletionData (
					(List, sw) => new TooltipInformation (), 
					displayText, 
					member.GetStockIcon ());
				return result;
			}


			ICompletionData ICompletionDataFactory.CreateLiteralCompletionData (string title, string description, string insertText)
			{
				return new GenericTooltipCompletionData ((list, smartWrap) => {
					var sig = new SignatureMarkupCreator (list.Resolver, ext.FormattingPolicy.CreateOptions ());
					sig.BreakLineAfterReturnType = smartWrap;
					return sig.GetKeywordTooltip (title, null);
				}, title, "md-keyword", description, insertText ?? title);
			}

			class XmlDocCompletionData : CompletionData, IListData
			{
				readonly CSharpCompletionTextEditorExtension ext;
				readonly string title;

				#region IListData implementation

				CSharpCompletionDataList list;
				public CSharpCompletionDataList List {
					get {
						return list;
					}
					set {
						list = value;
					}
				}

				#endregion

				public XmlDocCompletionData (CSharpCompletionTextEditorExtension ext, string title, string description, string insertText) : base (title, "md-keyword", description, insertText ?? title)
				{
					this.ext = ext;
					this.title = title;
				}

				public override TooltipInformation CreateTooltipInformation (bool smartWrap)
				{
					var sig = new SignatureMarkupCreator (List.Resolver, ext.FormattingPolicy.CreateOptions ());
					sig.BreakLineAfterReturnType = smartWrap;
					return sig.GetKeywordTooltip (title, null);
				}



				public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
				{
					var currentWord = GetCurrentWord (window);
					var text = CompletionText;
					if (keyChar != '>')
						text += ">";
					window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, text);
				}
			}

			ICompletionData ICompletionDataFactory.CreateXmlDocCompletionData (string title, string description, string insertText)
			{
				return new XmlDocCompletionData (ext, title, description, insertText);
			}

			ICompletionData ICompletionDataFactory.CreateNamespaceCompletionData (INamespace name)
			{
				return new CompletionData (name.Name, AstStockIcons.Namespace, "", CSharpAmbience.FilterName (name.Name));
			}

			ICompletionData ICompletionDataFactory.CreateVariableCompletionData (IVariable variable)
			{
				return new VariableCompletionData (ext, variable);
			}

			ICompletionData ICompletionDataFactory.CreateVariableCompletionData (ITypeParameter parameter)
			{
				return new CompletionData (parameter.Name, parameter.GetStockIcon ());
			}

			ICompletionData ICompletionDataFactory.CreateEventCreationCompletionData (string varName, IType delegateType, IEvent evt, string parameterDefinition, IUnresolvedMember currentMember, IUnresolvedTypeDefinition currentType)
			{
				return new EventCreationCompletionData (ext, varName, delegateType, evt, parameterDefinition, currentMember, currentType);
			}
			
			ICompletionData ICompletionDataFactory.CreateNewOverrideCompletionData (int declarationBegin, IUnresolvedTypeDefinition type, IMember m)
			{
				return new NewOverrideCompletionData (ext, declarationBegin, type, m);
			}
			ICompletionData ICompletionDataFactory.CreateNewPartialCompletionData (int declarationBegin, IUnresolvedTypeDefinition type, IUnresolvedMember m)
			{
				var ctx = ext.CSharpUnresolvedFile.GetTypeResolveContext (ext.UnresolvedFileCompilation, ext.Editor.CaretLocation);
				return new NewOverrideCompletionData (ext, declarationBegin, type, m.CreateResolved (ctx));
			}
			IEnumerable<ICompletionData> ICompletionDataFactory.CreateCodeTemplateCompletionData ()
			{
				var result = new CompletionDataList ();
				if (EnableAutoCodeCompletion || IncludeCodeSnippetsInCompletionList.Value) {
					CodeTemplateService.AddCompletionDataForMime ("text/x-csharp", result);
				}
				return result;
			}
			
			IEnumerable<ICompletionData> ICompletionDataFactory.CreatePreProcessorDefinesCompletionData ()
			{
				var project = ext.DocumentContext.Project;
				if (project == null)
					yield break;
				var configuration = project.GetConfiguration (MonoDevelop.Ide.IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
				if (configuration == null)
					yield break;
				foreach (var define in configuration.GetDefineSymbols ())
					yield return new CompletionData (define, "md-keyword");
					
			}

			class FormatItemCompletionData : CompletionData
			{
				string format;
				string description;
				object example;

				public FormatItemCompletionData (string format, string description, object example)
				{
					this.format = format;
					this.description = description;
					this.example = example;
				}

				
				public override string DisplayText {
					get {
						return format;
					}
				}
				public override string GetDisplayDescription (bool isSelected)
				{
					return "- <span foreground=\"darkgray\" size='small'>" + description + "</span>";
				}


				string rightSideDescription = null;
				public override string GetRightSideDescription (bool isSelected)
				{
					if (rightSideDescription == null) {
						try {
							rightSideDescription = "<span size='small'>" + string.Format ("{0:" +format +"}", example) +"</span>";
						} catch (Exception e) {
							rightSideDescription = "";
							LoggingService.LogError ("Format error.", e);
						}
					}
					return rightSideDescription;
				}

				public override string CompletionText {
					get {
						return format;
					}
				}

				public override int CompareTo (object obj)
				{
					return 0;
				}
			}


			ICompletionData ICompletionDataFactory.CreateFormatItemCompletionData(string format, string description, object example)
			{
				return new FormatItemCompletionData (format, description, example);
			}


			class ImportSymbolCompletionData : CompletionData, IEntityCompletionData
			{
				readonly IType type;
				readonly bool useFullName;
				readonly CSharpCompletionTextEditorExtension ext;
				public IType Type {
					get { return this.type; }
				}

				public ImportSymbolCompletionData (CSharpCompletionTextEditorExtension ext, bool useFullName, IType type, bool addConstructors)
				{
					this.ext = ext;
					this.useFullName = useFullName;
					this.type = type;
					this.DisplayFlags |= ICSharpCode.NRefactory.Completion.DisplayFlags.IsImportCompletion;
				}

				public override TooltipInformation CreateTooltipInformation (bool smartWrap)
				{
					return MemberCompletionData.CreateTooltipInformation (ext, null, type.GetDefinition (), smartWrap);
				}

				bool initialized = false;
				bool generateUsing, insertNamespace;

				void Initialize ()
				{
					if (initialized)
						return;
					initialized = true;
					if (string.IsNullOrEmpty (type.Namespace)) 
						return;
					generateUsing = !useFullName;
					insertNamespace = useFullName;
				}

				#region IActionCompletionData implementation
				public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
				{
					Initialize ();
					var doc = ext.DocumentContext;
					using (var undo = ext.Editor.OpenUndoGroup ()) {
						string text = insertNamespace ? type.Namespace + "." + type.Name : type.Name;
						if (text != GetCurrentWord (window)) {
							if (window.WasShiftPressed && generateUsing) 
								text = type.Namespace + "." + text;
							window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, GetCurrentWord (window), text);
						}
					
						if (!window.WasShiftPressed && generateUsing) {
							var generator = CodeGenerator.CreateGenerator (ext.Editor, doc);
							if (generator != null) {
								generator.AddGlobalNamespaceImport (ext.Editor, doc, type.Namespace);
							}
						}
					}
					ka |= KeyActions.Ignore;
				}
				#endregion

				#region ICompletionData implementation
				public override IconId Icon {
					get {
						return type.GetStockIcon ();
					}
				}
				
				public override string DisplayText {
					get {
						return type.Name;
					}
				}

				static string GetDefaultDisplaySelection (string description, bool isSelected)
				{
					if (!isSelected)
						return "<span foreground=\"darkgray\">" + description + "</span>";
					return description;
				}

				string displayDescription = null;
				public override string GetDisplayDescription (bool isSelected)
				{
					if (displayDescription == null) {
						Initialize ();
						if (generateUsing || insertNamespace) {
							displayDescription = string.Format (GettextCatalog.GetString ("(from '{0}')"), type.Namespace);
						} else {
							displayDescription = "";
						}
					}
					return GetDefaultDisplaySelection (displayDescription, isSelected);
				}

				public override string Description {
					get {
						return type.Namespace;
					}
				}

				public override string CompletionText {
					get {
						return type.Name;
					}
				}
				#endregion


				List<CompletionData> overloads;

				public override IEnumerable<ICompletionData> OverloadedData {
					get {
						yield return this;
						if (overloads == null)
							yield break;
						foreach (var overload in overloads)
							yield return overload;
					}
				}

				public override bool HasOverloads {
					get { return overloads != null && overloads.Count > 0; }
				}

				public override void AddOverload (ICSharpCode.NRefactory.Completion.ICompletionData data)
				{
					AddOverload ((ImportSymbolCompletionData)data);
				}

				void AddOverload (ImportSymbolCompletionData overload)
				{
					if (overloads == null)
						overloads = new List<CompletionData> ();
					overloads.Add (overload);
				}

				IEntity IEntityCompletionData.Entity {
					get {
						return type.GetDefinition ();
					}
				}
			}


			ICompletionData ICompletionDataFactory.CreateImportCompletionData(IType type, bool useFullName, bool addConstructors)
			{
				return new ImportSymbolCompletionData (ext, useFullName, type, addConstructors);
			}

		}
		#endregion

		#region IParameterCompletionDataFactory implementation
		IParameterDataProvider IParameterCompletionDataFactory.CreateConstructorProvider (int startOffset, IType type)
		{
			return new ConstructorParameterDataProvider (startOffset, this, type);
		}

		IParameterDataProvider IParameterCompletionDataFactory.CreateConstructorProvider (int startOffset, IType type, AstNode initializer)
		{
			return new ConstructorParameterDataProvider (startOffset, this, type, initializer);
		}

		IParameterDataProvider IParameterCompletionDataFactory.CreateMethodDataProvider (int startOffset, IEnumerable<IMethod> methods)
		{
			return new MethodParameterDataProvider (startOffset, this, methods);
		}
		
		IParameterDataProvider IParameterCompletionDataFactory.CreateDelegateDataProvider (int startOffset, IType type)
		{
			return new DelegateDataProvider (startOffset, this, type);
		}
		
		IParameterDataProvider IParameterCompletionDataFactory.CreateIndexerParameterDataProvider (int startOffset, IType type, IEnumerable<IProperty> indexers, AstNode resolvedNode)
		{
			var arrayType = type as ArrayType;
			if (arrayType != null)
				return new ArrayTypeParameterDataProvider (startOffset, this, arrayType);
			return new IndexerParameterDataProvider (startOffset, this, type, indexers, resolvedNode);
		}
		
		IParameterDataProvider IParameterCompletionDataFactory.CreateTypeParameterDataProvider (int startOffset, IEnumerable<IType> types)
		{
			return new TypeParameterDataProvider (startOffset, this, types);
		}

		IParameterDataProvider IParameterCompletionDataFactory.CreateTypeParameterDataProvider (int startOffset, IEnumerable<IMethod> methods)
		{
			return new TypeParameterDataProvider (startOffset, this, methods);
		}
		#endregion

		#region IDebuggerExpressionResolver implementation

		static string GetIdentifierName (IReadonlyTextDocument editor, Identifier id, out int startOffset)
		{
			startOffset = editor.LocationToOffset (id.StartLocation.Line, id.StartLocation.Column);

			return editor.GetTextBetween (id.StartLocation, id.EndLocation);
		}

		internal static string ResolveExpression (IReadonlyTextDocument editor, ResolveResult result, AstNode node, out int startOffset)
		{
			//Console.WriteLine ("result is a {0}", result.GetType ().Name);
			startOffset = -1;

			if (result is NamespaceResolveResult ||
				result is ConversionResolveResult ||
				result is ConstantResolveResult ||
				result is ForEachResolveResult ||
				result is TypeIsResolveResult ||
				result is TypeOfResolveResult ||
				result is ErrorResolveResult)
				return null;

			if (result.IsCompileTimeConstant)
				return null;

			startOffset = editor.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);

			if (result is InvocationResolveResult) {
				var ir = (InvocationResolveResult) result;
				if (ir.Member.Name == ".ctor") {
					// if the user is hovering over something like "new Abc (...)", we want to show them type information for Abc
					return ir.Member.DeclaringType.FullName;
				}

				// do not support general method invocation for tooltips because it could cause side-effects
				return null;
			} else if (result is LocalResolveResult) {
				if (node is ParameterDeclaration) {
					// user is hovering over a method parameter, but we don't want to include the parameter type
					var param = (ParameterDeclaration) node;

					return GetIdentifierName (editor, param.NameToken, out startOffset);
				}

				if (node is VariableInitializer) {
					// user is hovering over something like "int fubar = 5;", but we don't want the expression to include the " = 5"
					var variable = (VariableInitializer) node;

					return GetIdentifierName (editor, variable.NameToken, out startOffset);
				}
			} else if (result is MemberResolveResult) {
				var mr = (MemberResolveResult) result;

				if (node is PropertyDeclaration) {
					var prop = (PropertyDeclaration) node;
					var name = GetIdentifierName (editor, prop.NameToken, out startOffset);

					// if the property is static, then we want to return "Full.TypeName.Property"
					if (prop.Modifiers.HasFlag (Modifiers.Static))
						return mr.Member.DeclaringType.FullName + "." + name;

					// otherwise we want to return "this.Property" so that it won't conflict with anything else in the local scope
					return "this." + name;
				}

				if (node is FieldDeclaration) {
					var field = (FieldDeclaration) node;
					var name = GetIdentifierName (editor, field.NameToken, out startOffset);

					// if the field is static, then we want to return "Full.TypeName.Field"
					if (field.Modifiers.HasFlag (Modifiers.Static))
						return mr.Member.DeclaringType.FullName + "." + name;

					// otherwise we want to return "this.Field" so that it won't conflict with anything else in the local scope
					return "this." + name;
				}

				if (node is VariableInitializer) {
					// user is hovering over a field declaration that includes initialization
					var variable = (VariableInitializer) node;
					var name = GetIdentifierName (editor, variable.NameToken, out startOffset);

					// walk up the AST to find the FieldDeclaration so that we can determine if it is static or not
					var field = variable.GetParent<FieldDeclaration> ();

					// if the field is static, then we want to return "Full.TypeName.Field"
					if (field.Modifiers.HasFlag (Modifiers.Static))
						return mr.Member.DeclaringType.FullName + "." + name;

					// otherwise we want to return "this.Field" so that it won't conflict with anything else in the local scope
					return "this." + name;
				}

				if (node is NamedExpression) {
					// user is hovering over 'Property' in an expression like: var fubar = new Fubar () { Property = baz };
					var variable = node.GetParent<VariableInitializer> ();
					if (variable != null) {
						var variableName = GetIdentifierName (editor, variable.NameToken, out startOffset);
						var name = GetIdentifierName (editor, ((NamedExpression) node).NameToken, out startOffset);

						return variableName + "." + name;
					}
				}
			} else if (result is TypeResolveResult) {
				return ((TypeResolveResult) result).Type.FullName;
			}

			return editor.GetTextBetween (node.StartLocation, node.EndLocation);
		}

		static bool TryResolveAt (Document doc, MonoDevelop.Ide.Editor.DocumentLocation loc, out ResolveResult result, out AstNode node)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");

			result = null;
			node = null;

			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null)
				return false;

			var unit = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;

			if (unit == null || parsedFile == null)
				return false;

			try {
				result = ResolveAtLocation.Resolve (new Lazy<ICompilation> (() => doc.Compilation), parsedFile, unit, loc, out node);
				if (result == null || node is Statement)
					return false;
			} catch {
				return false;
			}

			return true;
		}

		string IDebuggerExpressionResolver.ResolveExpression (IReadonlyTextDocument editor, Document doc, int offset, out int startOffset)
		{
			ResolveResult result;
			AstNode node;

			var loc = editor.OffsetToLocation (offset);
			if (!TryResolveAt (doc, loc, out result, out node)) {
				startOffset = -1;
				return null;
			}

			return ResolveExpression (editor, result, node, out startOffset);
		}

		#endregion
		
		#region TypeSystemSegmentTree

		TypeSystemSegmentTree validTypeSystemSegmentTree;
		TypeSystemSegmentTree unstableTypeSystemSegmentTree;

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

		internal TypeSystemTreeSegment GetMemberSegmentAt (int offset)
		{
			TypeSystemTreeSegment result = null;
			if (unstableTypeSystemSegmentTree != null)
				result = unstableTypeSystemSegmentTree.GetMemberSegmentAt (offset);
			if (result == null && validTypeSystemSegmentTree != null)
				result = validTypeSystemSegmentTree.GetMemberSegmentAt (offset);
			return result;
		}
		
		internal class TypeSystemSegmentTree : SegmentTree<TypeSystemTreeSegment>
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
			
			
			internal static TypeSystemSegmentTree Create (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context)
			{
				TypeSystemSegmentTree result = new TypeSystemSegmentTree ();
				
				foreach (var type in context.ParsedDocument.TopLevelTypeDefinitions)
					AddType (editor, context, result, type);
				
				return result;
			}
			
			static void AddType (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext context, TypeSystemSegmentTree result, IUnresolvedTypeDefinition type)
			{
				int offset = editor.LocationToOffset (type.Region.Begin);
				int endOffset = type.Region.End.IsEmpty ? int.MaxValue : editor.LocationToOffset (type.Region.End);
				if (endOffset < 0)
					endOffset = int.MaxValue;
				result.Add (new TypeSystemTreeSegment (offset, endOffset - offset, type));
				foreach (var entity in type.Members) {
					if (entity.IsSynthetic)
						continue;
					offset = editor.LocationToOffset (entity.Region.Begin);
					endOffset = editor.LocationToOffset (entity.Region.End);
					if (endOffset < 0)
						endOffset = int.MaxValue;
					result.Add (new TypeSystemTreeSegment (offset, endOffset - offset, entity));
				}
				
				foreach (var nested in type.NestedTypes)
					AddType (editor, context, result, nested);
			}

			ITextDocument ownerDocument;
			public void InstallListener (ITextDocument doc)
			{
				if (ownerDocument != null)
					throw new InvalidOperationException ("Segment tree already installed");
				ownerDocument = doc;
				doc.TextChanged += UpdateOnTextChanged;
			}

			public new void RemoveListener ()
			{
				if (ownerDocument == null)
					throw new InvalidOperationException ("Segment tree is not installed");
				ownerDocument.TextChanged -= UpdateOnTextChanged;
				ownerDocument = null;
			}

			void UpdateOnTextChanged (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
			{
				UpdateOnTextReplace (sender, new DocumentChangeEventArgs (e.Offset, e.RemovedText.Text, e.InsertedText.Text));
			}
		}
		
		public IUnresolvedTypeDefinition GetTypeAt (int offset)
		{
			if (unstableTypeSystemSegmentTree == null && validTypeSystemSegmentTree == null)
				return null;
			IUnresolvedTypeDefinition type = null;
			if (unstableTypeSystemSegmentTree != null)
				type = unstableTypeSystemSegmentTree.GetTypeAt (offset);
			if (type == null && validTypeSystemSegmentTree != null)
				type = validTypeSystemSegmentTree.GetTypeAt (offset);
			return type;
		}
			
		public IUnresolvedMember GetMemberAt (int offset)
		{
			if (unstableTypeSystemSegmentTree == null && validTypeSystemSegmentTree == null)
				return null;

			IUnresolvedMember member = null;
			if (unstableTypeSystemSegmentTree != null)
				member = unstableTypeSystemSegmentTree.GetMemberAt (offset);
			if (member == null && validTypeSystemSegmentTree != null)
				member = validTypeSystemSegmentTree.GetMemberAt (offset);

			return member;
		}
		#endregion


		class CompletionContextProvider : ICompletionContextProvider
		{
			MonoDevelop.Ide.Editor.TextEditor editor;
			DocumentContext document;
			TypeSystemSegmentTree validTypeSystemSegmentTree;
			TypeSystemSegmentTree unstableTypeSystemSegmentTree;

			public CompletionContextProvider (MonoDevelop.Ide.Editor.TextEditor editor, DocumentContext document, TypeSystemSegmentTree validTypeSystemSegmentTree, TypeSystemSegmentTree unstableTypeSystemSegmentTree)
			{
				this.editor = editor;
				this.document = document;
				this.validTypeSystemSegmentTree = validTypeSystemSegmentTree;
				this.unstableTypeSystemSegmentTree = unstableTypeSystemSegmentTree;
			}

			IList<string> ICompletionContextProvider.ConditionalSymbols {
				get {
					return document.ParsedDocument.GetAst<SyntaxTree> ().ConditionalSymbols;
				}
			}

			void ICompletionContextProvider.GetCurrentMembers (int offset, out IUnresolvedTypeDefinition currentType, out IUnresolvedMember currentMember)
			{
				currentType = GetTypeAt (offset);
				currentMember = GetMemberAt (offset);
			}

			public IUnresolvedTypeDefinition GetTypeAt (int offset)
			{
				if (unstableTypeSystemSegmentTree == null && validTypeSystemSegmentTree == null)
					return null;
				IUnresolvedTypeDefinition type = null;
				if (unstableTypeSystemSegmentTree != null)
					type = unstableTypeSystemSegmentTree.GetTypeAt (offset);
				if (type == null && validTypeSystemSegmentTree != null)
					type = validTypeSystemSegmentTree.GetTypeAt (offset);
				return type;
			}

			public IUnresolvedMember GetMemberAt (int offset)
			{
				if (unstableTypeSystemSegmentTree == null && validTypeSystemSegmentTree == null)
					return null;

				IUnresolvedMember member = null;
				if (unstableTypeSystemSegmentTree != null)
					member = unstableTypeSystemSegmentTree.GetMemberAt (offset);
				if (member == null && validTypeSystemSegmentTree != null)
					member = validTypeSystemSegmentTree.GetMemberAt (offset);
				return member;
			}

			Tuple<string, TextLocation> ICompletionContextProvider.GetMemberTextToCaret (int caretOffset, IUnresolvedTypeDefinition currentType, IUnresolvedMember currentMember)
			{
				int startOffset;
				if (currentMember != null && currentType != null && currentType.Kind != TypeKind.Enum) {
					startOffset = editor.LocationToOffset(currentMember.Region.Begin);
				} else if (currentType != null) {
					startOffset = editor.LocationToOffset(currentType.Region.Begin);
				} else {
					startOffset = 0;
				}
				while (startOffset > 0) {
					char ch = editor.GetCharAt(startOffset - 1);
					if (ch != ' ' && ch != '\t') {
						break;
					}
					--startOffset;
				}
				return Tuple.Create (caretOffset > startOffset ? editor.GetTextAt (startOffset, caretOffset - startOffset) : "", 
				                     (TextLocation)editor.OffsetToLocation (startOffset));
			}


			CSharpAstResolver ICompletionContextProvider.GetResolver (CSharpResolver resolver, AstNode rootNode)
			{
				return new CSharpAstResolver (resolver, rootNode, document.ParsedDocument.ParsedFile as CSharpUnresolvedFile);
			}
		}
	}
}
