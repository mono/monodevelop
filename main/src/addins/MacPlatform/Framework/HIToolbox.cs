// 
// HIToolbox.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//       Miguel de Icaza
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
	
	
	internal static class HIToolbox
	{
		const string hiToolboxLib = "/System/Library/Frameworks/Carbon.framework/Versions/A/Frameworks/HIToolbox.framework/Versions/A/HIToolbox";
		
		[DllImport (hiToolboxLib)]
		static extern MenuResult CreateNewMenu (ushort menuId, MenuAttributes attributes, out IntPtr menuRef);
		
		public static IntPtr CreateMenu (ushort id, string title, MenuAttributes attributes)
		{
			IntPtr menuRef;
			CheckResult (CreateNewMenu (id, attributes, out menuRef));
			SetMenuTitle (menuRef, title);
			return menuRef;
		}

		[DllImport (hiToolboxLib)]
		internal static extern MenuResult SetRootMenu (IntPtr menuRef);

		[DllImport (hiToolboxLib)]
		internal static extern void DeleteMenu (IntPtr menuRef);

		[DllImport (hiToolboxLib)]
		internal static extern void ClearMenuBar ();
		
		[DllImport (hiToolboxLib)]
		internal static extern void InsertMenu (IntPtr menuRef, ushort before_id);
		
		[DllImport (hiToolboxLib)]
		static extern MenuResult AppendMenuItemTextWithCFString (IntPtr menuRef, IntPtr cfstring, MenuItemAttributes inAttributes, uint commandId, out ushort index);
		
		public static ushort AppendMenuItem (IntPtr parentRef, string title, MenuItemAttributes inAttributes, uint commandId)
		{
			ushort index;
			IntPtr str = CoreFoundation.CreateString (title);
			MenuResult result = AppendMenuItemTextWithCFString (parentRef, str, inAttributes, commandId, out index);
			CoreFoundation.Release (str);
			CheckResult (result);
			return index;
		}
		
		public static ushort AppendMenuSeparator (IntPtr parentRef)
		{
			ushort index;
			MenuResult result = AppendMenuItemTextWithCFString (parentRef, IntPtr.Zero, MenuItemAttributes.Separator, 0, out index);
			CheckResult (result);
			return index;
		}
		
		[DllImport (hiToolboxLib)]
		internal static extern MenuResult SetMenuItemHierarchicalMenu (IntPtr parentMenu, ushort parent_index, IntPtr submenu);

		[DllImport (hiToolboxLib)]
		static extern MenuResult SetMenuTitleWithCFString (IntPtr menuRef, IntPtr cfstring);
		
		public static void SetMenuTitle (IntPtr menuRef, string title)
		{
			IntPtr str = CoreFoundation.CreateString (title);
			MenuResult result = SetMenuTitleWithCFString (menuRef, str);
			CoreFoundation.Release (str);
			CheckResult (result);
		}

		[DllImport (hiToolboxLib)]
		internal static extern MenuResult SetMenuItemKeyGlyph (IntPtr menuRef, ushort index, short glyph);

		[DllImport (hiToolboxLib)]
		internal static extern MenuResult SetMenuItemCommandKey (IntPtr menuRef, ushort index, bool isVirtualKey, short key);

		[DllImport (hiToolboxLib)]
		internal static extern MenuResult SetMenuItemModifiers (IntPtr menuRef, ushort index, MenuModifier modifiers);
		
		[DllImport (hiToolboxLib)]
		static extern MenuResult GetMenuItemCommandID (IntPtr menuRef, ushort index, out uint commandId);
		
		public static uint GetMenuItemCommandID (HIMenuItem item)
		{
			uint id;
			CheckResult (GetMenuItemCommandID (item.MenuRef, item.Index, out id));
			return id;
		}
		
		internal static void CheckResult (MenuResult result)
		{
			if (result != MenuResult.Ok)
				throw new CarbonMenuException (result);
		}
	}
	
	class CarbonMenuException : Exception
	{
		public CarbonMenuException (MenuResult result)
		{
			this.Result = result;
		}
		
		public MenuResult Result { get; private set; }
		
		public override string ToString ()
		{
			return string.Format("CarbonMenuException: Result={0}\n{1}", Result, StackTrace);
		}
	}
	
	internal enum MenuResult {
		Ok = 0,
		PropertyInvalid = -5603,
		PropertyNotFound = -5604,
		NotFound  = -5620,
		UsesSystemDef = -5621,
		ItemNotFound = -5622,
		Invalid = -5623
	}

	[Flags]
	internal enum MenuAttributes {
		ExcludesMarkColumn = 1,
		AutoDisable = 1 << 2,
		UsePencilGlyph = 1 << 3,
		Hidden = 1 << 4,
		CondenseSeparators = 1 << 5,
		DoNotCacheImage = 1 << 6,
		DoNotUseUserCommandKeys = 1 << 7
	}

	internal enum MenuModifier : byte {
		NoModifier = 0,
		ShiftModifier = 1 << 0,
		OptionModifier = 1 << 1,
		ControlModifier = 1 << 2,
		NoCommandModifier = 1 << 3
	}
	
	[Flags]
	internal enum MenuItemAttributes {
		Disabled = 1 << 0,
		IconDisabled = 1 << 1,
		SubmenuParentChoosable = 1 << 2,
		Dynamic = 1 << 3,
		NotPreviousAlternate = 1 << 4,
		Hidden = 1 << 5,
		Separator = 1 << 6,
		SectionHeader = 1 << 7,
		IgnoreMeta = 1 << 8,
		AutoRepeat = 1 << 9,
		UseVirtualKey = 1 << 10,
		CustomDraw = 1 << 11,
		IncludeInCmdKeyMatching = 1 << 12,
		AutoDisable = 1 << 13,
		UpdateSingleItem = 1 << 14
	}
}
