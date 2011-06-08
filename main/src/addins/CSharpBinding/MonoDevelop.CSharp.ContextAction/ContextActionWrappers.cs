// 
// ContextActionWrappers.cs
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
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.ContextAction
{
	public abstract class ContextActionWrapper<T> : MDRefactoringContextAction where T : ICSharpCode.NRefactory.CSharp.Refactoring.IContextAction, new ()
	{
		protected T action = new T ();
		
		protected override bool IsValid (MDRefactoringContext context)
		{
			return action.IsValid (context);
		}
		
		protected override void Run (MDRefactoringContext context)
		{
			action.Run (context);
		}
	}
	
	public class IntroduceFormatItem : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.IntroduceFormatItem>
	{
	}
	
	public class AddAnotherAccessor : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.AddAnotherAccessor>
	{
	}
	
	public class CheckIfParameterIsNull : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.CheckIfParameterIsNull>
	{
	}
	
	public class ConvertDecToHex : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.ConvertDecToHex>
	{
	}
	
	public class ConvertHexToDec : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.ConvertHexToDec>
	{
	}
	
	public class ConvertForeachToFor : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.ConvertForeachToFor>
	{
	}
	
	public class GenerateSwitchLabels : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.GenerateSwitchLabels>
	{
	}
	
	public class FlipOperatorArguments : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.FlipOperatorArguments>
	{
		protected override string GetMenuText (MDRefactoringContext context)
		{
			var binop = ICSharpCode.NRefactory.CSharp.Refactoring.FlipOperatorArguments.GetBinaryOperatorExpression (context);
			string op;
			switch (binop.Operator) {
			case ICSharpCode.NRefactory.CSharp.BinaryOperatorType.Equality:
				op = "==";
				break;
			case ICSharpCode.NRefactory.CSharp.BinaryOperatorType.InEquality:
				op = "!=";
				break;
			default:
				throw new InvalidOperationException ();
			}
			return string.Format (GettextCatalog.GetString ("Flip '{0}' operator arguments"), op);
		}
	}
	
	public class InsertAnonymousMethodSignature : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.InsertAnonymousMethodSignature>
	{
	}
	
	public class RemoveBraces : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.RemoveBraces>
	{
	}
	
	public class SplitString : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.SplitString>
	{
	}
	
	public class ReplaceEmptyString : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.ReplaceEmptyString>
	{
	}
	
	public class UseVarKeyword : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.UseVarKeyword>
	{
	}
	
	public class UseExplicitType : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.UseExplicitType>
	{
	}
	
	public class InvertIf : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.InvertIf>
	{
	}
	
	public class CreateBackingStore : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.CreateBackingStore>
	{
	}
	
	public class SplitDeclarationAndAssignment : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.SplitDeclarationAndAssignment>
	{
	}
	
	public class CreateField : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.CreateField>
	{
		protected override string GetMenuText (MDRefactoringContext context)
		{
			var identifier = ICSharpCode.NRefactory.CSharp.Refactoring.CreateField.GetIdentifier (context);
			return string.Format (GettextCatalog.GetString ("Create field '{0}'"), identifier);
		}
	}
	
	public class CreateProperty : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.CreateProperty>
	{
		protected override string GetMenuText (MDRefactoringContext context)
		{
			var identifier = ICSharpCode.NRefactory.CSharp.Refactoring.CreateField.GetIdentifier (context);
			return string.Format (GettextCatalog.GetString ("Create property '{0}'"), identifier);
		}
	}
	
	public class CreateLocalVariable : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.CreateLocalVariable>
	{
		protected override string GetMenuText (MDRefactoringContext context)
		{
			if (action.GetUnresolvedArguments (context).Count > 0)
				return GettextCatalog.GetString ("Create local variable declarations for arguments");
			
			var identifier = ICSharpCode.NRefactory.CSharp.Refactoring.CreateField.GetIdentifier (context);
			return string.Format (GettextCatalog.GetString ("Create local variable '{0}'"), identifier);
		}
	}
	
	public class GenerateGetter : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.GenerateGetter>
	{
	}
	
	public class CreateEventInvocator : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.CreateEventInvocator>
	{
	}
	
	public class RemoveBackingStore : ContextActionWrapper<ICSharpCode.NRefactory.CSharp.Refactoring.RemoveBackingStore>
	{
	}
}

