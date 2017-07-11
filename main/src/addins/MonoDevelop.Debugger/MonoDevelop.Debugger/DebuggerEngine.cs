// 
// DebuggerEngine.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using Mono.Debugging.Client;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Debugger
{
	public class DebuggerEngine
	{
		DebuggerEngineBackend engine;
		DebuggerEngineExtensionNode node;
		bool gotEngine;
		
		public string Id {
			get { return node.Id; }
		}
		
		public string Name {
			get { return node.Name; }
		}
		
		public DebuggerFeatures SupportedFeatures { get; private set; }
		
		internal DebuggerEngine (DebuggerEngineExtensionNode node)
		{
			this.node = node;
			
			foreach (string s in node.SupportedFeatures) {
				try {
					object res = Enum.Parse (typeof(DebuggerFeatures), s, true);
					if (res != null)
						SupportedFeatures |= (DebuggerFeatures) res;
				} catch {
					LoggingService.LogError ("Invalid feature '" + s + "' in debugger engine node (" + node.Addin.Id + ")");
				}
			}
		}
		
		void LoadEngine ()
		{
			if (!gotEngine) {
				gotEngine = true;
				var ob = node.GetInstance ();
				#pragma warning disable 618
				var legacyEngine = ob as IDebuggerEngine;
				#pragma warning restore 618
				if (legacyEngine != null)
					engine = new LegacyDebuggerEngineBackend (legacyEngine);
				else
					engine = (DebuggerEngineBackend) node.GetInstance ();
			}
		}
		
		public bool CanDebugCommand (ExecutionCommand cmd)
		{
			LoadEngine ();
			return engine != null && engine.CanDebugCommand (cmd);
		}
		
		public bool IsDefaultDebugger (ExecutionCommand cmd)
		{
			LoadEngine ();
			return engine != null && engine.IsDefaultDebugger (cmd);
		}

		public DebuggerStartInfo CreateDebuggerStartInfo (ExecutionCommand cmd)
		{
			LoadEngine ();
			return engine.CreateDebuggerStartInfo (cmd);
		}

		public ProcessAttacher GetProcessAttacher ()
		{
			LoadEngine ();

			return engine?.GetProcessAttacher ();
		}

		public ProcessInfo[] GetAttachableProcesses ()
		{
			LoadEngine ();

			return engine != null ? engine.GetAttachableProcesses () : new ProcessInfo [0];
		}
		
		public DebuggerSession CreateSession ()
		{
			LoadEngine ();
			return engine.CreateSession ();
		}
	}
}

