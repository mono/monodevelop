using System;
using System.Collections;
using MonoDevelop.SourceEditor.Gui;

namespace MonoDevelop.SourceEditor.Actions
{
	public sealed class EditActionCollection //: IEnumerable
	{
		ArrayList actions = new ArrayList ();

		public void Add (IEditAction action)
		{
			actions.Add (action);
		}
		
		/* requires C# 2.0 for iterators
		public IEnumerator GetEnumerator ()
		{
			foreach (IEditAction action in actions)
				yield return action;
		}
		*/

		public IEditAction GetAction (Gdk.Key key, Gdk.ModifierType state)
		{
			// an exact match
			foreach (IEditAction action in actions)
			{
				if (action.State == state && action.Key == key)
					return action;
			}

			// try an inexact match
			// this is needed so LockMask, or some other
			// wierd modifier in addition doesn't cause us to miss
			// ex. cntrl+space = ControlMask | Mod2Mask + space
			foreach (IEditAction action in actions)
			{
				if ((action.State | state) != 0 && action.Key == key)
					return action;
			}
			return null;
		}
	}
}

