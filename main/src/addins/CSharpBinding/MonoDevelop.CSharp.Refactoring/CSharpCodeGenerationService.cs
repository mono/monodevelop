//
// CSharpCodeGenerationService.cs
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Linq;
using Atk;
using Gdk;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpCodeGenerationService : DefaultCodeGenerationService
	{
		public override EntityDeclaration GenerateMemberImplementation (RefactoringContext context, IMember member, bool explicitImplementation)
		{
			var result = base.GenerateMemberImplementation (context, member, explicitImplementation);
			if (CSharpCodeGenerator.IsMonoTouchModelMember (member)) {
				var m = result as MethodDeclaration;
				if (m != null) {
					for (int i = CSharpCodeGenerator.MonoTouchComments.Length - 1; i >= 0; i--) {
						m.Body.InsertChildBefore (m.Body.Statements.First (), new Comment (CSharpCodeGenerator.MonoTouchComments [i]), Roles.Comment);
					}
				}

				var p = result as PropertyDeclaration;
				if (p != null) {
					for (int i = CSharpCodeGenerator.MonoTouchComments.Length - 1; i >= 0; i--) {
						if (!p.Getter.IsNull)
							p.Getter.Body.InsertChildBefore (p.Getter.Body.Statements.First (), new Comment (CSharpCodeGenerator.MonoTouchComments [i]), Roles.Comment);
						if (!p.Setter.IsNull)
							p.Setter.Body.InsertChildBefore (p.Setter.Body.Statements.First (), new Comment (CSharpCodeGenerator.MonoTouchComments [i]), Roles.Comment);
					}
				}
			}
			return result;
		}
	}
}
