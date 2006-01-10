//
// PersistentAttribute.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.CodeDom;
using System.IO;
using System.Reflection;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	internal sealed class PersistentAttribute : AbstractAttribute
	{
		public static PersistentAttribute Resolve (IAttribute source, ITypeResolver typeResolver)
		{
			PersistentAttribute par = new PersistentAttribute ();
			par.name = source.Name;
			par.positionalArguments = source.PositionalArguments;
			par.namedArguments = source.NamedArguments;
			return par;
		}
		
		public static PersistentAttribute Read (BinaryReader reader, INameDecoder nameTable)
		{
			PersistentAttribute par = new PersistentAttribute ();
			par.name = PersistentHelper.ReadString (reader, nameTable);
			par.positionalArguments = (CodeExpression[]) PersistentHelper.DeserializeObject (reader);
			par.namedArguments = (NamedAttributeArgument[]) PersistentHelper.DeserializeObject (reader);
			return par;
		}
		
		public static void WriteTo (IAttribute p, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString (p.Name, writer, nameTable);
			PersistentHelper.SerializeObject (writer, p.PositionalArguments);
			PersistentHelper.SerializeObject (writer, p.NamedArguments);
		}
	}
}
