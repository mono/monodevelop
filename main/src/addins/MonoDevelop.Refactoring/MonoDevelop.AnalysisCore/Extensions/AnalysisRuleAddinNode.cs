// 
// RuleAddinNode.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using Mono.Addins;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace MonoDevelop.AnalysisCore.Extensions
{
	class NamedAnalysisRuleAddinNode : AnalysisRuleAddinNode
	{
		[NodeAttribute ("_name", Required=true, Localizable=true, Description="User-visible name of the rule")]
		string name = null;
		
		public string Name { get { return name; } }
		
		public override string Output { get { return RuleTreeLeaf.TYPE; } }
	}
	
	//hidden in GUIs
	class AdaptorAnalysisRuleAddinNode : AnalysisRuleAddinNode
	{
		[NodeAttribute (Required=true, Description="The ID of the output type.")]
		string output = null;
		
		public override string Output { get { return output; } }
	}
	
	abstract class AnalysisRuleAddinNode : ExtensionNode
	{
		//generally rules should not need to use fileExtensions, since their input types (e.g.CSharpDom) should be enough
		//but it's needed for the proprocessor/typemapping rules like ParsedDocument->CSharpDom
		[NodeAttribute (Description="Comma separated list of file extensions to which this rule applies. It applies to all if none is specified.")]
		string[] fileExtensions = null;
	
		[NodeAttribute (Required=true, Description="The ID of the input type")]
		string input = null;
	
		[NodeAttribute ("func", Required=true, Description="The static Func<T,CancellationToken,T> that processes the rule.")]
		string funcName = null;
		
		public string[] FileExtensions { get { return fileExtensions; } }
		public string Input { get { return input; } }
		public abstract string Output {get; }
		public string FuncName { get { return funcName; } }
	
		//Lazy so we avoid loading assemblies until needed
		Func<object, CancellationToken, object> cachedInstance;
		
		public Func<object, CancellationToken, object> Analyze {
			get {
				if (cachedInstance == null)
					CreateFunc ();
				return cachedInstance;
			}
		}
		
		//TODO: allow generics
		void CreateFunc ()
		{
			Func<object, CancellationToken, object> rule = null;
			try {
				if (string.IsNullOrEmpty (funcName))
					throw new InvalidOperationException ("Rule extension does not specify a func " + GetErrSource ());
				
				int dotIdx = funcName.LastIndexOf ('.');
				if (dotIdx <= 0)
					throw new InvalidOperationException ("Rule func name ' " + funcName + " 'is invalid " + GetErrSource ());
				
				string typeName = funcName.Substring (0, dotIdx);
				var type = this.Addin.GetType (typeName, true);
				
				var inputType = AnalysisExtensions.GetType (Input);
				var outputType = AnalysisExtensions.GetType (Output);
				
				string methodName = funcName.Substring (dotIdx + 1);
				var methodInfo = type.GetMethod (methodName,
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
					Type.DefaultBinder, new Type[] { inputType, typeof(CancellationToken)}, 
					new ParameterModifier [] { new ParameterModifier () });
				
				if (methodInfo == null)
					throw new InvalidOperationException ("Rule func ' " + funcName + "' could not be resolved " + GetErrSource ());
				
				if (methodInfo.ReturnType != outputType)
					throw new InvalidOperationException ("Rule func ' " + funcName + "' has wrong output type " + GetErrSource ());
				
				var wrapper = new DynamicMethod (methodName + "_obj" + (dynamicMethodKey++),
					typeof(object), new Type[] { typeof(object), typeof(CancellationToken) }, true);
				
				var il = wrapper.GetILGenerator ();
				il.Emit (OpCodes.Ldarg_0);
				il.Emit (OpCodes.Castclass, inputType);
				il.Emit (OpCodes.Ldarg_1);
				il.Emit ((methodInfo.IsFinal || !methodInfo.IsVirtual) ? OpCodes.Call : OpCodes.Callvirt, methodInfo);
				il.Emit (OpCodes.Ret);
				
				rule = (Func<object, CancellationToken, object>)wrapper.CreateDelegate (typeof(Func<object, CancellationToken, object>));
			} finally {
				cachedInstance = rule ?? NullRule;
			}
		}
		
		static int dynamicMethodKey = 0;
		
		internal string GetErrSource ()
		{
			return string.Format ("({0}:{1})", Addin.Id, Path);
		}
		
		static object NullRule (object o, CancellationToken cancellationToken)
		{
			return null;
		}
	
		public bool Supports (string extension)
		{
			if (fileExtensions != null && fileExtensions.Length > 0)
				return fileExtensions.Any (fe => string.Compare (fe, extension, StringComparison.OrdinalIgnoreCase) == 0);
			return true;
		}
	}
}

