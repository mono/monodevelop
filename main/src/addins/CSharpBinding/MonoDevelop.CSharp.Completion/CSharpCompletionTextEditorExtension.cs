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
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using MonoDevelop.CSharp.Completion.Provider;

namespace MonoDevelop.CSharp.Completion
{
	sealed partial class CSharpCompletionTextEditorExtension : CompletionTextEditorExtension
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
			if (DocumentContext.AnalysisDocument == null)
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
			if (ctx == null) 
				throw new ArgumentNullException (nameof (ctx));
			if (result == null)
				throw new ArgumentNullException (nameof (result));
			if (semanticModel == null)
				throw new ArgumentNullException (nameof (semanticModel));
			try {
				if (result.Count == 0 || position < 0)
					return;
				var syntaxTree = semanticModel.SyntaxTree;
				var root = syntaxTree.GetRoot ();

				if (syntaxTree.IsInNonUserCode (position, cancellationToken) ||
					syntaxTree.GetContainingTypeOrEnumDeclaration (position, cancellationToken) is EnumDeclarationSyntax ||
					syntaxTree.IsPreProcessorDirectiveContext (position, cancellationToken))
					return;

				var extensionMethodImport = syntaxTree.IsRightOfDotOrArrowOrColonColon (position, cancellationToken);
				ITypeSymbol extensionMethodReceiverType = null;

				if (extensionMethodImport) {
					if (ctx.TargetToken.Parent is MemberAccessExpressionSyntax memberAccess) {
						var symbolInfo = ctx.SemanticModel.GetSymbolInfo (memberAccess.Expression);
						if (symbolInfo.Symbol.Kind == SymbolKind.NamedType)
							return;
						extensionMethodReceiverType = ctx.SemanticModel.GetTypeInfo (memberAccess.Expression).Type;
						if (extensionMethodReceiverType == null) 
							return;
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
					var node = root.FindNode (TextSpan.FromBounds (position, position));
					if (node != null) {
						foreach (var un in semanticModel.GetUsingNamespacesInScope (node)) {
							usedNamespaces.Add (un.GetFullName ());
						}
					}
					var enclosingNamespaceName = semanticModel.GetEnclosingNamespace (position, cancellationToken)?.GetFullName () ?? "";

					var stack = new Stack<INamespaceOrTypeSymbol> ();
					foreach (var member in semanticModel.Compilation.GlobalNamespace.GetNamespaceMembers ())
						stack.Push (member);
					var extMethodDict = extensionMethodImport ? new Dictionary<INamespaceSymbol, List<ImportSymbolCompletionData>> () : null;
					var typeDict = new Dictionary<INamespaceSymbol, HashSet<string>> ();
					while (stack.Count > 0) {
						if (cancellationToken.IsCancellationRequested)
							break;
						var current = stack.Pop ();
						if (current is INamespaceSymbol currentNs) {
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
							continue;
						} 
						if (current is INamedTypeSymbol type) {
							if (type.IsImplicitClass || type.IsScriptClass)
								continue;
							if (type.DeclaredAccessibility != Accessibility.Public) {
								if (type.DeclaredAccessibility != Accessibility.Internal)
									continue;
								if (!type.IsAccessibleWithin (semanticModel.Compilation.Assembly))
									continue;
							}
							if (extensionMethodImport) {
								if (type.MightContainExtensionMethods)
									AddImportExtensionMethodCompletionData (result, type, extensionMethodReceiverType, extMethodDict);
							} else {
								if (!typeDict.TryGetValue (type.ContainingNamespace, out var existingTypeHashSet)) {
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
			} catch (Exception e) {
				LoggingService.LogError ("Exception while AddImportCompletionData", e);
			}
		}

		void AddImportExtensionMethodCompletionData (CompletionDataList result, INamedTypeSymbol fromType, ITypeSymbol receiverType, Dictionary<INamespaceSymbol, List<ImportSymbolCompletionData>> extMethodDict)
		{
			try {
				foreach (var extMethod in fromType.GetMembers ().OfType<IMethodSymbol> ().Where (method => method.IsExtensionMethod)) {
					var reducedMethod = extMethod.ReduceExtensionMethod (receiverType);
					if (reducedMethod != null) {
						if (!extMethodDict.TryGetValue (fromType.ContainingNamespace, out var importSymbolList))
							extMethodDict.Add (fromType.ContainingNamespace, importSymbolList = new List<ImportSymbolCompletionData> ());

						var newData = new ImportSymbolCompletionData (this, reducedMethod, false);
						ImportSymbolCompletionData existingItem = null;
						foreach (var data in importSymbolList) {
							if (data.Symbol.Name == extMethod.Name) {
								existingItem = data;
								break;
							}
						}

						if (existingItem != null) {
							existingItem.AddOverload (newData);
						} else {
							result.Add (newData);
							importSymbolList.Add (newData);
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Exception while AddImportExtensionMethodCompletionData", e);
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
				if (triggerInfo.TriggerCharacter == '{')
					return EmptyCompletionDataList;
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
			bool first = true, addProtocolCompletion = false;
			foreach (var item in completionList.Items) {
				if (string.IsNullOrEmpty (item.DisplayText))
					continue;
				var data = new CSharpCompletionData (analysisDocument, triggerSnapshot, cs, item);
				if (first) {
					first = false;
					addProtocolCompletion = data.Provider is OverrideCompletionProvider;
				}
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

			if (addProtocolCompletion) {
				var provider = new ProtocolMemberCompletionProvider ();

				var protocolMemberContext = new CompletionContext (provider, analysisDocument, completionContext.TriggerOffset, new TextSpan (completionContext.TriggerOffset, completionContext.TriggerWordLength), trigger, customOptions, token);

				await provider.ProvideCompletionsAsync (protocolMemberContext);

				foreach (var item in protocolMemberContext.Items) {
					if (string.IsNullOrEmpty (item.DisplayText))
						continue;
					var data = new CSharpCompletionData (analysisDocument, triggerSnapshot, cs, item);
					result.Add (data);
				}
			}

			if (forceSymbolCompletion || IdeApp.Preferences.AddImportedItemsToCompletionList) {
				Counters.ProcessCodeCompletion.Trace ("C#: Adding import completion data");
				AddImportCompletionData (syntaxContext, result, semanticModel, completionContext.TriggerOffset, token);
				Counters.ProcessCodeCompletion.Trace ("C#: Added import completion data");
			}
			if (defaultCompletionData != null) {
				result.DefaultCompletionString = defaultCompletionData.DisplayText;
			}

			if (completionList.SuggestionModeItem != null) {
				if (completionList.Items.Contains (completionList.SuggestionModeItem)) {
					result.DefaultCompletionString = completionList.SuggestionModeItem.DisplayText;
				}
				// if a suggestion mode item is present autoselection is disabled
				// for example in the lambda case the suggestion mode item is '<lambda expression>' which is not part of the completion item list but taggs the completion list as auto select == false.
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

		async void HandleEventHandlerContext ()
		{
			var analysisDocument = DocumentContext.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var partialDoc = analysisDocument.WithFrozenPartialSemantics (default (CancellationToken));
			var semanticModel = await partialDoc.GetSemanticModelAsync (default (CancellationToken));
			
			var syntaxContext = CSharpSyntaxContext.CreateContext (DocumentContext.RoslynWorkspace, semanticModel, Editor.CaretOffset, default (CancellationToken));
			if (syntaxContext.InferredTypes.Any(t => t.TypeKind == TypeKind.Delegate)) {
				CompletionWindowManager.HideWindow ();
				RunCompletionCommand ();
			}
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			var result = base.KeyPress (descriptor);
			if (descriptor.KeyChar == ' ') {
				// Work around for handling the += context which doesn't pop up code completion automatically.
				if (Editor.CaretOffset > 2 && Editor.GetCharAt (Editor.CaretOffset - 2) == '=' && Editor.GetCharAt (Editor.CaretOffset - 3) == '+') {
					HandleEventHandlerContext ();
				}
			}
			return result;
		}
	}
}
