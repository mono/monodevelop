// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 3660 $</version>
// </file>

using System;
using ICSharpCode.NRefactory.Ast;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class OperatorDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpImplictOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static implicit operator double(MyObject f)  { return 0.5d; }");
			Assert.IsTrue(od.IsConversionOperator);
			Assert.AreEqual(1, od.Parameters.Count);
			Assert.AreEqual(ConversionType.Implicit, od.ConversionType);
			Assert.AreEqual("System.Double", od.TypeReference.Type);
		}
		
		[Test]
		public void CSharpExplicitOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static explicit operator double(MyObject f)  { return 0.5d; }");
			Assert.IsTrue(od.IsConversionOperator);
			Assert.AreEqual(1, od.Parameters.Count);
			Assert.AreEqual(ConversionType.Explicit, od.ConversionType);
			Assert.AreEqual("System.Double", od.TypeReference.Type);
		}
		
		[Test]
		public void CSharpPlusOperatorDeclarationTest()
		{
			OperatorDeclaration od = ParseUtilCSharp.ParseTypeMember<OperatorDeclaration>("public static MyObject operator +(MyObject a, MyObject b)  {}");
			Assert.IsTrue(!od.IsConversionOperator);
			Assert.AreEqual(2, od.Parameters.Count);
			Assert.AreEqual("MyObject", od.TypeReference.Type);
		}
		#endregion
		
		#region VB.NET
		
		[Test]
		public void VBNetImplictOperatorDeclarationTest()
		{
			string programm = @"Public Shared Operator + (ByVal v As Complex) As Complex
					Return v
				End Operator";
			
			OperatorDeclaration od = ParseUtilVBNet.ParseTypeMember<OperatorDeclaration>(programm);
			Assert.IsFalse(od.IsConversionOperator);
			Assert.AreEqual(1, od.Parameters.Count);
			Assert.AreEqual(ConversionType.None, od.ConversionType);
			Assert.AreEqual("Complex", od.TypeReference.Type);
		}
		#endregion 
	}
}
