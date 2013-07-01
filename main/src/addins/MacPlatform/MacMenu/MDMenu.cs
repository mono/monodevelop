//
// MDMenu.cs
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

using System;
using System.Linq;
using MonoMac.AppKit;
using MonoDevelop.Components.Commands;
using MonoMac.Foundation;
using System.Diagnostics;
using MonoDevelop.Core;

namespace MonoDevelop.MacIntegration.MacMenu
{
	class MDMenu : NSMenu
	{
		static readonly string servicesID = (string) CommandManager.ToCommandId (MacIntegrationCommands.Services);

		public MDMenu (CommandManager manager, CommandEntrySet ces)
		{
			this.WeakDelegate = this;

			AutoEnablesItems = false;

			Title = (ces.Name ?? "").Replace ("_", "");

			foreach (CommandEntry ce in ces) {
				if (ce.CommandId == Command.Separator) {
					AddItem (NSMenuItem.SeparatorItem);
					continue;
				}

				if (string.Equals (ce.CommandId as string, servicesID, StringComparison.Ordinal)) {
					AddItem (new MDServicesMenuItem ());
					continue;
				}

				var subset = ce as CommandEntrySet;
				if (subset != null) {
					AddItem (new MDSubMenuItem (manager, subset));
					continue;
				}

				var lce = ce as LinkCommandEntry;
				if (lce != null) {
					AddItem (new MDLinkMenuItem (lce));
					continue;
				}

				Command cmd = manager.GetCommand (ce.CommandId);
				if (cmd == null) {
					LoggingService.LogError ("MacMenu: '{0}' maps to null command", ce.CommandId);
					continue;
				}

				if (cmd is CustomCommand) {
					LoggingService.LogWarning ("MacMenu: '{0}' is unsupported custom-rendered command' '", ce.CommandId);
					continue;
				}

				var acmd = cmd as ActionCommand;
				if (acmd == null) {
					LoggingService.LogWarning ("MacMenu: '{0}' has unknown command type '{1}'", cmd.GetType (), ce.CommandId);
					continue;
				}

				AddItem (new MDMenuItem (manager, ce, acmd));
			}
		}

		// http://lists.apple.com/archives/cocoa-dev/2008/Apr/msg01696.html
		void FlashMenu ()
		{
			var f35 = ((char)0xF726).ToString ();
			var blink = new NSMenuItem ("* blink *") {
				KeyEquivalent = f35,
			};
			var f35Event = NSEvent.KeyEvent (
				NSEventType.KeyDown, System.Drawing.PointF.Empty, NSEventModifierMask.CommandKeyMask, 0, 0,
				NSGraphicsContext.CurrentContext, f35, f35, false, 0);
			AddItem (blink);
			PerformKeyEquivalent (f35Event);
			RemoveItem (blink);
		}

		public bool FlashIfContainsCommand (object command)
		{
			foreach (var item in ItemArray ().OfType<MDMenuItem> ()) {
				if (item.CommandEntry.CommandId == command) {
					FlashMenu ();
					return true;
				}
				var submenu = item.Submenu as MDMenu;
				if (submenu != null && submenu.FlashIfContainsCommand (command))
					return true;
			}
			return false;
		}

		public void UpdateCommands ()
		{
			NSMenuItem lastSeparator = NSMenuItem.SeparatorItem;

			for (int i = 0; i < Count; i++) {
				var item = this.ItemAt (i);

				if (item.IsSeparatorItem) {
					if (lastSeparator == null) {
						lastSeparator = item;
					}
					item.Hidden = true;
					continue;
				}

				var mdItem = item as IUpdatableMenuItem;
				if (mdItem != null) {
					mdItem.Update (this, ref lastSeparator, ref i);
					continue;
				}

				//hide unknown builtins
				item.Hidden = true;
			}
		}

		[ExportAttribute ("menuNeedsUpdate:")]
		void MenuNeedsUpdate (NSMenu menu)
		{
			Debug.Assert (menu == this);

			// MacOS calls this for each menu when it's about to open, but also for every menu on every keystroke.
			// We only want to do the update when the menu's about to open, since it's expensive. Checking whether
			// NSMenuProperty.Image needs to be updated is the only way to distinguish between these cases.
			//
			// http://www.cocoabuilder.com/archive/cocoa/285859-reason-for-menuneedsupdate-notification.html
			//
			if (PropertiesToUpdate ().HasFlag (NSMenuProperty.Image))
				UpdateCommands ();
		}

		public static void ShowLastSeparator (ref NSMenuItem lastSeparator)
		{
			if (lastSeparator != null) {
				lastSeparator.Hidden = false;
				lastSeparator = null;
			}
		}
	}

	interface IUpdatableMenuItem
	{
		void Update (MDMenu parent, ref NSMenuItem lastSeparator, ref int index);
	}
}
