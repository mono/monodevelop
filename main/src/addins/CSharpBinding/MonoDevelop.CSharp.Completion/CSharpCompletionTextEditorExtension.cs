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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using ICSharpCode.NRefactory6.CSharp;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;

using Mono.Addins;
using MonoDevelop.CodeGeneration;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.Debugger;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Refactoring;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Platform;

using Counters = MonoDevelop.Ide.Counters;

namespace MonoDevelop.CSharp.Completion
{
	sealed class CSharpCompletionTextEditorExtension : CompletionTextEditorExtension, IDebuggerExpressionResolver
	{
		/*		internal protected virtual Mono.TextEditor.TextEditorData TextEditorData {
					get {
						var doc = Document;
						if (doc == null)
							return null;
						return doc.Editor;
					}
				}

				protected virtual IProjectContent ProjectContent {
					get { return Document.GetProjectContext (); }
				}
		*/
		SyntaxTree unit;
		static readonly SyntaxTree emptyUnit = CSharpSyntaxTree.ParseText ("");

		SyntaxTree Unit {
			get {
				return unit ?? emptyUnit;
			}
			set {
				unit = value;
			}
		}

		public MonoDevelop.Ide.TypeSystem.ParsedDocument ParsedDocument {
			get {
				return DocumentContext.ParsedDocument;
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
					policy = DocumentContext.GetPolicy<CSharpFormattingPolicy> (types);
				}
				return policy;
			}
		}

		public override string CompletionLanguage {
			get {
				return "C#";
			}
		}

		static List<CompletionData> snippets;

		static CSharpCompletionTextEditorExtension ()
		{
			//try {
			//	CompletionEngine.SnippetCallback = delegate (CancellationToken arg) {
			//		if (snippets != null)
			//			return Task.FromResult ((IEnumerable<CompletionData>)snippets);
			//		var newSnippets = new List<CompletionData> ();
			//		foreach (var ct in MonoDevelop.Ide.CodeTemplates.CodeTemplateService.GetCodeTemplates ("text/x-csharp")) {
			//			if (string.IsNullOrEmpty (ct.Shortcut) || ct.CodeTemplateContext != MonoDevelop.Ide.CodeTemplates.CodeTemplateContext.Standard)
			//				continue;
			//			newSnippets.Add (new RoslynCompletionData (null) {
			//				CompletionText = ct.Shortcut,
			//				DisplayText = ct.Shortcut,
			//				Description = ct.Shortcut + Environment.NewLine + GettextCatalog.GetString (ct.Description),
			//				Icon = ct.Icon
			//			});
			//		}
			//		snippets = newSnippets;
			//		return Task.FromResult ((IEnumerable<CompletionData>)newSnippets);
			//	};
			//} catch (Exception e) {
			//	LoggingService.LogError ("Error while loading c# completion text editor extension.", e);
			//}
		}

		internal static Task<Document> WithFrozenPartialSemanticsAsync (Document doc, CancellationToken token)
		{
			return Task.FromResult (doc.WithFrozenPartialSemantics (token));
		}

		bool addEventHandlersInInitialization = true;

		/// <summary>
		/// Used in testing environment.
		/// </summary>
		[System.ComponentModel.Browsable (false)]
		public CSharpCompletionTextEditorExtension (MonoDevelop.Ide.Gui.Document doc, bool addEventHandlersInInitialization = true)
		{
			this.addEventHandlersInInitialization = addEventHandlersInInitialization;
			Initialize (doc.Editor, doc);
		}

		public CSharpCompletionTextEditorExtension ()
		{
		}

		protected override void Initialize ()
		{
			base.Initialize ();

			var parsedDocument = DocumentContext.ParsedDocument;
			if (parsedDocument != null) {
				//				this.Unit = parsedDocument.GetAst<SyntaxTree> ();
				//					this.UnresolvedFileCompilation = DocumentContext.Compilation;
				//					this.CSharpUnresolvedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
				//					Editor.CaretPositionChanged += HandlePositionChanged;
			}

			if (addEventHandlersInInitialization)
				DocumentContext.DocumentParsed += HandleDocumentParsed;
		}

