// Main.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//
using System;
using Library1;

namespace CompletionDbTest
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			PartialTest t;
		}
	}

	public class SomeGeneric<T>
	{
		public T Run (T val)
		{
			return val;
		}
	}

	public class CustomWidget1: CBin, ISimple
	{
	}

	public class CustomWidget2: SomeContainer.CInnerWidget, Library2.IObject
	{
	}
	
	public class ClassWithInnerDelegates
	{
		public delegate void SomeCallback ();
		
		public class SomeInner
		{
			public int n;
		}
		
		public void Run (SomeCallback cb, SomeInner inner)
		{
		}
	}
	
#region Generic types with constraints
	
	public class GenericConstraintTest0<T>
	{
		T field;
	}
	
	public class GenericConstraintTest1<T> where T:class
	{
		T field;
	}
	
	public class GenericConstraintTest2<T> where T:struct
	{
		T field;
	}
	
	public class GenericConstraintTest3<T> where T:new ()
	{
		T field;
	}
	
	public class GenericConstraintTest4<T,U>
		where T:CBin
		where U:T
	{
		T field1;
		U field2;
		
		void Run ()
		{
		}
	}
	
	// This is wrong
	public class GenericConstraintTest5<T,U>
		where T:U
		where U:T
	{
		T field1;
		U field2;
		
		void Run ()
		{
		}
	}
	
	public class GenericConstraintTest6<T> where T:CBin, ICloneable
	{
		T field;
		
		void Run ()
		{
		}
	}
	
#endregion

#region enums/structs
	public enum TestEnum {}
	public struct TestStruct {}
#endregion
	
	#region Attributes
	
	[Serializable]
	public class AttributeTest {}
	
	public class AttributeTest2 {
		[Obsolete]
		public int Property { get; set; }
		
		[Obsolete]
		public void Method () 
		{}
	}
	
	[Test ("str1", 5, Blah="str2")]
	public class AttributeTest3 {}
	
	#endregion
}

