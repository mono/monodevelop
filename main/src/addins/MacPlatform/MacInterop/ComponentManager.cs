// 
// ComponentManager.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using System.Runtime.InteropServices;

namespace MonoDevelop.MacInterop
{
	// http://developer.apple.com/mac/library/documentation/Carbon/Reference/Component_Manager/Reference/reference.html
	
	internal class ComponentManager
	{
		[DllImport (Carbon.CarbonLib)]
		public static extern ComponentInstance OpenDefaultComponent (OSType componentType, OSType componentSubType);
		
		[DllImport (Carbon.CarbonLib)]
		public static extern ComponentErr CloseComponent (ComponentInstance componentInstance);
		
		[DllImport (Carbon.CarbonLib)]
		public static extern int GetComponentInstanceError (ComponentInstance componentInstance); //returns an oserr
	}
	
	[StructLayout (LayoutKind.Sequential, Pack=2)]
	internal struct ComponentInstance
	{
		IntPtr handle;
		
		public IntPtr Handle { get { return handle; } }
		
		public bool IsNull {
			get { return Handle == IntPtr.Zero; }
		}
	}
	
	internal enum ComponentResult : long
	{
		InvalidComponentId = -3000,
		ValidInstancesExist = -3001,
		ComponentNotCaptured = -3002,
		ComponentDontRegister = -3003,
		UnresolvedComponentDll = -3004,
		RetryComponentRegistration =-3005,
		BadComponentSelector	 = 0x80008002,
		BadComponentInstance	 = 0x80008001
	}
	
	internal enum ComponentErr : int //this is an OSErr
	{
		InvalidComponentId = -3000,
		ValidInstancesExist = -3001,
		ComponentNotCaptured = -3002,
		ComponentDontRegister = -3003,
		UnresolvedComponentDll = -3004,
		RetryComponentRegistration =-3005,
		BadComponentSelector	 = -32766, // 0x80008002,
		BadComponentInstance	 = -32767, //0x80008001
	}
	
	[Flags]
	internal enum DefaultComponent {
		Identical = 0,
		AnyFlags = 1,
		AnyManufacturer = 2,
		AnySubType = 4,
		AnyFlagsAnyManufacturer = (AnyFlags & AnyManufacturer),
		AnyFlagsAnyManufacturerAnySubType =  (AnyFlags & AnyManufacturer & AnySubType)
	}
	
	internal enum ComponentRequest {
		OpenSelect = -1,
		CloseSelect = -2,
		CanDoSelect = -3,
		VersionSelect = -4,
		RegisterSelect = -5,
		TargetSelect = -6,
		UnregisterSelect = -7,
		GetMPWorkFunctionSelect = -8,
		ExecuteWiredActionSelect = -9,
		GetPublicResourceSelect = -10
	}
}

