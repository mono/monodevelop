// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Collections.Utility;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public class AbstractAttributeSection : IAttributeSection
	{
		protected AttributeTarget     attributeTarget;
		protected AttributeCollection attributes = new AttributeCollection();

		public virtual AttributeTarget AttributeTarget {
			get {
				return attributeTarget;
			}
			set {
				attributeTarget = value;
			}
		}

		public virtual AttributeCollection Attributes {
			get {
				return attributes;
			}
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
	}
	
	public abstract class AbstractAttribute : IAttribute
	{
		protected string name;
		protected ArrayList positionalArguments = new ArrayList();
		protected SortedList namedArguments = new SortedList();

		public virtual string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		public virtual ArrayList PositionalArguments { // [expression]
			get {
				return positionalArguments;
			}
			set {
				positionalArguments = value;
			}
		}
		public virtual SortedList NamedArguments { // string/expression
			get {
				return namedArguments;
			}
			set {
				namedArguments = value;
			}
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
	}
}
