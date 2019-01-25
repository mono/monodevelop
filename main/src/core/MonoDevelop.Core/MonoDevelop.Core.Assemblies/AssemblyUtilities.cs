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

namespace MonoDevelop.Core.Assemblies
{
	public static class AssemblyUtilities
	{
		static bool Is64BitPE (Mono.Cecil.TargetArchitecture machine)
		{
			return machine == Mono.Cecil.TargetArchitecture.AMD64 ||
				   machine == Mono.Cecil.TargetArchitecture.IA64 ||
				   machine == Mono.Cecil.TargetArchitecture.ARM64;
		}

		public static ProcessExecutionArchitecture GetProcessExecutionArchitectureForAssembly (string assemblyPath)
		{
			if (string.IsNullOrEmpty (assemblyPath))
				throw new ArgumentNullException (nameof (assemblyPath));

			try {
				Mono.Cecil.ModuleAttributes peKind;
				Mono.Cecil.TargetArchitecture machine;
				if (!File.Exists (assemblyPath))
					return ProcessExecutionArchitecture.Unspecified;
				try {
					using (var adef = Mono.Cecil.AssemblyDefinition.ReadAssembly (assemblyPath)) {
						peKind = adef.MainModule.Attributes;
						machine = adef.MainModule.Architecture;
					}
				} catch {
					peKind = Mono.Cecil.ModuleAttributes.ILOnly;
					machine = Mono.Cecil.TargetArchitecture.I386;
				}
				if ((peKind & (Mono.Cecil.ModuleAttributes.Required32Bit | Mono.Cecil.ModuleAttributes.Preferred32Bit)) != 0)
					return ProcessExecutionArchitecture.X86;
				if (Is64BitPE (machine))
					return ProcessExecutionArchitecture.X64;
			} catch (Exception e) {
				LoggingService.LogError ("Error while determining 64/32 bit assembly.", e);
			}
			return ProcessExecutionArchitecture.Unspecified;
		}
	}
}
