using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a lightweight representation of a regular expression literal.
	/// </summary>
	public class RegularExpressionLiteral
	{
		/// <summary>
		/// Creates a new regular expression literal.
		/// </summary>
		/// <param name="pattern"> The unescaped regular expression pattern. </param>
		/// <param name="flags"> The regular expression flags. </param>
		public RegularExpressionLiteral (string pattern, string flags)
		{
			Pattern = pattern;
			Flags = flags;
		}

		/// <summary>
		/// Gets the regular expression pattern.
		/// </summary>
		public string Pattern {
			get;
			private set;
		}

		/// <summary>
		/// Gets a string that contains the flags.
		/// </summary>
		public string Flags {
			get;
			private set;
		}

		/// <summary>
		/// Determines whether the specified Object is equal to the current Object.
		/// </summary>
		/// <param name="obj"> The Object to compare with the current Object. </param>
		/// <returns> <c>true</c> if the specified Object is equal to the current Object;
		/// otherwise, <c>false</c>. </returns>
		public override bool Equals (object obj)
		{
			if (!(obj is RegularExpressionLiteral))
				return false;
			return Pattern == ((RegularExpressionLiteral)obj).Pattern &&
			Flags == ((RegularExpressionLiteral)obj).Flags;
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns> A hash code for the current Object. </returns>
		public override int GetHashCode ()
		{
			return Pattern.GetHashCode () ^ Flags.GetHashCode ();
		}

		/// <summary>
		/// Returns a String that represents the current Object.
		/// </summary>
		/// <returns> A String that represents the current Object. </returns>
		public override string ToString ()
		{
			return string.Format ("/{0}/{1}", Pattern, Flags);
		}
	}

}