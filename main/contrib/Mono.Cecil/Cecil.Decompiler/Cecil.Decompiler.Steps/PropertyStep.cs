#region license
//
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;

using Mono.Cecil;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Steps {

	class PropertyStep : BaseCodeTransformer, IDecompilationStep {

		public static readonly IDecompilationStep Instance = new PropertyStep ();

		public override ICodeNode VisitMethodInvocationExpression (MethodInvocationExpression node)
		{
			var method_ref = node.Method as MethodReferenceExpression;
			if (method_ref == null)
				goto skip;

			//var method = method_ref.Method.Resolve ();
			var method = method_ref.Method as MethodDefinition;
			if (method == null)
				goto skip;

			if (method.IsGetter)
				return ProcessGetter (method_ref, method);
			if (method.IsSetter)
				return ProcessSetter (node, method_ref, method);

		skip:
			return base.VisitMethodInvocationExpression (node);
		}

		static PropertyReferenceExpression ProcessGetter (MethodReferenceExpression method_ref, MethodDefinition method)
		{
			return CreatePropertyReferenceFromMethod (method_ref, method);
		}

		static AssignExpression ProcessSetter (MethodInvocationExpression invoke, MethodReferenceExpression method_ref, MethodDefinition method)
		{
			return new AssignExpression (
				CreatePropertyReferenceFromMethod (method_ref, method),
				invoke.Arguments [0]);
		}

		static PropertyReferenceExpression CreatePropertyReferenceFromMethod (MethodReferenceExpression method_ref, MethodDefinition method)
		{
			return new PropertyReferenceExpression (method_ref.Target, GetProperty (method));
		}

		static PropertyDefinition GetProperty (MethodDefinition accessor)
		{
			return GetProperty (accessor.DeclaringType, accessor);
		}

		static PropertyDefinition GetProperty (TypeDefinition type, MethodDefinition accessor)
		{
			foreach (PropertyDefinition property in type.Properties) {
				if (property.GetMethod == accessor)
					return property;
				if (property.SetMethod == accessor)
					return property;
			}

			return null;
		}

		public BlockStatement Process (DecompilationContext context, BlockStatement body)
		{
			return (BlockStatement) VisitBlockStatement (body);
		}
	}
}
