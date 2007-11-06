//  DefaultAttribute.cs
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
using System.Collections;
using System.CodeDom;
using MonoDevelop.Projects.Utility;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public class DefaultAttributeSection : IAttributeSection
	{
		AttributeTarget attributeTarget;
		AttributeCollection attributes = new AttributeCollection();

		IRegion region;
		
		public DefaultAttributeSection ()
		{
		}
		
		public DefaultAttributeSection (AttributeTarget target, IRegion region)
		{
			this.attributeTarget = target;
			this.region = region;
		}

		public DefaultAttributeSection (AttributeTarget target, IRegion region, AttributeCollection attributes)
		{
			this.attributeTarget = target;
			this.attributes = attributes;
			this.region = region;
		}

		public virtual AttributeTarget AttributeTarget {
			get {
				return attributeTarget;
			}
		}

		public virtual AttributeCollection Attributes {
			get {
				return attributes;
			}
		}
		
		public IRegion Region
		{
			get { return region; }
		}
		
		public virtual int CompareTo(IAttributeSection value) {
			int cmp;
			
			if(0 != (cmp = (int)(AttributeTarget - value.AttributeTarget)))
				return cmp;
			
			return DiffUtility.Compare(Attributes, value.Attributes);
		}
		
		int IComparable.CompareTo(object value) {
			return CompareTo((IAttributeSection)value);
		}
		
		public override bool Equals (object ob)
		{
			IAttributeSection sec = ob as IAttributeSection;
			if (sec == null) return false;
			return CompareTo (sec) == 0;
		}
		
		public override int GetHashCode ()
		{
			int c = 0;
			foreach (IAttribute at in Attributes)
				c += at.GetHashCode ();

			return attributeTarget.GetHashCode () + c;
		}
	}
	
	public class DefaultAttribute : IAttribute
	{
		protected string name;
		protected CodeExpression[] positionalArguments;
		protected NamedAttributeArgument[] namedArguments;
		protected IRegion region;

		public DefaultAttribute ()
		{
		}
		
		public DefaultAttribute (string name, CodeExpression[] positionalArguments, NamedAttributeArgument[] namedArguments)
		: this (name, positionalArguments, namedArguments, null)
		{
		}
		
		public DefaultAttribute (string name, CodeExpression[] positionalArguments, NamedAttributeArgument[] namedArguments, IRegion region)
		{
			this.name = name;
			this.positionalArguments = positionalArguments;
			this.namedArguments = namedArguments;
			this.region = region;
		}
		
		public virtual string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		public virtual CodeExpression[] PositionalArguments { // [expression]
			get {
				return positionalArguments;
			}
			set {
				positionalArguments = value;
			}
		}
		public virtual NamedAttributeArgument[] NamedArguments { // string/expression
			get {
				return namedArguments;
			}
			set {
				namedArguments = value;
			}
		}
		
		public IRegion Region
		{
			get { return region; }
		}
		
		public virtual int CompareTo(IAttribute value) {
			int cmp;
			
			cmp = Name.CompareTo(value.Name);
			if (cmp != 0) {
				return cmp;
			}
			
			cmp = DiffUtility.Compare(PositionalArguments, value.PositionalArguments);
			if (cmp != 0) {
				return cmp;
			}
			
			return DiffUtility.Compare(NamedArguments, value.NamedArguments);
		}
		
		int IComparable.CompareTo(object value) {
			return CompareTo((IAttribute)value);
		}
		
		public override bool Equals (object ob)
		{
			IAttribute other = ob as IAttribute;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			return Name.GetHashCode ();
		}
	}
}
