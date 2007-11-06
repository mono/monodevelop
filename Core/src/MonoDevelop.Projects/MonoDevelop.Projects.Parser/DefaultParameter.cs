//  DefaultParameter.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
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
