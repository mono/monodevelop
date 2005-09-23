// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Reflection;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.AddIns.Codons;

using MonoDevelop.Gui.Components;
using MonoDevelop.TextEditor.Actions;

namespace MonoDevelop.DefaultEditor.Codons
{
	[CodonNameAttribute("EditAction")]
	public class EditActionCodon : AbstractCodon
	{
		[XmlMemberArrayAttribute("keys", IsRequired=true)]
		string[] keys = null;
		
		public string[] Keys {
			get {
				return keys;
			}
			set {
				keys = value;
			}
		}
		
		/// <summary>
		/// Creates an item with the specified sub items. And the current
		/// Condition status for this item.
		/// </summary>
		public override object BuildItem (object owner, ArrayList subItems, ConditionCollection conditions)
		{
			//FIXME: This code is *not* fully ported yet
			if (subItems.Count > 0) {
				throw new ApplicationException ("more than one level of edit actions don't make sense!");
			}
			
			IEditAction editAction = (IEditAction) AddIn.CreateObject (Class);
							
			Gdk.Key[] actionKeys = new Gdk.Key[keys.Length];
			for (int j = 0; j < keys.Length; ++j) {
				string[] keydescr = keys[j].Split (new char[] { '|' });
				//Keys key = (Keys)((System.Windows.Forms.Keys.Space.GetType()).InvokeMember(keydescr[0], BindingFlags.GetField, null, System.Windows.Forms.Keys.Space, new object[0]));
				//Console.Write (keydescr[0] + " -- ");
				for (int k = 1; k < keydescr.Length; ++k) {
					//key |= (Keys)((System.Windows.Forms.Keys.Space.GetType()).InvokeMember(keydescr[k], BindingFlags.GetField, null, System.Windows.Forms.Keys.Space, new object[0]));
					//Console.Write (keydescr[k] + " -- ");
				}
				//actionKeys[j] = key;
			}
			
			editAction.Keys = actionKeys;
			
			return editAction;
		}
	}
}
