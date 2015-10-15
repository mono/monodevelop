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

namespace ICSharpCode.NRefactory6.CSharp
{
	static class Resources 
	{
		public static string GenerateConstructor = "Generate constructor '{0}({1})'";
		public static string GenerateFieldAssigningConstructor = "Generate field assigning constructor '{0}({1})'";
		public static string GenerateDelegatingConstructor = "Generate delegating constructor '{0}({1})'";

		public static string GenerateEnumMemberIn = "Generate enum member '{0}' in '{1}'";
		public static string GenerateNewConstructorIn = "Generate constructor in '{0}'";

		public static string GenerateAbstractMethod = "Generate abstract method '{0}' in '{1}'";
		public static string GenerateAbstractProperty = "Generate abstract property '{0}' in '{1}&apos";
		public static string GeneratePropertyIn = "Generate property '{1}.{0}'";
		public static string GenerateMethodIn = "Generate method '{1}.{0}'";

		public static string GenerateAll = "Generate all";

		public static string GenerateConstantIn = "Generate constant '{0}' in '{1}'";
		public static string GenerateReadonlyProperty = "Generate read-only property '{1}.{0}'";
		public static string GenerateReadonlyField = "Generate read-only field '{1}.{0}'";
		public static string GenerateFieldIn = "Generate field '{0}' in '{1}'";
		public static string GenerateLocal = "Generate local '{0}'";

		public static string ImplementInterface = "Implement interface";
		public static string ImplementInterfaceAbstractly = "Implement interface abstractly";
		public static string ImplementInterfaceExplicitly = "Implement interface explicitly";
		public static string ImplementInterfaceExplicitlyWithDisposePattern = "Implement interface explicitly with Dispose pattern";
		public static string ImplementInterfaceThrough = "Implement interface through '{0}&apos";
		public static string ImplementInterfaceWithDisposePattern = "Implement interface with Dispose pattern";

		public static string ImplicitConversionDisplayText = "Generate implicit conversion operator in '{0}'";
		public static string ExplicitConversionDisplayText = "Generate explicit conversion operator in '{0}'";

		public static string GenerateForIn = "Generate {0} for '{1}' in '{2}'";
		public static string GenerateForInNewFile = "Generate {0} for '{1}' in '{2}' (new file)";
		public static string GlobalNamespace = "Global Namespace";
		public static string GenerateNewType = "Generate new type...";
		public static string ToDetectRedundantCalls = "To detect redundant calls";

		public static string DisposeManagedStateTodo = "TODO: dispose managed state (managed objects).";
		public static string FreeUnmanagedResourcesTodo = "TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.";
		public static string SetLargeFieldsToNullTodo = "TODO: set large fields to null.";
		public static string OverrideAFinalizerTodo = "TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.";
		public static string DoNotChangeThisCodeUseDispose = "Do not change this code. Put cleanup code in Dispose(bool disposing) above.";
		public static string ThisCodeAddedToCorrectlyImplementDisposable = "This code added to correctly implement the disposable pattern.";
		public static string UncommentTheFollowingIfFinalizerOverriddenTodo = "TODO: uncomment the following line if the finalizer is overridden above.";


		public static string IntroduceConstantFor = "Introduce constant for '{0}'";
		public static string IntroduceConstantForAllOccurrences = "Introduce constant for all occurrences of '{0}'";
		public static string IntroduceFieldFor = "Introduce field for '{0}'";
		public static string IntroduceFieldForAllOccurrences = "Introduce field for all occurrences of '{0}'";
		public static string IntroduceLocalConstantFor = "Introduce local constant for '{0}'";
		public static string IntroduceLocalConstantForAll = "Introduce local constant for all occurrences of '{0}'";
		public static string IntroduceLocalFor = "Introduce local for '{0}'";
		public static string IntroduceLocalForAllOccurrences = "Introduce local for all occurrences of '{0}'";
		public static string IntroduceQueryVariableFor = "Introduce query variable for '{0}'";
		public static string IntroduceQueryVariableForAll = "Introduce query variable for all occurrences of '{0}'";

		public static string OrganizeUsingsWithAccelerator = "_Organize Usings";
		public static string RemoveAndSortUsingsWithAccelerator = "Remove _and Sort Usings";
		public static string SortUsingsWithAccelerator = "_Sort Usings";
		public static string RemoveUnnecessaryUsingsWithAccelerator = "_Remove Unnecessary Usings";
	}
}

