// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1609 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory.Tests.Ast
{
	[TestFixture]
	public class TryCatchStatementTests
	{
		#region C#
		[Test]
		public void CSharpSimpleTryCatchStatementTest()
		{
			TryCatchStatement tryCatchStatement = ParseUtilCSharp.ParseStatement<TryCatchStatement>("try { } catch { } ");
			// TODO : Extend test.
		}
		
		[Test]
		public void CSharpSimpleTryCatchStatementTest2()
		{
			TryCatchStatement tryCatchStatement = ParseUtilCSharp.ParseStatement<TryCatchStatement>("try { } catch (Exception e) { } ");
			// TODO : Extend test.
		}
		
		[Test]
		public void CSharpSimpleTryCatchFinallyStatementTest()
		{
			TryCatchStatement tryCatchStatement = ParseUtilCSharp.ParseStatement<TryCatchStatement>("try { } catch (Exception) { } finally { } ");
			// TODO : Extend test.
		}
		#endregion
		
		#region VB.NET
			// TODO
		#endregion 
	}
}
