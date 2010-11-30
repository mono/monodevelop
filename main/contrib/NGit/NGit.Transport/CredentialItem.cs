/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// A credential requested from a
	/// <see cref="CredentialsProvider">CredentialsProvider</see>
	/// .
	/// Most users should work with the specialized subclasses:
	/// <ul>
	/// <li>
	/// <see cref="Username">Username</see>
	/// for usernames</li>
	/// <li>
	/// <see cref="Password">Password</see>
	/// for passwords</li>
	/// <li>
	/// <see cref="StringType">StringType</see>
	/// for other general string information</li>
	/// <li>
	/// <see cref="CharArrayType">CharArrayType</see>
	/// for other general secret information</li>
	/// </ul>
	/// This class is not thread-safe. Applications should construct their own
	/// instance for each use, as the value is held within the CredentialItem object.
	/// </summary>
	public abstract class CredentialItem
	{
		private readonly string promptText;

		private readonly bool valueSecure;

		/// <summary>Initialize a prompt.</summary>
		/// <remarks>Initialize a prompt.</remarks>
		/// <param name="promptText">
		/// prompt to display to the user alongside of the input field.
		/// Should be sufficient text to indicate what to supply for this
		/// item.
		/// </param>
		/// <param name="maskValue">
		/// true if the value should be masked from displaying during
		/// input. This should be true for passwords and other secrets,
		/// false for names and other public data.
		/// </param>
		public CredentialItem(string promptText, bool maskValue)
		{
			this.promptText = promptText;
			this.valueSecure = maskValue;
		}

		/// <returns>prompt to display to the user.</returns>
		public virtual string GetPromptText()
		{
			return promptText;
		}

		/// <returns>true if the value should be masked when entered.</returns>
		public virtual bool IsValueSecure()
		{
			return valueSecure;
		}

		/// <summary>Clear the stored value, destroying it as much as possible.</summary>
		/// <remarks>Clear the stored value, destroying it as much as possible.</remarks>
		public abstract void Clear();

		/// <summary>An item whose value is stored as a string.</summary>
		/// <remarks>
		/// An item whose value is stored as a string.
		/// When working with secret data, consider
		/// <see cref="CharArrayType">CharArrayType</see>
		/// instead, as
		/// the internal members of the array can be cleared, reducing the chances
		/// that the password is left in memory after authentication is completed.
		/// </remarks>
		public class StringType : CredentialItem
		{
			private string value;

			/// <summary>Initialize a prompt for a single string.</summary>
			/// <remarks>Initialize a prompt for a single string.</remarks>
			/// <param name="promptText">
			/// prompt to display to the user alongside of the input
			/// field. Should be sufficient text to indicate what to
			/// supply for this item.
			/// </param>
			/// <param name="maskValue">
			/// true if the value should be masked from displaying during
			/// input. This should be true for passwords and other
			/// secrets, false for names and other public data.
			/// </param>
			public StringType(string promptText, bool maskValue) : base(promptText, maskValue
				)
			{
			}

			public override void Clear()
			{
				value = null;
			}

			/// <returns>the current value</returns>
			public virtual string GetValue()
			{
				return value;
			}

			/// <param name="newValue"></param>
			public virtual void SetValue(string newValue)
			{
				value = newValue;
			}
		}

		/// <summary>An item whose value is stored as a char[] and is therefore clearable.</summary>
		/// <remarks>An item whose value is stored as a char[] and is therefore clearable.</remarks>
		public class CharArrayType : CredentialItem
		{
			private char[] value;

			/// <summary>Initialize a prompt for a secure value stored in a character array.</summary>
			/// <remarks>Initialize a prompt for a secure value stored in a character array.</remarks>
			/// <param name="promptText">
			/// prompt to display to the user alongside of the input
			/// field. Should be sufficient text to indicate what to
			/// supply for this item.
			/// </param>
			/// <param name="maskValue">
			/// true if the value should be masked from displaying during
			/// input. This should be true for passwords and other
			/// secrets, false for names and other public data.
			/// </param>
			public CharArrayType(string promptText, bool maskValue) : base(promptText, maskValue
				)
			{
			}

			/// <summary>Destroys the current value, clearing the internal array.</summary>
			/// <remarks>Destroys the current value, clearing the internal array.</remarks>
			public override void Clear()
			{
				if (value != null)
				{
					Arrays.Fill(value, (char)0);
					value = null;
				}
			}

			/// <summary>Get the current value.</summary>
			/// <remarks>
			/// Get the current value.
			/// The returned array will be cleared out when
			/// <see cref="Clear()">Clear()</see>
			/// is
			/// called. Callers that need the array elements to survive should delay
			/// invoking
			/// <code>clear()</code>
			/// until the value is no longer necessary.
			/// </remarks>
			/// <returns>
			/// the current value array. The actual internal array is
			/// returned, reducing the number of copies present in memory.
			/// </returns>
			public virtual char[] GetValue()
			{
				return value;
			}

			/// <summary>Set the new value, clearing the old value array.</summary>
			/// <remarks>Set the new value, clearing the old value array.</remarks>
			/// <param name="newValue">if not null, the array is copied.</param>
			public virtual void SetValue(char[] newValue)
			{
				Clear();
				if (newValue != null)
				{
					value = new char[newValue.Length];
					System.Array.Copy(newValue, 0, value, 0, newValue.Length);
				}
			}

			/// <summary>Set the new value, clearing the old value array.</summary>
			/// <remarks>Set the new value, clearing the old value array.</remarks>
			/// <param name="newValue">the new internal array. The array is <b>NOT</b> copied.</param>
			public virtual void SetValueNoCopy(char[] newValue)
			{
				Clear();
				value = newValue;
			}
		}

		/// <summary>An item whose value is a boolean choice, presented as Yes/No.</summary>
		/// <remarks>An item whose value is a boolean choice, presented as Yes/No.</remarks>
		public class YesNoType : CredentialItem
		{
			private bool value;

			/// <summary>Initialize a prompt for a single boolean answer.</summary>
			/// <remarks>Initialize a prompt for a single boolean answer.</remarks>
			/// <param name="promptText">
			/// prompt to display to the user alongside of the input
			/// field. Should be sufficient text to indicate what to
			/// supply for this item.
			/// </param>
			public YesNoType(string promptText) : base(promptText, false)
			{
			}

			public override void Clear()
			{
				value = false;
			}

			/// <returns>the current value</returns>
			public virtual bool GetValue()
			{
				return value;
			}

			/// <summary>Set the new value.</summary>
			/// <remarks>Set the new value.</remarks>
			/// <param name="newValue"></param>
			public virtual void SetValue(bool newValue)
			{
				value = newValue;
			}
		}

		/// <summary>An advice message presented to the user, with no response required.</summary>
		/// <remarks>An advice message presented to the user, with no response required.</remarks>
		public class InformationalMessage : CredentialItem
		{
			/// <summary>Initialize an informational message.</summary>
			/// <remarks>Initialize an informational message.</remarks>
			/// <param name="messageText">message to display to the user.</param>
			public InformationalMessage(string messageText) : base(messageText, false)
			{
			}

			public override void Clear()
			{
			}
			// Nothing to clear.
		}

		/// <summary>Prompt for a username, which is not masked on input.</summary>
		/// <remarks>Prompt for a username, which is not masked on input.</remarks>
		public class Username : CredentialItem.StringType
		{
			/// <summary>Initialize a new username item, with a default username prompt.</summary>
			/// <remarks>Initialize a new username item, with a default username prompt.</remarks>
			public Username() : base(JGitText.Get().credentialUsername, false)
			{
			}
		}

		/// <summary>Prompt for a password, which is masked on input.</summary>
		/// <remarks>Prompt for a password, which is masked on input.</remarks>
		public class Password : CredentialItem.CharArrayType
		{
			/// <summary>Initialize a new password item, with a default password prompt.</summary>
			/// <remarks>Initialize a new password item, with a default password prompt.</remarks>
			public Password() : base(JGitText.Get().credentialPassword, true)
			{
			}
		}
	}
}
