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
	internal delegate CarbonEventHandlerStatus EventDelegate (IntPtr callRef, IntPtr eventRef, IntPtr userData);
	
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
		static extern EventStatus InstallEventHandler (IntPtr target, EventDelegate handler, uint count,
		                                               CarbonEventTypeSpec [] types, IntPtr user_data, out IntPtr handlerRef);
		
		[DllImport (CarbonLib)]
		public static extern EventStatus RemoveEventHandler (IntPtr handlerRef);
		
		public static IntPtr InstallEventHandler (IntPtr target, EventDelegate handler, CarbonEventTypeSpec [] types)
		{
			IntPtr handlerRef;
			CheckReturn (InstallEventHandler (target, handler, (uint)types.Length, types, IntPtr.Zero, out handlerRef));
			return handlerRef;
		}
		
		public static IntPtr InstallEventHandler (IntPtr target, EventDelegate handler, CarbonEventTypeSpec type)
		{
			return InstallEventHandler (target, handler, new CarbonEventTypeSpec[] { type });
		}
		
		public static IntPtr InstallApplicationEventHandler (EventDelegate handler, CarbonEventTypeSpec [] types)
		{
			return InstallEventHandler (GetApplicationEventTarget (), handler, types);
		}
		
		public static IntPtr InstallApplicationEventHandler (EventDelegate handler, CarbonEventTypeSpec type)
		{
			return InstallEventHandler (GetApplicationEventTarget (), handler, new CarbonEventTypeSpec[] { type });
		}
		
		#endregion
		
		
		[DllImport (CarbonLib)]
		public static extern EventStatus GetEventParameter (IntPtr eventRef, CarbonEventParameterName name, CarbonEventParameterType desiredType,
		                                                    out CarbonEventParameterType actualType, uint size, ref uint outSize, ref IntPtr outPtr);
		
		public static IntPtr GetEventParameter (IntPtr eventRef, CarbonEventParameterName name, CarbonEventParameterType desiredType)
		{
			CarbonEventParameterType actualType;
			uint outSize = 0;
			IntPtr val = IntPtr.Zero;
			CheckReturn (GetEventParameter (eventRef, name, desiredType, out actualType, (uint)IntPtr.Size, ref outSize, ref val));
			return val;
		} 
		
		[DllImport (CarbonLib)]
		static extern EventStatus GetEventParameter (IntPtr eventRef, CarbonEventParameterName name, CarbonEventParameterType desiredType,	
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
		
		public static void CheckReturn (EventStatus status)
		{
			int intStatus = (int) status;
			if (intStatus < 0)
				throw new EventStatusException (status);
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
	
	internal enum CarbonEventHandlerStatus //this is an OSStatus
	{
		Handled = 0,
		NotHandled = -9874,
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
	
	public enum CarbonCommandID : uint
	{
		OK = 1869291552, // 'ok  '
		Cancel = 1852797985, // 'not!'
		Quit = 1903520116, // 'quit'
		Undo = 1970168943, // 'undo'
		Redo = 1919247471, // 'redo'
		Cut = 1668641824, // 'cut '
		Copy = 1668247673, // 'copy'
		Paste = 1885434740, // 'past'
		Clear = 1668048225, // 'clea',
		SelectAll = 1935764588, // 'sall',
		Preferences = 1886545254, //'pref'
		About = 1633841013, // 'abou'
		New = 1852143392, // 'new ',
		Open = 1869636974, // 'open'
		Close = 1668050803, // 'clos'
		Save = 1935767141, // 'save',
		SaveAs = 1937138035, // 'svas'
		Revert = 1920365172, //
		Print = 1886547572, // 'prnt'
		PageSetup = 1885431653, // 'page',
		AppHelp = 1634233456, //'ahlp'
		
		//menu manager handles these automatically
		
		Hide = 1751737445, // 'hide'
		HideOthers = 1751737455, // 'hido'
		ShowAll = 1936220524, // 'shal'
		ZoomWindow = 2054123373, // 'zoom'
		MinimizeWindow = 1835626089, // 'mini'
		MinimizeAll = 1835626081, // 'mina'
		MaximizeAll = 1835104353, // 'maxa'
		ArrangeInFront = 1718775412, // 'frnt'
		BringAllToFront = 1650881140, // 'bfrt'
		SelectWindow = 1937205614, // 'swin'
		RotateWindowsForward = 1919906935, // 'rotw'
		RotateWindowsBackward = 1919906914, // 'rotb'
		RotateFloatingWindowsForward = 1920231031, // 'rtfw'
		RotateFloatingWindowsBackward = 1920231010, // 'rtfb'
		
		//created automatically -- used for inserting before/after the default window list
		WindowListSeparator = 2003592310, // 'wldv'
		WindowListTerminator = 2003596148, // 'wlst'
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
	
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
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
	
	class EventStatusException : SystemException
	{
		public EventStatusException (EventStatus status)
		{
			StatusCode = status;
		}
		
		public EventStatus StatusCode {
			get; private set;
		}
	}
	
	enum EventStatus // this is an OSStatus
	{
		Ok = 0,
		
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
		
		public bool IsFromMenu {
			get { return attributes == CarbonHICommandAttributes.FromMenu; }
		}
		
		public bool IsFromControl {
			get { return attributes == CarbonHICommandAttributes.FromControl; }
		}
		
		public bool IsFromWindow {
			get { return attributes == CarbonHICommandAttributes.FromWindow; }
		}
	}
	
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
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
