// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Collections;
using System.Collections.Specialized;
using ICSharpCode.SharpRefactory.Parser;

namespace MonoDevelop.Internal.Parser
{
	public class FoldingRegion
	{
		string  name;
		IRegion region;
		
		public string Name {
			get {
				return name;
			}
		}
		
		public IRegion Region {
			get {
				return region;
			}
		}
		
		public FoldingRegion(string name, IRegion region)
		{
			this.name = name;
			this.region = region;
		}
	}
	
	[Serializable]
	public abstract class AbstractCompilationUnit : ICompilationUnit
	{
		protected IUsingCollection usings = new IUsingCollection();
		protected ClassCollection classes = new ClassCollection();
		protected AttributeSectionCollection attributes = new AttributeSectionCollection();
		protected bool errorsDuringCompile = false;
		protected object tag               = null;
		protected ArrayList foldingRegions = new ArrayList();
		protected ErrorInfo[] errorInfo;
		
		public bool ErrorsDuringCompile {
			get {
				return errorsDuringCompile;
			}
			set {
				errorsDuringCompile = value;
			}
		}

		public ErrorInfo[] ErrorInformation {
			get {
				return errorInfo;
			}
			set {
				errorInfo = value;
			}
		}
		
		public object Tag {
			get {
				return tag;
			}
			set {
				tag = value;
			}
		}
		
		public virtual IUsingCollection Usings {
			get {
				return usings;
			}
		}

		public virtual AttributeSectionCollection Attributes {
			get {
				return attributes;
			}
		}

		public virtual ClassCollection Classes {
			get {
				return classes;
			}
		}
		
		public ArrayList FoldingRegions {
			get {
				return foldingRegions;
			}
		}

		public abstract CommentCollection MiscComments {
			get;
		}

		public abstract CommentCollection DokuComments {
			get;
		}

		public abstract TagCollection TagComments {
			get;
		}
	}
}
