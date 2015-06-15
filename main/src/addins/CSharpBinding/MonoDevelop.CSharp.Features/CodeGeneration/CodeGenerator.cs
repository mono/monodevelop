//
// CodeGenerator.cs
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
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp.CodeGeneration;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp
{
	#if NR6
	public
	#endif
	static class CodeGenerator
	{
		readonly static Type typeInfo;

		static CodeGenerator ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CodeGeneration.CodeGenerator" + ReflectionNamespaces.WorkspacesAsmName, true);
			addPropertyDeclarationAsyncMethod = typeInfo.GetMethod ("AddPropertyDeclarationAsync", BindingFlags.Static | BindingFlags.Public);
			addMethodDeclarationAsyncMethod = typeInfo.GetMethod ("AddMethodDeclarationAsync", BindingFlags.Static | BindingFlags.Public);
			addFieldDeclarationAsyncMethod = typeInfo.GetMethod ("AddFieldDeclarationAsync", BindingFlags.Static | BindingFlags.Public);
			addNamespaceOrTypeDeclarationAsyncMethod = typeInfo.GetMethod ("AddNamespaceOrTypeDeclarationAsync", BindingFlags.Static | BindingFlags.Public);
			addNamedTypeDeclarationAsyncMethod1 = typeInfo.GetMethod ("AddNamedTypeDeclarationAsync", new [] { typeof(Solution), typeof(INamedTypeSymbol), typeof(INamedTypeSymbol), CodeGenerationOptions.typeInfo, typeof(CancellationToken) });
			addNamedTypeDeclarationAsyncMethod2 = typeInfo.GetMethod ("AddNamedTypeDeclarationAsync", new [] { typeof(Solution), typeof(INamespaceSymbol), typeof(INamedTypeSymbol), CodeGenerationOptions.typeInfo, typeof(CancellationToken) });
		}

		static MethodInfo addNamedTypeDeclarationAsyncMethod1;

		public static Task<Document> AddNamedTypeDeclarationAsync(Solution solution, INamedTypeSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options = default(CodeGenerationOptions), CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addNamedTypeDeclarationAsyncMethod1.Invoke (null, new object[] { solution, destination, namedType, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		static MethodInfo addNamedTypeDeclarationAsyncMethod2;
		public static Task<Document> AddNamedTypeDeclarationAsync(Solution solution, INamespaceSymbol destination, INamedTypeSymbol namedType, CodeGenerationOptions options = default(CodeGenerationOptions), CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addNamedTypeDeclarationAsyncMethod2.Invoke (null, new object[] { solution, destination, namedType, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		static MethodInfo addNamespaceOrTypeDeclarationAsyncMethod;

		public static Task<Document> AddNamespaceOrTypeDeclarationAsync(Solution solution, INamespaceSymbol destination, INamespaceOrTypeSymbol namespaceOrType, CodeGenerationOptions options = default(CodeGenerationOptions), CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addNamespaceOrTypeDeclarationAsyncMethod.Invoke (null, new object[] { solution, destination, namespaceOrType, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}


		readonly static MethodInfo addFieldDeclarationAsyncMethod;

		public static Task<Document> AddFieldDeclarationAsync(Solution solution, INamedTypeSymbol destination, IFieldSymbol field, CodeGenerationOptions options = default(CodeGenerationOptions), CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addFieldDeclarationAsyncMethod.Invoke (null, new object[] { solution, destination, field, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}


		readonly static MethodInfo addPropertyDeclarationAsyncMethod;
		public static Task<Document> AddPropertyDeclarationAsync(Solution solution, INamedTypeSymbol destination, IPropertySymbol property, CodeGenerationOptions options = default(CodeGenerationOptions), CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addPropertyDeclarationAsyncMethod.Invoke (null, new object[] { solution, destination, property, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		readonly static MethodInfo addMethodDeclarationAsyncMethod;
		public static Task<Document> AddMethodDeclarationAsync(Solution solution, INamedTypeSymbol destination, IMethodSymbol method, CodeGenerationOptions options = default(CodeGenerationOptions), CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<Document>)addMethodDeclarationAsyncMethod.Invoke (null, new object[] { solution, destination, method, options != null ? options.Instance : null, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		public static Task<Document> AddMemberDeclarationsAsync(Solution solution, INamedTypeSymbol destination, IEnumerable<ISymbol> members, CodeGenerationOptions options = default(CodeGenerationOptions), CancellationToken cancellationToken = default(CancellationToken))
		{
			return  new CSharpCodeGenerationService(solution.Workspace, destination.Language).AddMembersAsync(solution, destination, members, options, cancellationToken);
		}

		public static bool CanAdd(Solution solution, ISymbol destination, CancellationToken cancellationToken = default(CancellationToken))
		{
			return new CSharpCodeGenerationService(solution.Workspace, destination.Language).CanAddTo(destination, solution, cancellationToken);
		}
	}
}

