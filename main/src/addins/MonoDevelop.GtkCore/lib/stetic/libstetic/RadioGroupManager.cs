using System;
using System.Collections;
using System.Reflection;
using System.CodeDom;

// The stetic representation of "radio widget" (Gtk.RadioButton,
// Gtk.RadioToolButton, and Gtk.RadioMenuItem) groups is that the
// groups have names, and each widget's "Group" property stores the
// name of its group. The glade representation is similar, except that
// the group names aren't arbitrary; they have to be the name of the
// first widget in the group. (On disk, the "group leader" has no
// "group" property, and the other widgets have the leader's name as
// their group property.)
//
// The internal gtk representation of groups is that each radio widget
// has a GLib.SList "Group" property. The content of the list is
// essentially opaque. (For Gtk.RadioButton, the list contains all of
// the RadioButtons in the group. But for Gtk.ToolRadioButton it
// contains pointers to internal widgets, not the ToolRadioButtons
// themselves.) The only thing we can do with them then is to read one
// widget's group and then immediately assign it to another widget. We
// can't look into the list, or assume that a widget's Group property
// will keep the same value if any other widget's Group changes.
//
// Each radio widget type wrapper class keeps a static
// RadioGroupManager to handle this string<->GLib.SList translation
// for it.

namespace Stetic {

	public class RadioGroup {
		public string Name;
		public ArrayList Widgets;

		public RadioGroup (string name)
		{
			Name = name;
			Widgets = new ArrayList ();
		}
	}
	
	public class RadioGroupManager: IRadioGroupManager
	{
		PropertyInfo groupProperty;
		ArrayList groups;
		Hashtable widgets;

		public RadioGroupManager (Type widgetType)
		{
			groupProperty = widgetType.GetProperty ("Group");
			if (groupProperty == null || groupProperty.PropertyType != typeof (GLib.SList))
				throw new ArgumentException ("No 'public GLib.SList Group' property on '" + widgetType.FullName + "'");

			groups = new ArrayList ();
			widgets = new Hashtable ();
		}

		public event GroupsChangedDelegate GroupsChanged; 

		void EmitGroupsChanged ()
		{
			if (GroupsChanged != null)
				GroupsChanged ();
		}

		public IEnumerable GroupNames {
			get {
				string[] names = new string[groups.Count];
				for (int i = 0; i < groups.Count; i++)
					names[i] = ((RadioGroup)groups[i]).Name;
				return names;
			}
		}

		public string LastGroup {
			get {
				if (groups.Count == 0)
					Add ("group1");
				RadioGroup group = groups[groups.Count - 1] as RadioGroup;
				return group.Name;
			}
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

		void IRadioGroupManager.Add (string name)
		{
			Add (name);
		}
		
		public RadioGroup Add (string name)
		{
			RadioGroup group = new RadioGroup (name);
			groups.Add (group);
			EmitGroupsChanged ();
			return group;
		}

		public void Rename (string oldName, string newName)
		{
			RadioGroup group = FindGroup (oldName);
			if (group != null) {
				group.Name = newName;
				EmitGroupsChanged ();
			}
		}

		void RadioDestroyed (object obj, EventArgs args)
		{
			Gtk.Widget radio = obj as Gtk.Widget;
			this[radio] = null;
		}

		public string this[Gtk.Widget radio] {
			get {
				RadioGroup group = widgets[radio] as RadioGroup;
				if (group != null)
					return group.Name;
				else
					return null;
			}
			set {
				GLib.SList group_value;

				RadioGroup oldGroup = widgets[radio] as RadioGroup;
				if (oldGroup == null) {
					radio.Destroyed += RadioDestroyed;
				} else {
					if (oldGroup.Name == value)
						return;
					oldGroup.Widgets.Remove (radio);
					if (oldGroup.Widgets.Count == 0) {
						groups.Remove (oldGroup);
						EmitGroupsChanged ();
					}
				}

				if (value == null) {
					radio.Destroyed -= RadioDestroyed;
					groupProperty.SetValue (radio, new GLib.SList (IntPtr.Zero), null);
					widgets.Remove (radio);
					return;
				}

				RadioGroup newGroup = FindGroup (value);
				if (newGroup == null)
					newGroup = Add (value);

				if (newGroup.Widgets.Count == 0)
					group_value = new GLib.SList (IntPtr.Zero);
				else
					group_value = (GLib.SList)groupProperty.GetValue (newGroup.Widgets[0], null);

				groupProperty.SetValue (radio, group_value, null);
				newGroup.Widgets.Add (radio);
				widgets[radio] = newGroup;
			}
		}

		public string GladeGroupName (Gtk.Widget radio)
		{
			RadioGroup group = widgets[radio] as RadioGroup;
			if (group == null || group.Widgets.Count == 0)
				return null;

			Gtk.Widget leader = (Gtk.Widget)group.Widgets[0];
			return leader.Name;
		}
		
		public CodeExpression GenerateGroupExpression (GeneratorContext ctx, Gtk.Widget widget)
		{
			// Returns and expression that represents the group to which the radio belongs.
			// This expression can be an empty SList, if this is the first radio of the
			// group that has been generated, or an SList taken from previously generated
			// radios from the same group.
			
			RadioGroup group = widgets[widget] as RadioGroup;
			CodeExpression var = null;
			
			foreach (Gtk.Widget radio in group.Widgets) {
				if (radio == widget)
					continue;
				var = ctx.WidgetMap.GetWidgetExp (radio);
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
