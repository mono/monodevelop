
using System;
using System.Collections;
using System.CodeDom;

namespace Stetic.Wrapper
{
	public class RadioActionGroupManager: IRadioGroupManager
	{
		public event GroupsChangedDelegate GroupsChanged;
		Hashtable actions = new Hashtable ();
		ArrayList groups = new ArrayList ();
		
		public IEnumerable GroupNames {
			get {
				foreach (string grp in groups)
					yield return grp;
			}
		}
		
		public void Rename (string oldName, string newName)
		{
			int i = groups.IndexOf (oldName);
			if (i == -1)
				return;

			groups [i] = newName;

			ArrayList list = new ArrayList ();
			foreach (Action a in FindActionsInGroup (oldName))
				list.Add (a);
	
			foreach (Action a in list)
				actions [a] = newName;
				
			EmitGroupsChanged ();
		}
		
		public void Add (string name)
		{
			groups.Add (name);
			EmitGroupsChanged ();
		}
		
		public RadioGroup FindGroup (string name)
		{
			for (int i = 0; i < groups.Count; i++) {
				RadioGroup group = groups[i] as RadioGroup;
				if (group.Name == name)
					return group;
			}
			return null;
		}

		public string GetGroup (Action action)
		{
			return actions [action] as string;
		}
		
		public void SetGroup (Action action, string group)
		{
			if (group == null) {
				if (actions.Contains (action)) {
					actions.Remove (action);
					action.Disposed -= OnActionDisposed;
				}
				return;
			}
			
			if (!actions.Contains (action))
				action.Disposed += OnActionDisposed;
			actions [action] = group;
			if (!groups.Contains (group))
				groups.Add (group);
		}
		
		void OnActionDisposed (object s, EventArgs a)
		{
			Action ac = (Action) s;
			if (ac != null) {
				ac.Disposed -= OnActionDisposed;
				actions.Remove (ac);
			}
		}
		
		public string LastGroup {
			get {
				if (groups.Count == 0)
					Add ("group1");
				return groups [groups.Count - 1] as string;
			}
		}
		
		void EmitGroupsChanged ()
		{
			if (GroupsChanged != null)
				GroupsChanged ();
		}
		
		IEnumerable FindActionsInGroup (string grp)
		{
			foreach (DictionaryEntry e in actions)
				if (((string)e.Value) == grp)
					yield return e.Key;
		}
		
		public CodeExpression GenerateGroupExpression (GeneratorContext ctx, Action action)
		{
			// Returns and expression that represents the group to which the radio belongs.
			// This expression can be an empty SList, if this is the first radio of the
			// group that has been generated, or an SList taken from previously generated
			// radios from the same group.
			
			string group = actions [action] as string;
			if (group == null)
				return new CodePrimitiveExpression (null);

			CodeExpression var = null;
			
			foreach (Action a in FindActionsInGroup (group)) {
				if (a == action)
					continue;
				var = ctx.WidgetMap.GetWidgetExp (a);
				if (var != null)
					break;
			}
			
			if (var == null) {
				return new CodeObjectCreateExpression (
					"GLib.SList",
					new CodePropertyReferenceExpression (
						new CodeTypeReferenceExpression (typeof(IntPtr)),
						"Zero"
					)
				);
			} else {
				return new CodePropertyReferenceExpression (
					var,
					"Group"
				);
			}
		}
	}
}
