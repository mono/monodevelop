// 
// Carbon.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//       Geoff Norton  <gnorton@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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

namespace OSXIntegration.Framework
{
	internal delegate CarbonEventReturn EventDelegate (IntPtr callRef, IntPtr eventRef, IntPtr userData);
	
	internal static class Carbon
	{
		const string CarbonLib = "/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon";
		
		[DllImport (CarbonLib)]
		public static extern IntPtr GetApplicationEventTarget ();
		
		[DllImport (CarbonLib)]
		public static extern IntPtr GetControlEventTarget (IntPtr control);
		
		[DllImport (CarbonLib)]
		public static extern IntPtr GetWindowEventTarget (IntPtr window);
		
		[DllImport (CarbonLib)]
		public static extern IntPtr GetMenuEventTarget (IntPtr menu);

		[DllImport (CarbonLib)]
		public static extern CarbonEventClass GetEventClass (IntPtr eventref);
		
		[DllImport (CarbonLib)]
		public static extern uint GetEventKind (IntPtr eventref);
		
		#region Event handler installation
		
		[DllImport (CarbonLib)]
		static extern OSStatus InstallEventHandler (IntPtr target, EventDelegate handler, uint count,
		                                            CarbonEventTypeSpec [] types, IntPtr user_data, out IntPtr handlerRef);
		
		[DllImport (CarbonLib)]
		public static extern OSStatus RemoveEventHandler (IntPtr handlerRef);
		
		public static void InstallEventHandler (IntPtr target, EventDelegate handler, CarbonEventTypeSpec [] types, out IntPtr handlerRef)
		{
			CheckReturn (InstallEventHandler (target, handler, (uint)types.Length, types, IntPtr.Zero, out handlerRef));
		}
		
		public static void InstallEventHandler (IntPtr target, EventDelegate handler, CarbonEventTypeSpec type, out IntPtr handlerRef)
		{
			InstallEventHandler (target, handler, new CarbonEventTypeSpec[] { type }, out handlerRef);
		}
		
		public static void InstallApplicationEventHandler (EventDelegate handler, CarbonEventTypeSpec [] types, out IntPtr handlerRef)
		{
			InstallEventHandler (GetApplicationEventTarget (), handler, types, out handlerRef);
		}
		
		public static void InstallApplicationEventHandler (EventDelegate handler, CarbonEventTypeSpec type, out IntPtr handlerRef)
		{
			InstallEventHandler (GetApplicationEventTarget (), handler, new CarbonEventTypeSpec[] { type }, out handlerRef);
		}
		
		#endregion
		
		[DllImport (CarbonLib)]
		static extern OSStatus GetEventParameter (IntPtr eventRef, CarbonEventParameterName name, CarbonEventParameterType desiredType,
		                                          out CarbonEventParameterType actualType, uint size, ref uint outSize, IntPtr dataBuffer);
		
		public static T GetEventParameter<T> (IntPtr eventRef, CarbonEventParameterName name, CarbonEventParameterType desiredType) where T : struct
		{
			CarbonEventParameterType actualType;
			uint outSize = 0;
			int len = Marshal.SizeOf (typeof (T));
			IntPtr bufferPtr = Marshal.AllocHGlobal (len);
			CheckReturn (GetEventParameter (eventRef, name, desiredType, out actualType, (uint)len, ref outSize, bufferPtr));
			T val = (T)Marshal.PtrToStructure (bufferPtr, typeof (T));
			Marshal.FreeHGlobal (bufferPtr);
			return val;
		}                                     
		
		public static void CheckReturn (OSStatus status)
		{
			int intStatus = (int) status;
			if (intStatus < 0)
				throw new OSStatusException (status);
		}
		
		internal static int ConvertCharCode (string code)
		{
			return (code[3]) | (code[2] << 8) | (code[1] << 16) | (code[0] << 24);
		}
		
		internal static string UnConvertCharCode (int i)
		{
			return new string (new char[] {
				(char)(i >> 24),
				(char)(0xFF & (i >> 16)),
				(char)(0xFF & (i >> 8)),
				(char)(0xFF & i),
			});
		}
	}
	
	internal enum CarbonEventReturn
	{
		NotHandled = 0,
		Handled = -9874,
	}
	
	internal enum CarbonEventParameterName : uint
	{
		DirectObject = 757935405, // '----'
	}
	
	internal enum CarbonEventParameterType : uint
	{
		HICommand = 1751346532, // 'hcmd'
		MenuRef = 1835363957, // 'menu'
		WindowRef = 2003398244, // 'wind'
		Char = 1413830740, // 'TEXT'
		UInt32 = 1835100014, // 'magn'
		UnicodeText = 1970567284, // 'utxt'
	}
	
