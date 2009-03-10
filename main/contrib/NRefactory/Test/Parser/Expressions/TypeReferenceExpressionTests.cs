// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 3715 $</version>
// </file>

/*
 * Created by SharpDevelop.
 * User: Omnibrain
 * Date: 13.09.2004
 * Time: 19:54
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class TypeReferenceExpressionTests
	{
		#region C#
		[Test]
		public void GlobalTypeReferenceExpression()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::System");
			Assert.AreEqual("System", tr.TypeReference.Type);
			Assert.IsTrue(tr.TypeReference.IsGlobal);
		}
		
		[Test]
		public void GlobalTypeReferenceExpressionWithoutTypeName()
		{
			TypeReferenceExpression tr = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("global::", true);
			Assert.AreEqual("?", tr.TypeReference.Type);
			Assert.IsTrue(tr.TypeReference.IsGlobal);
		}
		
		[Test]
		public void IntReferenceExpression()
		{
			MemberReferenceExpression fre = ParseUtilCSharp.ParseExpression<MemberReferenceExpression>("int.MaxValue");
			Assert.AreEqual("MaxValue", fre.MemberName);
			Assert.AreEqual("System.Int32", ((TypeReferenceExpression)fre.TargetObject).TypeReference.Type);
		}
		
		[Test]
		public void StandaloneIntReferenceExpression()
		{
			TypeReferenceExpression tre = ParseUtilCSharp.ParseExpression<TypeReferenceExpression>("int");
			Assert.AreEqual("System.Int32", tre.TypeReference.Type);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBIntReferenceExpression()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("inTeGer.MaxValue");
			Assert.AreEqual("MaxValue", fre.MemberName);
			Assert.AreEqual("System.Int32", ((TypeReferenceExpression)fre.TargetObject).TypeReference.Type);
		}
		
		[Test]
		public void VBStandaloneIntReferenceExpression()
		{
			TypeReferenceExpression tre = ParseUtilVBNet.ParseExpression<TypeReferenceExpression>("inTeGer");
			Assert.AreEqual("System.Int32", tre.TypeReference.Type);
		}
		
		[Test]
		public void VBObjectReferenceExpression()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("Object.ReferenceEquals");
			Assert.AreEqual("ReferenceEquals", fre.MemberName);
			Assert.AreEqual("System.Object", ((TypeReferenceExpression)fre.TargetObject).TypeReference.Type);
		}
		
		[Test]
		public void VBStandaloneObjectReferenceExpression()
		{
			TypeReferenceExpression tre = ParseUtilVBNet.ParseExpression<TypeReferenceExpression>("obJect");
			Assert.AreEqual("System.Object", tre.TypeReference.Type);
		}
		#endregion
	}
}
