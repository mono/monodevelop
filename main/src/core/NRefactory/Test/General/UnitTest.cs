// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version>$Revision: 1076 $</version>
// </file>

using System;
using System.Reflection;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser.AST;
using ICSharpCode.NRefactory.Parser;

namespace ICSharpCode.NRefactory.Tests
{
	[TestFixture]
	public class StructuralTest
	{
		[Test]
		public void TestToStringMethods()
		{
			Type[] allTypes = typeof(INode).Assembly.GetTypes();
			
			foreach (Type type in allTypes) {
				if (type.IsClass && !type.IsAbstract && type.GetInterface(typeof(INode).FullName) != null) {
					MethodInfo methodInfo = type.GetMethod("ToString", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
					Assert.IsNotNull(methodInfo, "ToString() not found in " + type.FullName);
				}
			}
		}
		
//		[Test]
//		public void TestAcceptVisitorMethods()
//		{
//			Type[] allTypes = typeof(AbstractNode).Assembly.GetTypes();
//			
//			foreach (Type type in allTypes) {
//				if (type.IsClass && !type.IsAbstract && type.GetInterface(typeof(INode).FullName) != null) {
//					MethodInfo methodInfo = type.GetMethod("AcceptVisitor", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public);
//					Assertion.AssertNotNull("AcceptVisitor() not found in " + type.FullName, methodInfo);
//				}
//			}
//		}
		
		[Test]
		public void TestIASTVisitor()
		{
			Type[] allTypes = typeof(AbstractNode).Assembly.GetTypes();
			Type visitor = typeof(IAstVisitor);
			
			foreach (Type type in allTypes) {
				if (type.IsClass && !type.IsAbstract && type.GetInterface(typeof(INode).FullName) != null && !type.Name.StartsWith("Null")) {
					MethodInfo methodInfo = visitor.GetMethod("Visit", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] {type, typeof(object)}, null);
					Assert.IsNotNull(methodInfo, "Visit with parameter " + type.FullName + " not found");
					Assert.AreEqual(2, methodInfo.GetParameters().Length);
					ParameterInfo first = methodInfo.GetParameters()[0];
					Assert.AreEqual(Char.ToLower(first.ParameterType.Name[0]) + first.ParameterType.Name.Substring(1), first.Name);
					
					ParameterInfo second = methodInfo.GetParameters()[1];
					Assert.AreEqual(typeof(System.Object), second.ParameterType);
					Assert.AreEqual("data", second.Name);
				}
			}
		}
		
		[Test]
		public void TestAbstractASTVisitorVisitor()
		{
			Type[] allTypes = typeof(AbstractNode).Assembly.GetTypes();
			Type visitor = typeof(AbstractAstVisitor);
			
			foreach (Type type in allTypes) {
				if (type.IsClass && !type.IsAbstract && type.GetInterface(typeof(INode).FullName) != null && !type.Name.StartsWith("Null")) {
					MethodInfo methodInfo = visitor.GetMethod("Visit", BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.ExactBinding, null, new Type[] {type, typeof(object)}, null);
					Assert.IsNotNull(methodInfo, "Visit with parameter " + type.FullName + " not found");
					
					Assert.AreEqual(2, methodInfo.GetParameters().Length);
					ParameterInfo first = methodInfo.GetParameters()[0];
					Assert.AreEqual(Char.ToLower(first.ParameterType.Name[0]) + first.ParameterType.Name.Substring(1), first.Name);
					
					ParameterInfo second = methodInfo.GetParameters()[1];
					Assert.AreEqual(typeof(System.Object), second.ParameterType);
					Assert.AreEqual("data", second.Name);
				}
			}
		}
	}
}
