// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser
{

	[Serializable]
	public class DefaultParameter: IParameter
	{
		protected string              name;
		protected string              documentation;

		protected IReturnType         returnType;
		protected ParameterModifier   modifier;
		protected AttributeCollection attributeCollection = new AttributeCollection();
		protected IMember             declaringMember;

		public DefaultParameter ()
		{
		}
		
		public DefaultParameter (IMember declaringMember, string name, IReturnType type)
		{
			this.name = name;
			returnType = type;
			this.declaringMember = declaringMember;
		}
		
		public bool IsOut {
			get {
				return (modifier & ParameterModifier.Out) == ParameterModifier.Out;
			}
		}
		public bool IsRef {
			get {
				return (modifier & ParameterModifier.Ref) == ParameterModifier.Ref;
			}
		}
		public bool IsParams {
			get {
				return (modifier & ParameterModifier.Params) == ParameterModifier.Params;
			}
		}

		public virtual string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}

		public virtual IReturnType ReturnType {
			get {
				return returnType;
			}
			set {
				returnType = value;
			}
		}

		public virtual AttributeCollection AttributeCollection {
			get {
				return attributeCollection;
			}
		}

		public virtual ParameterModifier Modifier {
			get {
				return modifier;
			}
			set {
				modifier = value;
			}
		}
		
		public virtual IMember DeclaringMember {
			get { return declaringMember; }
		}

		public string Documentation {
			get {
				return documentation == null ? "" : documentation;
			}
			set {
				documentation = value;
			}
		}
		
		public virtual int CompareTo (object ob) 
		{
			int cmp;
			IParameter value = (IParameter) ob;
			
			if (Name != null) {
				if (value.Name == null)
					return -1;
				cmp = Name.CompareTo(value.Name);
				if (cmp != 0) {
					return cmp;
				}
			} else if (value.Name != null)
				return 1;
				
			if (ReturnType != null) {
				if (value.ReturnType == null)
					return -1;
				if(0 != (cmp = ReturnType.CompareTo(value.ReturnType)))
					return cmp;
			} else if (value.ReturnType != null)
				return 1;
			
			if(0 != (cmp = (int)(Modifier - value.Modifier)))
				return cmp;
			
			return DiffUtility.Compare(AttributeCollection, value.AttributeCollection);
		}
		
		public override bool Equals (object ob)
		{
			IParameter other = ob as IParameter;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = (int) Modifier;
			if (Name != null) c += Name.GetHashCode ();
			if (ReturnType != null) c += ReturnType.GetHashCode ();
			return c;
		}
	}
}
