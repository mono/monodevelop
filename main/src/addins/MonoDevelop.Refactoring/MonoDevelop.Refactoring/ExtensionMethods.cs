//
// ExtensionMethods.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using System.Threading.Tasks;
using MonoDevelop.Core;
using System.Threading;
using MonoDevelop.Core.Instrumentation;

namespace MonoDevelop.Refactoring
{
    public static class ExtensionMethods
    {

		class ResolverAnnotation
		{
			public CancellationTokenSource SharedTokenSource;
			public TaskWrapper Task;
			public CSharpUnresolvedFile ParsedFile;
		}

		public class TaskWrapper {
			readonly Task<CSharpAstResolver> underlyingTask;

			public CSharpAstResolver Result {
				get {
					if (underlyingTask.IsCanceled)
						return null;
					try {
						return underlyingTask.Result;
					} catch (AggregateException ae) {
						if (ae.InnerException is TaskCanceledException)
							return null;
						throw;
					}  catch (TaskCanceledException) {
						return null;
					} catch (Exception e) {
						LoggingService.LogWarning ("Exception while getting shared AST resolver.", e);
						return null;
					}
				}
			}
			public TaskWrapper (Task<CSharpAstResolver> underlyingTask)
			{
				this.underlyingTask = underlyingTask;
			}
			
		}

		public static TimerCounter ResolveCounter = InstrumentationService.CreateTimerCounter("Resolve document", "Parsing");

		/// <summary>
		/// Returns a full C# syntax tree resolver which is shared between semantic highlighting, source analysis and refactoring.
		/// For code analysis tasks this should be used instead of generating an own resolver. Only exception is if a local resolving is done using a 
		/// resolve navigator.
		/// Note: The shared resolver is fully resolved.
		/// </summary>
		public static TaskWrapper GetSharedResolver (this Document document)
		{
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument == null || document.IsProjectContextInUpdate)
				return null;

			var unit       = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
			if (unit == null || parsedFile == null)
				return null;
			var compilation = document.Compilation;

			var resolverAnnotation = document.Annotation<ResolverAnnotation> ();

			if (resolverAnnotation != null) {
				if (resolverAnnotation.ParsedFile == parsedFile)
					return resolverAnnotation.Task;
				if (resolverAnnotation.SharedTokenSource != null)
					resolverAnnotation.SharedTokenSource.Cancel ();
				document.RemoveAnnotations<ResolverAnnotation> ();
			}

			var tokenSource = new CancellationTokenSource ();
			var token = tokenSource.Token;
			var resolveTask = Task.Factory.StartNew (delegate {
				try {
					using (var timer = ResolveCounter.BeginTiming ()) {
						var result = new CSharpAstResolver (compilation, unit, parsedFile);
						result.ApplyNavigator (new ConstantModeResolveVisitorNavigator (ResolveVisitorNavigationMode.Resolve, null), token);
						return result;
					}
				} catch (OperationCanceledException) {
					return null;
				} catch (Exception e) {
					LoggingService.LogError ("Error while creating the resolver.", e);
					return null;
				}
			}, token);
			var wrapper = new TaskWrapper (resolveTask);
			document.AddAnnotation (new ResolverAnnotation {
				Task = wrapper,
				ParsedFile = parsedFile,
				SharedTokenSource = tokenSource
			});
			return wrapper;
		}

		public sealed class ConstantModeResolveVisitorNavigator : IResolveVisitorNavigator
		{
			readonly ResolveVisitorNavigationMode mode;
			readonly IResolveVisitorNavigator targetForResolveCalls;

			public ConstantModeResolveVisitorNavigator(ResolveVisitorNavigationMode mode, IResolveVisitorNavigator targetForResolveCalls)
			{
				this.mode = mode;
				this.targetForResolveCalls = targetForResolveCalls;
			}

			ResolveVisitorNavigationMode IResolveVisitorNavigator.Scan(AstNode node)
			{
				return mode;
			}

			void IResolveVisitorNavigator.Resolved(AstNode node, ResolveResult result)
			{
				if (targetForResolveCalls != null)
					targetForResolveCalls.Resolved(node, result);
			}

			void IResolveVisitorNavigator.ProcessConversion(Expression expression, ResolveResult result, Conversion conversion, IType targetType)
			{
				if (targetForResolveCalls != null)
					targetForResolveCalls.ProcessConversion(expression, result, conversion, targetType);
			}
		}

    }
}