		CancellationTokenSource src = new CancellationTokenSource ();

		void StopPositionChangedTask ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
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
			DocumentContext.DocumentParsed -= HandleDocumentParsed;

			base.Dispose ();
		}

		CancellationTokenSource documentParsedTokenSrc = new CancellationTokenSource ();

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			var parsedDocument = DocumentContext.ParsedDocument;
			if (parsedDocument == null)
				return;
			var semanticModel = parsedDocument.GetAst<SemanticModel> ();
			if (semanticModel == null)
				return;
			TypeSegmentTreeUpdated?.Invoke (this, EventArgs.Empty);
		}

		public event EventHandler TypeSegmentTreeUpdated;

		public void UpdateParsedDocument ()
		{
			HandleDocumentParsed (null, null);
		}

		public override Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			int triggerWordLength = 0;
			switch (triggerInfo.CompletionTriggerReason) {
			case CompletionTriggerReason.CharTyped:
				try {
					var completionChar = triggerInfo.TriggerCharacter.Value;
					if (char.IsLetterOrDigit (completionChar) || completionChar == '_') {
						if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit (Editor.GetCharAt (completionContext.TriggerOffset - 2)))
							return null;
						triggerWordLength = 1;
					}
					return InternalHandleCodeCompletion (completionContext, triggerInfo, triggerWordLength, token);
				} catch (Exception e) {
					LoggingService.LogError ("Unexpected code completion exception." + Environment.NewLine +
						"FileName: " + DocumentContext.Name + Environment.NewLine +
						"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine +
						"Line text: " + Editor.GetLineText (completionContext.TriggerLine),
						e);
					return null;
				}
			case CompletionTriggerReason.BackspaceOrDeleteCommand:
				try {
					return InternalHandleCodeCompletion (completionContext, triggerInfo, triggerWordLength, token).ContinueWith (t => {
						var result = (CompletionDataList)t.Result;
						if (result == null)
							return null;
						result.AutoCompleteUniqueMatch = false;
						result.AutoCompleteEmptyMatch = false;
						return (ICompletionDataList)result;
					});
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
			default:
				var ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt (completionContext.TriggerOffset - 1) : '\0';
				return InternalHandleCodeCompletion (completionContext, new CompletionTriggerInfo (CompletionTriggerReason.CompletionCommand, ch), triggerWordLength, default (CancellationToken));
			}
		}

		static bool IsIdentifierPart (char ch)
		{
			return char.IsLetterOrDigit (ch) || ch == '_';
		}

		internal class CSharpCompletionDataList : CompletionDataList
		{
		}

		interface IListData
		{
			CSharpCompletionDataList List { get; set; }
		}


		internal void AddImportCompletionData (CSharpSyntaxContext ctx, CompletionDataList result, SemanticModel semanticModel, int position, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (result.Count == 0)
				return;
			var root = semanticModel.SyntaxTree.GetRoot ();
			var node = root.FindNode (TextSpan.FromBounds (position, position));
			var syntaxTree = root.SyntaxTree;

			if (syntaxTree.IsInNonUserCode (position, cancellationToken) ||
				syntaxTree.GetContainingTypeOrEnumDeclaration (position, cancellationToken) is EnumDeclarationSyntax ||
				syntaxTree.IsPreProcessorDirectiveContext (position, cancellationToken))
				return;
			
			var extensionMethodImport = syntaxTree.IsRightOfDotOrArrowOrColonColon (position, cancellationToken);
			ITypeSymbol extensionType = null;

			if (extensionMethodImport) {
				var memberAccess = ctx.TargetToken.Parent as MemberAccessExpressionSyntax;
				if (memberAccess != null) {
					var symbolInfo = ctx.SemanticModel.GetSymbolInfo (memberAccess.Expression);
					if (symbolInfo.Symbol.Kind == SymbolKind.NamedType)
						return;
					extensionType = ctx.SemanticModel.GetTypeInfo (memberAccess.Expression).Type;
					if (extensionType == null) {
						return;
					}
				} else {
					return;
				}
			}

			var tokenLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition (position, cancellationToken);

			if (extensionMethodImport ||
				syntaxTree.IsGlobalStatementContext (position, cancellationToken) ||
				syntaxTree.IsExpressionContext (position, tokenLeftOfPosition, true, cancellationToken) ||
				syntaxTree.IsStatementContext (position, tokenLeftOfPosition, cancellationToken) ||
				syntaxTree.IsTypeContext (position, cancellationToken) ||
				syntaxTree.IsTypeDeclarationContext (position, tokenLeftOfPosition, cancellationToken) ||
				syntaxTree.IsMemberDeclarationContext (position, tokenLeftOfPosition, cancellationToken) ||
				syntaxTree.IsLabelContext (position, cancellationToken)) {
				var usedNamespaces = new HashSet<string> ();
				foreach (var un in semanticModel.GetUsingNamespacesInScope (node)) {
					usedNamespaces.Add (un.GetFullName ());
				}
				var enclosingNamespaceName = semanticModel.GetEnclosingNamespace (position, cancellationToken).GetFullName ();

				var stack = new Stack<INamespaceOrTypeSymbol> ();
				foreach (var member in semanticModel.Compilation.GlobalNamespace.GetNamespaceMembers ())
					stack.Push (member);
				var extMethodDict = extensionMethodImport ? new Dictionary<INamespaceSymbol, List<ImportSymbolCompletionData>> () : null;
				var typeDict = new Dictionary<INamespaceSymbol, HashSet<string>> ();
				while (stack.Count > 0) {
					if (cancellationToken.IsCancellationRequested)
						break;
					var current = stack.Pop ();
					var currentNs = current as INamespaceSymbol;
					if (currentNs != null) {
						var currentNsName = currentNs.GetFullName ();
						if (usedNamespaces.Contains (currentNsName) ||
							enclosingNamespaceName == currentNsName ||
							(enclosingNamespaceName.StartsWith (currentNsName, StringComparison.Ordinal) &&
							enclosingNamespaceName [currentNsName.Length] == '.')) {
							foreach (var member in currentNs.GetNamespaceMembers ())
								stack.Push (member);
						} else {
							foreach (var member in currentNs.GetMembers ())
								stack.Push (member);
						}
					} else {
						var type = (INamedTypeSymbol)current;
						if (type.IsImplicitClass || type.IsScriptClass)
							continue;
						if (type.DeclaredAccessibility != Accessibility.Public) {
							if (type.DeclaredAccessibility != Accessibility.Internal)
								continue;
							if (!type.IsAccessibleWithin (semanticModel.Compilation.Assembly))
								continue;
						}
						if (extensionMethodImport) {
							if (!type.MightContainExtensionMethods)
								continue;
							foreach (var extMethod in type.GetMembers ().OfType<IMethodSymbol> ().Where (method => method.IsExtensionMethod)) {
								var reducedMethod = extMethod.ReduceExtensionMethod (extensionType);
								if (reducedMethod != null) {
									List<ImportSymbolCompletionData> importSymbolList;
									if (!extMethodDict.TryGetValue (type.ContainingNamespace, out importSymbolList)) {
										extMethodDict.Add (type.ContainingNamespace, importSymbolList = new List<ImportSymbolCompletionData> ());
									}
									var newData = new ImportSymbolCompletionData (this, reducedMethod, false);
									var existingItem = importSymbolList.FirstOrDefault (data => data.Symbol.Name == extMethod.Name);
									if (existingItem != null) {
										existingItem.AddOverload (newData);
									} else {
										result.Add (newData);
										importSymbolList.Add (newData);
									}
								}
							}
						} else {
							HashSet<string> existingTypeHashSet;
							if (!typeDict.TryGetValue (type.ContainingNamespace, out existingTypeHashSet)) {
								typeDict.Add (type.ContainingNamespace, existingTypeHashSet = new HashSet<string> ());
							}
							if (!existingTypeHashSet.Contains (type.Name)) {
								result.Add (new ImportSymbolCompletionData (this, type, false));
								existingTypeHashSet.Add (type.Name);
							}
						}
					}
				}
			}
		}

		static ICompletionDataList EmptyCompletionDataList = new CompletionDataList ();

		async Task<ICompletionDataList> InternalHandleCodeCompletion (CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, int triggerWordLength, CancellationToken token, bool forceSymbolCompletion = false)
		{
			var analysisDocument = DocumentContext.AnalysisDocument;
			if (analysisDocument == null)
				return EmptyCompletionDataList;


			var cs = DocumentContext.RoslynWorkspace.Services.GetLanguageServices (LanguageNames.CSharp).GetService<CompletionService> ();
			SourceText sourceText;
			if (!analysisDocument.TryGetText (out sourceText))
				return EmptyCompletionDataList;

			CompletionTriggerKind kind;
			switch (triggerInfo.CompletionTriggerReason) {
			case CompletionTriggerReason.CharTyped:
				kind = CompletionTriggerKind.Insertion;
				break;
			case CompletionTriggerReason.CompletionCommand:
				kind = CompletionTriggerKind.InvokeAndCommitIfUnique;
				break;
			case CompletionTriggerReason.BackspaceOrDeleteCommand:
				kind = CompletionTriggerKind.Deletion;
				break;
			case CompletionTriggerReason.RetriggerCommand:
				kind = CompletionTriggerKind.InvokeAndCommitIfUnique;
				break;
			default:
				kind = CompletionTriggerKind.Insertion;
				break;
			}
			var triggerSnapshot = Editor.GetPlatformTextBuffer ().CurrentSnapshot;
			var trigger = new CompletionTrigger(kind, triggerInfo.TriggerCharacter.HasValue ? triggerInfo.TriggerCharacter.Value : '\0');
			if (triggerInfo.CompletionTriggerReason == CompletionTriggerReason.CharTyped) {
				if (!cs.ShouldTriggerCompletion (sourceText, completionContext.TriggerOffset, trigger, null)) {
					return EmptyCompletionDataList;
				}
			}

			Counters.ProcessCodeCompletion.Trace ("C#: Getting completions");
			var customOptions = DocumentContext.RoslynWorkspace.Options
				.WithChangedOption (CompletionOptions.TriggerOnDeletion, LanguageNames.CSharp, true)
				.WithChangedOption (CompletionOptions.HideAdvancedMembers, LanguageNames.CSharp, IdeApp.Preferences.CompletionOptionsHideAdvancedMembers);

			var completionList = await Task.Run (() => cs.GetCompletionsAsync (analysisDocument, Editor.CaretOffset, trigger, options: customOptions, cancellationToken: token)).ConfigureAwait (false);
			Counters.ProcessCodeCompletion.Trace ("C#: Got completions");

			if (completionList == null)
				return EmptyCompletionDataList;

			var result = new CompletionDataList ();
			result.TriggerWordLength = triggerWordLength;
			CSharpCompletionData defaultCompletionData = null;
			foreach (var item in completionList.Items) {
				if (string.IsNullOrEmpty (item.DisplayText))
					continue;
				var data = new CSharpCompletionData (analysisDocument, triggerSnapshot, cs, item);
				result.Add (data);
				if (item.Rules.MatchPriority > 0) {
					if (defaultCompletionData == null || defaultCompletionData.Rules.MatchPriority < item.Rules.MatchPriority)
						defaultCompletionData = data;
				}
			}
			result.AutoCompleteUniqueMatch = (triggerInfo.CompletionTriggerReason == CompletionTriggerReason.CompletionCommand);

			var partialDoc = analysisDocument.WithFrozenPartialSemantics (token);
			var semanticModel = await partialDoc.GetSemanticModelAsync (token).ConfigureAwait (false);
			var syntaxContext = CSharpSyntaxContext.CreateContext (DocumentContext.RoslynWorkspace, semanticModel, completionContext.TriggerOffset, token);

			if (forceSymbolCompletion || IdeApp.Preferences.AddImportedItemsToCompletionList) {
				Counters.ProcessCodeCompletion.Trace ("C#: Adding import completion data");
				AddImportCompletionData (syntaxContext, result, semanticModel, completionContext.TriggerOffset, token);
				Counters.ProcessCodeCompletion.Trace ("C#: Added import completion data");
			}

			if (defaultCompletionData != null)
				result.DefaultCompletionString = defaultCompletionData.DisplayText;

			if (completionList.SuggestionModeItem != null) {
				result.DefaultCompletionString = completionList.SuggestionModeItem.DisplayText;
				result.AutoSelect = false;
			}

			if (triggerInfo.TriggerCharacter == '_' && triggerWordLength == 1)
				result.AutoSelect = false;

			return result;
		}

		static bool HasAllUsedParameters (MonoDevelop.Ide.CodeCompletion.ParameterHintingData provider, string [] list)
		{
			if (provider == null || list == null)
				return true;
			int pc = provider.ParameterCount;
			foreach (var usedParam in list) {
				bool found = false;
				for (int m = 0; m < pc; m++) {
					if (usedParam == provider.GetParameterName (m)) {
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}

		public override async Task<int> GuessBestMethodOverload (MonoDevelop.Ide.CodeCompletion.ParameterHintingResult provider, int currentOverload, CancellationToken token)
		{
			var analysisDocument = DocumentContext.AnalysisDocument;
			if (analysisDocument == null)
				return -1;
			var result = await ICSharpCode.NRefactory6.CSharp.ParameterUtil.GetCurrentParameterIndex (analysisDocument, provider.ApplicableSpan.Start, Editor.CaretOffset);
			var cparam = result.ParameterIndex;
			var list = result.UsedNamespaceParameters;
			if (cparam > provider [currentOverload].ParameterCount && !provider [currentOverload].IsParameterListAllowed || !HasAllUsedParameters (provider [currentOverload], list)) {
				// Look for an overload which has more parameters
				int bestOverload = -1;
				int bestParamCount = int.MaxValue;
				for (int n = 0; n < provider.Count; n++) {
					int pc = provider [n].ParameterCount;
					if (pc < bestParamCount && pc >= cparam) {

						if (HasAllUsedParameters (provider [n], list)) {
							bestOverload = n;
							bestParamCount = pc;
						}
					}


				}
				if (bestOverload == -1) {
					for (int n = 0; n < provider.Count; n++) {
						if (provider [n].IsParameterListAllowed && HasAllUsedParameters (provider [n], list)) {
							bestOverload = n;
							break;
						}
					}
				}
				return bestOverload;
			}
			return -1;
		}


		//		static bool ContainsPublicConstructors (ITypeDefinition t)
		//		{
		//			if (t.Methods.Count (m => m.IsConstructor) == 0)
		//				return true;
		//			return t.Methods.Any (m => m.IsConstructor && m.IsPublic);
		//		}


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

		public override Task<Ide.CodeCompletion.ParameterHintingResult> ParameterCompletionCommand (CodeCompletionContext completionContext)
		{
			if (completionContext == null)
				throw new ArgumentNullException (nameof (completionContext));
			char ch = completionContext.TriggerOffset > 0 ? Editor.GetCharAt (completionContext.TriggerOffset - 1) : '\0';
			var info = new Ide.Editor.Extension.SignatureHelpTriggerInfo (Ide.Editor.Extension.SignatureHelpTriggerReason.InvokeSignatureHelpCommand, ch);
			return InternalHandleParameterCompletionCommand (completionContext, info, default (CancellationToken));
		}

		public override Task<MonoDevelop.Ide.CodeCompletion.ParameterHintingResult> HandleParameterCompletionAsync (CodeCompletionContext completionContext, Ide.Editor.Extension.SignatureHelpTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			return InternalHandleParameterCompletionCommand (completionContext, triggerInfo, token);
		}

		internal static Lazy<ISignatureHelpProvider []> signatureProviders = new Lazy<ISignatureHelpProvider []> (() => {
			var workspace = TypeSystemService.Workspace;
			var mefExporter = (IMefHostExportProvider)workspace.Services.HostServices;
			var helpProviders = mefExporter.GetExports<ISignatureHelpProvider, LanguageMetadata> ()
				.FilterToSpecificLanguage (LanguageNames.CSharp);

			return helpProviders.ToArray ();
		});
		readonly static Task<MonoDevelop.Ide.CodeCompletion.ParameterHintingResult> emptyParameterHintingResultTask = Task.FromResult (ParameterHintingResult.Empty);

		public Task<MonoDevelop.Ide.CodeCompletion.ParameterHintingResult> InternalHandleParameterCompletionCommand (CodeCompletionContext completionContext, Ide.Editor.Extension.SignatureHelpTriggerInfo triggerInfo, CancellationToken token = default (CancellationToken))
		{
			var data = Editor;
			bool force = triggerInfo.TriggerReason != Ide.Editor.Extension.SignatureHelpTriggerReason.InvokeSignatureHelpCommand;
			List<ISignatureHelpProvider> providers;
			if (!force) {
				if (triggerInfo.TriggerReason == Ide.Editor.Extension.SignatureHelpTriggerReason.TypeCharCommand) {
					providers = signatureProviders.Value.Where (provider => provider.IsTriggerCharacter (triggerInfo.TriggerCharacter.Value)).ToList ();
				} else if (triggerInfo.TriggerReason == Ide.Editor.Extension.SignatureHelpTriggerReason.RetriggerCommand) {
					providers = signatureProviders.Value.Where (provider => provider.IsRetriggerCharacter (triggerInfo.TriggerCharacter.Value)).ToList ();
				} else {
					providers = signatureProviders.Value.ToList ();
				}
				if (providers.Count == 0)
					return emptyParameterHintingResultTask;
			} else
				providers = signatureProviders.Value.ToList ();

			if (Editor.EditMode != EditMode.Edit)
				return emptyParameterHintingResultTask;
			var offset = Editor.CaretOffset;
			try {
				var analysisDocument = DocumentContext.AnalysisDocument;
				if (analysisDocument == null)
					return emptyParameterHintingResultTask;
				var result = new RoslynParameterHintingEngine ().GetParameterDataProviderAsync (
					providers,
					analysisDocument,
					offset,
					triggerInfo.ToRoslyn (),
					token
				);
				return result;
			} catch (Exception e) {
				LoggingService.LogError ("Unexpected parameter completion exception." + Environment.NewLine +
					"FileName: " + DocumentContext.Name + Environment.NewLine +
					"Position: line=" + completionContext.TriggerLine + " col=" + completionContext.TriggerLineOffset + Environment.NewLine +
					"Line text: " + Editor.GetLineText (completionContext.TriggerLine),
					e);
				return emptyParameterHintingResultTask;
			}
		}

		//		List<string> GetUsedNamespaces ()
		//		{
		//			var scope = CSharpUnresolvedFile.GetUsingScope (document.Editor.Caret.Location);
		//			var result = new List<string> ();
		//			while (scope != null) {
		//				result.Add (scope.NamespaceName);
		//				var ctx = CSharpUnresolvedFile.GetResolver (Document.Compilation, scope.Region.Begin);
		//				foreach (var u in scope.Usings) {
		//					var ns = u.ResolveNamespace (ctx);
		//					if (ns == null)
		//						continue;
		//					result.Add (ns.FullName);
		//				}
		//				scope = scope.Parent;
		//			}
		//			return result;
		//		}
		public override async Task<int> GetCurrentParameterIndex (int startOffset, CancellationToken token)
		{
			var analysisDocument = DocumentContext.AnalysisDocument;
			var caretOffset = Editor.CaretOffset;
			if (analysisDocument == null || startOffset > caretOffset)
				return -1;
			var partialDoc = analysisDocument.WithFrozenPartialSemantics (token);
			var result = await ParameterUtil.GetCurrentParameterIndex (partialDoc, startOffset, caretOffset, token).ConfigureAwait (false);
			return result.ParameterIndex;
		}

		/*
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

						public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
						{
							var currentWord = GetCurrentWord (window);
							if (CompletionText == "new()" && descriptor.KeyChar == '(') {
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
<<<<<<< HEAD
							string name = showFullName ? builder.ConvertType(type).ToString() : type.Name;
=======
							string name = showFullName ? builder.ConvertType(type).ToString() : type.Name; 
>>>>>>> master
							if (isInAttributeContext && name.EndsWith("Attribute") && name.Length > "Attribute".Length) {
								name = name.Substring(0, name.Length - "Attribute".Length);
							}
							return name;
						});

						var result = new TypeCompletionData (type, ext,
<<<<<<< HEAD
							displayText,
=======
							displayText, 
>>>>>>> master
							type.GetStockIcon (),
							addConstructors);
						return result;
					}

					ICompletionData ICompletionDataFactory.CreateMemberCompletionData(IType type, IEntity member)
					{
						Lazy<string> displayText = new Lazy<string> (delegate {
<<<<<<< HEAD
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
=======
							string name = builder.ConvertType(type).ToString(); 
							return name + "."+ member.Name;
						});

						var result = new LazyGenericTooltipCompletionData (
							(List, sw) => new TooltipInformation (), 
							displayText, 
							member.GetStockIcon ());
						return result;
>>>>>>> master
					}

					class XmlDocCompletionData : CompletionData, IListData
					{
						readonly CSharpCompletionTextEditorExtension ext;
						readonly string title;

<<<<<<< HEAD
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


=======
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
>>>>>>> master

						public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
						{
							var currentWord = GetCurrentWord (window);
							var text = CompletionText;
							if (descriptor.KeyChar != '>')
								text += ">";
							window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, currentWord, text);
						}
					}

					ICompletionData ICompletionDataFactory.CreateXmlDocCompletionData (string title, string description, string insertText)
					{
						return new XmlDocCompletionData (ext, title, description, insertText);
					}

<<<<<<< HEAD
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
=======
						public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
						{
							var currentWord = GetCurrentWord (window);
							var text = CompletionText;
							if (descriptor.KeyChar != '>')
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
>>>>>>> master

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

<<<<<<< HEAD
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

=======

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

>>>>>>> master
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



				#endregion
		*/


		#region IDebuggerExpressionResolver implementation

		async Task<DebugDataTipInfo> IDebuggerExpressionResolver.ResolveExpressionAsync (IReadonlyTextDocument editor, DocumentContext doc, int offset, CancellationToken cancellationToken)
		{
			return await Resolver.DebuggerExpressionResolver.ResolveAsync (editor, doc, offset, cancellationToken).ConfigureAwait (false);
		}

		#endregion

		[CommandHandler (RefactoryCommands.ImportSymbol)]
		async void ImportSymbolCommand ()
		{
			if (Editor.SelectionMode == SelectionMode.Block)
				return;
			var analysisDocument = DocumentContext.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var offset = Editor.CaretOffset;

			int cpos, wlen;
			if (!GetCompletionCommandOffset (out cpos, out wlen)) {
				cpos = Editor.CaretOffset;
				wlen = 0;
			}
			CurrentCompletionContext = CompletionWidget.CreateCodeCompletionContext (cpos);
			CurrentCompletionContext.TriggerWordLength = wlen;

			int triggerWordLength = 0;
			char ch = CurrentCompletionContext.TriggerOffset > 0 ? Editor.GetCharAt (CurrentCompletionContext.TriggerOffset - 1) : '\0';
			var completionList = await InternalHandleCodeCompletion (CurrentCompletionContext, new CompletionTriggerInfo (CompletionTriggerReason.CompletionCommand, ch), triggerWordLength, default (CancellationToken), true);
			if (completionList != null)
				CompletionWindowManager.ShowWindow (this, (char)0, completionList, CompletionWidget, CurrentCompletionContext);
			else
				CurrentCompletionContext = null;
		}

	}
}
