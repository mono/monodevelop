﻿using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a string, number, boolean or null literal in the source code.
	/// </summary>
	public class LiteralToken : Token
	{
		private object value;

		/// <summary>
		/// Creates a new LiteralToken instance with the given value.
		/// </summary>
		/// <param name="value"></param>
		public LiteralToken (object value)
		{
			this.value = value;
		}

		// Literal keywords.
		public readonly static LiteralToken True = new LiteralToken (true);
		public readonly static LiteralToken False = new LiteralToken (false);
		public readonly static LiteralToken Null = new LiteralToken (Jurassic.Null.Value);

		/// <summary>
		/// The value of the literal.
		/// </summary>
		public object Value {
			get { return value; }
		}

		/// <summary>
		/// Gets a value that indicates whether the literal is a keyword.  Literal keywords are
		/// <c>false</c>, <c>true</c> and <c>null</c>.
		/// </summary>
		public bool IsKeyword {
			get { return value is bool || value is Null; }
		}

		/// <summary>
		/// Gets a string that represents the token in a parseable form.
		/// </summary>
		public override string Text {
			get {
				if (Value is string)
					return string.Format ("\"{0}\"", ((string)Value).Replace ("\"", "\\\""));
				if (Value is bool)
					return (bool)Value ? "true" : "false";
				return Value.ToString ();
			}
		}
	}

	/// <summary>
	/// Represents a string literal.
	/// </summary>
	public class StringLiteralToken : LiteralToken
	{
		public StringLiteralToken (string value, int escapeSequenceCount, int lineContinuationCount)
			: base (value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			EscapeSequenceCount = escapeSequenceCount;
			LineContinuationCount = lineContinuationCount;
		}

		/// <summary>
		/// Gets the number of character escape sequences encounted while parsing the string
		/// literal.
		/// </summary>
		public int EscapeSequenceCount {
			get;
			private set;
		}

		/// <summary>
		/// Gets the number of line continuations encounted while parsing the string literal.
		/// </summary>
		public int LineContinuationCount {
			get;
			private set;
		}

		/// <summary>
		/// Gets the contents of the string literal.
		/// </summary>
		public new string Value {
			get { return (string)base.Value; }
		}
	}

}