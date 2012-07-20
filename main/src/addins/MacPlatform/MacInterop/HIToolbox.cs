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

namespace MonoDevelop.MacInterop
{
	internal static class HIToolbox
	{
		const string hiToolboxLib = "/System/Library/Frameworks/Carbon.framework/Versions/A/Frameworks/HIToolbox.framework/Versions/A/HIToolbox";
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus CreateNewMenu (ushort menuId, MenuAttributes attributes, out IntPtr menuRef);
		
		public static IntPtr CreateMenu (ushort id, string title, MenuAttributes attributes)
		{
			IntPtr menuRef;
			CheckResult (CreateNewMenu (id, attributes, out menuRef));
			SetMenuTitle (menuRef, title);
			return menuRef;
		}

		[DllImport (hiToolboxLib)]
		internal static extern CarbonMenuStatus SetRootMenu (IntPtr menuRef);

		[DllImport (hiToolboxLib)]
		internal static extern void DeleteMenu (IntPtr menuRef);

		[DllImport (hiToolboxLib)]
		internal static extern void FlashMenuBar (uint menuID);
		
		[DllImport (hiToolboxLib)]
		internal static extern uint GetMenuID (IntPtr menuRef);
		
		[DllImport (hiToolboxLib)]
		internal static extern void ClearMenuBar ();
		
		[DllImport (hiToolboxLib)]
		internal static extern ushort CountMenuItems (IntPtr menuRef);
		
		[DllImport (hiToolboxLib)]
		static extern void DeleteMenuItem (IntPtr menuRef, ushort index);

		public static void DeleteMenuItem (HIMenuItem item)
		{
			DeleteMenuItem (item.MenuRef, item.Index);
		}
		
		[DllImport (hiToolboxLib)]
		internal static extern void InsertMenu (IntPtr menuRef, ushort beforeId);
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus AppendMenuItemTextWithCFString (IntPtr menuRef, IntPtr cfString, MenuItemAttributes attributes, uint commandId, out ushort index);
		
		public static ushort AppendMenuItem (IntPtr parentRef, string title, MenuItemAttributes attributes, uint commandId)
		{
			ushort index;
			IntPtr str = CoreFoundation.CreateString (title);
			CarbonMenuStatus result = AppendMenuItemTextWithCFString (parentRef, str, attributes, commandId, out index);
			CoreFoundation.Release (str);
			CheckResult (result);
			return index;
		}
		
		public static ushort AppendMenuSeparator (IntPtr parentRef)
		{
			ushort index;
			CarbonMenuStatus result = AppendMenuItemTextWithCFString (parentRef, IntPtr.Zero, MenuItemAttributes.Separator, 0, out index);
			CheckResult (result);
			return index;
		}
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus InsertMenuItemTextWithCFString (IntPtr menuRef, IntPtr cfString,
			ushort afterItemIndex, MenuItemAttributes attributes, uint commandID);
		
		public static ushort InsertMenuItem (IntPtr parentRef, string title, ushort afterItemIndex, MenuItemAttributes attributes, uint commandId)
		{
			IntPtr str = CoreFoundation.CreateString (title);
			CarbonMenuStatus result = InsertMenuItemTextWithCFString (parentRef, str, afterItemIndex, attributes, commandId);
			CoreFoundation.Release (str);
			CheckResult (result);
			return (ushort) (afterItemIndex + 1);
		}
		
		public static ushort InsertMenuSeparator (IntPtr parentRef, ushort afterItemIndex)
		{
			CarbonMenuStatus result = InsertMenuItemTextWithCFString (parentRef, IntPtr.Zero, afterItemIndex, MenuItemAttributes.Separator, 0);
			CheckResult (result);
			return (ushort) (afterItemIndex + 1);
		}

		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus EnableMenuItem (IntPtr menuRef, ushort index);
		
		public static void EnableMenuItem (HIMenuItem item)
		{
			CheckResult (EnableMenuItem (item.MenuRef, item.Index));
		}
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus DisableMenuItem (IntPtr menuRef, ushort index);
		
