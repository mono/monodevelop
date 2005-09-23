// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;
using System.Collections.Utility;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public abstract class AbstractMethod : AbstractMember, IMethod
	{
		protected IRegion bodyRegion;
		
		protected ParameterCollection parameters = new ParameterCollection();

		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
		}
		
		public virtual ParameterCollection Parameters {
			get {
				return parameters;
			}
			set {
				parameters = value;
			}
		}

		public virtual bool IsConstructor {
			get {
				return returnType == null || Name == "ctor";
			}
		}

		public override string ToString()
		{
			return String.Format("[AbstractMethod: FullyQualifiedName={0}, ReturnType = {1}, IsConstructor={2}, Modifier={3}]",
			                     FullyQualifiedName,
			                     ReturnType,
			                     IsConstructor,
			                     base.Modifiers);
		}
		
		public virtual int CompareTo(IMethod value)
		{
			int cmp;
			
			cmp = base.CompareTo((IDecoration)value);
			
			if (cmp != 0) {
				return cmp;
			}
			
			if (FullyQualifiedName != null) {
				cmp = FullyQualifiedName.CompareTo(value.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			if (ReturnType != null) {
				cmp = ReturnType.CompareTo(value.ReturnType);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			if (Region != null) {
				cmp = Region.CompareTo(value.Region);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			return DiffUtility.Compare(Parameters, value.Parameters);
		}
		
		int IComparable.CompareTo(object value)
		{
			if (value == null) {
				return 0;
			}
			return CompareTo((IMethod)value);
		}
	}
}
