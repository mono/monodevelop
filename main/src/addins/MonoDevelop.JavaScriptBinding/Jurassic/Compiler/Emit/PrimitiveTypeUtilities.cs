using System;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Methods related to the PrimitiveType enum.
	/// </summary>
	public static class PrimitiveTypeUtilities
	{
		/// <summary>
		/// Checks if the given primitive type is numeric.
		/// </summary>
		/// <param name="type"> The primitive type to check. </param>
		/// <returns> <c>true</c> if the given primitive type is numeric; <c>false</c> otherwise. </returns>
		public static bool IsNumeric (PrimitiveType type)
		{
			return type == PrimitiveType.Number || type == PrimitiveType.Int32 || type == PrimitiveType.UInt32;
		}

		/// <summary>
		/// Checks if the given primitive type is a string type.
		/// </summary>
		/// <param name="type"> The primitive type to check. </param>
		/// <returns> <c>true</c> if the given primitive type is a string type; <c>false</c>
		/// otherwise. </returns>
		public static bool IsString (PrimitiveType type)
		{
			return type == PrimitiveType.String || type == PrimitiveType.ConcatenatedString;
		}

		/// <summary>
		/// Checks if the given primitive type is a value type.
		/// </summary>
		/// <param name="type"> The primitive type to check. </param>
		/// <returns> <c>true</c> if the given primitive type is a value type; <c>false</c> otherwise. </returns>
		public static bool IsValueType (PrimitiveType type)
		{
			return type == PrimitiveType.Bool || type == PrimitiveType.Number || type == PrimitiveType.Int32 || type == PrimitiveType.UInt32;
		}

		/// <summary>
		/// Gets a type that can hold values of both the given types.
		/// </summary>
		/// <param name="a"> The first of the two types to find the LCD for. </param>
		/// <param name="b"> The second of the two types to find the LCD for. </param>
		/// <returns> A type that can hold values of both the given types. </returns>
		public static PrimitiveType GetCommonType (PrimitiveType a, PrimitiveType b)
		{
			// If the types are the same, then trivially that type will do.
			if (a == b)
				return a;

			// If both types are numeric, return the number type.
			if (IsNumeric (a) && IsNumeric (b))
				return PrimitiveType.Number;

			// Otherwise, fall back on the generic Any type.
			return PrimitiveType.Any;
		}
	}

}
