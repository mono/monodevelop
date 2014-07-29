using System;
using System.Collections.Generic;
using System.Text;

namespace Jurassic
{

	/// <summary>
	/// Represents the JavaScript "null" type and provides the one and only instance of that type.
	/// </summary>
	[Serializable]
	public sealed class Null : System.Runtime.Serialization.ISerializable
	{
		/// <summary>
		/// Creates a new Null instance.
		/// </summary>
		Null ()
		{
		}

		/// <summary>
		/// Gets the one and only "null" instance.
		/// </summary>
		public static readonly Null Value = new Null ();

		[Serializable]
		class SerializationHelper : System.Runtime.Serialization.IObjectReference
		{
			public object GetRealObject (System.Runtime.Serialization.StreamingContext context)
			{
				return Null.Value;
			}
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
			// Save the object state.
			info.SetType (typeof(SerializationHelper));
		}

		/// <summary>
		/// Returns a string representing the current object.
		/// </summary>
		/// <returns> A string representing the current object. </returns>
		public override string ToString ()
		{
			return "null";
		}
	}

}
