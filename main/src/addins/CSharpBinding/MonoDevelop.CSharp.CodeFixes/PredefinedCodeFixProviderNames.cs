//
// PredefinedCodeFixProviderNames.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.CodeFixes
{
	static class PredefinedCodeFixProviderNames
	{
		public const string AddAwait = "Add Await For Expression";
		public const string AddAsync = "Add Async To Member";
		public const string ChangeReturnType = "Change Return Type";
		public const string ChangeToYield = "Change To Yield";
		public const string ConvertToAsync = "Convert To Async";
		public const string ConvertToIterator = "Convert To Iterator";
		public const string CorrectNextControlVariable = "Correct Next Control Variable";
		public const string AddMissingReference = "Add Missing Reference";
		public const string AddUsingOrImport = "Add Using or Import";
		public const string FullyQualify = "Fully Qualify";
		public const string FixIncorrectFunctionReturnType = "Fix Incorrect Function Return Type";
		public const string FixIncorrectExitContinue = "Fix Incorrect Exit Continue";
		public const string GenerateConstructor = "Generate Constructor";
		public const string GenerateEndConstruct = "Generate End Construct";
		public const string GenerateEnumMember = "Generate Enum Member";
		public const string GenerateEvent = "Generate Event";
		public const string GenerateVariable = "Generate Variable";
		public const string GenerateMethod = "Generate Method";
		public const string GenerateConversion = "Generate Conversion";
		public const string GenerateType = "Generate Type";
		public const string ImplementAbstractClass = "Implement Abstract Class";
		public const string ImplementInterface = "Implement Interface";
		public const string InsertMissingCast = "InsertMissingCast";
		public const string MoveToTopOfFile = "Move To Top Of File";
		public const string RemoveUnnecessaryCast = "Remove Unnecessary Casts";
		public const string RemoveUnnecessaryImports = "Remove Unnecessary Usings or Imports";
		public const string RenameTracking = "Rename Tracking";
		public const string SimplifyNames = "Simplify Names";
		public const string SpellCheck = "Spell Check";
		public const string Suppression = "Suppression";
	}
}

