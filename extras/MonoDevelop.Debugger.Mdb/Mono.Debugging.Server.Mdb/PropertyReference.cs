// PropertyVariable.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Mono.Debugger.Languages;
using Mono.Debugger;
using Mono.Debugging.Client;

namespace DebuggerServer
{
	class PropertyReference: ValueReference
	{
		TargetPropertyInfo prop;
		TargetStructObject thisobj;
		
		public PropertyReference (Thread thread, TargetPropertyInfo prop, TargetStructObject thisobj): base (thread)
		{
			this.prop = prop;
			if (!prop.IsStatic)
				this.thisobj = thisobj;
		}
		
		public override TargetType Type {
			get {
				return prop.Type;
			}
		}
		
		public override TargetObject Value {
			get {
				return Util.GetRealObject (Thread, Server.Instance.RuntimeInvoke (Thread, prop.Getter, thisobj, new TargetObject[0]));
			}
			set {
				Server.Instance.RuntimeInvoke (Thread, prop.Setter, thisobj, new TargetObject[] { value });
			}
		}
		
		public override string Name {
			get {
				return prop.Name;
			}
		}

		public override ObjectValueFlags Flags {
			get {
				ObjectValueFlags flags = ObjectValueFlags.Property | Util.GetAccessibility (prop.Accessibility);
				if (!prop.CanWrite) flags |= ObjectValueFlags.ReadOnly;
				return flags;
			}
		}
	}
}
