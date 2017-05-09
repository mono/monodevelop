//
// MDMenuItem.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using AppKit;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using System.Text;
using Foundation;
using ObjCRuntime;
using System.Collections.Generic;

namespace MonoDevelop.Components.Mac
{
	//TODO: autohide, arrray
	class MDMenuItem : NSMenuItem, IUpdatableMenuItem
	{
		public const string ActionSelName = "run:";
		public static Selector ActionSel = new Selector (ActionSelName);

		CommandEntry ce;
		CommandManager manager;

		bool isArrayItem;
		object initialCommandTarget;
		CommandSource commandSource;
		CommandInfo lastInfo;

		public MDMenuItem (CommandManager manager, CommandEntry ce, ActionCommand command, CommandSource commandSource, object initialCommandTarget)
		{
			this.ce = ce;
			this.manager = manager;
			this.initialCommandTarget = initialCommandTarget;
			this.commandSource = commandSource;

			isArrayItem = command.CommandArray;

			Target = this;
			Action = ActionSel;
		}

		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
		}

		public CommandEntry CommandEntry { get { return ce; } }

		[Export (ActionSelName)]
		public void Run (NSMenuItem sender)
		{
			var a = sender as MDExpandedArrayItem;
			//if the command opens a modal subloop, give cocoa a chance to unhighlight the menu item
			GLib.Timeout.Add (1, () => {
				if (a != null) {
					manager.DispatchCommand (ce.CommandId, a.Info.DataItem, initialCommandTarget, commandSource, lastInfo);
				} else {
					manager.DispatchCommand (ce.CommandId, null, initialCommandTarget, commandSource, lastInfo);
				}
				return false;
			});
		}

		//NOTE: This is used to disable the whole menu when there's a modal dialog.
		// We can justify this because safari 3.2.1 does it ("do you want to close all tabs?").
		public static bool IsGloballyDisabled {
			get {
				return MonoDevelop.Ide.DesktopService.IsModalDialogRunning ();
			}
		}

		int FindMeInParent (MDMenu parent)
		{
			for (int n = 0; n < parent.Count; n++)
				if (parent.ItemAt (n) == this)
					return n;
			return -1;
		}

		public void Update (MDMenu parent, ref int index)
		{
			var info = manager.GetCommandInfo (ce.CommandId, new CommandTargetRoute (initialCommandTarget));
			if (lastInfo != info) {
				if (lastInfo != null)
					lastInfo.CancelAsyncUpdate ();
				lastInfo = info;
				if (lastInfo.IsUpdatingAsynchronously) {
					lastInfo.Changed += delegate {
						var ind = FindMeInParent (parent);
						if (info == lastInfo) {
							Update (parent, ref ind, info);
							parent.UpdateSeparators ();
						}
					};
				}
			}
			Update (parent, ref index, info);
		}

		void Update (MDMenu parent, ref int index, CommandInfo info)
		{
			if (!isArrayItem) {
				SetItemValues (this, info, ce.DisabledVisible, ce.OverrideLabel);
				return;
			}

			Hidden = true;

			if (index < parent.Count - 1) {
				for (int i = index + 1; i < parent.Count; i++) {
					var nextItem = parent.ItemAt (i);
					if (nextItem == null || nextItem.Target != this)
						break;
					parent.RemoveItemAt (i);
					i--;
				}
			}

			index++;
			PopulateArrayItems (info.ArrayInfo, parent, ref index);
		}

		void PopulateArrayItems (CommandArrayInfo infos, NSMenu parent, ref int index)
		{
			if (infos == null)
				return;

			foreach (CommandInfo ci in infos) {
				if (ci.IsArraySeparator) {
					var n = NSMenuItem.SeparatorItem;
					n.Hidden = true;
					n.Target = this;
					if (parent.Count > index)
						parent.InsertItem (n, index);
					else
						parent.AddItem (n);
					index++;
					continue;
				}

				var item = new MDExpandedArrayItem {
					Info = ci,
					Target = this,
					Action = ActionSel,
				};

				if (ci is CommandInfoSet) {
					item.Submenu = new NSMenu ();
					int i = 0;
					PopulateArrayItems (((CommandInfoSet)ci).CommandInfos, item.Submenu, ref i);
				}
				SetItemValues (item, ci, true);

				if (parent.Count > index)
					parent.InsertItem (item, index);
				else
					parent.AddItem (item);
				index++;
			}
			index--;
		}

