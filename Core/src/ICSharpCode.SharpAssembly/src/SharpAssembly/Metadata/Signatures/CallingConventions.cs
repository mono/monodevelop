// Assembly.cs
// Copyright (C) 2003 Mike Krueger
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.IO;

namespace MonoDevelop.SharpAssembly.Metadata
{
	public enum CallingConvention : uint
	{
		Default      = 0x00,
		
		Cdecl        = 0x01,
		Stdcall      = 0x02,
		Thiscall     = 0x03,
		Fastcall     = 0x04,
		
		VarArg       = 0x05,
		Field        = 0x06,
		LocalSig     = 0x07,
		Property     = 0x08,
		UnMngd       = 0x09,
		
		HasThis      = 0x20,
		ExplicitThis = 0x40
	}
}
