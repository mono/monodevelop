using Gtk;
using System;
using System.Collections;
using System.CodeDom;
using MonoDevelop.Projects.Parser;

namespace GladeAddIn.Gui
{
	internal class ClassUtils
	{
		public static IField FindWidgetField (IClass cls, string name)
		{
			foreach (IField field in cls.Fields) {
				if (name == GetWidgetFieldName (field))
					return field;
			}
			return null;
		}
		
		public static string GetWidgetFieldName (IField field)
		{
			foreach (IAttributeSection asec in field.Attributes) {
				foreach (IAttribute att in asec.Attributes)	{
					if (att.Name == "Glade.Widget" || att.Name == "Widget" || att.Name == "Glade.WidgetAttribute" || att.Name == "WidgetAttribute") {
						if (att.PositionalArguments != null && att.PositionalArguments.Length > 0) {
							CodePrimitiveExpression exp = att.PositionalArguments [0] as CodePrimitiveExpression;
							if (exp != null)
								return exp.Value.ToString ();
						} else {
							return field.Name;
						}
					}
				}
			}
			return null;
		}
		
		public static string GetWindowId (IClass cls)
		{
			foreach (IField f in cls.Fields) {
				if (f.ReturnType.FullyQualifiedName != "Gtk.Dialog" && f.ReturnType.FullyQualifiedName != "Gtk.Window")
					continue;
				
				string name = GetWidgetFieldName (f);
				if (name != null)
					return name;
			}
			return null;
		}
	}
}
