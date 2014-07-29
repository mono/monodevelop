﻿using System;
using System.Collections.Generic;
using Jurassic.Library;

namespace Jurassic
{
	/// <summary>
	/// Represents the JavaScript script engine.  This is the first object that needs to be
	/// instantiated in order to execute javascript code.
	/// </summary>
	[Serializable]
	public sealed class ScriptEngine : System.Runtime.Serialization.ISerializable
	{
		// Compatibility mode.
		CompatibilityMode compatibilityMode;
		GlobalObject globalObject;

		public ScriptEngine ()
		{
			
			// Create the global object.
			globalObject = new GlobalObject (this);
		}

		/// <summary>
		/// Initializes a new instance of the ObjectInstance class with serialized data.
		/// </summary>
		/// <param name="info"> The SerializationInfo that holds the serialized object data about
		/// the exception being thrown. </param>
		/// <param name="context"> The StreamingContext that contains contextual information about
		/// the source or destination. </param>
		ScriptEngine (System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			// Deserialize the compatibility mode.
			compatibilityMode = (CompatibilityMode)info.GetInt32 ("compatibilityMode");

			// Deserialize the ForceStrictMode flag.
			ForceStrictMode = info.GetBoolean ("forceStrictMode");
		}

		/// <summary>
		/// Sets the SerializationInfo with information about the exception.
		/// </summary>
		/// <param name="info"> The SerializationInfo that holds the serialized object data about
		/// the exception being thrown. </param>
		/// <param name="context"> The StreamingContext that contains contextual information about
		/// the source or destination. </param>
		public void GetObjectData (System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			// Serialize the compatibility mode.
			info.AddValue ("compatibilityMode", (int)compatibilityMode);

			// Serialize the ForceStrictMode flag.
			info.AddValue ("forceStrictMode", ForceStrictMode);
		}

		/// <summary>
		/// Gets or sets a value that indicates whether to force ECMAScript 5 strict mode, even if
		/// the code does not contain a strict mode directive ("use strict").  The default is
		/// <c>false</c>.
		/// </summary>
		public bool ForceStrictMode {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the script engine should run in
		/// compatibility mode.
		/// </summary>
		public CompatibilityMode CompatibilityMode {
			get { return compatibilityMode; }
			set {
				compatibilityMode = value;
			}
		}

		public GlobalObject Global {
			get { return globalObject; }
		}

		public Compiler.ObjectScope CreateGlobalScope ()
		{
			return Compiler.ObjectScope.CreateGlobalScope (Global);
		}

	}
}
