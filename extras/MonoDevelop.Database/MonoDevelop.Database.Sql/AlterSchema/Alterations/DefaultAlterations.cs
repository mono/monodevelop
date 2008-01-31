//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2008 Ben Motmans
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
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public class NameAlteration : AbstractAlteration<string>
	{
		public NameAlteration (string oldName, string newName)
			: base (oldName, newName)
		{
		}
	}
	
	public class StatementAlteration : AbstractAlteration<string>
	{
		public StatementAlteration (string oldValue, string newValue)
			: base (oldValue, newValue)
		{
		}
	}
	
	public class CommentAlteration : AbstractAlteration<string>
	{
		public CommentAlteration (string oldValue, string newValue)
			: base (oldValue, newValue)
		{
		}
	}
	
	public class DefinitionAlteration : AbstractAlteration<string>
	{
		public DefinitionAlteration (string oldValue, string newValue)
			: base (oldValue, newValue)
		{
		}
	}
	
	public class SchemaAlteration : AbstractAlteration<string>
	{
		public SchemaAlteration (string oldValue, string newValue)
			: base (oldValue, newValue)
		{
		}
	}
	
	public class OwnerNameAlteration : AbstractAlteration<string>
	{
		public OwnerNameAlteration (string oldValue, string newValue)
			: base (oldValue, newValue)
		{
		}
	}
	
	public class SourceAlteration : AbstractAlteration<string>
	{
		public SourceAlteration (string oldValue, string newValue)
			: base (oldValue, newValue)
		{
		}
	}
	
	public class PositionAlteration : AbstractAlteration<int>
	{
		public PositionAlteration (int oldValue, int newValue)
			: base (oldValue, newValue)
		{
		}
	}
}