		class MDExpandedArrayItem : NSMenuItem
		{
			public CommandInfo Info;
		}

		void SetItemValues (NSMenuItem item, CommandInfo info, bool disabledVisible, string overrideLabel = null)
		{
			item.SetTitleWithMnemonic (GetCleanCommandText (info, overrideLabel));
			if (!string.IsNullOrEmpty (info.Description) && item.ToolTip != info.Description)
				item.ToolTip = info.Description;

			bool enabled = info.Enabled && (!IsGloballyDisabled || commandSource == CommandSource.ContextMenu);
			bool visible = info.Visible && (disabledVisible || info.Enabled);

			item.Enabled = enabled;
			item.Hidden = !visible;

			SetAccel (item, info.AccelKey);

			if (info.Checked) {
				item.State = NSCellStateValue.On;
			} else if (info.CheckedInconsistent) {
				item.State = NSCellStateValue.Mixed;
			} else {
				item.State = NSCellStateValue.Off;
			}
		}

		static void SetAccel (NSMenuItem item, string accelKey)
		{
			uint modeKey;
			Gdk.ModifierType modeMod;
			uint key;
			Gdk.ModifierType mod;

			if (!KeyBindingManager.BindingToKeys (accelKey, out modeKey, out modeMod, out key, out mod)) {
				item.KeyEquivalent = "";
				item.KeyEquivalentModifierMask = (NSEventModifierMask) 0;
				return;
			}

			if (modeKey != 0) {
				LoggingService.LogWarning ("Mac menu cannot display accelerators with mode keys ({0})", accelKey);
				item.KeyEquivalent = "";
				item.KeyEquivalentModifierMask = (NSEventModifierMask) 0;
				return;
			}

			var keyEq = GetKeyEquivalent ((Gdk.Key) key);
			item.KeyEquivalent = keyEq;
			if (keyEq.Length == 0) {
				item.KeyEquivalentModifierMask = 0;
			}

			NSEventModifierMask outMod = 0;
			if ((mod & Gdk.ModifierType.Mod1Mask) != 0) {
				outMod |= NSEventModifierMask.AlternateKeyMask;
				mod ^= Gdk.ModifierType.Mod1Mask;
			}
			if ((mod & Gdk.ModifierType.ShiftMask) != 0) {
				outMod |= NSEventModifierMask.ShiftKeyMask;
				mod ^= Gdk.ModifierType.ShiftMask;
			}
			if ((mod & Gdk.ModifierType.ControlMask) != 0) {
				outMod |= NSEventModifierMask.ControlKeyMask;
				mod ^= Gdk.ModifierType.ControlMask;
			}
			if ((mod & Gdk.ModifierType.MetaMask) != 0) {
				outMod |= NSEventModifierMask.CommandKeyMask;
				mod ^= Gdk.ModifierType.MetaMask;
			}

			if (mod != 0) {
				LoggingService.LogWarning ("Mac menu cannot display accelerators with modifiers {0}", mod);
			}
			item.KeyEquivalentModifierMask = outMod;
		}

		static string GetKeyEquivalent (Gdk.Key key)
		{
			// Gdk.Keyval.ToUnicode returns NULL for TAB, fix it
			if (key == Gdk.Key.Tab)
				return "\t";
			
			char c = (char) Gdk.Keyval.ToUnicode ((uint) key);
			if (c != 0)
				return c.ToString ();

			var fk = GetFunctionKey (key);
			if (fk != 0)
				return ((char) fk).ToString ();

			LoggingService.LogError ("Mac menu cannot display key '{0}'", key);
			return "";
		}

