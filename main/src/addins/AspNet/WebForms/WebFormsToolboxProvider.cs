//
// ToolboxProvider.cs: Provides default ASP.NET controls to the toolbox.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
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

using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.Toolbox;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.AspNet.WebForms
{
	
	public class WebFormsToolboxProvider : IToolboxDefaultProvider
	{
		public IEnumerable<string> GetDefaultFiles ()
		{
			return GetFxAssemblies ().Where (s => !string.IsNullOrEmpty (s));
		}
		
		IEnumerable<string> GetFxAssemblies ()
		{
			var ctx = Runtime.SystemAssemblyService.DefaultAssemblyContext;
			
			TargetFramework fx = Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_1_1);
			
			yield return ctx.GetAssemblyLocation ("System.Web, Version=1.0.5000.0", fx);
			
			fx = Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_2_0);
			
			yield return ctx.GetAssemblyLocation ("System.Web, Version=2.0.0.0", fx);
			
			fx = Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_3_5);
			
			yield return ctx.GetAssemblyLocation ("System.Web.Extensions, Version=3.5.0.0", fx);
			yield return ctx.GetAssemblyLocation ("System.Web.Mvc, Version=1.0.0.0", fx);
			
			fx = Runtime.SystemAssemblyService.GetTargetFramework (TargetFrameworkMoniker.NET_4_0);
			
			yield return ctx.GetAssemblyLocation ("System.Web, Version=4.0.0.0", fx);
			yield return ctx.GetAssemblyLocation ("System.Web.Extensions, Version=4.0.0.0", fx);
			yield return ctx.GetAssemblyLocation ("System.Web.Mvc, Version=2.0.0.0", fx);
		}
		
		public IEnumerable<ItemToolboxNode> GetDefaultItems ()
		{
			return null;
		}
	}
}
