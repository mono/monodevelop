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

namespace MonoDevelop.Projects
{
	///<summary>This should really be called DotNetCompilerParameters</summary>
	[DataItem (FallbackType=typeof(UnknownCompilationParameters))]
	public abstract class ConfigurationParameters: ProjectParameters
	{
		DotNetProjectConfiguration configuration;

		public virtual IEnumerable<string> GetDefineSymbols ()
		{
			yield break;
		}
		
		[Obsolete]
		public virtual void AddDefineSymbol (string symbol)
		{
		}

		[Obsolete]
		public virtual void RemoveDefineSymbol (string symbol)
		{
		}

		[Obsolete]
		public virtual bool HasDefineSymbol (string symbol)
		{
			return GetDefineSymbols ().Any (s => s == symbol);
		}
		
		public new ConfigurationParameters Clone ()
		{
			return (ConfigurationParameters) base.Clone ();
		}
		
		public DotNetProjectConfiguration ParentConfiguration {
			get { return configuration; }
			internal set {
				configuration = value; 
				if (configuration != null)
					ParentProject = configuration.ParentItem;
			}
		}
	}

	public abstract class DotNetConfigurationParameters : ConfigurationParameters
	{
		public abstract bool NoStdLib { get; set; }
		public virtual string DebugType { get { return ""; } set {} }
	}

}
