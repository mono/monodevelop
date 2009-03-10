// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 3763 $</version>
// </file>

using System;
using System.IO;
using NUnit.Framework;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

namespace ICSharpCode.NRefactory.Tests.PrettyPrinter
{
	[TestFixture]
	public class CSharpOutputTest
	{
		void TestProgram(string program)
		{
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(program));
			parser.Parse();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor();
			outputVisitor.VisitCompilationUnit(parser.CompilationUnit, null);
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(StripWhitespace(program), StripWhitespace(outputVisitor.Text));
		}
		
		internal static string StripWhitespace(string text)
		{
			return text.Trim().Replace("\t", "").Replace("\r", "").Replace("\n", " ").Replace("  ", " ");
		}
		
		void TestTypeMember(string program)
		{
			TestProgram("class A { " + program + " }");
		}
		
		void TestStatement(string statement)
		{
			TestTypeMember("void Method() { " + statement + " }");
		}
		
		void TestExpression(string expression)
		{
			// SEMICOLON HACK : without a trailing semicolon, parsing expressions does not work correctly
			IParser parser = ParserFactory.CreateParser(SupportedLanguage.CSharp, new StringReader(expression + ";"));
			Expression e = parser.ParseExpression();
			Assert.AreEqual("", parser.Errors.ErrorOutput);
			Assert.IsNotNull(e, "ParseExpression returned null");
			CSharpOutputVisitor outputVisitor = new CSharpOutputVisitor();
			e.AcceptVisitor(outputVisitor, null);
			Assert.AreEqual("", outputVisitor.Errors.ErrorOutput);
			Assert.AreEqual(StripWhitespace(expression), StripWhitespace(outputVisitor.Text));
		}
		
		[Test]
		public void Namespace()
		{
			TestProgram("namespace System { }");
		}
		
		[Test]
		public void CustomEvent()
		{
			TestTypeMember("public event EventHandler Click {" +
			               " add { obj.Click += value; }" +
			               " remove { obj.Click -= value; } " +
			               "}");
		}
		
		[Test]
		public void EventWithInitializer()
		{
			TestTypeMember("public event EventHandler Click = delegate { };");
		}
		
		[Test]
		public void Field()
		{
			TestTypeMember("int a;");
		}
		
		[Test]
		public void Method()
		{
			TestTypeMember("void Method() { }");
		}
		
		[Test]
		public void StaticMethod()
		{
			TestTypeMember("static void Method() { }");
		}
		
		[Test]
		public void PartialModifier()
		{
			TestProgram("public partial class Foo { }");
		}
		
		[Test]
		public void GenericClassDefinition()
		{
			TestProgram("public class Foo<T> where T : IDisposable, ICloneable { }");
		}
		
		[Test]
		public void InterfaceWithOutParameters()
		{
			TestProgram("public interface ITest { void Method(out int a, ref double b); }");
		}
		
		[Test]
		public void GenericClassDefinitionWithBaseType()
		{
			TestProgram("public class Foo<T> : BaseClass where T : IDisposable, ICloneable { }");
		}
		
		[Test]
		public void GenericMethodDefinition()
		{
			TestTypeMember("public void Foo<T>(T arg) where T : IDisposable, ICloneable { }");
		}
		
		[Test]
		public void ArrayRank()
		{
			TestStatement("object[,,] a = new object[1, 2, 3];");
		}
		
		[Test]
		public void JaggedArrayRank()
		{
			TestStatement("object[,][,,] a = new object[1, 2][,,];");
		}
		
		[Test]
		public void ArrayInitializer()
		{
			TestStatement("object[] a = new object[] { 1, 2, 3 };");
		}
		
		[Test]
		public void IfStatement()
		{
			TestStatement("if (a) { m1(); } else { m2(); }");
			
			TestStatement("if (a) m1(); else m2(); ");
			
			TestStatement("if (a) {\n" +
			              "\tm1();\n" +
			              "} else if (b) {\n" +
			              "\tm2();\n" +
			              "} else {\n" +
			              "\tm3();\n" +
			              "}");
		}
		
		[Test]
		public void Assignment()
		{
			TestExpression("a = b");
		}
		
		[Test]
		public void UnaryOperator()
		{
			TestExpression("a = -b");
		}
		
		[Test]
		public void BlockStatements()
		{
			TestStatement("checked { }");
			TestStatement("unchecked { }");
			TestStatement("unsafe { }");
		}
		
		[Test]
		public void ExceptionHandling()
		{
			TestStatement("try { throw new Exception(); } " +
			              "catch (FirstException e) { } " +
			              "catch (SecondException) { } " +
			              "catch { throw; } " +
			              "finally { }");
		}
		
		[Test]
		public void LoopStatements()
		{
			TestStatement("foreach (Type var in col) { }");
			TestStatement("while (true) { }");
			TestStatement("do { } while (true);");
		}
		