		public static void DisableMenuItem (HIMenuItem item)
		{
			CheckResult (DisableMenuItem (item.MenuRef, item.Index));
		}
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus ChangeMenuItemAttributes (IntPtr menu, ushort item, MenuItemAttributes setTheseAttributes, 
		                                                         MenuItemAttributes clearTheseAttributes);
		
		public static void ChangeMenuItemAttributes (HIMenuItem item, MenuItemAttributes toSet, MenuItemAttributes toClear)
		{
			CheckResult (ChangeMenuItemAttributes (item.MenuRef, item.Index, toSet, toClear));
		}
		
		[DllImport (hiToolboxLib)]
		internal static extern CarbonMenuStatus SetMenuItemHierarchicalMenu (IntPtr parentMenu, ushort parent_index, IntPtr submenu);
		
		[DllImport (hiToolboxLib)]
		internal static extern CarbonMenuStatus GetMenuItemHierarchicalMenu (IntPtr parentMenu, ushort parent_index, out IntPtr submenu);
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus SetMenuTitleWithCFString (IntPtr menuRef, IntPtr cfstring);
		
		public static void SetMenuTitle (IntPtr menuRef, string title)
		{
			IntPtr str = CoreFoundation.CreateString (title);
			CarbonMenuStatus result = SetMenuTitleWithCFString (menuRef, str);
			CoreFoundation.Release (str);
			CheckResult (result);
		}

		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus SetMenuItemTextWithCFString (IntPtr menuRef, ushort index, IntPtr cfstring);
		
		public static void SetMenuItemText (IntPtr menuRef, ushort index, string title)
		{
			IntPtr str = CoreFoundation.CreateString (title);
			CarbonMenuStatus result = SetMenuItemTextWithCFString (menuRef, index, str);
			CoreFoundation.Release (str);
			CheckResult (result);
		}

		[DllImport (hiToolboxLib)]
		internal static extern CarbonMenuStatus SetMenuItemKeyGlyph (IntPtr menuRef, ushort index, short glyph);

		[DllImport (hiToolboxLib)]
		internal static extern CarbonMenuStatus SetMenuItemCommandKey (IntPtr menuRef, ushort index, bool isVirtualKey, ushort key);

		[DllImport (hiToolboxLib)]
		internal static extern CarbonMenuStatus SetMenuItemModifiers (IntPtr menuRef, ushort index, MenuAccelModifier modifiers);
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus GetMenuItemCommandID (IntPtr menuRef, ushort index, out uint commandId);
		
		public static uint GetMenuItemCommandID (HIMenuItem item)
		{
			uint id;
			CheckResult (GetMenuItemCommandID (item.MenuRef, item.Index, out id));
			return id;
		}
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus GetIndMenuItemWithCommandID (IntPtr startAtMenuRef, uint commandID, uint commandItemIndex, 
		                                                      out IntPtr itemMenuRef, out ushort itemIndex);
		
		public static HIMenuItem GetMenuItem (uint commandId)
		{
			IntPtr itemMenuRef;
			ushort itemIndex;
			CheckResult (GetIndMenuItemWithCommandID (IntPtr.Zero, commandId, 1, out itemMenuRef, out itemIndex));
			return new HIMenuItem (itemMenuRef, itemIndex);
		}
		
		[DllImport (hiToolboxLib)]
		public static extern CarbonMenuStatus CancelMenuTracking (IntPtr rootMenu, bool inImmediate, MenuDismissalReason reason);
		
		[DllImport (hiToolboxLib)]
		public static extern CarbonMenuStatus SetMenuItemData (IntPtr menu, uint indexOrCommandID, bool isCommandID, ref MenuItemData data);
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus SetMenuItemRefCon (IntPtr menuRef, ushort index, uint inRefCon);
		
		public static void SetMenuItemReferenceConstant (HIMenuItem item, uint value)
		{
			CheckResult (SetMenuItemRefCon (item.MenuRef, item.Index, value));
		}
		
		[DllImport (hiToolboxLib)]
		static extern CarbonMenuStatus GetMenuItemRefCon (IntPtr menuRef, ushort index, out uint inRefCon);
		
		public static uint GetMenuItemReferenceConstant (HIMenuItem item)
		{
			uint val;
			CheckResult (GetMenuItemRefCon (item.MenuRef, item.Index, out val));
			return val;
		}
		
