// AssemblyName.cs
// Copyright (C) 2003 Georg Brandl
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
using MonoDevelop.SharpAssembly.Metadata;
using MonoDevelop.SharpAssembly.Metadata.Rows;

namespace MonoDevelop.SharpAssembly.Assembly
{
	/// <summary>
	/// imitates Reflection.AssemblyName, but has less functionality
	/// </summary>
	public class SharpAssemblyName : object
	{
		public string Name;
		public uint   Flags;
		public string Culture;
		public Version Version;
		public byte[] PublicKey;
		public int    RefId;
		
		public string FullName {
			get {
				string cult = (Culture == "" ? "neutral" : Culture);
				
				return Name + ", Version=" + Version.ToString() + ", Culture=" + cult;
						// + ", PublicKeyToken=" + PublicKey.ToString();
			}
		}
		
		public SharpAssemblyName()
		{
		}
	}
}