		[Test]
		public void Switch()
		{
			TestStatement("switch (a) {" +
			              " case 0:" +
			              " case 1:" +
			              "  break;" +
			              " case 2:" +
			              "  return;" +
			              " default:" +
			              "  throw new Exception(); " +
			              "}");
		}
		
		[Test]
		public void MultipleVariableForLoop()
		{
			TestStatement("for (int a = 0, b = 0; b < 100; ++b,a--) { }");
		}
		
		[Test]
		public void SizeOf()
		{
			TestExpression("sizeof(IntPtr)");
		}
		
		[Test]
		public void ParenthesizedExpression()
		{
			TestExpression("(a)");
		}
		
		[Test]
		public void MethodOnGenericClass()
		{
			TestExpression("Container<string>.CreateInstance()");
		}
		
		[Test]
		public void EmptyStatement()
		{
			TestStatement(";");
		}
		
		[Test]
		public void Yield()
		{
			TestStatement("yield break;");
			TestStatement("yield return null;");
		}
		
		[Test]
		public void Integer()
		{
			TestExpression("16");
		}
		
		[Test]
		public void HexadecimalInteger()
		{
			TestExpression("0x10");
		}
		
		[Test]
		public void LongInteger()
		{
			TestExpression("12L");
		}
		
		[Test]
		public void LongUnsignedInteger()
		{
			TestExpression("12uL");
		}
		
		[Test]
		public void UnsignedInteger()
		{
			TestExpression("12u");
		}
		
		[Test]
		public void Double()
		{
			TestExpression("12.5");
			TestExpression("12.0");
		}
		
		[Test]
		public void StringWithUnicodeLiteral()
		{
			TestExpression(@"""\u0001""");
		}
		
		[Test]
		public void GenericMethodInvocation()
		{
			TestExpression("GenericMethod<T>(arg)");
		}
		
		[Test]
		public void Cast()
		{
			TestExpression("(T)a");
		}
		
		[Test]
		public void AsCast()
		{
			TestExpression("a as T");
		}
		
		[Test]
		public void NullCoalescing()
		{
			TestExpression("a ?? b");
		}
		
		[Test]
		public void SpecialIdentifierName()
		{
			TestExpression("@class");
		}
		
		[Test]
		public void InnerClassTypeReference()
		{
			TestExpression("typeof(List<string>.Enumerator)");
		}
		
		[Test]
		public void GenericDelegate()
		{
			TestProgram("public delegate void Predicate<T>(T item) where T : IDisposable, ICloneable;");
		}
		
		[Test]
		public void Enum()
		{
			TestProgram("enum MyTest { Red, Green, Blue, Yellow }");
		}
		
		[Test]
		public void EnumWithInitializers()
		{
			TestProgram("enum MyTest { Red = 1, Green = 2, Blue = 4, Yellow = 8 }");
		}
		
		[Test]
		public void SyncLock()
		{
			TestStatement("lock (a) { Work(); }");
		}
		
		[Test]
		public void Using()
		{
			TestStatement("using (A a = new A()) { a.Work(); }");
		}
		
		[Test]
		public void AbstractProperty()
		{
			TestTypeMember("public abstract bool ExpectsValue { get; set; }");
			TestTypeMember("public abstract bool ExpectsValue { get; }");
			TestTypeMember("public abstract bool ExpectsValue { set; }");
		}
		
		[Test]
		public void SetOnlyProperty()
		{
			TestTypeMember("public bool ExpectsValue { set { DoSomething(value); } }");
		}
		
		[Test]
		public void AbstractMethod()
		{
			TestTypeMember("public abstract void Run();");
			TestTypeMember("public abstract bool Run();");
		}
		
		[Test]
		public void AnonymousMethod()
		{
			TestStatement("Func b = delegate { return true; };");
			TestStatement("Func a = delegate() { return false; };");
		}
		
		[Test]
		public void Interface()
		{
			TestProgram("interface ITest {" +
			            " bool GetterAndSetter { get; set; }" +
			            " bool GetterOnly { get; }" +
			            " bool SetterOnly { set; }" +
			            " void InterfaceMethod();" +
			            " string InterfaceMethod2();\n" +
			            "}");
		}
		
		[Test]
		public void IndexerDeclaration()
		{
			TestTypeMember("public string this[int index] { get { return index.ToString(); } set { } }");
			TestTypeMember("public string IList.this[int index] { get { return index.ToString(); } set { } }");
		}
		
		[Test]
		public void OverloadedConversionOperators()
		{
			TestTypeMember("public static explicit operator TheBug(XmlNode xmlNode) { }");
			TestTypeMember("public static implicit operator XmlNode(TheBug bugNode) { }");
		}
		
		[Test]
		public void OverloadedTrueFalseOperators()
		{
			TestTypeMember("public static bool operator true(TheBug bugNode) { }");
			TestTypeMember("public static bool operator false(TheBug bugNode) { }");
		}
		
