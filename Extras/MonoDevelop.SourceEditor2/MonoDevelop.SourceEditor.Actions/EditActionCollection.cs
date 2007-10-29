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
			// some problematic ones have to be filtered
			Gdk.ModifierType filteredState = state & ~(Gdk.ModifierType.LockMask | Gdk.ModifierType.Mod2Mask);
			foreach (IEditAction action in actions)
			{
				if (action.State == filteredState && action.Key == key)
					return action;
			}

			return null;
		}
	}
}