		static string GetCleanCommandText (CommandInfo ci, string overrideLabel = null)
		{
			string txt = overrideLabel ?? ci.Text;
			if (txt == null)
				return "";

			bool isSpecial = ContextMenuItem.ContainsSpecialMnemonics;
			//FIXME: markup stripping could be done better
			var sb = new StringBuilder ();
			for (int i = 0; i < txt.Length; i++) {
				char ch = txt[i];

				if (isSpecial && ch == '(') {
					if (i + 3 < txt.Length && txt [i + 1] == '_' && txt [i + 3] == ')') {
						i += 3;
					}
				} else if (ch == '_') {
					if (i + 1 < txt.Length && txt[i + 1] == '_') {
						sb.Append ('_');
						i++;
					} else {
						sb.Append ('&');
					}
				} else if (!ci.UseMarkup) {
					sb.Append (ch);
				} else if (ch == '<') {
					while (++i < txt.Length && txt[i] != '>');
				} else if (ch == '&') {
					int j = i;
					while (++i < txt.Length && txt[i] != ';');
					int len = i - j - 1;
					if (len > 0) {
						string entityName = txt.Substring (j + 1, i - j - 1);
						switch (entityName) {
							case "quot":
							sb.Append ('"');
							break;
							case "amp":
							sb.Append ('&');
							break;
							case "apos":
							sb.Append ('\'');
							break;
							case "lt":
							sb.Append ('<');
							break;
							case "gt":
							sb.Append ('>');
							break;
							default:
							LoggingService.LogWarning ("Could not de-markup entity '{0}'", entityName);
							break;
						}
					}
				} else {
					sb.Append (ch);
				}
			}

			return sb.ToString ();
		}

		static FunctionKey GetFunctionKey (Gdk.Key key)
		{
			switch (key) {
			case Gdk.Key.Return:
				return (FunctionKey) (uint) '\n';
			case Gdk.Key.BackSpace:
				return (FunctionKey) 0x08;
				// NSBackspaceCharacter
			case Gdk.Key.KP_Delete:
			case Gdk.Key.Delete:
				return (FunctionKey) 0x7F;
				// NSDeleteCharacter
			case Gdk.Key.KP_Up:
			case Gdk.Key.Up:
				return FunctionKey.UpArrow;
			case Gdk.Key.KP_Down:
			case Gdk.Key.Down:
				return FunctionKey.DownArrow;
			case Gdk.Key.KP_Left:
			case Gdk.Key.Left:
				return FunctionKey.LeftArrow;
			case Gdk.Key.KP_Right:
			case Gdk.Key.Right:
				return FunctionKey.RightArrow;
			case Gdk.Key.F1:
				return FunctionKey.F1;
			case Gdk.Key.F2:
				return FunctionKey.F2;
			case Gdk.Key.F3:
				return FunctionKey.F3;
			case Gdk.Key.F4:
				return FunctionKey.F4;
			case Gdk.Key.F5:
				return FunctionKey.F5;
			case Gdk.Key.F6:
				return FunctionKey.F6;
			case Gdk.Key.F7:
				return FunctionKey.F7;
			case Gdk.Key.F8:
				return FunctionKey.F8;
			case Gdk.Key.F9:
				return FunctionKey.F9;
			case Gdk.Key.F10:
				return FunctionKey.F10;
			case Gdk.Key.F11:
				return FunctionKey.F11;
			case Gdk.Key.F12:
				return FunctionKey.F12;
			case Gdk.Key.F13:
				return FunctionKey.F13;
			case Gdk.Key.F14:
				return FunctionKey.F14;
			case Gdk.Key.F15:
				return FunctionKey.F15;
			case Gdk.Key.F16:
				return FunctionKey.F16;
			case Gdk.Key.F17:
				return FunctionKey.F17;
			case Gdk.Key.F18:
				return FunctionKey.F18;
			case Gdk.Key.F19:
				return FunctionKey.F19;
			case Gdk.Key.F20:
				return FunctionKey.F20;
			case Gdk.Key.F21:
				return FunctionKey.F21;
			case Gdk.Key.F22:
				return FunctionKey.F22;
			case Gdk.Key.F23:
				return FunctionKey.F23;
			case Gdk.Key.F24:
				return FunctionKey.F24;
			case Gdk.Key.F25:
				return FunctionKey.F25;
			case Gdk.Key.F26:
				return FunctionKey.F26;
			case Gdk.Key.F27:
				return FunctionKey.F27;
			case Gdk.Key.F28:
				return FunctionKey.F28;
			case Gdk.Key.F29:
				return FunctionKey.F29;
			case Gdk.Key.F30:
				return FunctionKey.F30;
			case Gdk.Key.F31:
				return FunctionKey.F31;
			case Gdk.Key.F32:
				return FunctionKey.F32;
			case Gdk.Key.F33:
				return FunctionKey.F33;
			case Gdk.Key.F34:
				return FunctionKey.F34;
			case Gdk.Key.F35:
				return FunctionKey.F35;
			case Gdk.Key.KP_Insert:
			case Gdk.Key.Insert:
				return FunctionKey.Insert;
			case Gdk.Key.KP_Home:
			case Gdk.Key.Home:
				return FunctionKey.Home;
			case Gdk.Key.Begin:
				return FunctionKey.Begin;
			case Gdk.Key.KP_End:
			case Gdk.Key.End:
				return FunctionKey.End;
			case Gdk.Key.KP_Page_Up:
			case Gdk.Key.Page_Up:
				return FunctionKey.PageUp;
			case Gdk.Key.KP_Page_Down:
			case Gdk.Key.Page_Down:
				return FunctionKey.PageDown;
			case Gdk.Key.Key_3270_PrintScreen:
				return FunctionKey.PrintScreen;
			case Gdk.Key.Scroll_Lock:
				return FunctionKey.ScrollLock;
			case Gdk.Key.Pause:
				return FunctionKey.Pause;
			case Gdk.Key.Sys_Req:
				return FunctionKey.SysReq;
			case Gdk.Key.Break:
				return FunctionKey.Break;
			case Gdk.Key.Key_3270_Reset:
				return FunctionKey.Reset;
			case Gdk.Key.Menu:
				return FunctionKey.Menu;
			case Gdk.Key.Print:
				return FunctionKey.Print;
			case Gdk.Key.Help:
				return FunctionKey.Help;
			case Gdk.Key.Find:
				return FunctionKey.Find;
			case Gdk.Key.Undo:
				return FunctionKey.Undo;
			case Gdk.Key.Redo:
				return FunctionKey.Redo;
			case Gdk.Key.Execute:
				return FunctionKey.Execute;
				/*
				return FunctionKey.Stop;
				return FunctionKey.User;
				return FunctionKey.System;
				return FunctionKey.ClearLine;
				return FunctionKey.ClearDisplay;
				return FunctionKey.InsertLine;
				return FunctionKey.DeleteLine;
				return FunctionKey.InsertChar;
				return FunctionKey.DeleteChar;
				return FunctionKey.Next;
				return FunctionKey.Prev;
				return FunctionKey.Select;
				return FunctionKey.ModeSwitch;
				*/
			}

			return 0;
		}

