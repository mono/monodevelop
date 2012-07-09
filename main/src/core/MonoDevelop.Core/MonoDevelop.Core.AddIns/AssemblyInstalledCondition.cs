//
// AssemblyInstalledCondition.cs
//
// Author:
//       Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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
using Mono.Addins;
using System.Linq;

namespace MonoDevelop.Core.AddIns
{
	class AssemblyInstalledCondition : ConditionType
	{
		public override bool Evaluate (NodeElement conditionNode)
		{
			string name = conditionNode.GetAttribute ("name");
			var assemblies = Runtime.SystemAssemblyService.CurrentRuntime.RuntimeAssemblyContext.GetAssemblies ()
								.Where (asm => asm.Name == name).ToList ();
			if (assemblies.Count == 0)
				return false;
			string version = conditionNode.GetAttribute ("version");
			if (!String.IsNullOrEmpty (version))
				return assemblies.Any (asm => asm.Version == version);
			string minVersion = conditionNode.GetAttribute ("minVersion");
			if (!String.IsNullOrEmpty (minVersion)) {
				if (!assemblies.Any (asm => Addin.CompareVersions (minVersion, asm.Version) >= 0))
					return false;
			}
			string maxVersion = conditionNode.GetAttribute ("maxVersion");
			if (!String.IsNullOrEmpty(maxVersion)) {
				if (assemblies.Any (asm => Addin.CompareVersions (maxVersion, asm.Version) > 0))
					return false;
			}
			return true;
		}
	}
}

