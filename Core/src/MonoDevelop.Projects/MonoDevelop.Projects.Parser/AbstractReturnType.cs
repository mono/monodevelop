// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractReturnType : System.MarshalByRefObject, IReturnType
	{
		protected int    pointerNestingLevel;
		protected int[]  arrayDimensions;
		protected object declaredin = null;
		string fname;
		
		public virtual string FullyQualifiedName {
			get {
				return fname;
			}
			set {
				if (value == null)
					fname = value;
				else {
					string sharedName = (string) AbstractNamedEntity.fullyQualifiedNames [value];
					if (sharedName == null) {
						AbstractNamedEntity.fullyQualifiedNames [value] = value;
						fname = value;
					}
					else
						fname = sharedName;
				}
			}
		}

		public virtual string Name {
			get {
				if (FullyQualifiedName == null) {
					return null;
				}
				string[] name = FullyQualifiedName.Split(new char[] {'.'});
				return name[name.Length - 1];
			}
		}

		public virtual string Namespace {
			get {
				if (FullyQualifiedName == null) {
					return null;
				}
				int index = FullyQualifiedName.LastIndexOf('.');
				return index < 0 ? String.Empty : FullyQualifiedName.Substring(0, index);
			}
		}

		public virtual int PointerNestingLevel {
			get {
				return pointerNestingLevel;
			}
		}

		public int ArrayCount {
			get {
				return ArrayDimensions.Length;
			}
		}

		public virtual int[] ArrayDimensions {
			get {
				if (arrayDimensions == null) return new int[0];
				return arrayDimensions;
			}
		}

		public virtual int CompareTo (object ob) 
		{
			int cmp;
			IReturnType value = (IReturnType) ob;
			
			if (FullyQualifiedName != null) {
				if (value.FullyQualifiedName == null)
					return -1;
				cmp = FullyQualifiedName.CompareTo(value.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			} else if (value.FullyQualifiedName != null)
				return 1;
			
			cmp = (PointerNestingLevel - value.PointerNestingLevel);
			if (cmp != 0) {
				return cmp;
			}
			
			return DiffUtility.Compare(ArrayDimensions, value.ArrayDimensions);
		}
		
		int IComparable.CompareTo(object value)
		{
			return CompareTo((IReturnType)value);
		}
		
		public override bool Equals (object ob)
		{
			IReturnType other = ob as IReturnType;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = PointerNestingLevel;
			if (ArrayDimensions != null)
				for (int n=0; n<ArrayDimensions.Length; n++)
					c += arrayDimensions [n];
			if (FullyQualifiedName != null)
				c += FullyQualifiedName.GetHashCode ();
			return c;
		}
		
		public virtual object DeclaredIn {
			get {
				return declaredin;
			}
		}
	}
	
}
