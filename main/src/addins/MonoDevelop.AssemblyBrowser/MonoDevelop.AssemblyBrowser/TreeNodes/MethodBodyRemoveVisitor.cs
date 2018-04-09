// 
// MethodBodyRemoveVisitor.cs
//  
// Author:
//       Kirill Osenkov <https://github.com/KirillOsenkov>
// 
// Copyright (c) 2018 Microsoft
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

using ICSharpCode.Decompiler.CSharp.Syntax;

namespace MonoDevelop.AssemblyBrowser
{
	class MethodBodyRemoveVisitor : DepthFirstAstVisitor
	{
		public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
		{
			methodDeclaration.Body = null;
		}

		public override void VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration)
		{
			constructorDeclaration.Body = null;
		}

		public override void VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration)
		{
			destructorDeclaration.Body = null;
		}

		public override void VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration)
		{
			operatorDeclaration.Body = null;
		}

		public override void VisitAccessor (Accessor accessor)
		{
			accessor.Body = null;
		}

		internal static void RemoveMethodBodies (SyntaxTree syntaxTree)
		{
			syntaxTree.AcceptVisitor (new MethodBodyRemoveVisitor ());
		}
	}
}
