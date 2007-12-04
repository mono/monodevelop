// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 915 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class PrimitiveExpressionTests
	{
		#region C#
		[Test]
		public void CSharpHexIntegerTest1()
		{
			InvocationExpression invExpr = ParseUtilCSharp.ParseExpression<InvocationExpression>("0xAFFE.ToString()");
			Assert.AreEqual(0, invExpr.Arguments.Count);
			Assert.IsTrue(invExpr.TargetObject is FieldReferenceExpression);
			FieldReferenceExpression fre = invExpr.TargetObject as FieldReferenceExpression;
			Assert.AreEqual("ToString", fre.FieldName);
			
			Assert.IsTrue(fre.TargetObject is PrimitiveExpression);
			PrimitiveExpression pe = fre.TargetObject as PrimitiveExpression;
			
			Assert.AreEqual("0xAFFE", pe.StringValue);
			Assert.AreEqual(0xAFFE, (int)pe.Value);
			
		}
		
		[Test]
		public void CSharpDoubleTest1()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>(".5e-06;");
			Assert.AreEqual(".5e-06", pe.StringValue);
			Assert.AreEqual(.5e-06, (double)pe.Value);
		}
		
		[Test]
		public void CSharpCharTest1()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("'\\u0356';");
			Assert.AreEqual("'\\u0356'", pe.StringValue);
			Assert.AreEqual('\u0356', (char)pe.Value);
		}
		
		[Test]
		public void CSharpStringTest1()
		{
			PrimitiveExpression pe = ParseUtilCSharp.ParseExpression<PrimitiveExpression>("\"\\n\\t\\u0005 Hello World !!!\";");
			Assert.AreEqual("\"\\n\\t\\u0005 Hello World !!!\"", pe.StringValue);
			Assert.AreEqual("\n\t\u0005 Hello World !!!", (string)pe.Value);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void PrimitiveExpression1Test()
		{
			InvocationExpression ie = ParseUtilVBNet.ParseExpression<InvocationExpression>("546.ToString()");
		}
		#endregion
	}
}
