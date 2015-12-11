//
// OperatorTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion
{
	[TestFixture()]
	public class CodeCompletionOperatorTests : TestBase
	{

		[Test()]
		public void TestAddOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator+(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a + b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}

		[Test()]
		public void TestSubtractOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator-(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a - b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestMultiplyOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator*(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a * b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestDivideOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator/(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a / b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}

		[Test()]
		public void TestModulusOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator%(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a % b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestBitwiseAndOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator&(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a & b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestBitwiseOrOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator|(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a | b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestExclusiveOrOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator^(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a ^ b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestShiftLeftOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator<<(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a << b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestShiftRightOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator>>(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a >> b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestGreaterThanOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator>(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a > b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestGreaterThanOrEqualOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator>=(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a >= b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestEqualityOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator==(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a == b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestInEqualityOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator!=(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a != b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestLessThanOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator<(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a < b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestLessThanOrEqualOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator<=(A left, A right)
	{
		return new B ();
	}
}

class B
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		A b = new A ();
		$(a <= b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryPlusOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator+(A left)
	{
		return new B ();
	}
}

class B : A
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		$(+a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryMinusOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator-(A left)
	{
		return new B ();
	}
}

class B : A
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		$(-a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryNotOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator!(A left)
	{
		return new B ();
	}
}

class B : A
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		$(!a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryBitwiseNotOperator ()
		{
			CompletionResult provider = CodeCompletionBugTests.CreateProvider (
@"class A
{
	public static B operator~(A left)
	{
		return new B ();
	}
}

class B : A
{
	public void BMethod ()
	{
	}
}

class TestClass
{
	public void Test ()
	{
		A a = new A ();
		$(~a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("BMethod"));
		}
	}
}
