// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections.Utility;
using System.Reflection;

namespace MonoDevelop.Internal.Parser {
	[Serializable]
	public abstract class AbstractField : AbstractMember, IField
	{
		public virtual int CompareTo(IField field) 
		{
			int cmp;
			
			cmp = base.CompareTo((IDecoration)field);
			if (cmp != 0) {
				return cmp;
			}
			
			if (FullyQualifiedName != null) {
				cmp = FullyQualifiedName.CompareTo(field.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			if (FullyQualifiedName != null) {
				cmp = FullyQualifiedName.CompareTo(field.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			if (ReturnType != null) {
				cmp = ReturnType.CompareTo(field.ReturnType);
				if (cmp != 0) {
					return cmp;
				}
			}
			if (Region != null) {
				return Region.CompareTo(field.Region);
			}
			return 0;
		}
		
		int IComparable.CompareTo(object value)
		{
			return CompareTo((IField)value);
		}
	}
}
