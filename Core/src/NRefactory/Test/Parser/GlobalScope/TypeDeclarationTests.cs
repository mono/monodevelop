// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision: 1388 $</version>
// </file>

using System;
using System.Drawing;
using System.IO;

using NUnit.Framework;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Tests.AST
{
	[TestFixture]
	public class TypeDeclarationTests
	{
		#region C#
		[Test]
		public void CSharpSimpleClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("class MyClass  : My.Base.Class  { }");
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual("My.Base.Class", td.BaseTypes[0].Type);
			Assert.AreEqual(Modifier.None, td.Modifier);
		}
		
		[Test]
		public void CSharpSimpleClassRegionTest()
		{
			const string program = "class MyClass\n{\n}\n";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(program);
			Assert.AreEqual(1, td.StartLocation.Y, "StartLocation.Y");
			Assert.AreEqual(1, td.StartLocation.X, "StartLocation.X");
			Assert.AreEqual(1, td.BodyStartLocation.Y, "BodyStartLocation.Y");
			Assert.AreEqual(14, td.BodyStartLocation.X, "BodyStartLocation.X");
			Assert.AreEqual(3, td.EndLocation.Y, "EndLocation.Y");
		}
		
		[Test]
		public void CSharpSimplePartialClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("partial class MyClass { }");
			Assert.IsNotNull(td);
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifier.Partial, td.Modifier);
		}
		
		[Test]
		public void CSharpNestedClassesTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("class MyClass { partial class P1 {} public partial class P2 {} static class P3 {} internal static class P4 {} }");
			Assert.IsNotNull(td);
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifier.Partial, ((TypeDeclaration)td.Children[0]).Modifier);
			Assert.AreEqual(Modifier.Partial | Modifier.Public, ((TypeDeclaration)td.Children[1]).Modifier);
			Assert.AreEqual(Modifier.Static, ((TypeDeclaration)td.Children[2]).Modifier);
			Assert.AreEqual(Modifier.Static | Modifier.Internal, ((TypeDeclaration)td.Children[3]).Modifier);
		}
		
		[Test]
		public void CSharpSimpleStaticClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("static class MyClass { }");
			Assert.IsNotNull(td);
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifier.Static, td.Modifier);
		}
		
		[Test]
		public void CSharpGenericClassTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("public class G<T> {}");
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("G", td.Name);
			Assert.AreEqual(Modifier.Public, td.Modifier);
			Assert.AreEqual(0, td.BaseTypes.Count);
			Assert.AreEqual(1, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
		}
		
		
		[Test]
		public void CSharpGenericClassWithWhere()
		{
			string declr = @"
public class Test<T> where T : IMyInterface
{
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("Test", td.Name);
			
			Assert.AreEqual(1, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
			Assert.AreEqual("IMyInterface", td.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void CSharpComplexGenericClassTypeDeclarationTest()
		{
			string declr = @"
public class Generic<T, S> : System.IComparable where S : G<T[]> where  T : MyNamespace.IMyInterface
{
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("Generic", td.Name);
			Assert.AreEqual(Modifier.Public, td.Modifier);
			Assert.AreEqual(1, td.BaseTypes.Count);
			Assert.AreEqual("System.IComparable", td.BaseTypes[0].Type);
			
			Assert.AreEqual(2, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
			Assert.AreEqual("MyNamespace.IMyInterface", td.Templates[0].Bases[0].Type);
			
			Assert.AreEqual("S", td.Templates[1].Name);
			Assert.AreEqual("G", td.Templates[1].Bases[0].Type);
			Assert.AreEqual(1, td.Templates[1].Bases[0].GenericTypes.Count);
			Assert.IsTrue(td.Templates[1].Bases[0].GenericTypes[0].IsArrayType);
			Assert.AreEqual("T", td.Templates[1].Bases[0].GenericTypes[0].Type);
			Assert.AreEqual(new int[] {0}, td.Templates[1].Bases[0].GenericTypes[0].RankSpecifier);
		}
		
		[Test]
		public void CSharpComplexClassTypeDeclarationTest()
		{
			string declr = @"
[MyAttr()]
public abstract class MyClass : MyBase, Interface1, My.Test.Interface2
{
}
";
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("MyClass", td.Name);
			Assert.AreEqual(Modifier.Public | Modifier.Abstract, td.Modifier);
			Assert.AreEqual(1, td.Attributes.Count);
			Assert.AreEqual(3, td.BaseTypes.Count);
			Assert.AreEqual("MyBase", td.BaseTypes[0].Type);
			Assert.AreEqual("Interface1", td.BaseTypes[1].Type);
			Assert.AreEqual("My.Test.Interface2", td.BaseTypes[2].Type);
		}
		
		[Test]
		public void CSharpSimpleStructTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("struct MyStruct {}");
			
			Assert.AreEqual(ClassType.Struct, td.Type);
			Assert.AreEqual("MyStruct", td.Name);
		}
		
		[Test]
		public void CSharpSimpleInterfaceTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("interface MyInterface {}");
			
			Assert.AreEqual(ClassType.Interface, td.Type);
			Assert.AreEqual("MyInterface", td.Name);
		}
		
		[Test]
		public void CSharpSimpleEnumTypeDeclarationTest()
		{
			TypeDeclaration td = ParseUtilCSharp.ParseGlobal<TypeDeclaration>("enum MyEnum {}");
			
			Assert.AreEqual(ClassType.Enum, td.Type);
			Assert.AreEqual("MyEnum", td.Name);
		}
		#endregion
		
		#region VB.NET
		[Test]
		public void VBNetSimpleClassTypeDeclarationTest()
		{
			string program = "Class TestClass\n" +
				"End Class\n";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			
			Assert.AreEqual("TestClass", td.Name);
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual(1, td.StartLocation.Y, "start line");
			Assert.AreEqual(1, td.BodyStartLocation.Y, "bodystart line");
			Assert.AreEqual(16, td.BodyStartLocation.X, "bodystart col");
			Assert.AreEqual(2, td.EndLocation.Y, "end line");
		}
		
		[Test]
		public void VBNetEnumWithBaseClassDeclarationTest()
		{
			string program = "Enum TestEnum As Byte\n" +
				"End Enum\n";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			
			Assert.AreEqual("TestEnum", td.Name);
			Assert.AreEqual(ClassType.Enum, td.Type);
			Assert.AreEqual("Byte", td.BaseTypes[0].Type);
			Assert.AreEqual(0, td.Children.Count);
		}
		
		[Test]
		public void VBNetEnumWithSystemBaseClassDeclarationTest()
		{
			string program = "Enum TestEnum As System.UInt16\n" +
				"End Enum\n";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			
			Assert.AreEqual("TestEnum", td.Name);
			Assert.AreEqual(ClassType.Enum, td.Type);
			Assert.AreEqual("System.UInt16", td.BaseTypes[0].Type);
			Assert.AreEqual(0, td.Children.Count);
		}
		
		[Test]
		public void VBNetSimpleClassTypeDeclarationWithoutLastNewLineTest()
		{
			string program = "Class TestClass\n" +
				"End Class";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			
			Assert.AreEqual("TestClass", td.Name);
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual(1, td.StartLocation.Y, "start line");
			Assert.AreEqual(2, td.EndLocation.Y, "end line");
		}
		
		[Test]
		public void VBNetSimplePartialClassTypeDeclarationTest()
		{
			string program = "Partial Class TestClass\n" +
				"End Class\n";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			
			Assert.AreEqual("TestClass", td.Name);
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual(Modifier.Partial, td.Modifier);
		}
		
		[Test]
		public void VBNetPartialPublicClass()
		{
			string program = "Partial Public Class TestClass\nEnd Class\n";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(program);
			
			Assert.AreEqual("TestClass", td.Name);
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual(Modifier.Partial | Modifier.Public, td.Modifier);
		}
		
		[Test]
		public void VBNetGenericClassTypeDeclarationTest()
		{
			string declr = @"
Public Class Test(Of T)

End Class
";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("Test", td.Name);
			Assert.AreEqual(Modifier.Public, td.Modifier);
			Assert.AreEqual(0, td.BaseTypes.Count);
			Assert.AreEqual(1, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
		}
		
		[Test]
		public void VBNetGenericClassWithConstraint()
		{
			string declr = @"
Public Class Test(Of T As IMyInterface)

End Class
";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("Test", td.Name);
			
			Assert.AreEqual(1, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
			Assert.AreEqual("IMyInterface", td.Templates[0].Bases[0].Type);
		}
		
		[Test]
		public void VBNetComplexGenericClassTypeDeclarationTest()
		{
			string declr = @"
Public Class Generic(Of T As MyNamespace.IMyInterface, S As {G(Of T()), IAnotherInterface})
	Implements System.IComparable

End Class
";
			TypeDeclaration td = ParseUtilVBNet.ParseGlobal<TypeDeclaration>(declr);
			
			Assert.AreEqual(ClassType.Class, td.Type);
			Assert.AreEqual("Generic", td.Name);
			Assert.AreEqual(Modifier.Public, td.Modifier);
			Assert.AreEqual(1, td.BaseTypes.Count);
			Assert.AreEqual("System.IComparable", td.BaseTypes[0].Type);
			
			Assert.AreEqual(2, td.Templates.Count);
			Assert.AreEqual("T", td.Templates[0].Name);
			Assert.AreEqual("MyNamespace.IMyInterface", td.Templates[0].Bases[0].Type);
			
			Assert.AreEqual("S", td.Templates[1].Name);
			Assert.AreEqual(2, td.Templates[1].Bases.Count);
			Assert.AreEqual("G", td.Templates[1].Bases[0].Type);
			Assert.AreEqual(1, td.Templates[1].Bases[0].GenericTypes.Count);
			Assert.IsTrue(td.Templates[1].Bases[0].GenericTypes[0].IsArrayType);
			Assert.AreEqual("T", td.Templates[1].Bases[0].GenericTypes[0].Type);
			Assert.AreEqual(new int[] {0}, td.Templates[1].Bases[0].GenericTypes[0].RankSpecifier);
			Assert.AreEqual("IAnotherInterface", td.Templates[1].Bases[1].Type);
		}
		#endregion
	}
}
