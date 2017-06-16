// 
// ConfigurationParameters.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Serialization;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public abstract class DotNetCompilerParameters
	{
		public DotNetProject ParentProject {
			get { return ParentConfiguration?.ParentItem; }
		}

		internal protected virtual void Read (IPropertySet pset)
		{
			pset.ReadObjectProperties (this, GetType (), true);
		}

		internal protected virtual void Write (IPropertySet pset)
		{
			pset.WriteObjectProperties (this, GetType (), true);
		}

		public virtual DotNetCompilerParameters Clone ()
		{
			return (DotNetCompilerParameters) MemberwiseClone ();
		}

		public virtual IEnumerable<string> GetDefineSymbols ()
		{
			yield break;
		}
		
		public virtual void AddDefineSymbol (string symbol)
		{
		}

		public virtual void RemoveDefineSymbol (string symbol)
		{
		}

		public bool HasDefineSymbol (string symbol)
		{
			return GetDefineSymbols ().Any (s => s == symbol);
		}
		
		public DotNetProjectConfiguration ParentConfiguration { get; internal set; }

		public virtual bool NoStdLib { get; set; }

		public virtual Microsoft.CodeAnalysis.CompilationOptions CreateCompilationOptions ()
		{
			return null;
		}

		public virtual Microsoft.CodeAnalysis.ParseOptions CreateParseOptions (DotNetProjectConfiguration configuration)
		{
			return null;
		}
	}
}
