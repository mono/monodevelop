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
using MonoDevelop.CSharpBinding.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture()]
	public class CodeCompletionOperatorTests
	{
		[Test()]
		public void TestAddOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a + b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}

		[Test()]
		public void TestSubtractOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a - b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestMultiplyOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a * b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestDivideOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a / b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}

		[Test()]
		public void TestModulusOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a % b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestBitwiseAndOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a & b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestBitwiseOrOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a | b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestExclusiveOrOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a ^ b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestShiftLeftOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a << b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestShiftRightOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a >> b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestGreaterThanOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a > b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestGreaterThanOrEqualOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a >= b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestEqualityOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a == b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestInEqualityOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a != b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestLessThanOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a < b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestLessThanOrEqualOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(a <= b).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryPlusOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(+a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryMinusOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(-a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryNotOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(!a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
		
		[Test()]
		public void TestUnaryBitwiseNotOperator ()
		{
			CodeCompletionDataProvider provider = CodeCompletionTests.CreateProvider (
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
		(~a).$
	}
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.SearchData ("BMethod"));
		}
	}
}