		[Test]
		public void OverloadedOperators()
		{
			TestTypeMember("public static TheBug operator +(TheBug bugNode, TheBug bugNode2) { }");
			TestTypeMember("public static TheBug operator >>(TheBug bugNode, int b) { }");
		}
		
		[Test]
		public void PropertyWithAccessorAccessModifiers()
		{
			TestTypeMember("public bool ExpectsValue {\n" +
			               "\tinternal get {\n" +
			               "\t}\n" +
			               "\tprotected set {\n" +
			               "\t}\n" +
			               "}");
		}
		
		[Test]
		public void UsingStatementForExistingVariable()
		{
			TestStatement("using (obj) {\n}");
		}
		
		[Test]
		public void NewConstraint()
		{
			TestProgram("public struct Rational<T, O> where O : IRationalMath<T>, new()\n{\n}");
		}
		
		[Test]
		public void StructConstraint()
		{
			TestProgram("public struct Rational<T, O> where O : struct\n{\n}");
		}
		
		[Test]
		public void ClassConstraint()
		{
			TestProgram("public struct Rational<T, O> where O : class\n{\n}");
		}
		
		[Test]
		public void ExtensionMethod()
		{
			TestTypeMember("public static T[] Slice<T>(this T[] source, int index, int count)\n{ }");
		}
		
		[Test]
		public void FixedStructField()
		{
			TestProgram(@"unsafe struct CrudeMessage
{
	public fixed byte data[256];
}");
		}
		
		[Test]
		public void FixedStructField2()
		{
			TestProgram(@"unsafe struct CrudeMessage
{
	fixed byte data[4 * sizeof(int)], data2[10];
}");
		}
		
		[Test]
		public void ImplicitlyTypedLambda()
		{
			TestExpression("x => x + 1");
		}
		
		[Test]
		public void ImplicitlyTypedLambdaWithBody()
		{
			TestExpression("x => { return x + 1; }");
			TestStatement("Func<int, int> f = x => { return x + 1; };");
		}
		
		[Test]
		public void ExplicitlyTypedLambda()
		{
			TestExpression("(int x) => x + 1");
		}
		
		[Test]
		public void ExplicitlyTypedLambdaWithBody()
		{
			TestExpression("(int x) => { return x + 1; }");
		}
		
		[Test]
		public void LambdaMultipleParameters()
		{
			TestExpression("(x, y) => x * y");
			TestExpression("(x, y) => { return x * y; }");
			TestExpression("(int x, int y) => x * y");
			TestExpression("(int x, int y) => { return x * y; }");
		}
		
		[Test]
		public void LambdaNoParameters()
		{
			TestExpression("() => Console.WriteLine()");
			TestExpression("() => { Console.WriteLine(); }");
		}
		
		[Test]
		public void ObjectInitializer()
		{
			TestExpression("new Point { X = 0, Y = 1 }");
			TestExpression("new Rectangle { P1 = new Point { X = 0, Y = 1 }, P2 = new Point { X = 2, Y = 3 } }");
			TestExpression("new Rectangle(arguments) { P1 = { X = 0, Y = 1 }, P2 = { X = 2, Y = 3 } }");
		}
		
		[Test]
		public void CollectionInitializer()
		{
			TestExpression("new List<int> { 0, 1, 2, 3, 4, 5 }");
			TestExpression(@"new List<Contact> { new Contact { Name = ""Chris Smith"", PhoneNumbers = { ""206-555-0101"", ""425-882-8080"" } }, new Contact { Name = ""Bob Harris"", PhoneNumbers = { ""650-555-0199"" } } }");
		}
		
		[Test]
		public void AnonymousTypeCreation()
		{
			TestExpression("new { obj.Name, Price = 26.9, ident }");
		}
		
		[Test]
		public void ImplicitlyTypedArrayCreation()
		{
			TestExpression("new[] { 1, 10, 100, 1000 }");
		}
		
		[Test]
		public void QuerySimpleWhere()
		{
			TestExpression("from n in numbers where n < 5 select n");
		}
		
		[Test]
		public void QueryMultipleFrom()
		{
			TestExpression("from c in customers" +
			               " where c.Region == \"WA\"" +
			               " from o in c.Orders" +
			               " where o.OrderDate >= cutoffDate" +
			               " select new { c.CustomerID, o.OrderID }");
		}
		
		[Test]
		public void QuerySimpleOrdering()
		{
			TestExpression("from w in words" +
			               " orderby w" +
			               " select w");
		}
		
		[Test]
		public void QueryComplexOrdering()
		{
			TestExpression("from w in words" +
			               " orderby w.Length descending, w ascending" +
			               " select w");
		}
		
		[Test]
		public void QueryGroupInto()
		{
			TestExpression("from n in numbers" +
			               " group n by n % 5 into g" +
			               " select new { Remainder = g.Key, Numbers = g }");
		}
		
		[Test]
		public void ExternAlias()
		{
			TestProgram("extern alias Name;");
		}
	}
}
