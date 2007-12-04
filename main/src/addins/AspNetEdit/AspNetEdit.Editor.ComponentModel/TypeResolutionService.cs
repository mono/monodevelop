/* 
* TypeResolutionService.cs - resolves types in referenced assemblies
* 
* Authors: 
*  Michael Hutchinson <m.j.hutchinson@gmail.com>
*  
* Copyright (C) 2005 Michael Hutchinson
*
* This sourcecode is licenced under The MIT License:
* 
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to permit
* persons to whom the Software is furnished to do so, subject to the
* following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
* OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
* NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
* DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
* OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
* USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.ComponentModel.Design;
using System.Reflection;
using System.Collections;

namespace AspNetEdit.Editor.ComponentModel
{
	public class TypeResolutionService : ITypeResolutionService
	{
		Hashtable referencedAssemblies;

		#region ITypeResolutionService Members

		public TypeResolutionService ()
		{
			referencedAssemblies = new Hashtable ();
			ReferenceAssembly (typeof (System.Web.UI.WebControls.WebControl).Assembly.GetName ());
		}

		public Assembly GetAssembly (AssemblyName name, bool throwOnError)
		{
			Assembly assembly = GetAssembly (name);

			if (assembly == null)
				throw new Exception ("The assembly could not be found");

			return assembly;
		}

		public Assembly GetAssembly(AssemblyName name)
		{
			Assembly assembly = referencedAssemblies[name] as Assembly;
			if (assembly == null)
			{
				assembly = Assembly.Load (name);
			}

			return assembly;
		}

		//TODO: should this do more?
		public string GetPathOfAssembly(AssemblyName name)
		{
			return name.CodeBase;
		}

		public Type GetType (string name, bool throwOnError, bool ignoreCase)
		{
			//try to get assembly-qualified types
			Type t = Type.GetType (name, false, ignoreCase);
			if (t != null) return t;
			
			//look in referenced assemblies
			foreach (Assembly a in referencedAssemblies.Values)
			{
				t = a.GetType (name, false, ignoreCase);
				if (t != null) break;
			}
			
			if (throwOnError && (t == null))
					throw new Exception ("The type " + name + "was not found in the referenced assemblies.");
			return t;
		}

		public Type GetType (string name, bool throwOnError)
		{
			return GetType (name, throwOnError, false);
		}

		public Type GetType (string name)
		{
			return GetType (name, false);
		}

		//TODO: Actually add reference: need project support
		public void ReferenceAssembly (AssemblyName name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");
			if (referencedAssemblies.ContainsKey (name))
				return;

			Assembly assembly = Assembly.Load (name);
			if (assembly == null)
				throw new InvalidOperationException ("The assembly could not be loaded");
			referencedAssemblies.Add (name, assembly);
		}

		#endregion
	}
}
