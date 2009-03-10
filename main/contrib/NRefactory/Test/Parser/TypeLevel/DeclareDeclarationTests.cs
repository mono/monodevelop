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
	public class DeclareDeclarationTests
	{
		#region C#
		// No C# representation
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetDeclareDeclarationTest()
		{
			string program = "Declare Ansi Function GetUserName Lib \"advapi32.dll\" Alias \"GetUserNameA\" (ByVal lpBuffer As String, ByRef nSize As Integer) As Integer\n";
			DeclareDeclaration dd = ParseUtilVBNet.ParseTypeMember<DeclareDeclaration>(program);
			Assert.AreEqual("System.Int32", dd.TypeReference.Type);
			Assert.AreEqual("GetUserName", dd.Name);
			Assert.AreEqual("advapi32.dll", dd.Library);
			Assert.AreEqual("GetUserNameA", dd.Alias);
			Assert.AreEqual(CharsetModifier.Ansi, dd.Charset);
		}
		#endregion
		
	}
}
