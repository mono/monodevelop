// 
// DomTests.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Serialization;

namespace MonoDevelop.Projects.DomTests
{
	[TestFixture()]
	public class DomTests : UnitTests.TestBase
	{
		[Test()]
		public void InstantiatedMethodByArgumentTest ()
		{
			// build "T MyMethod<T> (T a)"
			DomMethod method = new DomMethod ();
			method.Name = "MyMethod";
			method.ReturnType = new DomReturnType ("T");
			method.AddTypeParameter (new TypeParameter ("T"));
			method.Add (new DomParameter (method, "a", new DomReturnType ("T")));
			
			// give int as param type
			List<IReturnType> genArgs = new List<IReturnType> ();
			List<IReturnType> args    = new List<IReturnType> ();
			args.Add (DomReturnType.Int32);
			IMethod instMethod = DomMethod.CreateInstantiatedGenericMethod (method, genArgs, args);
			
			// check 
			Assert.AreEqual (DomReturnType.Int32.FullName, instMethod.ReturnType.FullName);
			Assert.AreEqual (DomReturnType.Int32.FullName, instMethod.Parameters[0].ReturnType.FullName);
		}
		
		[Test()]
		public void InstantiatedMethodByParameterTest ()
		{
			// build "T MyMethod<T> (T[] a)"
			DomMethod method = new DomMethod ();
			method.Name = "MyMethod";
			method.ReturnType = new DomReturnType ("T");
			method.AddTypeParameter (new TypeParameter ("T"));
			DomReturnType returnType = new DomReturnType ("T");
			returnType.ArrayDimensions = 1;
			method.Add (new DomParameter (method, "a", returnType));
			
			// give int[] as param type.
			List<IReturnType> genArgs = new List<IReturnType> ();
			List<IReturnType> args    = new List<IReturnType> ();
			returnType = new DomReturnType (DomReturnType.Int32.FullName);
			returnType.ArrayDimensions = 1;
			genArgs.Add (returnType);
			
			IMethod instMethod = DomMethod.CreateInstantiatedGenericMethod (method, genArgs, args);
			
			// check (note that return type should be int and not int[])
			Assert.AreEqual (DomReturnType.Int32.FullName, instMethod.ReturnType.FullName);
			Assert.AreEqual (0, instMethod.ReturnType.ArrayDimensions);
			Assert.AreEqual (DomReturnType.Int32.FullName, instMethod.Parameters[0].ReturnType.FullName);
		}
		
		[Test()]
		public void InstantiatedMethodByArgumentTest_Complex ()
		{
			// build "T MyMethod<T,S> (S b, KeyValuePair<S, T> a)"
			DomMethod method = new DomMethod ();
			method.Name = "MyMethod";
			method.ReturnType = new DomReturnType ("T");
			method.AddTypeParameter (new TypeParameter ("T"));
			method.AddTypeParameter (new TypeParameter ("S"));
			method.Add (new DomParameter (method, "b", new DomReturnType ("S")));
			
			DomReturnType returnType = new DomReturnType ("KeyValuePair");
			returnType.AddTypeParameter (new DomReturnType ("T"));
			returnType.AddTypeParameter (new DomReturnType ("S"));
			method.Add (new DomParameter (method, "a", returnType));
			
			// give int, object as param type
			List<IReturnType> genArgs = new List<IReturnType> ();
			List<IReturnType> args    = new List<IReturnType> ();
			genArgs.Add (DomReturnType.Int32);
			genArgs.Add (DomReturnType.Object);
			
			IMethod instMethod = DomMethod.CreateInstantiatedGenericMethod (method, genArgs, args);
			
			// check
			Assert.AreEqual (DomReturnType.Int32.FullName, instMethod.ReturnType.FullName);
			Assert.AreEqual (DomReturnType.Object.FullName, instMethod.Parameters[0].ReturnType.FullName);
			
			Assert.AreEqual (DomReturnType.Int32.FullName, instMethod.Parameters[1].ReturnType.GenericArguments[0].FullName);
			Assert.AreEqual (DomReturnType.Object.FullName, instMethod.Parameters[1].ReturnType.GenericArguments[1].FullName);
		}
		
		[Test()]
		public void ExtensionMethodTest ()
		{
			// build "T MyMethod<T, S> (this KeyValuePair<T, S> a, S b)"
			DomMethod method = new DomMethod ();
			method.Name = "MyMethod";
			method.ReturnType = new DomReturnType ("T");
			method.AddTypeParameter (new TypeParameter ("T"));
			method.AddTypeParameter (new TypeParameter ("S"));
			
			DomReturnType returnType = new DomReturnType ("KeyValuePair");
			returnType.AddTypeParameter (new DomReturnType ("T"));
			returnType.AddTypeParameter (new DomReturnType ("S"));
			method.Add (new DomParameter (method, "a", returnType));
			method.Add (new DomParameter (method, "b", new DomReturnType ("S")));
			
			// Build extendet type KeyValuePair<int, object>
			DomType type = new DomType ("KeyValuePair");
			type.AddTypeParameter (new TypeParameter ("T"));
			type.AddTypeParameter (new TypeParameter ("S"));
			IType extType = DomType.CreateInstantiatedGenericTypeInternal (type, new IReturnType[] { DomReturnType.Int32, DomReturnType.Object });
			Console.WriteLine (extType);
			
			// extend method
			List<IReturnType> genArgs = new List<IReturnType> ();
			List<IReturnType> args    = new List<IReturnType> ();
			
			ExtensionMethod extMethod = new ExtensionMethod (extType, method, genArgs, args);
			
			Console.WriteLine (extMethod);
			// check 
			Assert.AreEqual (DomReturnType.Int32.FullName, extMethod.ReturnType.FullName);
			Assert.AreEqual (DomReturnType.Object.FullName, extMethod.Parameters[0].ReturnType.FullName);
		}
	}
}
