﻿using System;
using System.Linq;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;
using Mono.Debugging.Backend;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger.VsCodeDebugProtocol
{
	class VSCodeObjectSource : IObjectValueSource
	{

		int parentVariablesReference;
		int variablesReference;
		int frameId;
		VSCodeDebuggerSession vsCodeDebuggerSession;
		ObjectValue [] objValChildren;
		readonly string name;
		readonly string evalName;
		readonly string type;
		readonly string val;


		public VSCodeObjectSource (VSCodeDebuggerSession vsCodeDebuggerSession, int variablesReference, int parentVariablesReference, string name, string type, string evalName, int frameId, string val)
		{
			this.type = type;
			this.frameId = frameId;
			this.evalName = evalName;
			this.name = name;
			this.vsCodeDebuggerSession = vsCodeDebuggerSession;
			this.variablesReference = variablesReference;
			this.parentVariablesReference = parentVariablesReference;
			this.val = val;
		}

		public ObjectValue [] GetChildren (ObjectPath path, int index, int count, EvaluationOptions options)
		{
			if (objValChildren == null) {
				var children = vsCodeDebuggerSession.protocolClient.SendRequestSync (new VariablesRequest (
					variablesReference
				)).Variables;
				objValChildren = children.Select (c => VSCodeDebuggerBacktrace.VsCodeVariableToObjectValue (vsCodeDebuggerSession, c.Name, c.EvaluateName, c.Type, c.Value, c.VariablesReference, variablesReference, frameId)).ToArray ();
			}
			return objValChildren;
		}

		class RawString : IRawValueString
		{
			string val;

			public RawString (string val)
			{
				this.val = val.Remove (val.Length - 1).Remove (0, 1);
			}

			public int Length {
				get {
					return val.Length;
				}
			}

			public string Value {
				get {
					return val;
				}
			}

			public string Substring (int index, int length)
			{
				return val.Substring (index, length);
			}
		}

		public object GetRawValue (ObjectPath path, EvaluationOptions options)
		{
			var val = vsCodeDebuggerSession.protocolClient.SendRequestSync (new EvaluateRequest (evalName, frameId)).Result;
			if (val.StartsWith ("\"", StringComparison.Ordinal))
				if (options.ChunkRawStrings)
					return new RawValueString (new RawString (val));
				else
					return val.Remove (val.Length - 1).Remove (0, 1);
			else
				throw new NotImplementedException ();
		}

		public ObjectValue GetValue (ObjectPath path, EvaluationOptions options)
		{
			string shortName = name;
			var indexOfSpace = name.IndexOf (' ');
			if (indexOfSpace != -1)//Remove " [TypeName]" from variable name
				shortName = name.Remove (indexOfSpace);
			if (type == null)
				return ObjectValue.CreateError (null, new ObjectPath (shortName), "", val, ObjectValueFlags.None);
			if (val == "null")
				return ObjectValue.CreateNullObject (this, shortName, type, parentVariablesReference > 0 ? ObjectValueFlags.None : ObjectValueFlags.ReadOnly);
			if (variablesReference == 0)//This is some kind of primitive...
				return ObjectValue.CreatePrimitive (this, new ObjectPath (shortName), type, new EvaluationResult (val), parentVariablesReference > 0 ? ObjectValueFlags.None : ObjectValueFlags.ReadOnly);
			return ObjectValue.CreateObject (this, new ObjectPath (shortName), type, new EvaluationResult (val), parentVariablesReference > 0 ? ObjectValueFlags.None : ObjectValueFlags.ReadOnly, null);
		}

		public void SetRawValue (ObjectPath path, object value, EvaluationOptions options)
		{
			var v = value.ToString ();
			if (type == "string")
				v = $"\"{v}\"";
			vsCodeDebuggerSession.protocolClient.SendRequestSync (new SetVariableRequest (parentVariablesReference, name, v));
		}

		public EvaluationResult SetValue (ObjectPath path, string value, EvaluationOptions options)
		{
			return new EvaluationResult (vsCodeDebuggerSession.protocolClient.SendRequestSync (new SetVariableRequest (parentVariablesReference, name, value)).Value);
		}
	}
}