		// "Function-Key Unicodes" from NSEvent reference
		enum FunctionKey : ushort
		{
			UpArrow = 0xF700,
			DownArrow = 0xF701,
			LeftArrow = 0xF702,
			RightArrow = 0xF703,
			F1 = 0xF704,
			F2 = 0xF705,
			F3 = 0xF706,
			F4 = 0xF707,
			F5 = 0xF708,
			F6 = 0xF709,
			F7 = 0xF70A,
			F8 = 0xF70B,
			F9 = 0xF70C,
			F10 = 0xF70D,
			F11 = 0xF70E,
			F12 = 0xF70F,
			F13 = 0xF710,
			F14 = 0xF711,
			F15 = 0xF712,
			F16 = 0xF713,
			F17 = 0xF714,
			F18 = 0xF715,
			F19 = 0xF716,
			F20 = 0xF717,
			F21 = 0xF718,
			F22 = 0xF719,
			F23 = 0xF71A,
			F24 = 0xF71B,
			F25 = 0xF71C,
			F26 = 0xF71D,
			F27 = 0xF71E,
			F28 = 0xF71F,
			F29 = 0xF720,
			F30 = 0xF721,
			F31 = 0xF722,
			F32 = 0xF723,
			F33 = 0xF724,
			F34 = 0xF725,
			F35 = 0xF726,
			Insert = 0xF727,
			Delete = 0xF728,
			Home = 0xF729,
			Begin = 0xF72A,
			End = 0xF72B,
			PageUp = 0xF72C,
			PageDown = 0xF72D,
			PrintScreen = 0xF72E,
			ScrollLock = 0xF72F,
			Pause = 0xF730,
			SysReq = 0xF731,
			Break = 0xF732,
			Reset = 0xF733,
			Stop = 0xF734,
			Menu = 0xF735,
			User = 0xF736,
			System = 0xF737,
			Print = 0xF738,
			ClearLine = 0xF739,
			ClearDisplay = 0xF73A,
			InsertLine = 0xF73B,
			DeleteLine = 0xF73C,
			InsertChar = 0xF73D,
			DeleteChar = 0xF73E,
			Prev = 0xF73F,
			Next = 0xF740,
			Select = 0xF741,
			Execute = 0xF742,
			Undo = 0xF743,
			Redo = 0xF744,
			Find = 0xF745,
			Help = 0xF746,
			ModeSwitch = 0xF747
		}
	}
}
#endif