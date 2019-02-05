//
// AssemblyUtilities.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Threading;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Serialization;
using Mono.Addins;
using Mono.PkgConfig;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;

namespace MonoDevelop.Core.Assemblies
{
	public static class AssemblyUtilities
	{
		static bool Is64BitPE (Machine machine)
		{
			return machine == Machine.Amd64 || machine == Machine.Arm64 || machine == Machine.IA64 || machine == Machine.Alpha64;
		}

		static bool TryReadPEHeaders (string assemblyPath, out PortableExecutableKinds peKind, out Machine machine)
		{
			peKind = default;
			machine = default;

			try {
				if (!File.Exists (assemblyPath))
					return false;

				using (var reader = new PEReader (File.OpenRead (assemblyPath))) {
					var peHeaders = reader.PEHeaders;

					var corFlags = peHeaders.CorHeader.Flags;
					if ((corFlags & CorFlags.ILOnly) != 0)
						peKind |= PortableExecutableKinds.ILOnly;

					if ((corFlags & CorFlags.Prefers32Bit) != 0)
						peKind |= PortableExecutableKinds.Preferred32Bit;
					else if ((corFlags & CorFlags.Requires32Bit) != 0)
						peKind |= PortableExecutableKinds.Required32Bit;

					if (peHeaders.PEHeader.Magic == PEMagic.PE32Plus)
						peKind |= PortableExecutableKinds.PE32Plus;

					machine = peHeaders.CoffHeader.Machine;
				}

				return true;
			} catch (Exception e) {
				LoggingService.LogError ("Error while determining 64/32 bit assembly.", e);
				return false;
			}
		}

		public static ProcessExecutionArchitecture GetProcessExecutionArchitectureForAssembly (string assemblyPath)
		{
			if (string.IsNullOrEmpty (assemblyPath))
				throw new ArgumentNullException (nameof (assemblyPath));

			if (TryReadPEHeaders (assemblyPath, out var peKind, out var machine)) {
				if ((peKind & (PortableExecutableKinds.Preferred32Bit | PortableExecutableKinds.Required32Bit)) != 0)
					return ProcessExecutionArchitecture.X86;

				if ((peKind & PortableExecutableKinds.PE32Plus) != 0 || Is64BitPE (machine))
					return ProcessExecutionArchitecture.X64;
			}

			return ProcessExecutionArchitecture.Unspecified;
		}
	}
}