		internal static void CheckResult (CarbonMenuStatus result)
		{
			if (result != CarbonMenuStatus.Ok)
				throw new CarbonMenuException (result);
		}
	}
	
	class CarbonMenuException : Exception
	{
		public CarbonMenuException (CarbonMenuStatus result)
		{
			this.Result = result;
		}
		
		public CarbonMenuStatus Result { get; private set; }
		
		public override string ToString ()
		{
			return string.Format("CarbonMenuException: Result={0}\n{1}", Result, StackTrace);
		}
	}
	
	internal enum CarbonMenuStatus // this is an OSStatus
	{ 
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

	internal enum MenuAccelModifier : byte
	{
		CommandModifier = 0,
		ShiftModifier = 1 << 0,
		OptionModifier = 1 << 1,
		ControlModifier = 1 << 2,
		None = 1 << 3
	}
	
	[Flags]
	internal enum MenuItemAttributes : uint
	{
		None = 0,
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
	
	internal enum MenuDismissalReason : uint
	{
		DismissedBySelection   = 1,
		DismissedByUserCancel  = 2,
		DismissedByMouseDown   = 3,
		DismissedByMouseUp     = 4,
		DismissedByKeyEvent    = 5,
		DismissedByAppSwitch   = 6,
		DismissedByTimeout     = 7,
		DismissedByCancelMenuTracking = 8,
		DismissedByActivationChange = 9,
		DismissedByFocusChange = 10,
	}
	
	
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	internal struct MenuItemData
	{
		#pragma warning disable 0169
		MenuItemDataFlags whichData; //8
		IntPtr text; //Str255 //12
		[MarshalAs (UnmanagedType.U2)] //14
		char mark;
		[MarshalAs (UnmanagedType.U2)] //16
		char cmdKey;
		uint cmdKeyGlyph; //20
		uint cmdKeyModifiers; //24
		byte style; //25
		[MarshalAs (UnmanagedType.U1)] //26
		bool enabled;
		[MarshalAs (UnmanagedType.U1)] //27
		bool iconEnabled;
		byte filler1; //28
		int iconID; //32
		uint iconType; //36
		IntPtr iconHandle; //40
		uint cmdID; //44
		CarbonTextEncoding encoding; //48
		ushort submenuID; //50
		IntPtr submenuHandle; //54
		int fontID; //58
		uint refcon; //62
		// LAMESPEC: this field is documented as OptionBits
		MenuItemAttributes attr; //66
		IntPtr cfText; //70
		// Collection 
		IntPtr properties; //74
		uint indent; //78
		ushort cmdVirtualKey; //80
		
		//these aren't documented
		IntPtr attributedText; //84
		IntPtr font; //88
		#pragma warning restore 0169
		
		#region Properties
		
		public IntPtr Text {
			get { return text; }
			set {
				whichData |= MenuItemDataFlags.Text;
				text = value;
			}
		}
		
		public char Mark {
			get { return mark; }
			set {
				whichData |= MenuItemDataFlags.Mark;
				mark = value;
			}
		}
		
		public char CommandKey {
			get { return cmdKey; }
			set {
				whichData |= MenuItemDataFlags.CmdKey;
				cmdKey = value;
			}
		}
		
		public uint CommandKeyGlyph {
			get { return cmdKeyGlyph; }
			set {
				whichData |= MenuItemDataFlags.CmdKeyGlyph;
				cmdKeyGlyph = value;
			}
		}
		
		public MenuAccelModifier CommandKeyModifiers {
			get { return (MenuAccelModifier) cmdKeyModifiers; }
			set {
				whichData |= MenuItemDataFlags.CmdKeyModifiers;
				cmdKeyModifiers = (uint) value;
			}
		}
		
		public byte Style {
			get { return style; }
			set {
				whichData |= MenuItemDataFlags.Style;
				style = value;
			}
		}
		
		public bool Enabled {
			get { return enabled; }
			set {
				whichData |= MenuItemDataFlags.Enabled;
				enabled = value;
			}
		}
		
		public bool IconEnabled {
			get { return iconEnabled; }
			set {
				whichData |= MenuItemDataFlags.IconEnabled;
				iconEnabled = value;
			}
		}
		
		public int IconID {
			get { return iconID; }
			set {
				whichData |= MenuItemDataFlags.IconID;
				iconID = value;
			}
		}
		
		public HIIconHandle IconHandle {
			get { return new HIIconHandle (iconHandle, iconType); }
			set {
				whichData |= MenuItemDataFlags.IconHandle;
				iconHandle = value.Ref;
				iconType = value.Type;
			}
		}
		
		public uint CommandID {
			get { return cmdID; }
			set {
				whichData |= MenuItemDataFlags.CommandID;
				cmdID = value;
			}
		}
		
		public CarbonTextEncoding Encoding {
			get { return encoding; }
			set {
				whichData |= MenuItemDataFlags.TextEncoding;
				encoding = value;
			}
		}
		
		public ushort SubmenuID {
			get { return submenuID; }
			set {
				whichData |= MenuItemDataFlags.SubmenuID;
				submenuID = value;
			}
		}
		
		public IntPtr SubmenuHandle {
			get { return submenuHandle; }
			set {
				whichData |= MenuItemDataFlags.SubmenuHandle;
				submenuHandle = value;
			}
		}
		
		public int FontID {
			get { return fontID; }
			set {
				whichData |= MenuItemDataFlags.FontID;
				fontID = value;
			}
		}
		
		public uint ReferenceConstant {
			get { return refcon; }
			set {
				whichData |= MenuItemDataFlags.Refcon;
				refcon = value;
			}
		}
		
		public MenuItemAttributes Attributes {
			get { return attr; }
			set {
				whichData |= MenuItemDataFlags.Attributes;
				attr = value;
			}
		}
		
		public IntPtr CFText {
			get { return cfText; }
			set {
				whichData |= MenuItemDataFlags.CFString;
				cfText = value;
			}
		}
		
		public IntPtr Properties {
			get { return properties; }
			set {
				whichData |= MenuItemDataFlags.Properties;
				properties = value;
			}
		}
		
		public uint Indent {
			get { return indent; }
			set {
				whichData |= MenuItemDataFlags.Indent;
				indent = value;
			}
		}
		
		public ushort CommandVirtualKey {
			get { return cmdVirtualKey; }
			set {
				whichData |= MenuItemDataFlags.CmdVirtualKey;
				cmdVirtualKey = value;
			}
		}
		
		#endregion
		
		#region 'Has' properties
		
		public bool HasText {
			get { return (whichData & MenuItemDataFlags.Text) != 0; }
		}
		
		public bool HasMark {
			get { return (whichData & MenuItemDataFlags.Mark) != 0; }
		}
		
		public bool HasCommandKey {
			get { return (whichData & MenuItemDataFlags.CmdKey) != 0; }
		}
		
		public bool HasCommandKeyGlyph {
			get { return (whichData & MenuItemDataFlags.CmdKeyGlyph) != 0; }
		}
		
		public bool HasCommandKeyModifiers {
			get { return (whichData & MenuItemDataFlags.CmdKeyModifiers) != 0; }
		}
		
		public bool HasStyle {
			get { return (whichData & MenuItemDataFlags.Style) != 0; }
		}
		
		public bool HasEnabled {
			get { return (whichData & MenuItemDataFlags.Enabled) != 0; }
		}
		
		public bool HasIconEnabled {
			get { return (whichData & MenuItemDataFlags.IconEnabled) != 0; }
		}
		
		public bool HasIconID {
			get { return (whichData & MenuItemDataFlags.IconID) != 0; }
		}
		
		public bool HasIconHandle {
			get { return (whichData & MenuItemDataFlags.IconHandle) != 0; }
		}
		
		public bool HasCommandID {
			get { return (whichData & MenuItemDataFlags.CommandID) != 0; }
		}
		
		public bool HasEncoding {
			get { return (whichData & MenuItemDataFlags.TextEncoding) != 0; }
		}
		
		public bool HasSubmenuID {
			get { return (whichData & MenuItemDataFlags.SubmenuID) != 0; }
		}
		
		public bool HasSubmenuHandle {
			get { return (whichData & MenuItemDataFlags.SubmenuHandle) != 0; }
		}
		
		public bool HasFontID {
			get { return (whichData & MenuItemDataFlags.FontID) != 0; }
		}
		
		public bool HasRefcon {
			get { return (whichData & MenuItemDataFlags.Refcon) != 0; }
		}
		
		public bool HasAttributes {
			get { return (whichData & MenuItemDataFlags.Attributes) != 0; }
		}
		
		public bool HasCFText {
			get { return (whichData & MenuItemDataFlags.CFString) != 0; }
		}
		
		public bool HasProperties {
			get { return (whichData & MenuItemDataFlags.Properties) != 0; }
		}
		
		public bool HasIndent {
			get { return (whichData & MenuItemDataFlags.Indent) != 0; }
		}
		
		public bool HasCommandVirtualKey {
			get { return (whichData & MenuItemDataFlags.CmdVirtualKey) != 0; }
		}
		
		#endregion
	}
	
