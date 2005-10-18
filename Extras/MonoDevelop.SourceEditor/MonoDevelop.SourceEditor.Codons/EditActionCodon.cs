// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.SourceEditor.Actions;

namespace MonoDevelop.SourceEditor.Codons
{
	[CodonNameAttribute("EditAction")]
	public class EditActionCodon : AbstractCodon
	{
		[XmlMemberArrayAttribute("keys", IsRequired=true)]
		string[] keys = null;

		public string[] Keys {
			get { return keys; }
			set { keys = value; }
		}
		
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object BuildItem (object owner, ArrayList subItems, ConditionCollection conditions)
		{
			if (subItems.Count > 0)
				throw new ApplicationException ("more than one level of edit actions don't make sense!");
			
			IEditAction editAction = (IEditAction) AddIn.CreateObject (Class);
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
					switch (keydescr[j]) {
						case "Control":
							state |= Gdk.ModifierType.ControlMask;
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
