// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using MonoDevelop.Projects.Utility;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	public abstract class AbstractClass : AbstractNamedEntity, IClass, IComparable
	{
		protected ClassType        classType;
		protected IRegion          region;
		protected IRegion          bodyRegion;
		protected object           declaredIn;
		
		protected GenericParameterList genericParamters;
		protected ReturnTypeList     baseTypes    = new ReturnTypeList();

		protected ClassCollection    innerClasses = new ClassCollection();
		protected FieldCollection    fields       = new FieldCollection();
		protected PropertyCollection properties   = new PropertyCollection();
		protected MethodCollection   methods      = new MethodCollection();
		protected EventCollection    events       = new EventCollection();
		protected IndexerCollection  indexer      = new IndexerCollection();

		public abstract ICompilationUnit CompilationUnit {
			get;
		}

		public virtual ClassType ClassType {
			get {
				return classType;
			}
		}

		public virtual IRegion Region {
			get {
				return region;
			}
		}
		
		public virtual IRegion BodyRegion {
			get {
				return bodyRegion;
			}
		}

		public object DeclaredIn {
			get {
				return declaredIn;
			}
		}
		
		/// <summary>
		/// Contains a list of formal parameters to a generic type. 
		/// <p>If this property returns null or an empty collection, the type is
		/// not generic.</p>
		/// </summary>
		public virtual GenericParameterList GenericParameters {
			get {
				return genericParamters;
			}
			set {
				genericParamters = value;
			}
		}

		public virtual ReturnTypeList BaseTypes {
			get {
				return baseTypes;
			}
		}
		
		public virtual ClassCollection InnerClasses {
			get {
				return innerClasses;
			}
		}

		public virtual FieldCollection Fields {
			get {
				return fields;
			}
		}

		public virtual PropertyCollection Properties {
			get {
				return properties;
			}
		}

		public IndexerCollection Indexer {
			get {
				return indexer;
			}
		}

		public virtual MethodCollection Methods {
			get {
				return methods;
			}
		}

		public virtual EventCollection Events {
			get {
				return events;
			}
		}


		public override int CompareTo (object ob)
		{
			int cmp;
			IClass value = (IClass) ob;
			
			if(0 != (cmp = base.CompareTo(value))) {
				return cmp;
			}
			
			if (FullyQualifiedName != null) {
				if (value.FullyQualifiedName == null)
					return -1;
				cmp = FullyQualifiedName.CompareTo(value.FullyQualifiedName);
				if (cmp != 0) {
					return cmp;
				}
			} else if (value.FullyQualifiedName != null)
				return 1;
			
			if (Region != null) {
				if (value.Region == null)
					return -1;
				cmp = Region.CompareTo(value.Region);
				if (cmp != 0)
					return cmp;
			} else
				if (value.Region != null)
					return 1;
			
			cmp = DiffUtility.Compare(BaseTypes, value.BaseTypes);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(InnerClasses, value.InnerClasses);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Fields, value.Fields);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Properties, value.Properties);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Indexer, value.Indexer);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Methods, value.Methods);
			if(cmp != 0)
				return cmp;
			
			cmp = DiffUtility.Compare(Events, value.Events);
			if (cmp != 0)
				return cmp;
			
			if (value.GenericParameters == GenericParameters)
				return 0;	// They are the same classes or are both null - 
							// which counts as 'being same'
			else
				return DiffUtility.Compare(GenericParameters, value.GenericParameters);
		}
		
		public override bool Equals (object ob)
		{
			IClass other = ob as IClass;
			if (other == null) return false;
			return CompareTo (other) == 0;
		}
		
		public override int GetHashCode ()
		{
			return base.GetHashCode() + FullyQualifiedName.GetHashCode ();
		}
		
		protected override bool CanBeSubclass {
			get {
				return true;
			}
		}
	}
}