	struct HIIconHandle
	{
		IntPtr _ref;
		uint type;
		
		public HIIconHandle (IntPtr @ref, uint type)
		{
			this._ref = @ref;
			this.type = type;
		}
		
		public IntPtr Ref { get { return _ref; } }
		public uint Type { get { return type; } }
	}
	
	enum CarbonTextEncoding : uint
	{
	}
	
	enum MenuItemDataFlags : ulong
	{
		Text = (1 << 0),
		Mark = (1 << 1),
		CmdKey = (1 << 2),
		CmdKeyGlyph = (1 << 3),
		CmdKeyModifiers = (1 << 4),
		Style = (1 << 5),
		Enabled = (1 << 6),
		IconEnabled = (1 << 7),
		IconID = (1 << 8),
		IconHandle = (1 << 9),
		CommandID = (1 << 10),
		TextEncoding = (1 << 11),
		SubmenuID = (1 << 12),
		SubmenuHandle = (1 << 13),
		FontID = (1 << 14),
		Refcon = (1 << 15),
		Attributes = (1 << 16),
		CFString = (1 << 17),
		Properties = (1 << 18),
		Indent = (1 << 19),
		CmdVirtualKey = (1 << 20),
		AllDataVersionOne = 0x000FFFFF,
		AllDataVersionTwo = AllDataVersionOne | CmdVirtualKey,
	}
	
