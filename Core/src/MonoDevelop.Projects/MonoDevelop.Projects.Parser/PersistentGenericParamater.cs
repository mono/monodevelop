//
// PersistentGenericParamater.cs: A utility class that handles serialization of
//                                generic parameters.
//
// Author:
//   Matej Urbas (matej.urbas@gmail.com)
//
// (C) 2006 Matej Urbas
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
using System.Reflection;

namespace MonoDevelop.Projects.Parser
{
	internal sealed class PersistentGenericParamater
	{
		private PersistentGenericParamater()
		{
		}
		
		public static GenericParameter Resolve(GenericParameter gp,
		                                       ITypeResolver typeResolver)
		{
			GenericParameter tmp = new GenericParameter();
			tmp.Name = gp.Name;
			tmp.SpecialConstraints = gp.SpecialConstraints;
			if (gp.BaseTypes != null && gp.BaseTypes.Count > 0) {
				tmp.BaseTypes = new ReturnTypeList();
				foreach (IReturnType rt in gp.BaseTypes) {
					tmp.BaseTypes.Add(PersistentReturnType.Resolve(rt, typeResolver));
				}
			}
			
			return tmp;
		}
		
		public static GenericParameter Read (BinaryReader reader, INameDecoder nameTable)
		{
			GenericParameter gp = new GenericParameter();
			gp.Name = PersistentHelper.ReadString(reader, nameTable);
			gp.SpecialConstraints = (GenericParameterAttributes) reader.ReadInt16();
			uint count = reader.ReadUInt32();
			if (count > 0) {
				gp.BaseTypes = new ReturnTypeList();
				for (uint j = 0; j < count; ++j) {
					gp.BaseTypes.Add(PersistentReturnType.Read(reader, nameTable));
				}
			}
			return gp;
		}

		public static void WriteTo (GenericParameter gp, BinaryWriter writer, INameEncoder nameTable)
		{
			PersistentHelper.WriteString(gp.Name, writer, nameTable);
			writer.Write((short)gp.SpecialConstraints);
			if (gp.BaseTypes == null)
				writer.Write((uint)0);
			else {
				writer.Write((uint)gp.BaseTypes.Count);
				foreach (IReturnType rt in gp.BaseTypes) {
					PersistentReturnType.WriteTo(rt, writer, nameTable);
				}
			}
		}
	}
}
