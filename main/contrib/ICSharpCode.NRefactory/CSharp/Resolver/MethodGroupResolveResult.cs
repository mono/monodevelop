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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp.Resolver
{
	/// <summary>
	/// Represents a group of methods.
	/// </summary>
	public class MethodGroupResolveResult : ResolveResult
	{
		readonly IList<IMethod> methods;
		readonly IList<IType> typeArguments;
		readonly ResolveResult targetResult;
		readonly string methodName;
		
		public MethodGroupResolveResult(ResolveResult targetResult, string methodName, IList<IMethod> methods, IList<IType> typeArguments) : base(SharedTypes.UnknownType)
		{
			if (targetResult == null)
				throw new ArgumentNullException("targetResult");
			if (methods == null)
				throw new ArgumentNullException("methods");
			this.targetResult = targetResult;
			this.methodName = methodName;
			this.methods = methods;
			this.typeArguments = typeArguments ?? EmptyList<IType>.Instance;
		}
		
		/// <summary>
		/// Gets the resolve result for the target object.
		/// </summary>
		public ResolveResult TargetResult {
			get { return targetResult; }
		}
		
		/// <summary>
		/// Gets the type of the reference to the target object.
		/// </summary>
		public IType TargetType {
			get { return targetResult.Type; }
		}
		
		/// <summary>
		/// Gets the name of the methods in this group.
		/// </summary>
		public string MethodName {
			get { return methodName; }
		}
		
		/// <summary>
		/// Gets the methods that were found.
		/// This list does not include extension methods.
		/// </summary>
		public IList<IMethod> Methods {
			get { return methods; }
		}
		
		/// <summary>
		/// Gets the type arguments that were explicitly provided.
		/// </summary>
		public IList<IType> TypeArguments {
			get { return typeArguments; }
		}
		
		/// <summary>
		/// List of extension methods, used to avoid re-calculating it in ResolveInvocation() when it was already
		/// calculated by ResolveMemberAccess().
		/// </summary>
		internal List<List<IMethod>> extensionMethods;
		
		// Resolver+UsingScope are used to fetch extension methods on demand
		internal CSharpResolver resolver;
		internal UsingScope usingScope;
		
		/// <summary>
		/// Gets all candidate extension methods.
		/// </summary>
		public List<List<IMethod>> GetExtensionMethods()
		{
			if (resolver != null) {
				Debug.Assert(extensionMethods == null);
				UsingScope oldUsingScope = resolver.UsingScope;
				try {
					resolver.UsingScope = usingScope;
					extensionMethods = resolver.GetExtensionMethods(this.TargetType, methodName, typeArguments);
				} finally {
					resolver.UsingScope = oldUsingScope;
					resolver = null;
					usingScope = null;
				}
			}
			return extensionMethods;
		}
		
		public override string ToString()
		{
			return string.Format("[{0} with {1} method(s)]", GetType().Name, methods.Count);
		}
		
		public OverloadResolution PerformOverloadResolution(ITypeResolveContext context, ResolveResult[] arguments, string[] argumentNames = null, bool allowExtensionMethods = true, bool allowExpandingParams = true)
		{
			Log.WriteLine("Performing overload resolution for " + this);
			Log.WriteCollection("  Arguments: ", arguments);
			
			var typeArgumentArray = this.TypeArguments.ToArray();
			OverloadResolution or = new OverloadResolution(context, arguments, argumentNames, typeArgumentArray);
			or.AllowExpandingParams = allowExpandingParams;
			foreach (IMethod method in this.Methods) {
				// TODO: grouping by class definition?
				
				Log.Indent();
				OverloadResolutionErrors errors = or.AddCandidate(method);
				Log.Unindent();
				LogOverloadResolution("  Candidate", method, errors, or);
			}
			if (allowExtensionMethods && !or.FoundApplicableCandidate) {
				// No applicable match found, so let's try extension methods.
				
				var extensionMethods = this.GetExtensionMethods();
				
				if (extensionMethods.Count > 0) {
					Log.WriteLine("No candidate is applicable, trying {0} extension methods groups...", extensionMethods.Count);
					ResolveResult[] extArguments = new ResolveResult[arguments.Length + 1];
					extArguments[0] = new ResolveResult(this.TargetType);
					arguments.CopyTo(extArguments, 1);
					string[] extArgumentNames = null;
					if (argumentNames != null) {
						extArgumentNames = new string[argumentNames.Length + 1];
						argumentNames.CopyTo(extArgumentNames, 1);
					}
					var extOr = new OverloadResolution(context, extArguments, extArgumentNames, typeArgumentArray);
					extOr.AllowExpandingParams = allowExpandingParams;
					extOr.IsExtensionMethodInvocation = true;
					
					foreach (var g in extensionMethods) {
						foreach (var method in g) {
							Log.Indent();
							OverloadResolutionErrors errors = extOr.AddCandidate(method);
							Log.Unindent();
							LogOverloadResolution("  Extension", method, errors, or);
						}
						if (extOr.FoundApplicableCandidate)
							break;
					}
					// For the lack of a better comparison function (the one within OverloadResolution
					// cannot be used as it depends on the argument set):
					if (extOr.FoundApplicableCandidate || or.BestCandidate == null) {
						// Consider an extension method result better than the normal result only
						// if it's applicable; or if there is no normal result.
						or = extOr;
					}
				}
			}
			Log.WriteLine("Overload resolution finished, best candidate is {0}.", or.GetBestCandidateWithSubstitutedTypeArguments());
			return or;
		}
		
		[Conditional("DEBUG")]
		void LogOverloadResolution(string text, IMethod method, OverloadResolutionErrors errors, OverloadResolution or)
		{
			#if DEBUG
			StringBuilder b = new StringBuilder(text);
			b.Append(' ');
			b.Append(method);
			b.Append(" = ");
			if (errors == OverloadResolutionErrors.None)
				b.Append("Success");
			else
				b.Append(errors);
			if (or.BestCandidate == method) {
				b.Append(" (best candidate so far)");
			} else if (or.BestCandidateAmbiguousWith == method) {
				b.Append(" (ambiguous)");
			}
			Log.WriteLine(b.ToString());
			#endif
		}
		
		public override IEnumerable<ResolveResult> GetChildResults()
		{
			if (targetResult != null)
				return new[] { targetResult };
			else
				return Enumerable.Empty<ResolveResult>();
		}
	}
}
