// 
// ClassPropertiesCollection.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 		 Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com), Nikhil Sarda
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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.CodeMetrics
{
	public class MetricsContext 
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cls">
		/// A <see cref="IType"/>
		/// </param>
		/// <returns>
		/// A <see cref="ClassProperties"/>
		/// </returns>
		internal ClassProperties GetInstanceOf (ITypeDefinition cls)
		{
			try {
				if(cls.BodyRegion.Begin==cls.BodyRegion.End)
						return null;
				foreach (var projprop in CodeMetricsWidget.widget.Projects)
				{
					foreach (var clsprop in projprop.Classes) {
						var ret = RecursiveFindInstance (cls, clsprop.Value);
						if(ret!=null)
							return ret;
					}
					foreach (var prop in projprop.Namespaces) {
						foreach (var clsprop in prop.Value.Classes) {
							var ret = RecursiveFindInstance (cls, clsprop.Value);
							if (ret != null)
				    			return ret;
						}
					}
				}
			} catch (NullReferenceException nre) { return null; }
			return null;
		}
		
		private static ClassProperties RecursiveFindInstance (ITypeDefinition cls, ClassProperties prop)
		{
			foreach (var innercls in prop.InnerClasses) {
				var ret = RecursiveFindInstance (cls, innercls.Value);
				if (ret != null)
					return ret;
			}
			
			if (prop.Class.FullName == cls.FullName)
				return prop;
			return null;
		}
		
		private int GetRootCount ()
		{
			//return instances.Where (c => c.IsRoot).Count ();
			return 0;
		}
	}
}
