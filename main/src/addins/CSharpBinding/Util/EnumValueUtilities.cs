//
// EnumValueUtilities.cs
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
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class EnumValueUtilities
	{
		readonly static Type typeInfo;

		static EnumValueUtilities ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.Shared.Utilities.EnumValueUtilities" + ReflectionNamespaces.WorkspacesAsmName, true);
			getNextEnumValueMethod = typeInfo.GetMethod ("GetNextEnumValue", BindingFlags.Static | BindingFlags.Public);
		}

		readonly static MethodInfo getNextEnumValueMethod;

		public static object GetNextEnumValue(INamedTypeSymbol enumType, CancellationToken cancellationToken)
		{
			try {
				return getNextEnumValueMethod.Invoke (null, new object[] { enumType, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}
	}
}

