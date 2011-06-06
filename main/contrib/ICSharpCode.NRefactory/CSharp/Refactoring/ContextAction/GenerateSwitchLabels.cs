// 
// GenerateSwitchLabels.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class GenerateSwitchLabels : IContextAction
	{
		// TODO: Resolver!
		
		public bool IsValid (RefactoringContext context)
		{
			return false;
//			var switchStatement = GetSwitchStatement (context);
//			if (switchStatement == null)
//				return false;
//			var resolver = context.Resolver;
//			var result = resolver.Resolve (switchStatement.Expression.ToString (), new DomLocation (switchStatement.StartLocation.Line, switchStatement.StartLocation.Column));
//			if (result == null || result.ResolvedType == null)
//				return false;
//			var type = context.Document.Dom.GetType (result.ResolvedType);
//			
//			return type != null && type.ClassType == ClassType.Enum;
		}
		
		public void Run (RefactoringContext context)
		{
//			var switchStatement = GetSwitchStatement (context);
//			var resolver = context.Resolver;
//			
//			var result = resolver.Resolve (switchStatement.Expression.ToString (), new DomLocation (switchStatement.StartLocation.Line, switchStatement.StartLocation.Column));
//			var type = context.Document.Dom.GetType (result.ResolvedType);
//			
//			var target = new TypeReferenceExpression (ShortenTypeName (context.Document, result.ResolvedType));
//			foreach (var field in type.Fields) {
//				if (!(field.IsLiteral || field.IsConst))
//					continue;
//				switchStatement.SwitchSections.Add (new SwitchSection () {
//					CaseLabels = {
//						new CaseLabel (new MemberReferenceExpression ( target.Clone (), field.Name))
//					},
//					Statements = {
//						new BreakStatement ()
//					}
//				});
//			}
//			
//			switchStatement.SwitchSections.Add (new SwitchSection () {
//				CaseLabels = {
//					new CaseLabel ()
//				},
//				Statements = {
//					new ThrowStatement (new ObjectCreateExpression (ShortenTypeName (context.Document, "System.ArgumentOutOfRangeException")))
//				}
//			});
//			
//			context.Do (switchStatement.Replace (context.Document, switchStatement));
		}
		
		static SwitchStatement GetSwitchStatement (RefactoringContext context)
		{
			var switchStatment = context.GetNode<SwitchStatement> ();
			if (switchStatment != null && switchStatment.SwitchSections.Count == 0)
				return switchStatment;
			return null;
		}
	}
}

