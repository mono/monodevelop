// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Traverses the DOM and resolves expressions.
	/// </summary>
	/// <remarks>
	/// The ResolveVisitor does two jobs at the same time: it tracks the resolve context (properties on CSharpResolver)
	/// and it resolves the expressions visited.
	/// To allow using the context tracking without having to resolve every expression in the file (e.g. when you want to resolve
	/// only a single node deep within the DOM), you can use the <see cref="IResolveVisitorNavigator"/> interface.
	/// The navigator allows you to switch the between scanning mode and resolving mode.
	/// In scanning mode, the context is tracked (local variables registered etc.), but nodes are not resolved.
	/// While scanning, the navigator will get asked about every node that the resolve visitor is about to enter.
	/// This allows the navigator whether to keep scanning, whether switch to resolving mode, or whether to completely skip the
	/// subtree rooted at that node.
	/// 
	/// In resolving mode, the context is tracked and nodes will be resolved.
	/// The resolve visitor may decide that it needs to resolve other nodes as well in order to resolve the current node.
	/// In this case, those nodes will be resolved automatically, without asking the navigator interface.
	/// For child nodes that are not essential to resolving, the resolve visitor will switch back to scanning mode (and thus will
	/// ask the navigator for further instructions).
	/// 
	/// Moreover, there is the <c>ResolveAll</c> mode - it works similar to resolving mode, but will not switch back to scanning mode.
	/// The whole subtree will be resolved without notifying the navigator.
	/// </remarks>
	public sealed class ResolveVisitor : DepthFirstAstVisitor<object, ResolveResult>
	{
		// The ResolveVisitor is also responsible for handling lambda expressions.
		
		static readonly ResolveResult errorResult = new ErrorResolveResult(SharedTypes.UnknownType);
		readonly ResolveResult voidResult;
		
		CSharpResolver resolver;
		SimpleNameLookupMode currentTypeLookupMode = SimpleNameLookupMode.Type;
		readonly ParsedFile parsedFile;
		readonly Dictionary<AstNode, ResolveResult> resolveResultCache = new Dictionary<AstNode, ResolveResult>();
		readonly Dictionary<AstNode, CSharpResolver> resolverBeforeDict = new Dictionary<AstNode, CSharpResolver>();
		
		readonly IResolveVisitorNavigator navigator;
		ResolveVisitorNavigationMode mode = ResolveVisitorNavigationMode.Scan;
		List<LambdaBase> undecidedLambdas;
		
		#region Constructor
		/// <summary>
		/// Creates a new ResolveVisitor instance.
		/// </summary>
		/// <param name="resolver">
		/// The CSharpResolver, describing the initial resolve context.
		/// If you visit a whole CompilationUnit with the resolve visitor, you can simply pass
		/// <c>new CSharpResolver(typeResolveContext)</c> without setting up the context.
		/// If you only visit a subtree, you need to pass a CSharpResolver initialized to the context for that subtree.
		/// </param>
		/// <param name="parsedFile">
		/// Result of the <see cref="TypeSystemConvertVisitor"/> for the file being passed. This is used for setting up the context on the resolver.
		/// You may pass <c>null</c> if you are only visiting a part of a method body and have already set up the context in the <paramref name="resolver"/>.
		/// </param>
		/// <param name="navigator">
		/// The navigator, which controls where the resolve visitor will switch between scanning mode and resolving mode.
		/// If you pass <c>null</c>, then <c>ResolveAll</c> mode will be used.
		/// </param>
		public ResolveVisitor(CSharpResolver resolver, ParsedFile parsedFile, IResolveVisitorNavigator navigator = null)
		{
			if (resolver == null)
				throw new ArgumentNullException("resolver");
			this.resolver = resolver;
			this.parsedFile = parsedFile;
			this.navigator = navigator;
			this.voidResult = new ResolveResult(KnownTypeReference.Void.Resolve(resolver.Context));
			if (navigator == null)
				mode = ResolveVisitorNavigationMode.ResolveAll;
		}
		#endregion
		
		#region Properties
		/// <summary>
		/// Gets the TypeResolveContext used by this ResolveVisitor.
		/// </summary>
		public ITypeResolveContext TypeResolveContext {
			get { return resolver.Context; }
		}
		
		/// <summary>
		/// Gets the CancellationToken used by this ResolveVisitor.
		/// </summary>
		public CancellationToken CancellationToken {
			get { return resolver.cancellationToken; }
		}
		#endregion
		
		#region ResetContext
		/// <summary>
		/// Resets the visitor to the stored position, runs the action, and then reverts the visitor to the previous position.
		/// </summary>
		void ResetContext(CSharpResolver storedContext, Action action)
		{
			var oldMode = this.mode;
			var oldResolver = this.resolver;
			var oldTypeLookupMode = this.currentTypeLookupMode;
			try {
				this.mode = (navigator == null) ? ResolveVisitorNavigationMode.ResolveAll : ResolveVisitorNavigationMode.Resolve;
				this.resolver = storedContext;
				this.currentTypeLookupMode = SimpleNameLookupMode.Type;
				
				action();
			} finally {
				this.mode = oldMode;
				this.resolver = oldResolver;
				this.currentTypeLookupMode = oldTypeLookupMode;
			}
		}
		#endregion
		
		#region Scan / Resolve
		bool resolverEnabled {
			get { return mode != ResolveVisitorNavigationMode.Scan; }
		}
		
		public void Scan(AstNode node)
		{
			if (node == null || node.IsNull)
				return;
			switch (node.NodeType) {
				case NodeType.Token:
				case NodeType.Whitespace:
					return; // skip tokens, identifiers, comments, etc.
			}
			if (mode == ResolveVisitorNavigationMode.ResolveAll) {
				Resolve(node);
			} else {
				ResolveVisitorNavigationMode oldMode = mode;
				mode = navigator.Scan(node);
				switch (mode) {
					case ResolveVisitorNavigationMode.Skip:
						if (node is VariableDeclarationStatement) {
							// Enforce scanning of variable declarations.
							goto case ResolveVisitorNavigationMode.Scan;
						}
						break;
					case ResolveVisitorNavigationMode.Scan:
						if (node is LambdaExpression || node is AnonymousMethodExpression) {
							// lambdas must be resolved so that they get stored in the 'undecided' list only once
							goto case ResolveVisitorNavigationMode.Resolve;
						}
						StoreState(node, resolver.Clone());
						node.AcceptVisitor(this, null);
						break;
					case ResolveVisitorNavigationMode.Resolve:
					case ResolveVisitorNavigationMode.ResolveAll:
						Resolve(node);
						break;
					default:
						throw new InvalidOperationException("Invalid value for ResolveVisitorNavigationMode");
				}
				mode = oldMode;
			}
		}
		
		public ResolveResult Resolve(AstNode node)
		{
			if (node == null || node.IsNull)
				return errorResult;
			bool wasScan = mode == ResolveVisitorNavigationMode.Scan;
			if (wasScan)
				mode = ResolveVisitorNavigationMode.Resolve;
			ResolveResult result;
			if (!resolveResultCache.TryGetValue(node, out result)) {
				resolver.cancellationToken.ThrowIfCancellationRequested();
				StoreState(node, resolver.Clone());
				result = node.AcceptVisitor(this, null) ?? errorResult;
				Log.WriteLine("Resolved '{0}' to {1}", node, result);
				StoreResult(node, result);
				ProcessConversionsInResult(result);
			}
			if (wasScan)
				mode = ResolveVisitorNavigationMode.Scan;
			return result;
		}
		
		void StoreState(AstNode node, CSharpResolver resolverState)
		{
			Debug.Assert(resolverState != null);
			// It's possible that we re-visit an expression that we scanned over earlier,
			// so we might have to overwrite an existing state.
			resolverBeforeDict[node] = resolverState;
		}
		
		void StoreResult(AstNode node, ResolveResult result)
		{
			Debug.Assert(result != null);
			resolveResultCache.Add(node, result);
			if (navigator != null)
				navigator.Resolved(node, result);
		}
		
		protected override ResolveResult VisitChildren(AstNode node, object data)
		{
			ScanChildren(node);
			return null;
		}
		
		void ScanChildren(AstNode node)
		{
			for (AstNode child = node.FirstChild; child != null; child = child.NextSibling) {
				Scan(child);
			}
		}
		#endregion
		
		#region Process Conversions
		/// <summary>
		/// Processes conversions within this resolve result.
		/// </summary>
		void ProcessConversionsInResult(ResolveResult result)
		{
			ConversionResolveResult crr = result as ConversionResolveResult;
			if (crr != null) {
				ProcessConversion(crr.Input, crr.Conversion, crr.Type);
			} else {
				foreach (ResolveResult argumentResult in result.GetChildResults()) {
					crr = argumentResult as ConversionResolveResult;
					if (crr != null)
						ProcessConversion(crr.Input, crr.Conversion, crr.Type);
				}
			}
		}
		
		/// <summary>
		/// Convert 'rr' to the target type.
		/// </summary>
		void ProcessConversion(ResolveResult rr, IType targetType)
		{
			ProcessConversion(rr, resolver.conversions.ImplicitConversion(rr, targetType), targetType);
		}
		
		sealed class AnonymousFunctionConversionData
		{
			public readonly IType ReturnType;
			public readonly ExplicitlyTypedLambda ExplicitlyTypedLambda;
			public readonly LambdaTypeHypothesis Hypothesis;
			
			public AnonymousFunctionConversionData(IType returnType, LambdaTypeHypothesis hypothesis)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				this.ReturnType = returnType;
				this.Hypothesis = hypothesis;
			}
			
			public AnonymousFunctionConversionData(IType returnType, ExplicitlyTypedLambda explicitlyTypedLambda)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				this.ReturnType = returnType;
				this.ExplicitlyTypedLambda = explicitlyTypedLambda;
			}
		}
		
		/// <summary>
		/// Convert 'rr' to the target type using the specified conversion.
		/// </summary>
		void ProcessConversion(ResolveResult rr, Conversion conversion, IType targetType)
		{
			if (conversion.IsAnonymousFunctionConversion) {
				Log.WriteLine("Processing conversion of anonymous function to " + targetType + "...");
				AnonymousFunctionConversionData data = conversion.data as AnonymousFunctionConversionData;
				if (data != null) {
					Log.Indent();
					if (data.Hypothesis != null)
						data.Hypothesis.MergeInto(this, data.ReturnType);
					if (data.ExplicitlyTypedLambda != null)
						data.ExplicitlyTypedLambda.ApplyReturnType(this, data.ReturnType);
					Log.Unindent();
				} else {
					Log.WriteLine("  Data not found.");
				}
			}
		}
		
		/// <summary>
		/// Resolves the specified expression and processes the conversion to targetType.
		/// </summary>
		void ResolveAndProcessConversion(Expression expr, IType targetType)
		{
			if (targetType.Kind == TypeKind.Unknown) {
				// no need to resolve the expression right now
				Scan(expr);
			} else {
				ProcessConversion(Resolve(expr), targetType);
			}
		}
		#endregion
		
		#region GetResolveResult
		/// <summary>
		/// Gets the cached resolve result for the specified node.
		/// Returns <c>null</c> if no cached result was found (e.g. if the node was not visited; or if it was visited in scanning mode).
		/// </summary>
		public ResolveResult GetResolveResult(AstNode node)
		{
			MergeUndecidedLambdas();
			ResolveResult result;
			if (resolveResultCache.TryGetValue(node, out result))
				return result;
			else
				return null;
		}
		
		/// <summary>
		/// Gets the resolver state in front of the specified node.
		/// Returns <c>null</c> if no cached resolver was found (e.g. if the node was skipped by the navigator)
		/// </summary>
		public CSharpResolver GetResolverStateBefore(AstNode node)
		{
			MergeUndecidedLambdas();
			CSharpResolver r;
			if (resolverBeforeDict.TryGetValue(node, out r))
				return r;
			else
				return null;
		}
		#endregion
		
		#region Track UsingScope
		public override ResolveResult VisitCompilationUnit(CompilationUnit unit, object data)
		{
			UsingScope previousUsingScope = resolver.UsingScope;
			try {
				if (parsedFile != null)
					resolver.UsingScope = parsedFile.RootUsingScope;
				ScanChildren(unit);
				return voidResult;
			} finally {
				resolver.UsingScope = previousUsingScope;
			}
		}
		
		public override ResolveResult VisitNamespaceDeclaration(NamespaceDeclaration namespaceDeclaration, object data)
		{
			UsingScope previousUsingScope = resolver.UsingScope;
			try {
				if (parsedFile != null) {
					resolver.UsingScope = parsedFile.GetUsingScope(namespaceDeclaration.StartLocation);
				}
				ScanChildren(namespaceDeclaration);
				return new NamespaceResolveResult(resolver.UsingScope.NamespaceName);
			} finally {
				resolver.UsingScope = previousUsingScope;
			}
		}
		#endregion
		
		#region Track CurrentTypeDefinition
		ResolveResult VisitTypeOrDelegate(AstNode typeDeclaration)
		{
			ITypeDefinition previousTypeDefinition = resolver.CurrentTypeDefinition;
			try {
				ITypeDefinition newTypeDefinition = null;
				if (resolver.CurrentTypeDefinition != null) {
					foreach (ITypeDefinition nestedType in resolver.CurrentTypeDefinition.NestedTypes) {
						if (nestedType.Region.IsInside(typeDeclaration.StartLocation)) {
							newTypeDefinition = nestedType;
							break;
						}
					}
				} else if (parsedFile != null) {
					newTypeDefinition = parsedFile.GetTopLevelTypeDefinition(typeDeclaration.StartLocation);
				}
				if (newTypeDefinition != null)
					resolver.CurrentTypeDefinition = newTypeDefinition;
				
				for (AstNode child = typeDeclaration.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == TypeDeclaration.BaseTypeRole) {
						currentTypeLookupMode = SimpleNameLookupMode.BaseTypeReference;
						Scan(child);
						currentTypeLookupMode = SimpleNameLookupMode.Type;
					} else {
						Scan(child);
					}
				}
				
				return newTypeDefinition != null ? new TypeResolveResult(newTypeDefinition) : errorResult;
			} finally {
				resolver.CurrentTypeDefinition = previousTypeDefinition;
			}
		}
		
		public override ResolveResult VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
		{
			return VisitTypeOrDelegate(typeDeclaration);
		}
		
		public override ResolveResult VisitDelegateDeclaration(DelegateDeclaration delegateDeclaration, object data)
		{
			return VisitTypeOrDelegate(delegateDeclaration);
		}
		#endregion
		
		#region Track CurrentMember
		public override ResolveResult VisitFieldDeclaration(FieldDeclaration fieldDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(fieldDeclaration);
		}
		
		public override ResolveResult VisitFixedFieldDeclaration(FixedFieldDeclaration fixedFieldDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(fixedFieldDeclaration);
		}
		
		public override ResolveResult VisitEventDeclaration(EventDeclaration eventDeclaration, object data)
		{
			return VisitFieldOrEventDeclaration(eventDeclaration);
		}
		
		ResolveResult VisitFieldOrEventDeclaration(AttributedNode fieldOrEventDeclaration)
		{
			int initializerCount = fieldOrEventDeclaration.GetChildrenByRole(FieldDeclaration.Roles.Variable).Count;
			ResolveResult result = null;
			for (AstNode node = fieldOrEventDeclaration.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == FieldDeclaration.Roles.Variable) {
					if (resolver.CurrentTypeDefinition != null) {
						resolver.CurrentMember = resolver.CurrentTypeDefinition.Fields.FirstOrDefault(f => f.Region.IsInside(node.StartLocation));
					}
					
					if (resolverEnabled && initializerCount == 1) {
						result = Resolve(node);
					} else {
						Scan(node);
					}
					
					resolver.CurrentMember = null;
				} else {
					Scan(node);
				}
			}
			return result;
		}
		
		public override ResolveResult VisitVariableInitializer(VariableInitializer variableInitializer, object data)
		{
			if (resolverEnabled) {
				ResolveResult result = errorResult;
				if (variableInitializer.Parent is FieldDeclaration) {
					if (resolver.CurrentMember != null) {
						result = new MemberResolveResult(null, resolver.CurrentMember, resolver.CurrentMember.ReturnType.Resolve(resolver.Context));
					}
				} else {
					string identifier = variableInitializer.Name;
					foreach (IVariable v in resolver.LocalVariables) {
						if (v.Name == identifier) {
							object constantValue = v.IsConst ? v.ConstantValue.GetValue(resolver.Context) : null;
							result = new LocalResolveResult(v, v.Type.Resolve(resolver.Context), constantValue);
							break;
						}
					}
				}
				ArrayInitializerExpression aie = variableInitializer.Initializer as ArrayInitializerExpression;
				ArrayType arrayType = result.Type as ArrayType;
				if (aie != null && arrayType != null) {
					StoreState(aie, resolver.Clone());
					List<ResolveResult> list = new List<ResolveResult>();
					UnpackArrayInitializer(list, aie, arrayType.Dimensions);
					ResolveResult[] initializerElements = list.ToArray();
					ResolveResult arrayCreation = resolver.ResolveArrayCreation(arrayType.ElementType, arrayType.Dimensions, null, initializerElements);
					StoreResult(aie, arrayCreation);
					ProcessConversionsInResult(arrayCreation);
				} else {
					ResolveAndProcessConversion(variableInitializer.Initializer, result.Type);
				}
				return result;
			} else {
				ScanChildren(variableInitializer);
				return null;
			}
		}
		
		public override ResolveResult VisitFixedVariableInitializer(FixedVariableInitializer fixedVariableInitializer, object data)
		{
			if (resolverEnabled) {
				ResolveResult result = errorResult;
				if (resolver.CurrentMember != null) {
					result = new MemberResolveResult(null, resolver.CurrentMember, resolver.CurrentMember.ReturnType.Resolve(resolver.Context));
				}
				ResolveAndProcessConversion(fixedVariableInitializer.CountExpression, KnownTypeReference.Int32.Resolve(resolver.Context));
				return result;
			} else {
				ScanChildren(fixedVariableInitializer);
				return null;
			}
		}
		
		ResolveResult VisitMethodMember(AttributedNode member)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Methods.FirstOrDefault(m => m.Region.IsInside(member.StartLocation));
				}
				
				ScanChildren(member);
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		public override ResolveResult VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data)
		{
			return VisitMethodMember(methodDeclaration);
		}
		
		public override ResolveResult VisitOperatorDeclaration(OperatorDeclaration operatorDeclaration, object data)
		{
			return VisitMethodMember(operatorDeclaration);
		}
		
		public override ResolveResult VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
		{
			return VisitMethodMember(constructorDeclaration);
		}
		
		public override ResolveResult VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
		{
			return VisitMethodMember(destructorDeclaration);
		}
		
		// handle properties/indexers
		ResolveResult VisitPropertyMember(MemberDeclaration propertyOrIndexerDeclaration)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Properties.FirstOrDefault(p => p.Region.IsInside(propertyOrIndexerDeclaration.StartLocation));
				}
				
				for (AstNode node = propertyOrIndexerDeclaration.FirstChild; node != null; node = node.NextSibling) {
					if (node.Role == PropertyDeclaration.SetterRole && resolver.CurrentMember != null) {
						resolver.PushBlock();
						resolver.AddVariable(resolver.CurrentMember.ReturnType, DomRegion.Empty, "value");
						Scan(node);
						resolver.PopBlock();
					} else {
						Scan(node);
					}
				}
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		public override ResolveResult VisitPropertyDeclaration(PropertyDeclaration propertyDeclaration, object data)
		{
			return VisitPropertyMember(propertyDeclaration);
		}
		
		public override ResolveResult VisitIndexerDeclaration(IndexerDeclaration indexerDeclaration, object data)
		{
			return VisitPropertyMember(indexerDeclaration);
		}
		
		public override ResolveResult VisitCustomEventDeclaration(CustomEventDeclaration eventDeclaration, object data)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Events.FirstOrDefault(e => e.Region.IsInside(eventDeclaration.StartLocation));
				}
				
				if (resolver.CurrentMember != null) {
					resolver.PushBlock();
					resolver.AddVariable(resolver.CurrentMember.ReturnType, DomRegion.Empty, "value");
					ScanChildren(eventDeclaration);
					resolver.PopBlock();
				} else {
					ScanChildren(eventDeclaration);
				}
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		
		public override ResolveResult VisitParameterDeclaration(ParameterDeclaration parameterDeclaration, object data)
		{
			ScanChildren(parameterDeclaration);
			if (resolverEnabled) {
				string name = parameterDeclaration.Name;
				// Look in lambda parameters:
				foreach (IParameter p in resolver.LocalVariables.OfType<IParameter>()) {
					if (p.Name == name)
						return new LocalResolveResult(p, p.Type.Resolve(resolver.Context));
				}
				
				IParameterizedMember pm = resolver.CurrentMember as IParameterizedMember;
				if (pm != null) {
					foreach (IParameter p in pm.Parameters) {
						if (p.Name == name) {
							return new LocalResolveResult(p, p.Type.Resolve(resolver.Context));
						}
					}
				}
				return errorResult;
			} else {
				return null;
			}
		}
		
		public override ResolveResult VisitTypeParameterDeclaration(TypeParameterDeclaration typeParameterDeclaration, object data)
		{
			ScanChildren(typeParameterDeclaration);
			if (resolverEnabled) {
				string name = typeParameterDeclaration.Name;
				IMethod m = resolver.CurrentMember as IMethod;
				if (m != null) {
					foreach (var tp in m.TypeParameters) {
						if (tp.Name == name)
							return new TypeResolveResult(tp);
					}
				}
				if (resolver.CurrentTypeDefinition != null) {
					var typeParameters = resolver.CurrentTypeDefinition.TypeParameters;
					// look backwards so that TPs in the current type take precedence over those copied from outer types
					for (int i = typeParameters.Count - 1; i >= 0; i--) {
						if (typeParameters[i].Name == name)
							return new TypeResolveResult(typeParameters[i]);
					}
				}
				return errorResult;
			} else {
				return null;
			}
		}
		
		public override ResolveResult VisitEnumMemberDeclaration(EnumMemberDeclaration enumMemberDeclaration, object data)
		{
			try {
				if (resolver.CurrentTypeDefinition != null) {
					resolver.CurrentMember = resolver.CurrentTypeDefinition.Fields.FirstOrDefault(f => f.Region.IsInside(enumMemberDeclaration.StartLocation));
				}
				
				ScanChildren(enumMemberDeclaration);
				
				if (resolverEnabled && resolver.CurrentMember != null)
					return new MemberResolveResult(null, resolver.CurrentMember, resolver.Context);
				else
					return errorResult;
			} finally {
				resolver.CurrentMember = null;
			}
		}
		#endregion
		
		#region Track CheckForOverflow
		public override ResolveResult VisitCheckedExpression(CheckedExpression checkedExpression, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = true;
				if (resolverEnabled) {
					return Resolve(checkedExpression.Expression);
				} else {
					ScanChildren(checkedExpression);
					return null;
				}
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		public override ResolveResult VisitUncheckedExpression(UncheckedExpression uncheckedExpression, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = false;
				if (resolverEnabled) {
					return Resolve(uncheckedExpression.Expression);
				} else {
					ScanChildren(uncheckedExpression);
					return null;
				}
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		public override ResolveResult VisitCheckedStatement(CheckedStatement checkedStatement, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = true;
				ScanChildren(checkedStatement);
				return voidResult;
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		
		public override ResolveResult VisitUncheckedStatement(UncheckedStatement uncheckedStatement, object data)
		{
			bool oldCheckForOverflow = resolver.CheckForOverflow;
			try {
				resolver.CheckForOverflow = false;
				ScanChildren(uncheckedStatement);
				return voidResult;
			} finally {
				resolver.CheckForOverflow = oldCheckForOverflow;
			}
		}
		#endregion
		
		#region Visit Expressions
		IType ResolveType(AstType type)
		{
			return Resolve(type).Type;
		}
		
		static string GetAnonymousTypePropertyName(Expression expr, out Expression resolveExpr)
		{
			if (expr is NamedArgumentExpression) {
				var namedArgExpr = (NamedArgumentExpression)expr;
				resolveExpr = namedArgExpr.Expression;
				return namedArgExpr.Identifier;
			}
			// no name given, so it's a projection initializer
			if (expr is MemberReferenceExpression) {
				resolveExpr = expr;
				return ((MemberReferenceExpression)expr).MemberName;
			}
			if (expr is IdentifierExpression) {
				resolveExpr = expr;
				return ((IdentifierExpression)expr).Identifier;
			}
			resolveExpr = null;
			return null;
		}
		
		public override ResolveResult VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression, object data)
		{
			ScanChildren(anonymousTypeCreateExpression);
			// 7.6.10.6 Anonymous object creation expressions
			var anonymousType = new DefaultTypeDefinition(resolver.CurrentTypeDefinition, "$Anonymous$");
			anonymousType.IsSynthetic = true;
			foreach (var expr in anonymousTypeCreateExpression.Initializers) {
				Expression resolveExpr;
				var name = GetAnonymousTypePropertyName(expr, out resolveExpr);
				if (string.IsNullOrEmpty(name))
					continue;
				
				var property = new DefaultProperty(anonymousType, name) {
					Accessibility = Accessibility.Public,
					ReturnType = new VarTypeReference(this, resolver.Clone(), resolveExpr, false)
				};
				anonymousType.Properties.Add(property);
			}
			return new ResolveResult(anonymousType);
		}
		
		public override ResolveResult VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, object data)
		{
			ScanChildren(arrayCreateExpression);
			if (!resolverEnabled) {
				return null;
			}
			
			int dimensions = arrayCreateExpression.Arguments.Count;
			ResolveResult[] sizeArguments;
			if (dimensions == 0) {
				dimensions = 1;
				sizeArguments = null;
			} else {
				if (arrayCreateExpression.Arguments.All(e => e is EmptyExpression)) {
					sizeArguments = null;
				} else {
					sizeArguments = new ResolveResult[dimensions];
					int pos = 0;
					foreach (var node in arrayCreateExpression.Arguments)
						sizeArguments[pos++] = Resolve(node);
				}
			}
			
			ResolveResult[] initializerElements;
			if (arrayCreateExpression.Initializer.IsNull) {
				initializerElements = null;
			} else {
				List<ResolveResult> list = new List<ResolveResult>();
				UnpackArrayInitializer(list, arrayCreateExpression.Initializer, dimensions);
				initializerElements = list.ToArray();
			}
			
			if (arrayCreateExpression.Type.IsNull) {
				return resolver.ResolveArrayCreation(null, dimensions, sizeArguments, initializerElements);
			} else {
				IType elementType = ResolveType(arrayCreateExpression.Type);
				foreach (var spec in arrayCreateExpression.AdditionalArraySpecifiers.Reverse()) {
					elementType = new ArrayType(elementType, spec.Dimensions);
				}
				return resolver.ResolveArrayCreation(elementType, dimensions, sizeArguments, initializerElements);
			}
		}
		
		void UnpackArrayInitializer(List<ResolveResult> list, ArrayInitializerExpression initializer, int dimensions)
		{
			Debug.Assert(dimensions >= 1);
			if (dimensions > 1) {
				foreach (var node in initializer.Elements) {
					ArrayInitializerExpression aie = node as ArrayInitializerExpression;
					if (aie != null)
						UnpackArrayInitializer(list, aie, dimensions - 1);
					else
						list.Add(Resolve(node));
				}
			} else {
				foreach (var expr in initializer.Elements)
					list.Add(Resolve(expr));
			}
		}
		
		public override ResolveResult VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			// Array initializers are handled by their parent expression.
			ScanChildren(arrayInitializerExpression);
			return errorResult;
		}
		
		public override ResolveResult VisitAsExpression(AsExpression asExpression, object data)
		{
			if (resolverEnabled) {
				Scan(asExpression.Expression);
				return new ResolveResult(ResolveType(asExpression.Type));
			} else {
				ScanChildren(asExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitAssignmentExpression(AssignmentExpression assignmentExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult left = Resolve(assignmentExpression.Left);
				ResolveAndProcessConversion(assignmentExpression.Right, left.Type);
				return new ResolveResult(left.Type);
			} else {
				ScanChildren(assignmentExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveBaseReference();
			} else {
				ScanChildren(baseReferenceExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult left = Resolve(binaryOperatorExpression.Left);
				ResolveResult right = Resolve(binaryOperatorExpression.Right);
				return resolver.ResolveBinaryOperator(binaryOperatorExpression.Operator, left, right);
			} else {
				ScanChildren(binaryOperatorExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitCastExpression(CastExpression castExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveCast(ResolveType(castExpression.Type), Resolve(castExpression.Expression));
			} else {
				ScanChildren(castExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveConditional(
					Resolve(conditionalExpression.Condition),
					Resolve(conditionalExpression.TrueExpression),
					Resolve(conditionalExpression.FalseExpression));
			} else {
				ScanChildren(conditionalExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveDefaultValue(ResolveType(defaultValueExpression.Type));
			} else {
				ScanChildren(defaultValueExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitDirectionExpression(DirectionExpression directionExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult rr = Resolve(directionExpression.Expression);
				return new ByReferenceResolveResult(rr, directionExpression.FieldDirection == FieldDirection.Out);
			} else {
				ScanChildren(directionExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitEmptyExpression(EmptyExpression emptyExpression, object data)
		{
			return errorResult;
		}
		
		public override ResolveResult VisitIndexerExpression(IndexerExpression indexerExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult target = Resolve(indexerExpression.Target);
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(indexerExpression.Arguments, out argumentNames);
				return resolver.ResolveIndexer(target, arguments, argumentNames);
			} else {
				ScanChildren(indexerExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitIsExpression(IsExpression isExpression, object data)
		{
			ScanChildren(isExpression);
			if (resolverEnabled)
				return new ResolveResult(KnownTypeReference.Boolean.Resolve(resolver.Context));
			else
				return null;
		}
		
		public override ResolveResult VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, object data)
		{
			// Usually, the parent expression takes care of handling NamedArgumentExpressions
			// by calling GetArguments().
			if (resolverEnabled) {
				return Resolve(namedArgumentExpression.Expression);
			} else {
				Scan(namedArgumentExpression.Expression);
				return null;
			}
		}
		
		public override ResolveResult VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolvePrimitive(null);
			} else {
				return null;
			}
		}
		
		public override ResolveResult VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, object data)
		{
			if (resolverEnabled) {
				IType type = ResolveType(objectCreateExpression.Type);
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(objectCreateExpression.Arguments, out argumentNames);
				
				Scan(objectCreateExpression.Initializer); // TODO
				
				return resolver.ResolveObjectCreation(type, arguments, argumentNames);
			} else {
				ScanChildren(objectCreateExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, object data)
		{
			if (resolverEnabled) {
				return Resolve(parenthesizedExpression.Expression);
			} else {
				Scan(parenthesizedExpression.Expression);
				return null;
			}
		}
		
		public override ResolveResult VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult target = Resolve(pointerReferenceExpression.Target);
				ResolveResult deferencedTarget = resolver.ResolveUnaryOperator(UnaryOperatorType.Dereference, target);
				List<IType> typeArguments = new List<IType>();
				foreach (AstType typeArgument in pointerReferenceExpression.TypeArguments) {
					typeArguments.Add(ResolveType(typeArgument));
				}
				return resolver.ResolveMemberAccess(deferencedTarget, pointerReferenceExpression.MemberName,
				                                    typeArguments,
				                                    IsTargetOfInvocation(pointerReferenceExpression));
			} else {
				ScanChildren(pointerReferenceExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolvePrimitive(primitiveExpression.Value);
			} else {
				return null;
			}
		}
		
		public override ResolveResult VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			if (resolverEnabled) {
				return resolver.ResolveSizeOf(ResolveType(sizeOfExpression.Type));
			} else {
				ScanChildren(sizeOfExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitStackAllocExpression(StackAllocExpression stackAllocExpression, object data)
		{
			if (resolverEnabled) {
				ResolveAndProcessConversion(stackAllocExpression.CountExpression, KnownTypeReference.Int32.Resolve(resolver.Context));
				return new ResolveResult(new PointerType(ResolveType(stackAllocExpression.Type)));
			} else {
				ScanChildren(stackAllocExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, object data)
		{
			if (resolverEnabled)
				return resolver.ResolveThisReference();
			else
				return null;
		}
		
		public override ResolveResult VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			ScanChildren(typeOfExpression);
			if (resolverEnabled)
				return new ResolveResult(KnownTypeReference.Type.Resolve(resolver.Context));
			else
				return null;
		}
		
		public override ResolveResult VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, object data)
		{
			if (resolverEnabled) {
				return Resolve(typeReferenceExpression.Type);
			} else {
				Scan(typeReferenceExpression.Type);
				return null;
			}
		}
		
		public override ResolveResult VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			if (resolverEnabled) {
				ResolveResult expr = Resolve(unaryOperatorExpression.Expression);
				return resolver.ResolveUnaryOperator(unaryOperatorExpression.Operator, expr);
			} else {
				ScanChildren(unaryOperatorExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, object data)
		{
			ScanChildren(undocumentedExpression);
			if (resolverEnabled) {
				ITypeReference resultType;
				switch (undocumentedExpression.UndocumentedExpressionType) {
					case UndocumentedExpressionType.ArgListAccess:
					case UndocumentedExpressionType.ArgList:
						resultType = typeof(RuntimeArgumentHandle).ToTypeReference();
						break;
					case UndocumentedExpressionType.RefValue:
						var tre = undocumentedExpression.Arguments.ElementAtOrDefault(1) as TypeReferenceExpression;
						if (tre != null)
							resultType = ResolveType(tre.Type);
						else
							resultType = SharedTypes.UnknownType;
						break;
					case UndocumentedExpressionType.RefType:
						resultType = KnownTypeReference.Type;
						break;
					case UndocumentedExpressionType.MakeRef:
						resultType = typeof(TypedReference).ToTypeReference();
						break;
					default:
						throw new InvalidOperationException("Invalid value for UndocumentedExpressionType");
				}
				return new ResolveResult(resultType.Resolve(resolver.Context));
			} else {
				return null;
			}
		}
		#endregion
		
		#region Visit Identifier/MemberReference/Invocation-Expression
		// IdentifierExpression, MemberReferenceExpression and InvocationExpression
		// are grouped together because they have to work together for
		// "7.6.4.1 Identical simple names and type names" support
		List<IType> GetTypeArguments(IEnumerable<AstType> typeArguments)
		{
			List<IType> result = new List<IType>();
			foreach (AstType typeArgument in typeArguments) {
				result.Add(ResolveType(typeArgument));
			}
			return result;
		}
		
		ResolveResult[] GetArguments(IEnumerable<Expression> argumentExpressions, out string[] argumentNames)
		{
			argumentNames = null;
			ResolveResult[] arguments = new ResolveResult[argumentExpressions.Count()];
			int i = 0;
			foreach (AstNode argument in argumentExpressions) {
				NamedArgumentExpression nae = argument as NamedArgumentExpression;
				AstNode argumentValue;
				if (nae != null) {
					if (argumentNames == null)
						argumentNames = new string[arguments.Length];
					argumentNames[i] = nae.Identifier;
					argumentValue = nae.Expression;
				} else {
					argumentValue = argument;
				}
				arguments[i++] = Resolve(argumentValue);
			}
			return arguments;
		}
		
		static bool IsTargetOfInvocation(AstNode node)
		{
			InvocationExpression ie = node.Parent as InvocationExpression;
			return ie != null && ie.Target == node;
		}
		
		bool IsVariableReferenceWithSameType(ResolveResult rr, string identifier, out TypeResolveResult trr)
		{
			if (!(rr is MemberResolveResult || rr is LocalResolveResult)) {
				trr = null;
				return false;
			}
			trr = resolver.LookupSimpleNameOrTypeName(identifier, EmptyList<IType>.Instance, SimpleNameLookupMode.Type) as TypeResolveResult;
			return trr != null && trr.Type.Equals(rr.Type);
		}
		
		/// <summary>
		/// Gets whether 'rr' is considered a static access on the target identifier.
		/// </summary>
		/// <param name="rr">Resolve Result of the MemberReferenceExpression</param>
		/// <param name="invocationRR">Resolve Result of the InvocationExpression</param>
		bool IsStaticResult(ResolveResult rr, ResolveResult invocationRR)
		{
			if (rr is TypeResolveResult)
				return true;
			MemberResolveResult mrr = (rr is MethodGroupResolveResult ? invocationRR : rr) as MemberResolveResult;
			return mrr != null && mrr.Member.IsStatic;
		}
		
		public override ResolveResult VisitIdentifierExpression(IdentifierExpression identifierExpression, object data)
		{
			// Note: this method is not called when it occurs in a situation where an ambiguity between
			// simple names and type names might occur.
			if (resolverEnabled) {
				var typeArguments = GetTypeArguments(identifierExpression.TypeArguments);
				return resolver.ResolveSimpleName(identifierExpression.Identifier, typeArguments,
				                                  IsTargetOfInvocation(identifierExpression));
			} else {
				ScanChildren(identifierExpression);
				return null;
			}
		}
		
		public override ResolveResult VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, object data)
		{
			// target = Resolve(identifierExpression = memberReferenceExpression.Target)
			// trr = ResolveType(identifierExpression)
			// rr = Resolve(memberReferenceExpression)
			
			IdentifierExpression identifierExpression = memberReferenceExpression.Target as IdentifierExpression;
			if (identifierExpression != null && identifierExpression.TypeArguments.Count == 0
			    && !resolveResultCache.ContainsKey(identifierExpression))
			{
				// Special handling for §7.6.4.1 Identicial simple names and type names
				StoreState(identifierExpression, resolver.Clone());
				ResolveResult target = resolver.ResolveSimpleName(identifierExpression.Identifier, EmptyList<IType>.Instance);
				TypeResolveResult trr;
				if (IsVariableReferenceWithSameType(target, identifierExpression.Identifier, out trr)) {
					// It's ambiguous
					ResolveResult rr = ResolveMemberReferenceOnGivenTarget(target, memberReferenceExpression);
					ResolveResult simpleNameRR = IsStaticResult(rr, null) ? trr : target;
					Log.WriteLine("Ambiguous simple name '{0}' was resolved to {1}", identifierExpression, simpleNameRR);
					StoreResult(identifierExpression, simpleNameRR);
					ProcessConversionsInResult(simpleNameRR);
					return rr;
				} else {
					// It's not ambiguous
					Log.WriteLine("Simple name '{0}' was resolved to {1}", identifierExpression, target);
					StoreResult(identifierExpression, target);
					ProcessConversionsInResult(target);
					if (resolverEnabled) {
						return ResolveMemberReferenceOnGivenTarget(target, memberReferenceExpression);
					} else {
						// Scan children (but not the IdentifierExpression which we already resolved)
						for (AstNode child = memberReferenceExpression.FirstChild; child != null; child = child.NextSibling) {
							if (child != identifierExpression)
								Scan(child);
						}
						return null;
					}
				}
			} else {
				// Regular code path
				if (resolverEnabled) {
					ResolveResult target = Resolve(memberReferenceExpression.Target);
					return ResolveMemberReferenceOnGivenTarget(target, memberReferenceExpression);
				} else {
					ScanChildren(memberReferenceExpression);
					return null;
				}
			}
		}
		
		ResolveResult ResolveMemberReferenceOnGivenTarget(ResolveResult target, MemberReferenceExpression memberReferenceExpression)
		{
			var typeArguments = GetTypeArguments(memberReferenceExpression.TypeArguments);
			return resolver.ResolveMemberAccess(
				target, memberReferenceExpression.MemberName, typeArguments,
				IsTargetOfInvocation(memberReferenceExpression));
		}
		
		public override ResolveResult VisitInvocationExpression(InvocationExpression invocationExpression, object data)
		{
			// rr = Resolve(invocationExpression)
			// target = Resolve(memberReferenceExpression = invocationExpression.Target)
			// idRR = Resolve(identifierExpression = memberReferenceExpression.Target)
			// trr = ResolveType(identifierExpression)
			
			MemberReferenceExpression mre = invocationExpression.Target as MemberReferenceExpression;
			IdentifierExpression identifierExpression = mre != null ? mre.Target as IdentifierExpression : null;
			if (identifierExpression != null && identifierExpression.TypeArguments.Count == 0
			    && !resolveResultCache.ContainsKey(identifierExpression))
			{
				// Special handling for §7.6.4.1 Identicial simple names and type names
				ResolveResult idRR = resolver.ResolveSimpleName(identifierExpression.Identifier, EmptyList<IType>.Instance);
				ResolveResult target = ResolveMemberReferenceOnGivenTarget(idRR, mre);
				Log.WriteLine("Member reference '{0}' on potentially-ambiguous simple-name was resolved to {1}", mre, target);
				StoreResult(mre, target);
				ProcessConversionsInResult(target);
				TypeResolveResult trr;
				if (IsVariableReferenceWithSameType(idRR, identifierExpression.Identifier, out trr)) {
					// It's ambiguous
					ResolveResult rr = ResolveInvocationOnGivenTarget(target, invocationExpression);
					ResolveResult simpleNameRR = IsStaticResult(target, rr) ? trr : idRR;
					Log.WriteLine("Ambiguous simple name '{0}' was resolved to {1}",
					              identifierExpression, simpleNameRR);
					StoreResult(identifierExpression, simpleNameRR);
					ProcessConversionsInResult(simpleNameRR);
					return rr;
				} else {
					// It's not ambiguous
					Log.WriteLine("Simple name '{0}' was resolved to {1}", identifierExpression, idRR);
					StoreResult(identifierExpression, idRR);
					ProcessConversionsInResult(idRR);
					if (resolverEnabled) {
						return ResolveInvocationOnGivenTarget(target, invocationExpression);
					} else {
						// Scan children (but not the MRE which we already resolved)
						for (AstNode child = invocationExpression.FirstChild; child != null; child = child.NextSibling) {
							if (child != mre)
								Scan(child);
						}
						return null;
					}
				}
			} else {
				// Regular code path
				if (resolverEnabled) {
					ResolveResult target = Resolve(invocationExpression.Target);
					return ResolveInvocationOnGivenTarget(target, invocationExpression);
				} else {
					ScanChildren(invocationExpression);
					return null;
				}
			}
		}
		
		ResolveResult ResolveInvocationOnGivenTarget(ResolveResult target, InvocationExpression invocationExpression)
		{
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(invocationExpression.Arguments, out argumentNames);
			return resolver.ResolveInvocation(target, arguments, argumentNames);
		}
		#endregion
		
		#region Lamdbas / Anonymous Functions
		public override ResolveResult VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			return HandleExplicitlyTypedLambda(
				anonymousMethodExpression.Parameters, anonymousMethodExpression.Body,
				isAnonymousMethod: true,
				hasParameterList: anonymousMethodExpression.HasParameterList);
		}
		
		public override ResolveResult VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
		{
			Debug.Assert(resolverEnabled);
			bool isExplicitlyTyped = false;
			bool isImplicitlyTyped = false;
			foreach (var p in lambdaExpression.Parameters) {
				isImplicitlyTyped |= p.Type.IsNull;
				isExplicitlyTyped |= !p.Type.IsNull;
			}
			if (isExplicitlyTyped || !isImplicitlyTyped) {
				return HandleExplicitlyTypedLambda(
					lambdaExpression.Parameters, lambdaExpression.Body,
					isAnonymousMethod: false, hasParameterList: true);
			} else {
				return new ImplicitlyTypedLambda(lambdaExpression, this);
			}
		}
		
		#region Explicitly typed
		ExplicitlyTypedLambda HandleExplicitlyTypedLambda(
			AstNodeCollection<ParameterDeclaration> parameterDeclarations,
			AstNode body, bool isAnonymousMethod, bool hasParameterList)
		{
			List<IParameter> parameters = new List<IParameter>();
			resolver.PushLambdaBlock();
			foreach (var pd in parameterDeclarations) {
				ITypeReference type = MakeTypeReference(pd.Type);
				if (pd.ParameterModifier == ParameterModifier.Ref || pd.ParameterModifier == ParameterModifier.Out)
					type = ByReferenceTypeReference.Create(type);
				
				var p = resolver.AddLambdaParameter(type, MakeRegion(pd), pd.Name,
				                                    isRef: pd.ParameterModifier == ParameterModifier.Ref,
				                                    isOut: pd.ParameterModifier == ParameterModifier.Out);
				parameters.Add(p);
				Scan(pd);
			}
			
			var lambda = new ExplicitlyTypedLambda(parameters, isAnonymousMethod, resolver.Clone(), this, body);
			
			Scan(body);
			
			resolver.PopBlock();
			return lambda;
		}
		
		DomRegion MakeRegion(AstNode node)
		{
			return new DomRegion(parsedFile.FileName, node.StartLocation, node.EndLocation);
		}
		
		sealed class ExplicitlyTypedLambda : LambdaBase
		{
			readonly IList<IParameter> parameters;
			readonly bool isAnonymousMethod;
			
			CSharpResolver storedContext;
			ResolveVisitor visitor;
			AstNode body;
			
			IType inferredReturnType;
			IList<ResolveResult> returnValues;
			bool isValidAsVoidMethod;
			bool success;
			
			// The actual return type is set when the lambda is applied by the conversion.
			IType actualReturnType;
			
			internal override bool IsUndecided {
				get { return actualReturnType == null; }
			}
			
			internal override AstNode LambdaExpression {
				get { return body.Parent; }
			}
			
			public ExplicitlyTypedLambda(IList<IParameter> parameters, bool isAnonymousMethod, CSharpResolver storedContext, ResolveVisitor visitor, AstNode body)
			{
				this.parameters = parameters;
				this.isAnonymousMethod = isAnonymousMethod;
				this.storedContext = storedContext;
				this.visitor = visitor;
				this.body = body;
				
				if (visitor.undecidedLambdas == null)
					visitor.undecidedLambdas = new List<LambdaBase>();
				visitor.undecidedLambdas.Add(this);
				Log.WriteLine("Added undecided explicitly-typed lambda: " + this.LambdaExpression);
			}
			
			public override IList<IParameter> Parameters {
				get {
					return parameters ?? EmptyList<IParameter>.Instance;
				}
			}
			
			bool Analyze()
			{
				// If it's not already analyzed
				if (inferredReturnType == null) {
					Log.WriteLine("Analyzing " + this.LambdaExpression + "...");
					Log.Indent();
					
					visitor.ResetContext(
						storedContext,
						delegate {
							visitor.AnalyzeLambda(body, out success, out isValidAsVoidMethod, out inferredReturnType, out returnValues);
						});
					Log.Unindent();
					Log.WriteLine("Finished analyzing " + this.LambdaExpression);
					
					if (inferredReturnType == null)
						throw new InvalidOperationException("AnalyzeLambda() didn't set inferredReturnType");
				}
				return success;
			}
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, Conversions conversions)
			{
				Log.WriteLine("Testing validity of {0} for return-type {1}...", this, returnType);
				Log.Indent();
				bool valid = Analyze() && IsValidLambda(isValidAsVoidMethod, returnValues, returnType, conversions);
				Log.Unindent();
				Log.WriteLine("{0} is {1} for return-type {2}", this, valid ? "valid" : "invalid", returnType);
				if (valid) {
					return Conversion.AnonymousFunctionConversion(new AnonymousFunctionConversionData(returnType, this));
				} else {
					return Conversion.None;
				}
			}
			
			public override IType GetInferredReturnType(IType[] parameterTypes)
			{
				Analyze();
				return inferredReturnType;
			}
			
			public override bool IsImplicitlyTyped {
				get { return false; }
			}
			
			public override bool IsAnonymousMethod {
				get { return isAnonymousMethod; }
			}
			
			public override bool HasParameterList {
				get { return parameters != null; }
			}
			
			public override string ToString()
			{
				return "[ExplicitlyTypedLambda " + this.LambdaExpression + "]";
			}
			
			public void ApplyReturnType(ResolveVisitor parentVisitor, IType returnType)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				if (parentVisitor != visitor) {
					// Explicitly typed lambdas do not use a nested visitor
					throw new InvalidOperationException();
				}
				if (actualReturnType != null) {
					if (actualReturnType.Equals(returnType))
						return; // return type already set
					throw new InvalidOperationException("inconsistent return types for explicitly-typed lambda");
				}
				actualReturnType = returnType;
				visitor.undecidedLambdas.Remove(this);
				Analyze();
				Log.WriteLine("Applying return type {0} to explicitly-typed lambda {1}", returnType, this.LambdaExpression);
				foreach (var returnValue in returnValues) {
					visitor.ProcessConversion(returnValue, returnType);
				}
			}
			
			internal override void EnforceMerge(ResolveVisitor parentVisitor)
			{
				ApplyReturnType(parentVisitor, SharedTypes.UnknownType);
			}
		}
		#endregion
		
		#region Implicitly typed
		sealed class ImplicitlyTypedLambda : LambdaBase
		{
			internal readonly LambdaExpression lambda;
			readonly CSharpResolver storedContext;
			readonly ParsedFile parsedFile;
			readonly List<LambdaTypeHypothesis> hypotheses = new List<LambdaTypeHypothesis>();
			readonly List<IParameter> parameters = new List<IParameter>();
			
			internal LambdaTypeHypothesis winningHypothesis;
			internal readonly ResolveVisitor parentVisitor;
			
			internal override bool IsUndecided {
				get { return winningHypothesis == null;  }
			}
			
			internal override AstNode LambdaExpression {
				get { return lambda; }
			}
			
			public ImplicitlyTypedLambda(LambdaExpression lambda, ResolveVisitor parentVisitor)
			{
				this.lambda = lambda;
				this.parentVisitor = parentVisitor;
				this.storedContext = parentVisitor.resolver.Clone();
				this.parsedFile = parentVisitor.parsedFile;
				foreach (var pd in lambda.Parameters) {
					parameters.Add(new DefaultParameter(SharedTypes.UnknownType, pd.Name) {
					               	Region = parentVisitor.MakeRegion(pd)
					               });
				}
				if (parentVisitor.undecidedLambdas == null)
					parentVisitor.undecidedLambdas = new List<LambdaBase>();
				parentVisitor.undecidedLambdas.Add(this);
				Log.WriteLine("Added undecided implicitly-typed lambda: " + lambda);
			}
			
			public override IList<IParameter> Parameters {
				get { return parameters; }
			}
			
			public override Conversion IsValid(IType[] parameterTypes, IType returnType, Conversions conversions)
			{
				Log.WriteLine("Testing validity of {0} for parameters ({1}) and return-type {2}...",
				              this, string.Join<IType>(", ", parameterTypes), returnType);
				Log.Indent();
				var hypothesis = GetHypothesis(parameterTypes);
				Conversion c = hypothesis.IsValid(returnType, conversions);
				Log.Unindent();
				Log.WriteLine("{0} is {1} for return-type {2}", hypothesis, c ? "valid" : "invalid", returnType);
				return c;
			}
			
			public override IType GetInferredReturnType(IType[] parameterTypes)
			{
				return GetHypothesis(parameterTypes).inferredReturnType;
			}
			
			LambdaTypeHypothesis GetHypothesis(IType[] parameterTypes)
			{
				if (parameterTypes.Length != parameters.Count)
					throw new ArgumentException("Incorrect parameter type count");
				foreach (var h in hypotheses) {
					bool ok = true;
					for (int i = 0; i < parameterTypes.Length; i++) {
						if (!parameterTypes[i].Equals(h.parameterTypes[i])) {
							ok = false;
							break;
						}
					}
					if (ok)
						return h;
				}
				ResolveVisitor visitor = new ResolveVisitor(storedContext.Clone(), parsedFile);
				LambdaTypeHypothesis newHypothesis = new LambdaTypeHypothesis(this, parameterTypes, visitor);
				hypotheses.Add(newHypothesis);
				return newHypothesis;
			}
			
			/// <summary>
			/// Get any hypothesis for this lambda.
			/// This method is used as fallback if the lambda isn't merged the normal way (AnonymousFunctionConversion)
			/// </summary>
			internal LambdaTypeHypothesis GetAnyHypothesis()
			{
				if (hypotheses.Count == 0) {
					// make a new hypothesis with unknown parameter types
					IType[] parameterTypes = new IType[parameters.Count];
					for (int i = 0; i < parameterTypes.Length; i++) {
						parameterTypes[i] = SharedTypes.UnknownType;
					}
					return GetHypothesis(parameterTypes);
				} else {
					// We have the choice, so pick the hypothesis with the least missing parameter types
					LambdaTypeHypothesis bestHypothesis = hypotheses[0];
					int bestHypothesisUnknownParameters = bestHypothesis.CountUnknownParameters();
					for (int i = 1; i < hypotheses.Count; i++) {
						int c = hypotheses[i].CountUnknownParameters();
						if (c < bestHypothesisUnknownParameters ||
						    (c == bestHypothesisUnknownParameters && hypotheses[i].success && !bestHypothesis.success))
						{
							bestHypothesis = hypotheses[i];
							bestHypothesisUnknownParameters = c;
						}
					}
					return bestHypothesis;
				}
			}
			
			internal override void EnforceMerge(ResolveVisitor parentVisitor)
			{
				GetAnyHypothesis().MergeInto(parentVisitor, SharedTypes.UnknownType);
			}
			
			public override bool IsImplicitlyTyped {
				get { return true; }
			}
			
			public override bool IsAnonymousMethod {
				get { return false; }
			}
			
			public override bool HasParameterList {
				get { return true; }
			}
			
			public override string ToString()
			{
				return "[ImplicitlyTypedLambda " + lambda + "]";
			}
		}
		
		/// <summary>
		/// Every possible set of parameter types gets its own 'hypothetical world'.
		/// It uses a nested ResolveVisitor that has its own resolve cache, so that resolve results cannot leave the hypothetical world.
		/// 
		/// Only after overload resolution is applied and the actual parameter types are known, the winning hypothesis will be merged
		/// with the parent ResolveVisitor.
		/// This is done when the AnonymousFunctionConversion is applied on the parent visitor.
		/// </summary>
		sealed class LambdaTypeHypothesis
		{
			readonly ImplicitlyTypedLambda lambda;
			internal readonly IType[] parameterTypes;
			readonly ResolveVisitor visitor;
			
			internal readonly IType inferredReturnType;
			IList<ResolveResult> returnValues;
			bool isValidAsVoidMethod;
			internal bool success;
			
			public LambdaTypeHypothesis(ImplicitlyTypedLambda lambda, IType[] parameterTypes, ResolveVisitor visitor)
			{
				Debug.Assert(parameterTypes.Length == lambda.Parameters.Count);
				
				this.lambda = lambda;
				this.parameterTypes = parameterTypes;
				this.visitor = visitor;
				
				Log.WriteLine("Analyzing " + ToString() + "...");
				Log.Indent();
				visitor.resolver.PushLambdaBlock();
				int i = 0;
				foreach (var pd in lambda.lambda.Parameters) {
					visitor.resolver.AddLambdaParameter(parameterTypes[i], visitor.MakeRegion(pd), pd.Name, false, false);
					i++;
					visitor.Scan(pd);
				}
				
				visitor.AnalyzeLambda(lambda.lambda.Body, out success, out isValidAsVoidMethod, out inferredReturnType, out returnValues);
				visitor.resolver.PopBlock();
				Log.Unindent();
				Log.WriteLine("Finished analyzing " + ToString());
			}
			
			internal int CountUnknownParameters()
			{
				int c = 0;
				foreach (IType t in parameterTypes) {
					if (t.Kind == TypeKind.Unknown)
						c++;
				}
				return c;
			}
			
			public Conversion IsValid(IType returnType, Conversions conversions)
			{
				if (success && IsValidLambda(isValidAsVoidMethod, returnValues, returnType, conversions)) {
					return Conversion.AnonymousFunctionConversion(new AnonymousFunctionConversionData(returnType, this));
				} else {
					return Conversion.None;
				}
			}
			
			public void MergeInto(ResolveVisitor parentVisitor, IType returnType)
			{
				if (returnType == null)
					throw new ArgumentNullException("returnType");
				if (parentVisitor != lambda.parentVisitor)
					throw new InvalidOperationException("parent visitor mismatch");
				
				if (lambda.winningHypothesis == this)
					return;
				else if (lambda.winningHypothesis != null)
					throw new InvalidOperationException("Trying to merge conflicting hypotheses");
				
				lambda.winningHypothesis = this;
				
				foreach (var returnValue in returnValues) {
					visitor.ProcessConversion(returnValue, returnType);
				}
				
				visitor.MergeUndecidedLambdas();
				Log.WriteLine("Merging " + ToString());
				foreach (var pair in visitor.resolveResultCache) {
					parentVisitor.StoreResult(pair.Key, pair.Value);
				}
				foreach (var pair in visitor.resolverBeforeDict) {
					parentVisitor.StoreState(pair.Key, pair.Value);
				}
				parentVisitor.undecidedLambdas.Remove(lambda);
			}
			
			public override string ToString()
			{
				StringBuilder b = new StringBuilder();
				b.Append("[LambdaTypeHypothesis (");
				for (int i = 0; i < parameterTypes.Length; i++) {
					if (i > 0) b.Append(", ");
					b.Append(parameterTypes[i]);
					b.Append(' ');
					b.Append(lambda.Parameters[i].Name);
				}
				b.Append(") => ");
				b.Append(lambda.lambda.Body.ToString());
				b.Append(']');
				return b.ToString();
			}
		}
		#endregion
		
		#region MergeUndecidedLambdas
		abstract class LambdaBase : LambdaResolveResult
		{
			internal abstract bool IsUndecided { get; }
			internal abstract AstNode LambdaExpression { get; }
			
			internal abstract void EnforceMerge(ResolveVisitor parentVisitor);
		}
		
		void MergeUndecidedLambdas()
		{
			if (undecidedLambdas == null)
				return;
			Log.WriteLine("MergeUndecidedLambdas()...");
			Log.Indent();
			while (undecidedLambdas.Count > 0) {
				LambdaBase lambda = undecidedLambdas[0];
				AstNode parent = lambda.LambdaExpression.Parent;
				// Continue going upwards until we find a node that can be resolved and provides
				// an expected type.
				while (parent is ParenthesizedExpression
				       || parent is CheckedExpression || parent is UncheckedExpression
				       || parent is NamedArgumentExpression || parent is ArrayInitializerExpression)
				{
					parent = parent.Parent;
				}
				CSharpResolver storedResolver;
				if (parent != null && resolverBeforeDict.TryGetValue(parent, out storedResolver)) {
					Log.WriteLine("Trying to resolve '" + parent + "' in order to merge the lambda...");
					Log.Indent();
					ResetContext(storedResolver, delegate { Resolve(parent); });
					Log.Unindent();
				} else {
					Log.WriteLine("Could not find a suitable parent for '" + lambda);
				}
				if (lambda.IsUndecided) {
					// Lambda wasn't merged by resolving its parent -> enforce merging
					Log.WriteLine("Lambda wasn't merged by conversion - enforce merging");
					lambda.EnforceMerge(this);
				}
			}
			Log.Unindent();
			Log.WriteLine("MergeUndecidedLambdas() finished.");
		}
		#endregion
		
		#region AnalyzeLambda
		void AnalyzeLambda(AstNode body, out bool success, out bool isValidAsVoidMethod, out IType inferredReturnType, out IList<ResolveResult> returnValues)
		{
			mode = ResolveVisitorNavigationMode.ResolveAll;
			Expression expr = body as Expression;
			if (expr != null) {
				isValidAsVoidMethod = ExpressionPermittedAsStatement(expr);
				returnValues = new[] { Resolve(expr) };
				inferredReturnType = returnValues[0].Type;
			} else {
				Scan(body);
				
				AnalyzeLambdaVisitor alv = new AnalyzeLambdaVisitor();
				body.AcceptVisitor(alv, null);
				isValidAsVoidMethod = (alv.ReturnExpressions.Count == 0);
				if (alv.HasVoidReturnStatements) {
					returnValues = EmptyList<ResolveResult>.Instance;
					inferredReturnType = KnownTypeReference.Void.Resolve(resolver.Context);
				} else {
					returnValues = new ResolveResult[alv.ReturnExpressions.Count];
					for (int i = 0; i < returnValues.Count; i++) {
						returnValues[i] = resolveResultCache[alv.ReturnExpressions[i]];
					}
					TypeInference ti = new TypeInference(resolver.Context);
					bool tiSuccess;
					inferredReturnType = ti.GetBestCommonType(returnValues, out tiSuccess);
					// Failure to infer a return type does not make the lambda invalid,
					// so we can ignore the 'tiSuccess' value
				}
			}
			Log.WriteLine("Lambda return type was inferred to: " + inferredReturnType);
			// TODO: check for compiler errors within the lambda body
			success = true;
		}
		
		static bool ExpressionPermittedAsStatement(Expression expr)
		{
			UnaryOperatorExpression uoe = expr as UnaryOperatorExpression;
			if (uoe != null) {
				switch (uoe.Operator) {
					case UnaryOperatorType.Increment:
					case UnaryOperatorType.Decrement:
					case UnaryOperatorType.PostIncrement:
					case UnaryOperatorType.PostDecrement:
					case UnaryOperatorType.Await:
						return true;
					default:
						return false;
				}
			}
			return expr is InvocationExpression
				|| expr is ObjectCreateExpression
				|| expr is AssignmentExpression;
		}
		
		static bool IsValidLambda(bool isValidAsVoidMethod, IList<ResolveResult> returnValues, IType returnType, Conversions conversions)
		{
			if (returnType.Kind == TypeKind.Void) {
				return isValidAsVoidMethod;
			} else {
				if (returnValues.Count == 0)
					return false;
				foreach (ResolveResult returnRR in returnValues) {
					if (!conversions.ImplicitConversion(returnRR, returnType))
						return false;
				}
				return true;
			}
		}
		
		sealed class AnalyzeLambdaVisitor : DepthFirstAstVisitor<object, object>
		{
			public bool HasVoidReturnStatements;
			public List<Expression> ReturnExpressions = new List<Expression>();
			
			public override object VisitReturnStatement(ReturnStatement returnStatement, object data)
			{
				Expression expr = returnStatement.Expression;
				if (expr.IsNull) {
					HasVoidReturnStatements = true;
				} else {
					ReturnExpressions.Add(expr);
				}
				return null;
			}
			
			public override object VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, object data)
			{
				// don't go into nested lambdas
				return null;
			}
			
			public override object VisitLambdaExpression(LambdaExpression lambdaExpression, object data)
			{
				return null;
			}
		}
		#endregion
		#endregion
		
		#region Local Variable Scopes (Block Statements)
		public override ResolveResult VisitBlockStatement(BlockStatement blockStatement, object data)
		{
			resolver.PushBlock();
			ScanChildren(blockStatement);
			resolver.PopBlock();
			return voidResult;
		}
		
		public override ResolveResult VisitUsingStatement(UsingStatement usingStatement, object data)
		{
			resolver.PushBlock();
			if (resolverEnabled) {
				for (AstNode child = usingStatement.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == UsingStatement.ResourceAcquisitionRole && child is Expression) {
						ITypeDefinition disposable = resolver.Context.GetTypeDefinition(
							"System", "IDisposable", 0, StringComparer.Ordinal);
						ResolveAndProcessConversion((Expression)child, disposable ?? SharedTypes.UnknownType);
					}
					Scan(child);
				}
			} else {
				ScanChildren(usingStatement);
			}
			resolver.PopBlock();
			return voidResult;
		}
		
		public override ResolveResult VisitFixedStatement(FixedStatement fixedStatement, object data)
		{
			resolver.PushBlock();
			
			VariableInitializer firstInitializer = fixedStatement.Variables.FirstOrDefault();
			ITypeReference type = MakeTypeReference(fixedStatement.Type,
			                                        firstInitializer != null ? firstInitializer.Initializer : null,
			                                        false);
			
			for (AstNode node = fixedStatement.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == FixedStatement.Roles.Variable) {
					VariableInitializer vi = (VariableInitializer)node;
					resolver.AddVariable(type, MakeRegion(vi) , vi.Name);
				}
				Scan(node);
			}
			resolver.PopBlock();
			return voidResult;
		}
		
		public override ResolveResult VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			resolver.PushBlock();
			ITypeReference type = MakeTypeReference(foreachStatement.VariableType, foreachStatement.InExpression, true);
			resolver.AddVariable(type, MakeRegion(foreachStatement.VariableNameToken), foreachStatement.VariableName);
			ScanChildren(foreachStatement);
			resolver.PopBlock();
			return voidResult;
		}
		
		public override ResolveResult VisitCatchClause(CatchClause catchClause, object data)
		{
			resolver.PushBlock();
			IVariable v = null;
			if (catchClause.VariableName != null) {
				v = resolver.AddVariable(MakeTypeReference(catchClause.Type, null, false), MakeRegion(catchClause.VariableNameToken), catchClause.VariableName);
			}
			ScanChildren(catchClause);
			resolver.PopBlock();
			if (resolverEnabled && v != null) {
				return new LocalResolveResult(v, v.Type.Resolve(resolver.Context));
			} else {
				return null;
			}
		}
		#endregion
		
		#region VariableDeclarationStatement
		public override ResolveResult VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, object data)
		{
			bool isConst = (variableDeclarationStatement.Modifiers & Modifiers.Const) != 0;
			VariableInitializer firstInitializer = variableDeclarationStatement.Variables.FirstOrDefault();
			ITypeReference type = MakeTypeReference(variableDeclarationStatement.Type,
			                                        firstInitializer != null ? firstInitializer.Initializer : null,
			                                        false);

			int initializerCount = variableDeclarationStatement.Variables.Count;
			ResolveResult result = null;
			for (AstNode node = variableDeclarationStatement.FirstChild; node != null; node = node.NextSibling) {
				if (node.Role == VariableDeclarationStatement.Roles.Variable) {
					VariableInitializer vi = (VariableInitializer)node;
					
					IConstantValue cv = null;
					if (isConst)
						throw new NotImplementedException();
					resolver.AddVariable(type, MakeRegion(vi), vi.Name, cv);
					
					if (resolverEnabled && initializerCount == 1) {
						result = Resolve(node);
					} else {
						Scan(node);
					}
				} else {
					Scan(node);
				}
			}
			return result;
		}
		#endregion
		
		#region Condition Statements
		public override ResolveResult VisitForStatement(ForStatement forStatement, object data)
		{
			resolver.PushBlock();
			HandleConditionStatement(forStatement);
			resolver.PopBlock();
			return voidResult;
		}
		
		public override ResolveResult VisitIfElseStatement(IfElseStatement ifElseStatement, object data)
		{
			HandleConditionStatement(ifElseStatement);
			return voidResult;
		}
		
		public override ResolveResult VisitWhileStatement(WhileStatement whileStatement, object data)
		{
			HandleConditionStatement(whileStatement);
			return voidResult;
		}
		
		public override ResolveResult VisitDoWhileStatement(DoWhileStatement doWhileStatement, object data)
		{
			HandleConditionStatement(doWhileStatement);
			return voidResult;
		}
		
		void HandleConditionStatement(Statement conditionStatement)
		{
			if (resolverEnabled) {
				for (AstNode child = conditionStatement.FirstChild; child != null; child = child.NextSibling) {
					if (child.Role == AstNode.Roles.Condition) {
						ResolveAndProcessConversion((Expression)child, KnownTypeReference.Boolean.Resolve(resolver.Context));
					} else {
						Scan(child);
					}
				}
			} else {
				ScanChildren(conditionStatement);
			}
		}
		#endregion
		
		#region Return Statements
		public override ResolveResult VisitReturnStatement(ReturnStatement returnStatement, object data)
		{
			if (resolverEnabled && !resolver.IsWithinLambdaExpression && resolver.CurrentMember != null) {
				ResolveAndProcessConversion(returnStatement.Expression, resolver.CurrentMember.ReturnType.Resolve(resolver.Context));
			} else {
				Scan(returnStatement.Expression);
			}
			return voidResult;
		}
		
		public override ResolveResult VisitYieldStatement(YieldStatement yieldStatement, object data)
		{
			if (resolverEnabled && resolver.CurrentMember != null) {
				IType returnType = resolver.CurrentMember.ReturnType.Resolve(resolver.Context);
				IType elementType = GetElementType(returnType, resolver.Context, true);
				ResolveAndProcessConversion(yieldStatement.Expression, elementType);
			} else {
				Scan(yieldStatement.Expression);
			}
			return voidResult;
		}
		
		public override ResolveResult VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, object data)
		{
			return voidResult;
		}
		#endregion
		
		#region Local Variable Type Inference
		/// <summary>
		/// Creates a type reference for the specified type node.
		/// If the type node is 'var', performs type inference on the initializer expression.
		/// </summary>
		ITypeReference MakeTypeReference(AstType type, AstNode initializerExpression, bool isForEach)
		{
			bool typeNeedsResolving;
			if (mode == ResolveVisitorNavigationMode.ResolveAll) {
				typeNeedsResolving = true;
			} else {
				var modeForType = navigator.Scan(type);
				typeNeedsResolving = (modeForType == ResolveVisitorNavigationMode.Resolve || modeForType == ResolveVisitorNavigationMode.ResolveAll);
			}
			if (initializerExpression != null && IsVar(type)) {
				var typeRef = new VarTypeReference(this, resolver.Clone(), initializerExpression, isForEach);
				if (typeNeedsResolving) {
					// Hack: I don't see a clean way to make the 'var' SimpleType resolve to the inferred type,
					// so we just do it here and store the result in the resolver cache.
					IType actualType = typeRef.Resolve(resolver.Context);
					if (actualType.Kind != TypeKind.Unknown) {
						StoreResult(type, new TypeResolveResult(actualType));
					} else {
						StoreResult(type, errorResult);
					}
					return actualType;
				} else {
					return typeRef;
				}
			} else {
				// Perf: avoid duplicate resolving of the type (once as ITypeReference, once directly in ResolveVisitor)
				// if possible. By using ResolveType when we know we need to resolve the node anyways, the resolve cache
				// can take care of the duplicate call.
				if (typeNeedsResolving)
					return ResolveType(type);
				else
					return MakeTypeReference(type);
			}
		}
		
		static bool IsVar(AstType returnType)
		{
			SimpleType st = returnType as SimpleType;
			return st != null && st.Identifier == "var" && st.TypeArguments.Count == 0;
		}
		
		ITypeReference MakeTypeReference(AstType type)
		{
			return TypeSystemConvertVisitor.ConvertType(type, resolver.CurrentTypeDefinition, resolver.CurrentMember as IMethod, resolver.UsingScope, currentTypeLookupMode);
		}
		
		sealed class VarTypeReference : ITypeReference
		{
			ResolveVisitor visitor;
			CSharpResolver storedContext;
			AstNode initializerExpression;
			bool isForEach;
			
			IType result;
			
			public VarTypeReference(ResolveVisitor visitor, CSharpResolver storedContext, AstNode initializerExpression, bool isForEach)
			{
				this.visitor = visitor;
				this.storedContext = storedContext;
				this.initializerExpression = initializerExpression;
				this.isForEach = isForEach;
			}
			
			public IType Resolve(ITypeResolveContext context)
			{
				if (visitor == null)
					return result ?? SharedTypes.UnknownType;
				
				visitor.ResetContext(
					storedContext,
					delegate {
						result = visitor.Resolve(initializerExpression).Type;
						
						if (isForEach) {
							result = GetElementType(result, storedContext.Context, false);
						}
					});
				visitor = null;
				storedContext = null;
				initializerExpression = null;
				return result;
			}
			
			public override string ToString()
			{
				if (visitor == null)
					return "var=" + result;
				else
					return "var (not yet resolved)";
			}
		}
		
		static IType GetElementType(IType result, ITypeResolveContext context, bool allowIEnumerator)
		{
			bool foundSimpleIEnumerable = false;
			foreach (IType baseType in result.GetAllBaseTypes(context)) {
				ITypeDefinition baseTypeDef = baseType.GetDefinition();
				if (baseTypeDef != null && (
					baseTypeDef.Name == "IEnumerable" || (allowIEnumerator && baseType.Name == "IEnumerator")))
				{
					if (baseTypeDef.Namespace == "System.Collections.Generic" && baseTypeDef.TypeParameterCount == 1) {
						ParameterizedType pt = baseType as ParameterizedType;
						if (pt != null) {
							return pt.GetTypeArgument(0);
						}
					} else if (baseTypeDef.Namespace == "System.Collections" && baseTypeDef.TypeParameterCount == 0) {
						foundSimpleIEnumerable = true;
					}
				}
			}
			// System.Collections.IEnumerable found in type hierarchy -> Object is element type.
			if (foundSimpleIEnumerable)
				return KnownTypeReference.Object.Resolve(context);
			return SharedTypes.UnknownType;
		}
		#endregion
		
		#region Attributes
		public override ResolveResult VisitAttribute(Attribute attribute, object data)
		{
			if (resolverEnabled) {
				var type = ResolveType(attribute.Type);
				
				// Separate arguments into ctor arguments and non-ctor arguments:
				var constructorArguments = attribute.Arguments.Where(a => !(a is AssignmentExpression));
				var nonConstructorArguments = attribute.Arguments.Where(a => a is AssignmentExpression);
				
				// Scan the non-constructor arguments
				foreach (var arg in nonConstructorArguments)
					Scan(arg); // TODO: handle these like object initializers
				
				// Resolve the ctor arguments and find the matching ctor overload
				string[] argumentNames;
				ResolveResult[] arguments = GetArguments(constructorArguments, out argumentNames);
				return resolver.ResolveObjectCreation(type, arguments, argumentNames);
			} else {
				ScanChildren(attribute);
				return null;
			}
		}
		#endregion
		
		#region Using Declaration
		public override ResolveResult VisitUsingDeclaration(UsingDeclaration usingDeclaration, object data)
		{
			currentTypeLookupMode = SimpleNameLookupMode.TypeInUsingDeclaration;
			ScanChildren(usingDeclaration);
			currentTypeLookupMode = SimpleNameLookupMode.Type;
			return null;
		}
		
		public override ResolveResult VisitUsingAliasDeclaration(UsingAliasDeclaration usingDeclaration, object data)
		{
			currentTypeLookupMode = SimpleNameLookupMode.TypeInUsingDeclaration;
			ScanChildren(usingDeclaration);
			currentTypeLookupMode = SimpleNameLookupMode.Type;
			return null;
		}
		#endregion
		
		#region Type References
		public override ResolveResult VisitPrimitiveType(PrimitiveType primitiveType, object data)
		{
			if (!resolverEnabled)
				return null;
			IType type = MakeTypeReference(primitiveType).Resolve(resolver.Context);
			if (type.Kind != TypeKind.Unknown)
				return new TypeResolveResult(type);
			else
				return errorResult;
		}
		
		ResolveResult HandleAttributeType(AstType astType)
		{
			ScanChildren(astType);
			IType type = TypeSystemConvertVisitor.ConvertAttributeType(astType, resolver.CurrentTypeDefinition, resolver.CurrentMember as IMethod, resolver.UsingScope).Resolve(resolver.Context);
			if (type.Kind != TypeKind.Unknown)
				return new TypeResolveResult(type);
			else
				return errorResult;
		}
		
		public override ResolveResult VisitSimpleType(SimpleType simpleType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(simpleType);
				return null;
			}
			if (simpleType.Parent is Attribute) {
				return HandleAttributeType(simpleType);
			}
			
			var typeArguments = GetTypeArguments(simpleType.TypeArguments);
			return resolver.LookupSimpleNameOrTypeName(simpleType.Identifier, typeArguments, currentTypeLookupMode);
		}
		
		public override ResolveResult VisitMemberType(MemberType memberType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(memberType);
				return null;
			}
			if (memberType.Parent is Attribute) {
				return HandleAttributeType(memberType);
			}
			ResolveResult target;
			if (memberType.IsDoubleColon && memberType.Target is SimpleType) {
				target = resolver.ResolveAlias(((SimpleType)memberType.Target).Identifier);
			} else {
				target = Resolve(memberType.Target);
			}
			
			var typeArguments = GetTypeArguments(memberType.TypeArguments);
			return resolver.ResolveMemberType(target, memberType.MemberName, typeArguments);
		}
		
		public override ResolveResult VisitComposedType(ComposedType composedType, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(composedType);
				return null;
			}
			IType t = ResolveType(composedType.BaseType);
			if (composedType.HasNullableSpecifier) {
				t = NullableType.Create(t, resolver.Context);
			}
			for (int i = 0; i < composedType.PointerRank; i++) {
				t = new PointerType(t);
			}
			foreach (var a in composedType.ArraySpecifiers.Reverse()) {
				t = new ArrayType(t, a.Dimensions);
			}
			return new TypeResolveResult(t);
		}
		#endregion
		
		#region Query Expressions
		public override ResolveResult VisitQueryExpression(QueryExpression queryExpression, object data)
		{
			throw new NotImplementedException();
		}
		#endregion
		
		#region Constructor Initializer
		public override ResolveResult VisitConstructorInitializer(ConstructorInitializer constructorInitializer, object data)
		{
			if (!resolverEnabled) {
				ScanChildren(constructorInitializer);
				return null;
			}
			ResolveResult target;
			if (constructorInitializer.ConstructorInitializerType == ConstructorInitializerType.Base) {
				target = resolver.ResolveBaseReference();
			} else {
				target = resolver.ResolveThisReference();
			}
			string[] argumentNames;
			ResolveResult[] arguments = GetArguments(constructorInitializer.Arguments, out argumentNames);
			return resolver.ResolveObjectCreation(target.Type, arguments, argumentNames);
		}
		#endregion
		
		#region Token Nodes
		public override ResolveResult VisitIdentifier(Identifier identifier, object data)
		{
			return null;
		}
		
		public override ResolveResult VisitComment(Comment comment, object data)
		{
			return null;
		}
		
		public override ResolveResult VisitCSharpTokenNode(CSharpTokenNode cSharpTokenNode, object data)
		{
			return null;
		}
		#endregion
	}
}
