// ResolveVisitor.cs created with MonoDevelop
// User: mkrueger at 15:56 24.06.2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharpBinding
{
	internal class ResolveVisitor : AbstractAstVisitor
	{
		NRefactoryResolver resolver;
		
		public ResolveVisitor (NRefactoryResolver resolver)
		{
			this.resolver = resolver;
		}
		
		public ResolveResult Resolve (Expression expression)
		{
			ResolveResult result = (ResolveResult)expression.AcceptVisitor (this, null);
			if (result == null)
				result = CreateResult (null);
			return result;
		}
		
		ResolveResult CreateResult (string fullTypeName)
		{
			ResolveResult result = new ResolveResult ();
			result.CallingType   = resolver.CallingType;
			result.CallingMember = resolver.CallingMember;
			if (!String.IsNullOrEmpty (fullTypeName))
				result.ResolvedType  = new DomReturnType (fullTypeName);
			return result;
		}
		
		public override object VisitSizeOfExpression(SizeOfExpression sizeOfExpression, object data)
		{
			return CreateResult (typeof (System.Int32).FullName);
		}
		
		public override object VisitTypeOfExpression(TypeOfExpression typeOfExpression, object data)
		{
			return CreateResult (typeof (System.Type).FullName);
		}
		
		public override object VisitTypeOfIsExpression(TypeOfIsExpression typeOfIsExpression, object data)
		{
			return CreateResult (typeof (System.Boolean).FullName);
		}

		
	}
}
