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

namespace MonoDevelop.Components.Mac
{
	public static class ObjcHelper
	{
		public static Class GetObjectClass (IntPtr obj)
			=> Xwt.Mac.ObjcHelper.GetObjectClass (obj);

		public static Class GetMetaClass (string name)
			=> Xwt.Mac.ObjcHelper.GetMetaClass (name);

		public static void InstanceMethodExchange (this Class cls, IntPtr sel1, IntPtr sel2)
			=> Xwt.Mac.ObjcHelper.InstanceMethodExchange (cls, sel1, sel2);

		public static void MethodExchange (this Class cls, IntPtr sel1, IntPtr sel2)
			=> Xwt.Mac.ObjcHelper.MethodExchange (cls, sel1, sel2);

		public static IntPtr SetMethodImplementation (this Class cls, IntPtr method, Delegate impl)
			=> Xwt.Mac.ObjcHelper.SetMethodImplementation (cls, method, impl);

		public static IntPtr GetMethod (this Class cls, IntPtr sel)
			=> Xwt.Mac.ObjcHelper.GetMethod (cls, sel);

		public static bool AddMethod (this Class cls, IntPtr sel, Delegate method, string argTypes)
			=> Xwt.Mac.ObjcHelper.AddMethod (cls, sel, method, argTypes);

		public static IntPtr GetProtocol (string name)
			=> Xwt.Mac.ObjcHelper.GetProtocol (name);

		public static bool ConformsToProtocol (this Class cls, IntPtr protocol)
			=> Xwt.Mac.ObjcHelper.ConformsToProtocol (cls, protocol);

		public static bool AddProtocol (this Class cls, IntPtr protocol)
			=> Xwt.Mac.ObjcHelper.AddProtocol (cls, protocol);
	}
}

#endif