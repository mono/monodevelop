//
// ConflictResolver.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using System;
using Microsoft.CodeAnalysis.Options;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp
{
	class ConflictResolution 
	{
		static Type typeInfo;
		internal object instance;

		static ConflictResolution ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Rename.ConflictEngine.ConflictResolution" + ReflectionNamespaces.WorkspacesAsmName, true);

		}

		public ConflictResolution (object instance)
		{
			this.instance = instance;
		}

	}

	class RenameLocations
	{
		static Type typeInfo;
		internal object instance;

		static RenameLocations ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Rename.RenameLocations" + ReflectionNamespaces.WorkspacesAsmName, true);

		}

		public RenameLocations (object instance)
		{
			this.instance = instance;
		}

	}


	class ConflictResolver
	{
		static MethodInfo resolveConflictsAsyncMethod;
		static Type typeInfo;

		static ConflictResolver ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Rename.ConflictEngine.ConflictResolver" + ReflectionNamespaces.WorkspacesAsmName, true);
			resolveConflictsAsyncMethod = typeInfo.GetMethod ("ResolveConflictsAsync");

		}

		public static Task<ConflictResolution> ResolveConflictsAsync(
			RenameLocations renameLocationSet,
			string originalText,
			string replacementText,
			OptionSet optionSet,
			Func<IEnumerable<ISymbol>, bool?> hasConflict,
			CancellationToken cancellationToken)
		{
			try {
				var task = resolveConflictsAsyncMethod.Invoke (null, new object [] { renameLocationSet.instance, originalText, replacementText, optionSet, hasConflict, cancellationToken });
				var propertyInfo = task.GetType ().GetProperty ("Result");
				return Task.FromResult (new ConflictResolution (propertyInfo.GetValue (task)));
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}
	}
}

