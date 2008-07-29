//  DefaultCompilationUnit.cs
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
using System.Collections.Specialized;

namespace MonoDevelop.Projects.Parser
{
	public class FoldingRegion
	{
		string  name;
		IRegion region;
		bool    defaultIsFolded = false;
		
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

		public bool DefaultIsFolded {
			get {
				return defaultIsFolded;
			}
			set {
				defaultIsFolded = value;
			}
		}
		
		public FoldingRegion(string name, IRegion region)
		{
			this.name = name;
			this.region = region;
		}
	}
	
	[Serializable]
	public class DefaultCompilationUnit : ICompilationUnit
	{
		protected IUsingCollection usings = new IUsingCollection();
		protected ClassCollection classes = new ClassCollection();
		protected AttributeSectionCollection attributes = new AttributeSectionCollection();
		protected bool errorsDuringCompile = false;
		protected object tag               = null;
		protected ArrayList foldingRegions = new ArrayList();
		protected ErrorInfo[] errorInfo;
		TagCollection tagComments;
		CommentCollection dokuComments;
		CommentCollection miscComments;
		
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

		public virtual CommentCollection MiscComments {
			get { return miscComments; }
			set { miscComments = value; }
		}

		public virtual CommentCollection DokuComments {
			get { return dokuComments; }
			set { dokuComments = value; }
		}

		public virtual TagCollection TagComments {
			get { return tagComments; }
			set { tagComments = value; }
		}
	}
}
