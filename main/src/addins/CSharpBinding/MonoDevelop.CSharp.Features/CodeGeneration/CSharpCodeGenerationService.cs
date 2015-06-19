//
// CSharpCodeGenerationService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp.CodeGeneration
{
	#if NR6
	public
	#endif
	class CSharpCodeGenerationService
	{
		readonly static Type typeInfo;
		readonly object instance;

		readonly static MethodInfo createEventDeclarationMethod;
		readonly static MethodInfo createFieldDeclaration;
		readonly static MethodInfo createMethodDeclaration;
		readonly static MethodInfo createPropertyDeclaration;
		readonly static MethodInfo createNamedTypeDeclaration;
		readonly static MethodInfo createNamespaceDeclaration;
		readonly static MethodInfo addMethodAsync;
		readonly static MethodInfo addMembersAsync;

		readonly static MethodInfo canAddTo1, canAddTo2;

		static CSharpCodeGenerationService ()
		{
			var abstractServiceType = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.AbstractCodeGenerationService" + ReflectionNamespaces.WorkspacesAsmName, true);
			var codeGenerationDestinationType = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationDestination" + ReflectionNamespaces.WorkspacesAsmName, true);
			var codeGenerationOptionsType = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationOptions" + ReflectionNamespaces.WorkspacesAsmName, true);




			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.CodeGeneration.CSharpCodeGenerationService" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			//TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options = null, CancellationToken cancellationToken = default(CancellationToken)


			addMethod = typeInfo.GetMethods ().Single (m => 
			                                           m.Name == "AddMethod" &&
			                                           m.GetParameters ().Count () == 4);
			if (addMethod == null)
				throw new InvalidOperationException ("AddMethod not found.");

			createEventDeclarationMethod = typeInfo.GetMethod ("CreateEventDeclaration", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(IEventSymbol), codeGenerationDestinationType, codeGenerationOptionsType }, null);
			if (createEventDeclarationMethod == null)
				throw new InvalidOperationException ("CreateEventDeclaration not found.");

			createFieldDeclaration = typeInfo.GetMethod ("CreateFieldDeclaration", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(IFieldSymbol), codeGenerationDestinationType, codeGenerationOptionsType }, null);
			if (createFieldDeclaration == null)
				throw new InvalidOperationException ("CreateFieldDeclaration not found.");

			createMethodDeclaration = typeInfo.GetMethod ("CreateMethodDeclaration", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(IMethodSymbol), codeGenerationDestinationType, codeGenerationOptionsType }, null);
			if (createMethodDeclaration == null)
				throw new InvalidOperationException ("CreateMethodDeclaration not found.");

			createPropertyDeclaration = typeInfo.GetMethod ("CreatePropertyDeclaration", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(IPropertySymbol), codeGenerationDestinationType, codeGenerationOptionsType }, null);
			if (createPropertyDeclaration == null)
				throw new InvalidOperationException ("CreatePropertyDeclaration not found.");


			createNamedTypeDeclaration = typeInfo.GetMethod ("CreateNamedTypeDeclaration", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(INamedTypeSymbol), codeGenerationDestinationType, codeGenerationOptionsType }, null);
			if (createNamedTypeDeclaration == null)
				throw new InvalidOperationException ("CreateNamedTypeDeclaration not found.");

			createNamespaceDeclaration = typeInfo.GetMethod ("CreateNamespaceDeclaration", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(INamespaceSymbol), codeGenerationDestinationType, codeGenerationOptionsType }, null);
			if (createNamespaceDeclaration == null)
				throw new InvalidOperationException ("CreateNamespaceDeclaration not found.");

			addMethodAsync = abstractServiceType.GetMethod ("AddMethodAsync", BindingFlags.Instance | BindingFlags.Public);
			if (addMethodAsync == null)
				throw new InvalidOperationException ("AddMethodAsync not found.");

			addMembersAsync = abstractServiceType.GetMethod ("AddMembersAsync", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(Solution), typeof(INamedTypeSymbol), typeof(IEnumerable<ISymbol>), CodeGenerationOptions.typeInfo, typeof(CancellationToken) }, null);
			if (addMembersAsync == null)
				throw new InvalidOperationException ("AddMembersAsync not found.");

			canAddTo1 = typeInfo.GetMethod ("CanAddTo", new [] {typeof(ISymbol), typeof(Solution), typeof(CancellationToken) });
			if (canAddTo1 == null)
				throw new InvalidOperationException ("CanAddTo1 not found.");

			canAddTo2 = typeInfo.GetMethod ("CanAddTo", new [] {typeof(SyntaxNode), typeof(Solution), typeof(CancellationToken) });
			if (canAddTo2 == null)
				throw new InvalidOperationException ("CanAddTo1 not found.");

			addFieldAsync = abstractServiceType.GetMethod ("AddFieldAsync", BindingFlags.Instance | BindingFlags.Public, null, new [] { typeof(Solution), typeof(INamedTypeSymbol), typeof(IFieldSymbol), CodeGenerationOptions.typeInfo, typeof(CancellationToken) }, null);
			if (addFieldAsync == null)
				throw new InvalidOperationException ("AddFieldAsync not found.");

			addStatements = typeInfo.GetMethod ("AddStatements", BindingFlags.Instance | BindingFlags.Public);
			if (addStatements == null)
				throw new InvalidOperationException ("AddStatements not found.");

		}

		public CSharpCodeGenerationService(HostLanguageServices languageServices)
		{
			instance = Activator.CreateInstance (typeInfo, new object[] {
				languageServices
			});
		}

		public CSharpCodeGenerationService (Workspace workspace, string language)
		{
			var languageService = workspace.Services.GetLanguageServices (language);

			this.instance = Activator.CreateInstance (typeInfo, new [] { languageService });
		}

		public CSharpCodeGenerationService (Workspace workspace) : this (workspace, LanguageNames.CSharp)
		{
		}

		static MethodInfo addStatements;
		public TDeclarationNode AddStatements<TDeclarationNode>(
			TDeclarationNode destinationMember,
			IEnumerable<SyntaxNode> statements,
			CodeGenerationOptions options,
			CancellationToken cancellationToken)
		{
			try {
				return (TDeclarationNode)addStatements.MakeGenericMethod (typeof (TDeclarationNode)).Invoke (instance, new object[] { destinationMember, statements, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return default(TDeclarationNode);
			}
		}

		static MethodInfo addMethod;

		/// <summary>
		/// Adds a method into destination.
		/// </summary>
		public TDeclarationNode AddMethod<TDeclarationNode>(TDeclarationNode destination, IMethodSymbol method, CodeGenerationOptions options = null, CancellationToken cancellationToken = default(CancellationToken)) where TDeclarationNode : SyntaxNode
		{
			try {
				return (TDeclarationNode)addMethod.MakeGenericMethod (typeof (TDeclarationNode)).Invoke (instance, new object[] { destination, method, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return default (TDeclarationNode);
			}
		}


		/// <summary>
		/// Returns a newly created event declaration node from the provided event.
		/// </summary
		public SyntaxNode CreateEventDeclaration(IEventSymbol @event, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			try {
				return (SyntaxNode)createEventDeclarationMethod.Invoke (instance, new object[] { @event, (int)destination, null });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		/// <summary>
		/// Returns a newly created field declaration node from the provided field.
		/// </summary>
		public SyntaxNode CreateFieldDeclaration(IFieldSymbol field, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			try {
				return (SyntaxNode)createFieldDeclaration.Invoke (instance, new object[] { @field, (int)destination, null });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		/// <summary>
		/// Returns a newly created method declaration node from the provided method.
		/// </summary>
		public SyntaxNode CreateMethodDeclaration(IMethodSymbol method, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			try {
				return (SyntaxNode)createMethodDeclaration.Invoke (instance, new object[] { @method, (int)destination, null });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		/// <summary>
		/// Returns a newly created property declaration node from the provided property.
		/// </summary>
		public SyntaxNode CreatePropertyDeclaration(IPropertySymbol property, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			try {
				return (SyntaxNode)createPropertyDeclaration.Invoke (instance, new object[] { @property, (int)destination, null });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		/// <summary>
		/// Returns a newly created named type declaration node from the provided named type.
		/// </summary>
		public SyntaxNode CreateNamedTypeDeclaration(INamedTypeSymbol namedType, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			try {
				return (SyntaxNode)createNamedTypeDeclaration.Invoke (instance, new object[] { @namedType, (int)destination, null });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		/// <summary>
		/// Returns a newly created namespace declaration node from the provided namespace.
		/// </summary>
		public SyntaxNode CreateNamespaceDeclaration(INamespaceSymbol @namespace, CodeGenerationDestination destination = CodeGenerationDestination.Unspecified)
		{
			try {
				return (SyntaxNode)createNamespaceDeclaration.Invoke (instance, new object[] { @namespace, (int)destination, null });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		public Task<Document> AddMethodAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addMethodAsync.Invoke (instance, new object[] { solution, destination, method, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		/// <summary>
		/// Adds all the provided members into destination.
		/// </summary>
		public Task<Document> AddMembersAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addMembersAsync.Invoke (instance, new object[] { solution, destination, members, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		static MethodInfo addFieldAsync;

		public Task<Document> AddFieldAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationOptions options, CancellationToken cancellationToken)
		{
			try {
				return (Task<Document>)addFieldAsync.Invoke (instance, new object[] { solution, destination, field, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}


		/// <summary>
		/// <c>true</c> if destination is a location where other symbols can be added to.
		/// </summary>
		public bool CanAddTo(ISymbol destination, Solution solution, CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (bool)canAddTo1.Invoke (instance, new object[] { destination, solution, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
		}

		/// <summary>
		/// <c>true</c> if destination is a location where other symbols can be added to.
		/// </summary>
		public bool CanAddTo(SyntaxNode destination, Solution solution, CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (bool)canAddTo2.Invoke (instance, new object[] { destination, solution, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return false;
			}
		}

	}
}

