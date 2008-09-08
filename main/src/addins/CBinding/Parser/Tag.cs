//
// Tag.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

namespace CBinding.Parser
{
	public enum TagKind {
		Class = 'c',
		Macro = 'd',
		Enumerator = 'e',
		Function = 'f',
		Enumeration = 'g',
		Local = 'l',
		Member = 'm',
		Namespace = 'n',
		Prototype = 'p',
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
		private UInt64 line;
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
		            UInt64 line,
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
			this.line = line;	
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

		public UInt64 Line {
			get { return line; }
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
