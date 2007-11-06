//  EditActionCodon.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.Reflection;
using System.ComponentModel;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.SourceEditor.Actions;

namespace MonoDevelop.SourceEditor.Codons
{
	[ExtensionNode (Description="A custom editor action. The provided class must implement IEditAction.")]
	public class EditActionCodon : TypeExtensionNode
	{
		[NodeAttribute("keys", true, "Key combinations that trigger the edit action (for example Control|k).")]
		string[] keys = null;

		public string[] Keys {
			get { return keys; }
			set { keys = value; }
		}
		
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object CreateInstance ()
		{
			IEditAction editAction = (IEditAction) base.CreateInstance ();
			if (editAction == null)
				return null;
							
			// basically, we want to take a string like:
			// Control|J and turn it into the value to trigger the edit action
			// in GTK+ lingo that is a Gdk.Key and Gdk.ModifierType
			// we assume the order is Modifier|Modifier|Key
			Gdk.Key key = Gdk.Key.VoidSymbol;
			Gdk.ModifierType state = Gdk.ModifierType.None;
			for (int i = 0; i < keys.Length; i++) {
				string[] keydescr = keys[i].Split ('|');

				// the last keydescr is the Gdk.Key
				key = (Gdk.Key) Enum.Parse (typeof (Gdk.Key), keydescr[keydescr.Length - 1]);

				// the rest, if any, are modifiers
				for (int j = 0; j < keydescr.Length - 1; j++) {
					// FIXME: newer gdk's have more values here
					switch (keydescr[j]) {
						// ignore the buttons
						case "Button1":
						case "Button2":
						case "Button3":
						case "Button4":
						case "Button5":
							break;
						case "Control":
							state |= Gdk.ModifierType.ControlMask;
							break;
						// Caps Lock or Shift Lock
						case "Lock":
							state |= Gdk.ModifierType.LockMask;
							break;
						// this is normally Alt
						case "Alt":
						case "Mod1":
							state |= Gdk.ModifierType.Mod1Mask;
							break;
						case "Mod2":
							state |= Gdk.ModifierType.Mod2Mask;
							break;
						case "Mod3":
							state |= Gdk.ModifierType.Mod3Mask;
							break;
						case "Mod4":
							state |= Gdk.ModifierType.Mod4Mask;
							break;
						case "Mod5":
							state |= Gdk.ModifierType.Mod5Mask;
							break;
						// all the modifiers
						case "Modifier":
							state |= Gdk.ModifierType.ModifierMask;
							break;
						// ignore internal to GTK+
						case "Release":
							break;
						case "Shift":
							state |= Gdk.ModifierType.ShiftMask;
							break;
						default:
							break;
					}
				}
			}
			
			editAction.Key = key;
			editAction.State = state;
			
			return editAction;
		}
	}
}
