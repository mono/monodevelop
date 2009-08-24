// PythonConfiguration.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;

using PyBinding.Runtime;

namespace PyBinding
{
	public class PythonConfiguration : ProjectConfiguration
	{
		static readonly string s_DefaultModule = "main";
		
		[ItemProperty("Runtime/Interpreter")]
		IPythonRuntime m_Runtime;
		
		[ItemProperty("Runtime/Module")]
		string m_Module = String.Empty;
		
		[ItemProperty("Runtime/PythonOptions")]
		string m_PythonOptions = String.Empty;
		
		[ItemProperty("Build/Optimize")]
		bool m_Optimize = false;
		
		public PythonConfiguration ()
		{
			this.m_Runtime = PythonHelper.FindPreferedRuntime ();
			this.m_Module = s_DefaultModule;
		}
		
		public string PythonOptions {
			get {
				return this.m_PythonOptions;
			}
			set {
				this.m_PythonOptions = value;
			}
		}
		
		public string Module {
			get {
				return this.m_Module;
			}
			set {
				this.m_Module = value;
			}
		}
		
		public bool Optimize {
			get {
				return this.m_Optimize;
			}
			set {
				this.m_Optimize = value;
			}
		}
		
		public IPythonRuntime Runtime {
			get {
				return this.m_Runtime;
			}
			set {
				this.m_Runtime = value;
			}
		}

		public override void CopyFrom (ItemConfiguration config)
		{
			PythonConfiguration pyConfig = config as PythonConfiguration;
			
			if (pyConfig == null)
				throw new ArgumentException ("not a PythonConfiguration");
			
			base.CopyFrom (config);
			
			this.m_Module        = pyConfig.Module;
			this.m_Runtime       = (IPythonRuntime) pyConfig.Runtime.Clone ();
			this.m_Optimize      = pyConfig.Optimize;
			this.m_PythonOptions = pyConfig.PythonOptions;
		}
	}
}