	internal enum CarbonEventClass : uint
	{
		Mouse = 1836021107, // 'mous'
		Keyboard = 1801812322, // 'keyb'
		TextInput = 1952807028, // 'text'
		Application = 1634758764, // 'appl'
		AppleEvent = 1701867619,  //'eppc'
		Menu = 1835363957, // 'menu'
		Window = 2003398244, // 'wind'
		Control = 1668183148, // 'cntl'
		Command = 1668113523, // 'cmds'
		Tablet = 1952607348, // 'tblt'
		Volume = 1987013664, // 'vol '
		Appearance = 1634758765, // 'appm'
		Service = 1936028278, // 'serv'
		Toolbar = 1952604530, // 'tbar'
		ToolbarItem = 1952606580, // 'tbit'
		Accessibility = 1633903461, // 'acce'
		HIObject = 1751740258, // 'hiob'
	}
	
	internal enum CarbonEventCommand : uint
	{
		Process = 1,
		UpdateStatus = 2,
	}
	
	internal enum CarbonEventMenu : uint
	{
		BeginTracking = 1,
		EndTracking = 2,
		ChangeTrackingMode = 3,
		Opening = 4,
		Closed = 5,
		TargetItem = 6,
		MatchKey = 7,
	}
	
	[StructLayout(LayoutKind.Sequential)]
	struct CarbonEventTypeSpec
	{
		public CarbonEventClass EventClass;
		public uint EventKind;

		public CarbonEventTypeSpec (CarbonEventClass eventClass, UInt32 eventKind)
		{
			this.EventClass = eventClass;
			this.EventKind = eventKind;
		}
		
		public CarbonEventTypeSpec (CarbonEventMenu kind) : this (CarbonEventClass.Menu, (uint) kind)
		{
		}
		
		public CarbonEventTypeSpec (CarbonEventCommand kind) : this (CarbonEventClass.Command, (uint) kind)
		{
		}
		
		public static implicit operator CarbonEventTypeSpec (CarbonEventMenu kind)
		{
			return new CarbonEventTypeSpec (kind);
		}
		
		public static implicit operator CarbonEventTypeSpec (CarbonEventCommand kind)
		{
			return new CarbonEventTypeSpec (kind);
		}
	}
	
	class OSStatusException : SystemException
	{
		public OSStatusException (OSStatus status)
		{
			StatusCode = status;
		}
		
		public OSStatus StatusCode {
			get; private set;
		}
	}
	
	enum OSStatus
	{
		NoErr = 0,
		
		//event manager
		EventAlreadyPostedErr = -9860,
		EventTargetBusyErr = -9861,
		EventClassInvalidErr = -9862,
		EventClassIncorrectErr = -9864,
		EventHandlerAlreadyInstalledErr = -9866,
		EventInternalErr = -9868,
		EventKindIncorrectErr = -9869,
		EventParameterNotFoundErr = -9870,
		EventNotHandledErr = -9874,
		EventLoopTimedOutErr = -9875,
		EventLoopQuitErr = -9876,
		EventNotInQueueErr = -9877,
		EventHotKeyExistsErr = -9878,
		EventHotKeyInvalidErr = -9879,
	}
	
	[StructLayout(LayoutKind.Explicit)]
	struct CarbonHICommand //technically HICommandExtended, but they're compatible
	{
		[FieldOffset(0)]
		CarbonHICommandAttributes attributes;
		
		[FieldOffset(4)]
		uint commandID;
		
		[FieldOffset(8)]
		IntPtr controlRef;
		
		[FieldOffset(8)]
		IntPtr windowRef;
		
		[FieldOffset(8)]
		HIMenuItem menuItem;
		
		public CarbonHICommandAttributes Attributes { get { return attributes; } }
		public uint CommandID { get { return commandID; } }
		public IntPtr ControlRef { get { return controlRef; } }
		public IntPtr WindowRef { get { return windowRef; } }
		public HIMenuItem MenuItem { get { return menuItem; } }
		
		public bool IsFromMenu { get { return attributes == CarbonHICommandAttributes.FromMenu; } }
		public bool IsFromControl { get { return attributes == CarbonHICommandAttributes.FromControl; } }
		public bool IsFromWindow { get { return attributes == CarbonHICommandAttributes.FromWindow; } }
	}
	
	[StructLayout(LayoutKind.Sequential)]
	struct HIMenuItem
	{
		IntPtr menuRef;
		ushort index;
		
		public HIMenuItem (IntPtr menuRef, ushort index)
		{
			this.index = index;
			this.menuRef = menuRef;
		}
		
		public IntPtr MenuRef { get { return menuRef; } }
		public ushort Index { get { return index; } }
	}
	
	//*NOT* flags
	enum CarbonHICommandAttributes : uint
	{
		FromMenu = 1,
		FromControl = 1 << 1,
		FromWindow  = 1 << 2,
	}
}
