//
// Resources.cs
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
using MonoDevelop.Core;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class Resources
	{
		public static string GenerateConstructor = GettextCatalog.GetString ("Generate constructor '{0}({1})'");
		public static string GenerateFieldAssigningConstructor = GettextCatalog.GetString ("Generate field assigning constructor '{0}({1})'");
		public static string GenerateDelegatingConstructor = GettextCatalog.GetString ("Generate delegating constructor '{0}({1})'");

		public static string GenerateEnumMemberIn = GettextCatalog.GetString ("Generate enum member '{0}' in '{1}'");
		public static string GenerateNewConstructorIn = GettextCatalog.GetString ("Generate constructor in '{0}'");

		public static string GenerateAbstractMethod = GettextCatalog.GetString ("Generate abstract method '{0}' in '{1}'");
		public static string GenerateAbstractProperty = GettextCatalog.GetString ("Generate abstract property '{0}' in '{1}'");
		public static string GeneratePropertyIn = GettextCatalog.GetString ("Generate property '{1}.{0}'");
		public static string GenerateMethodIn = GettextCatalog.GetString ("Generate method '{1}.{0}'");

		public static string GenerateAll = GettextCatalog.GetString ("Generate all");

		public static string GenerateConstantIn = GettextCatalog.GetString ("Generate constant '{0}' in '{1}'");
		public static string GenerateReadonlyProperty = GettextCatalog.GetString ("Generate read-only property '{1}.{0}'");
		public static string GenerateReadonlyField = GettextCatalog.GetString ("Generate read-only field '{1}.{0}'");
		public static string GenerateFieldIn = GettextCatalog.GetString ("Generate field '{0}' in '{1}'");
		public static string GenerateLocal = GettextCatalog.GetString ("Generate local '{0}'");

		public static string ImplementInterface = GettextCatalog.GetString ("Implement interface");
		public static string ImplementInterfaceAbstractly = GettextCatalog.GetString ("Implement interface abstractly");
		public static string ImplementInterfaceExplicitly = GettextCatalog.GetString ("Implement interface explicitly");
		public static string ImplementInterfaceExplicitlyWithDisposePattern = GettextCatalog.GetString ("Implement interface explicitly with Dispose pattern");
		public static string ImplementInterfaceThrough = GettextCatalog.GetString ("Implement interface through '{0}'");
		public static string ImplementInterfaceWithDisposePattern = GettextCatalog.GetString ("Implement interface with Dispose pattern");

		public static string ImplicitConversionDisplayText = GettextCatalog.GetString ("Generate implicit conversion operator in '{0}'");
		public static string ExplicitConversionDisplayText = GettextCatalog.GetString ("Generate explicit conversion operator in '{0}'");

		public static string GenerateForIn = GettextCatalog.GetString ("Generate {0} for '{1}' in '{2}'");
		public static string GenerateForInNewFile = GettextCatalog.GetString ("Generate {0} for '{1}' in '{2}' (new file)");
		public static string GlobalNamespace = GettextCatalog.GetString ("Global Namespace");
		public static string GenerateNewType = GettextCatalog.GetString ("Generate new type...");
		public static string ToDetectRedundantCalls = GettextCatalog.GetString ("To detect redundant calls");

		public static string DisposeManagedStateTodo = "TODO: dispose managed state (managed objects).";
		public static string FreeUnmanagedResourcesTodo = "TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.";
		public static string SetLargeFieldsToNullTodo = "TODO: set large fields to null.";
		public static string OverrideAFinalizerTodo = "TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.";
		public static string DoNotChangeThisCodeUseDispose = "Do not change this code. Put cleanup code in Dispose(bool disposing) above.";
		public static string ThisCodeAddedToCorrectlyImplementDisposable = "This code added to correctly implement the disposable pattern.";
		public static string UncommentTheFollowingIfFinalizerOverriddenTodo = "TODO: uncomment the following line if the finalizer is overridden above.";


		public static string IntroduceConstantFor = GettextCatalog.GetString ("Introduce constant for '{0}'");
		public static string IntroduceConstantForAllOccurrences = GettextCatalog.GetString ("Introduce constant for all occurrences of '{0}'");
		public static string IntroduceFieldFor = GettextCatalog.GetString ("Introduce field for '{0}'");
		public static string IntroduceFieldForAllOccurrences = GettextCatalog.GetString ("Introduce field for all occurrences of '{0}'");
		public static string IntroduceLocalConstantFor = GettextCatalog.GetString ("Introduce local constant for '{0}'");
		public static string IntroduceLocalConstantForAll = GettextCatalog.GetString ("Introduce local constant for all occurrences of '{0}'");
		public static string IntroduceLocalFor = GettextCatalog.GetString ("Introduce local for '{0}'");
		public static string IntroduceLocalForAllOccurrences = GettextCatalog.GetString ("Introduce local for all occurrences of '{0}'");
		public static string IntroduceQueryVariableFor = GettextCatalog.GetString ("Introduce query variable for '{0}'");
		public static string IntroduceQueryVariableForAll = GettextCatalog.GetString ("Introduce query variable for all occurrences of '{0}'");

		public static string OrganizeUsingsWithAccelerator = GettextCatalog.GetString ("_Organize Usings");
		public static string RemoveAndSortUsingsWithAccelerator = GettextCatalog.GetString ("Remove _and Sort Usings");
		public static string SortUsingsWithAccelerator = GettextCatalog.GetString ("_Sort Usings");
		public static string RemoveUnnecessaryUsingsWithAccelerator = GettextCatalog.GetString ("_Remove Unnecessary Usings");
		public static string EncapsulateFieldsUsages = GettextCatalog.GetString ("Encapsulate fields (and use property)");
		public static string EncapsulateFields = GettextCatalog.GetString ("Encapsulate fields (but still use field)");
		public static string EncapsulateFieldUsages = GettextCatalog.GetString ("Encapsulate field: '{0}' (and use property)");
		public static string EncapsulateField = GettextCatalog.GetString ("Encapsulate field: '{0}' (but still use field)");
	}
}