	enum MenuGlyphs : byte //char
	{
		None = 0x00,
		TabRight = 0x02,
		TabLeft = 0x03,
		Enter = 0x04,
		Shift = 0x05,
		Control = 0x06,
		Option = 0x07,
		Space = 0x09,
		DeleteRight = 0x0A,
		Return = 0x0B,
		ReturnR2L = 0x0C,
		NonmarkingReturn = 0x0D,
		Pencil = 0x0F,
		DownwardArrowDashed = 0x10,
		Command = 0x11,
		Checkmark = 0x12,
		Diamond = 0x13,
		AppleLogoFilled = 0x14,
		ParagraphKorean = 0x15,
		DeleteLeft = 0x17,
		LeftArrowDashed = 0x18,
		UpArrowDashed = 0x19,
		RightArrowDashed = 0x1A,
		Escape = 0x1B,
		Clear = 0x1C,
		LeftDoubleQuotesJapanese = 0x1D,
		RightDoubleQuotesJapanese = 0x1E,
		TrademarkJapanese = 0x1F,
		Blank = 0x61,
		PageUp = 0x62,
		CapsLock = 0x63,
		LeftArrow = 0x64,
		RightArrow = 0x65,
		NorthwestArrow = 0x66,
		Help = 0x67,
		UpArrow = 0x68,
		SoutheastArrow = 0x69,
		DownArrow = 0x6A,
		PageDown = 0x6B,
		AppleLogoOutline = 0x6C,
		ContextualMenu = 0x6D,
		Power = 0x6E,
		F1 = 0x6F,
		F2 = 0x70,
		F3 = 0x71,
		F4 = 0x72,
		F5 = 0x73,
		F6 = 0x74,
		F7 = 0x75,
		F8 = 0x76,
		F9 = 0x77,
		F10 = 0x78,
		F11 = 0x79,
		F12 = 0x7A,
		F13 = 0x87,
		F14 = 0x88,
		F15 = 0x89,
		ControlISO = 0x8A,
		Eject = 0x8C
	};
}
