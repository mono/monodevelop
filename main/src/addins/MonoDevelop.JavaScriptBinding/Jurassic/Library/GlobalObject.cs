using System;
using System.Collections.Generic;
using System.Text;

namespace Jurassic.Library
{
	/// <summary>
	/// Represents functions and properties within the global scope.
	/// </summary>
	[Serializable]
	public class GlobalObject : ObjectInstance
	{
		/// <summary>
		/// Creates a new Global object.
		/// </summary>
		/// <param name="prototype"> The next object in the prototype chain. </param>
		public GlobalObject (ScriptEngine engine)
			: base (engine)
		{
			// Add the global constants.
			// Infinity, NaN and undefined are read-only in ECMAScript 5.
		}

		/// <summary>
		/// Gets the public class name of the object.  Used by the default toString()
		/// implementation.
		/// </summary>
		protected override string publicClassName {
			get { return "Global"; }
		}
	}
}
