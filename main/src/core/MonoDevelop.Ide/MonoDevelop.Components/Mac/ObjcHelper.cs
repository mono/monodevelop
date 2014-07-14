// 
// ObjcHelper.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
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

#if MAC
using System;
using ObjCRuntime;
using System.Runtime.InteropServices;

namespace MonoDevelop.Components.Mac
{
	public static class ObjcHelper
	{
		[DllImport ("/usr/lib/libobjc.dylib")]
		static extern bool class_addMethod (IntPtr cls, IntPtr sel, Delegate method, string argTypes);
		
		[DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr objc_getMetaClass (string name);
		
		[DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr class_getInstanceMethod (IntPtr cls, IntPtr sel);
		
		[DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr class_getClassMethod (IntPtr cls, IntPtr sel);
		
		[System.Runtime.InteropServices.DllImport ("/usr/lib/libobjc.dylib")]
		static extern bool method_exchangeImplementations (IntPtr m1, IntPtr m2);
		
		[System.Runtime.InteropServices.DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr method_setImplementation (IntPtr m1, Delegate impl);
		
		[System.Runtime.InteropServices.DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr object_getClass (IntPtr obj);
		
		[System.Runtime.InteropServices.DllImport ("/usr/lib/libobjc.dylib")]
		static extern IntPtr objc_getProtocol (string name);
		
		[System.Runtime.InteropServices.DllImport ("/usr/lib/libobjc.dylib")]
		static extern bool class_conformsToProtocol(IntPtr cls, IntPtr protocol);
		
		[System.Runtime.InteropServices.DllImport ("/usr/lib/libobjc.dylib")]
		static extern bool class_addProtocol(IntPtr cls, IntPtr protocol);
		
		public static Class GetObjectClass (IntPtr obj)
		{
			return new Class (object_getClass (obj));
		}
		
		public static Class GetMetaClass (string name)
		{
			return new Class (objc_getMetaClass (name));
		}
		
		public static void InstanceMethodExchange (this Class cls, IntPtr sel1, IntPtr sel2)
		{
			IntPtr m1 = class_getInstanceMethod (cls.Handle, sel1);
			if (m1 == IntPtr.Zero)
				throw new Exception ("Class did not have a method for the first selector");
			IntPtr m2 = class_getInstanceMethod (cls.Handle, sel2);
			if (m2 == IntPtr.Zero)
				throw new Exception ("Class did not have a method for the second selector");
			if (!method_exchangeImplementations (m1, m2))
				throw new Exception ("Failed to exchange implementations");
		}
		
		public static void MethodExchange (this Class cls, IntPtr sel1, IntPtr sel2)
		{
			IntPtr m1 = class_getClassMethod (cls.Handle, sel1);
			if (m1 == IntPtr.Zero)
				throw new Exception ("Class did not have a method for the first selector");
			IntPtr m2 = cls.GetMethod (sel2);
			if (m2 == IntPtr.Zero)
				throw new Exception ("Class did not have a method for the second selector");
			if (!method_exchangeImplementations (m1, m2))
				throw new Exception ("Failed to exchange implementations");
		}
		
		public static IntPtr SetMethodImplementation (this Class cls, IntPtr method, Delegate impl)
		{
			IntPtr m1 = class_getClassMethod (cls.Handle, method);
			if (m1 == IntPtr.Zero)
				throw new Exception ("Class did not have a method for the first selector");
			return method_setImplementation (m1, impl);
		}
		
		public static IntPtr GetMethod (this Class cls, IntPtr sel)
		{
			return class_getClassMethod (cls.Handle, sel);
		}

		public static bool AddMethod (this Class cls, IntPtr sel, Delegate method, string argTypes)
		{
			return class_addMethod (cls.Handle, sel, method, argTypes);
		}
		
		public static IntPtr GetProtocol (string name)
		{
			return objc_getProtocol (name);
		}
		
		public static bool ConformsToProtocol(this Class cls, IntPtr protocol)
		{
			return class_conformsToProtocol (cls.Handle, protocol);
		}
		
		public static bool AddProtocol(this Class cls, IntPtr protocol)
		{
			return class_addProtocol (cls.Handle, protocol);
		}
	}
}

#endif