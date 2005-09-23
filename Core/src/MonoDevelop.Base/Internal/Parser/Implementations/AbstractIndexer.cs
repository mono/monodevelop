// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections.Utility;

namespace MonoDevelop.Internal.Parser
{
	[Serializable]
	public abstract class AbstractIndexer : AbstractMember, IIndexer
	{
		protected IRegion             bodyRegion;
		protected IRegion             getterRegion;
		protected IRegion             setterRegion;
		protected ParameterCollection parameters = new ParameterCollection();
		
		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
		}


		public IRegion GetterRegion {
			get {
				return getterRegion;
			}
		}

		public IRegion SetterRegion {
			get {
				return setterRegion;
			}
		}

		public virtual ParameterCollection Parameters {
			get {
				return parameters;
			}
		}
		
		public virtual int CompareTo(IIndexer value) {
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
			
			if (GetterRegion != null) {
				cmp = GetterRegion.CompareTo(value.GetterRegion);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			if (SetterRegion != null) {
				cmp = SetterRegion.CompareTo(value.SetterRegion);
				if (cmp != 0) {
					return cmp;
				}
			}
			
			return DiffUtility.Compare(Parameters, value.Parameters);
		}
		
		int IComparable.CompareTo(object value) {
			return CompareTo((IIndexer)value);
		}
	}
}
