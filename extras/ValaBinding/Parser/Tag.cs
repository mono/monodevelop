//
// Tag.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU Lesser General Public License as published by
//    the Free Software Foundation, either version 2 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.
//


using System;
using System.IO;

namespace MonoDevelop.ValaBinding.Parser
{
	public enum TagKind {
		Class = 'c',
		Macro = 'd',
		Enumerator = 'e',
		Function = 'm',
		Enumeration = 'g',
		Local = 'l',
		Member = 'f',
		Namespace = 'n',
		Prototype = 'm',
		Structure = 's',
		Typedef = 't',
		Union = 'u',
		Variable = 'v',
		ExternalVariable = 'x',
		Unknown = ' '
	}
	
	public enum AccessModifier {
		Private,
		Protected,
		Public
	}
	
	public class Tag
	{
		private string name;
		private string file;
		private string pattern;
		private TagKind kind;
		private AccessModifier access;
		private string field_class;
		private string field_namespace;
		private string field_struct;
		private string field_union;
		private string field_enum;
		private string field_signature;
		
		public Tag (string name,
		            string file,
		            string pattern,
		            TagKind kind,
		            AccessModifier access,
		            string field_class,
		            string field_namespace,
		            string field_struct,
		            string field_union,
		            string field_enum,
		            string field_signature)
		{
			this.name = name;
			this.file = file;
			this.pattern = pattern;	
			this.kind = kind;
			this.access = access;
			this.field_class = field_class;
			this.field_namespace = field_namespace;
			this.field_struct = field_struct;
			this.field_union = field_union;
			this.field_enum = field_enum;
			this.field_signature = field_signature;
		}
		
		public string Name {
			get { return name; }
		}
		
		public string File {
			get { return file; }
		}

		public string Pattern {
			get { return pattern; }
		}
		
		public TagKind Kind {
			get { return kind; }
		}
		
		public AccessModifier Access {
			get { return access; }
		}
		
		public string Class {
			get { return field_class; }
		}
		
		public string Namespace {
			get { return field_namespace; }
		}
		
		public string Structure {
			get { return field_struct; }
		}
		
		public string Union {
			get { return field_union; }
		}
		
		public string Enum {
			get { return field_enum; }
		}
		
		public string Signature {
			get { return field_signature; }
		}
	}
}
