// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="?" email="?"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractNamedEntity : AbstractDecoration
	{
		public static Hashtable fullyQualifiedNames = new Hashtable();
		string name;
		
		public override string Name {
			get { return name; }
			set { name = value; }
//			set { name = GetSharedString (value); }
		}
		
		static int req;
		public static string GetSharedString (string value)
		{
			req++;
			if (value == null)
				return null;
			else {
				string sharedVal = fullyQualifiedNames[value] as string;
				if (sharedVal != null)
					return sharedVal;
				else {
					fullyQualifiedNames[value] = value;
					return value;
				}
			}
		}

		protected virtual bool CanBeSubclass {
			get {
				return false;
			}
		}
	}
}
