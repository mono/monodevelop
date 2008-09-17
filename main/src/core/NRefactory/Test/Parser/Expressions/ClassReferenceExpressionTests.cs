// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 2676 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class ClassReferenceExpressionTests
	{
		#region C#
			// No C# representation
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetClassReferenceExpressionTest1()
		{
			MemberReferenceExpression fre = ParseUtilVBNet.ParseExpression<MemberReferenceExpression>("MyClass.myField");
			Assert.IsTrue(fre.TargetObject is ClassReferenceExpression);
		}
		#endregion
	}
}
