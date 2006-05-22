// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class DefaultValueExpressionTests
	{
		[Test]
		public void CSharpSimpleDefaultValue()
		{
			DefaultValueExpression toe = ParseUtilCSharp.ParseExpression<DefaultValueExpression>("default(T)");
			Assert.AreEqual("T", toe.TypeReference.Type);
		}
		
		[Test]
		public void CSharpFullQualifiedDefaultValue()
		{
			DefaultValueExpression toe = ParseUtilCSharp.ParseExpression<DefaultValueExpression>("default(MyNamespace.N1.MyType)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
		}
		
		[Test]
		public void CSharpGenericDefaultValue()
		{
			DefaultValueExpression toe = ParseUtilCSharp.ParseExpression<DefaultValueExpression>("default(MyNamespace.N1.MyType<string>)");
			Assert.AreEqual("MyNamespace.N1.MyType", toe.TypeReference.Type);
			Assert.AreEqual("string", toe.TypeReference.GenericTypes[0].Type);
		}
		
		[Test]
		public void CSharpDefaultValueAsIntializer()
		{
			// This test is failing because we need a resolver for the "default:" / "default(" conflict.
			LocalVariableDeclaration lvd = ParseUtilCSharp.ParseStatement<LocalVariableDeclaration>("T a = default(T);");
			DefaultValueExpression dve = (DefaultValueExpression)lvd.Variables[0].Initializer;
			Assert.AreEqual("T", dve.TypeReference.Type);
		}
	}
}
