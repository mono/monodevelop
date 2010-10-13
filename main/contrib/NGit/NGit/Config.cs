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

using System;
using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Events;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Git style
	/// <code>.config</code>
	/// ,
	/// <code>.gitconfig</code>
	/// ,
	/// <code>.gitmodules</code>
	/// file.
	/// </summary>
	public class Config
	{
		private static readonly string[] EMPTY_STRING_ARRAY = new string[] {  };

		private const long KiB = 1024;

		private const long MiB = 1024 * KiB;

		private const long GiB = 1024 * MiB;

		/// <summary>the change listeners</summary>
		private readonly ListenerList listeners = new ListenerList();

		/// <summary>Immutable current state of the configuration data.</summary>
		/// <remarks>
		/// Immutable current state of the configuration data.
		/// <p>
		/// This state is copy-on-write. It should always contain an immutable list
		/// of the configuration keys/values.
		/// </remarks>
		private readonly AtomicReference<Config.State> state;

		private readonly NGit.Config baseConfig;

		/// <summary>Magic value indicating a missing entry.</summary>
		/// <remarks>
		/// Magic value indicating a missing entry.
		/// <p>
		/// This value is tested for reference equality in some contexts, so we
		/// must ensure it is a special copy of the empty string.  It also must
		/// be treated like the empty string.
		/// </remarks>
		private static readonly string MAGIC_EMPTY_VALUE = string.Empty;

		/// <summary>Create a configuration with no default fallback.</summary>
		/// <remarks>Create a configuration with no default fallback.</remarks>
		public Config() : this(null)
		{
		}

		/// <summary>Create an empty configuration with a fallback for missing keys.</summary>
		/// <remarks>Create an empty configuration with a fallback for missing keys.</remarks>
		/// <param name="defaultConfig">
		/// the base configuration to be consulted when a key is missing
		/// from this configuration instance.
		/// </param>
		public Config(NGit.Config defaultConfig)
		{
			baseConfig = defaultConfig;
			state = new AtomicReference<Config.State>(NewState());
		}

		/// <summary>Escape the value before saving</summary>
		/// <param name="x">the value to escape</param>
		/// <returns>the escaped value</returns>
		private static string EscapeValue(string x)
		{
			bool inquote = false;
			int lineStart = 0;
			StringBuilder r = new StringBuilder(x.Length);
			for (int k = 0; k < x.Length; k++)
			{
				char c = x[k];
				switch (c)
				{
					case '\n':
					{
						if (inquote)
						{
							r.Append('"');
							inquote = false;
						}
						r.Append("\\n\\\n");
						lineStart = r.Length;
						break;
					}

					case '\t':
					{
						r.Append("\\t");
						break;
					}

					case '\b':
					{
						r.Append("\\b");
						break;
					}

					case '\\':
					{
						r.Append("\\\\");
						break;
					}

					case '"':
					{
						r.Append("\\\"");
						break;
					}

					case ';':
					case '#':
					{
						if (!inquote)
						{
							r.Insert(lineStart, '"');
							inquote = true;
						}
						r.Append(c);
						break;
					}

					case ' ':
					{
						if (!inquote && r.Length > 0 && r[r.Length - 1] == ' ')
						{
							r.Insert(lineStart, '"');
							inquote = true;
						}
						r.Append(' ');
						break;
					}

					default:
					{
						r.Append(c);
						break;
						break;
					}
				}
			}
			if (inquote)
			{
				r.Append('"');
			}
			return r.ToString();
		}

		/// <summary>Obtain an integer value from the configuration.</summary>
		/// <remarks>Obtain an integer value from the configuration.</remarks>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>an integer value from the configuration, or defaultValue.</returns>
		public virtual int GetInt(string section, string name, int defaultValue)
		{
			return GetInt(section, null, name, defaultValue);
		}

		/// <summary>Obtain an integer value from the configuration.</summary>
		/// <remarks>Obtain an integer value from the configuration.</remarks>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="subsection">subsection name, such a remote or branch name.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>an integer value from the configuration, or defaultValue.</returns>
		public virtual int GetInt(string section, string subsection, string name, int defaultValue
			)
		{
			long val = GetLong(section, subsection, name, defaultValue);
			if (int.MinValue <= val && val <= int.MaxValue)
			{
				return (int)val;
			}
			throw new ArgumentException(MessageFormat.Format(JGitText.Get().integerValueOutOfRange
				, section, name));
		}

		/// <summary>Obtain an integer value from the configuration.</summary>
		/// <remarks>Obtain an integer value from the configuration.</remarks>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>an integer value from the configuration, or defaultValue.</returns>
		public virtual long GetLong(string section, string name, long defaultValue)
		{
			return GetLong(section, null, name, defaultValue);
		}

		/// <summary>Obtain an integer value from the configuration.</summary>
		/// <remarks>Obtain an integer value from the configuration.</remarks>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="subsection">subsection name, such a remote or branch name.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>an integer value from the configuration, or defaultValue.</returns>
		public virtual long GetLong(string section, string subsection, string name, long 
			defaultValue)
		{
			string str = GetString(section, subsection, name);
			if (str == null)
			{
				return defaultValue;
			}
			string n = str.Trim();
			if (n.Length == 0)
			{
				return defaultValue;
			}
			long mul = 1;
			switch (StringUtils.ToLowerCase(n[n.Length - 1]))
			{
				case 'g':
				{
					mul = GiB;
					break;
				}

				case 'm':
				{
					mul = MiB;
					break;
				}

				case 'k':
				{
					mul = KiB;
					break;
				}
			}
			if (mul > 1)
			{
				n = Sharpen.Runtime.Substring(n, 0, n.Length - 1).Trim();
			}
			if (n.Length == 0)
			{
				return defaultValue;
			}
			try
			{
				return mul * long.Parse(n);
			}
			catch (FormatException)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidIntegerValue
					, section, name, str));
			}
		}

		/// <summary>Get a boolean value from the git config</summary>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>
		/// true if any value or defaultValue is true, false for missing or
		/// explicit false
		/// </returns>
		public virtual bool GetBoolean(string section, string name, bool defaultValue)
		{
			return GetBoolean(section, null, name, defaultValue);
		}

		/// <summary>Get a boolean value from the git config</summary>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="subsection">subsection name, such a remote or branch name.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>
		/// true if any value or defaultValue is true, false for missing or
		/// explicit false
		/// </returns>
		public virtual bool GetBoolean(string section, string subsection, string name, bool
			 defaultValue)
		{
			string n = GetRawString(section, subsection, name);
			if (n == null)
			{
				return defaultValue;
			}
			if (MAGIC_EMPTY_VALUE == n)
			{
				return true;
			}
			try
			{
				return StringUtils.ToBoolean(n);
			}
			catch (ArgumentException)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidBooleanValue
					, section, name, n));
			}
		}

		/// <summary>Parse an enumeration from the configuration.</summary>
		/// <remarks>Parse an enumeration from the configuration.</remarks>
		/// <?></?>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="subsection">subsection name, such a remote or branch name.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>
		/// the selected enumeration value, or
		/// <code>defaultValue</code>
		/// .
		/// </returns>
		public virtual T GetEnum<T>(string section, string subsection, string name, T defaultValue
			)
		{
			Array all = AllValuesOf(defaultValue);
			return GetEnum(all, section, subsection, name, defaultValue);
		}

		private static Array AllValuesOf<T>(T value)
		{
			try
			{
				return Enum.GetValues (typeof(T));
			}
			catch (Exception err)
			{
				string typeName = value.GetType().FullName;
				string msg = MessageFormat.Format(JGitText.Get().enumValuesNotAvailable, typeName
					);
				throw new ArgumentException(msg, err);
			}
		}

		/// <summary>Parse an enumeration from the configuration.</summary>
		/// <remarks>Parse an enumeration from the configuration.</remarks>
		/// <?></?>
		/// <param name="all">
		/// all possible values in the enumeration which should be
		/// recognized. Typically
		/// <code>EnumType.values()</code>
		/// .
		/// </param>
		/// <param name="section">section the key is grouped within.</param>
		/// <param name="subsection">subsection name, such a remote or branch name.</param>
		/// <param name="name">name of the key to get.</param>
		/// <param name="defaultValue">default value to return if no value was present.</param>
		/// <returns>
		/// the selected enumeration value, or
		/// <code>defaultValue</code>
		/// .
		/// </returns>
		public virtual T GetEnum<T>(Array all, string section, string subsection, string name
			, T defaultValue)
		{
			string value = GetString(section, subsection, name);
			if (value == null)
			{
				return defaultValue;
			}
			string n = value.Replace(' ', '_');
			object trueState = null;
			object falseState = null;
			foreach (object e in all)
			{
				if (StringUtils.EqualsIgnoreCase(e.ToString(), n))
				{
					return (T)e;
				}
				else
				{
					if (StringUtils.EqualsIgnoreCase(e.ToString(), "TRUE"))
					{
						trueState = e;
					}
					else
					{
						if (StringUtils.EqualsIgnoreCase(e.ToString(), "FALSE"))
						{
							falseState = e;
						}
					}
				}
			}
			// This is an odd little fallback. C Git sometimes allows boolean
			// values in a tri-state with other things. If we have both a true
			// and a false value in our enumeration, assume its one of those.
			//
			if (trueState != null && falseState != null)
			{
				try
				{
					return StringUtils.ToBoolean(n) ? (T)trueState : (T)falseState;
				}
				catch (ArgumentException)
				{
				}
			}
			// Fall through and use our custom error below.
			if (subsection != null)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().enumValueNotSupported3
					, section, name, value));
			}
			else
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().enumValueNotSupported2
					, section, name, value));
			}
		}

		/// <summary>Get string value</summary>
		/// <param name="section">the section</param>
		/// <param name="subsection">the subsection for the value</param>
		/// <param name="name">the key name</param>
		/// <returns>a String value from git config.</returns>
		public virtual string GetString(string section, string subsection, string name)
		{
			return GetRawString(section, subsection, name);
		}

		/// <summary>
		/// Get a list of string values
		/// <p>
		/// If this instance was created with a base, the base's values are returned
		/// first (if any).
		/// </summary>
		/// <remarks>
		/// Get a list of string values
		/// <p>
		/// If this instance was created with a base, the base's values are returned
		/// first (if any).
		/// </remarks>
		/// <param name="section">the section</param>
		/// <param name="subsection">the subsection for the value</param>
		/// <param name="name">the key name</param>
		/// <returns>array of zero or more values from the configuration.</returns>
		public virtual string[] GetStringList(string section, string subsection, string name
			)
		{
			string[] baseList;
			if (baseConfig != null)
			{
				baseList = baseConfig.GetStringList(section, subsection, name);
			}
			else
			{
				baseList = EMPTY_STRING_ARRAY;
			}
			IList<string> lst = GetRawStringList(section, subsection, name);
			if (lst != null)
			{
				string[] res = new string[baseList.Length + lst.Count];
				int idx = baseList.Length;
				System.Array.Copy(baseList, 0, res, 0, idx);
				foreach (string val in lst)
				{
					res[idx++] = val;
				}
				return res;
			}
			return baseList;
		}

		/// <param name="section">section to search for.</param>
		/// <returns>
		/// set of all subsections of specified section within this
		/// configuration and its base configuration; may be empty if no
		/// subsection exists.
		/// </returns>
		public virtual ICollection<string> GetSubsections(string section)
		{
			return Get(new Config.SubsectionNames(section));
		}

		/// <returns>
		/// the sections defined in this
		/// <see cref="Config">Config</see>
		/// </returns>
		public virtual ICollection<string> GetSections()
		{
			return Get(new Config.SectionNames());
		}

		/// <param name="section">the section</param>
		/// <returns>the list of names defined for this section</returns>
		public virtual ICollection<string> GetNames(string section)
		{
			return GetNames(section, null);
		}

		/// <param name="section">the section</param>
		/// <param name="subsection">the subsection</param>
		/// <returns>the list of names defined for this subsection</returns>
		public virtual ICollection<string> GetNames(string section, string subsection)
		{
			return Get(new Config.NamesInSection(section, subsection));
		}

		/// <summary>Obtain a handle to a parsed set of configuration values.</summary>
		/// <remarks>Obtain a handle to a parsed set of configuration values.</remarks>
		/// <?></?>
		/// <param name="parser">
		/// parser which can create the model if it is not already
		/// available in this configuration file. The parser is also used
		/// as the key into a cache and must obey the hashCode and equals
		/// contract in order to reuse a parsed model.
		/// </param>
		/// <returns>the parsed object instance, which is cached inside this config.</returns>
		public virtual T Get<T>(Config.SectionParser<T> parser)
		{
			Config.State myState = GetState();
			T obj = (T)myState.cache.Get(parser);
			if (obj == null)
			{
				obj = parser.Parse(this);
				myState.cache.Put(parser, obj);
			}
			return obj;
		}

		/// <summary>Remove a cached configuration object.</summary>
		/// <remarks>
		/// Remove a cached configuration object.
		/// <p>
		/// If the associated configuration object has not yet been cached, this
		/// method has no effect.
		/// </remarks>
		/// <param name="parser">parser used to obtain the configuration object.</param>
		/// <seealso cref="Get{T}(SectionParser{T})">Get&lt;T&gt;(SectionParser&lt;T&gt;)</seealso>
		public virtual void Uncache<_T0>(Config.SectionParser<_T0> parser)
		{
			Sharpen.Collections.Remove(state.Get().cache, parser);
		}

		/// <summary>Adds a listener to be notified about changes.</summary>
		/// <remarks>
		/// Adds a listener to be notified about changes.
		/// <p>
		/// Clients are supposed to remove the listeners after they are done with
		/// them using the
		/// <see cref="NGit.Events.ListenerHandle.Remove()">NGit.Events.ListenerHandle.Remove()
		/// 	</see>
		/// method
		/// </remarks>
		/// <param name="listener">the listener</param>
		/// <returns>the handle to the registered listener</returns>
		public virtual ListenerHandle AddChangeListener(ConfigChangedListener listener)
		{
			return listeners.AddConfigChangedListener(listener);
		}

		/// <summary>Determine whether to issue change events for transient changes.</summary>
		/// <remarks>
		/// Determine whether to issue change events for transient changes.
		/// <p>
		/// If <code>true</code> is returned (which is the default behavior),
		/// <see cref="FireConfigChangedEvent()">FireConfigChangedEvent()</see>
		/// will be called upon each change.
		/// <p>
		/// Subclasses that override this to return <code>false</code> are
		/// responsible for issuing
		/// <see cref="FireConfigChangedEvent()">FireConfigChangedEvent()</see>
		/// calls
		/// themselves.
		/// </remarks>
		/// <returns><code></code></returns>
		protected internal virtual bool NotifyUponTransientChanges()
		{
			return true;
		}

		/// <summary>Notifies the listeners</summary>
		protected internal virtual void FireConfigChangedEvent()
		{
			listeners.Dispatch(new ConfigChangedEvent());
		}

		private string GetRawString(string section, string subsection, string name)
		{
			IList<string> lst = GetRawStringList(section, subsection, name);
			if (lst != null)
			{
				return lst[0];
			}
			else
			{
				if (baseConfig != null)
				{
					return baseConfig.GetRawString(section, subsection, name);
				}
				else
				{
					return null;
				}
			}
		}

		private IList<string> GetRawStringList(string section, string subsection, string 
			name)
		{
			IList<string> r = null;
			foreach (Config.Entry e in state.Get().entryList)
			{
				if (e.Match(section, subsection, name))
				{
					r = Add(r, e.value);
				}
			}
			return r;
		}

		private static IList<string> Add(IList<string> curr, string value)
		{
			if (curr == null)
			{
				return Sharpen.Collections.SingletonList(value);
			}
			if (curr.Count == 1)
			{
				IList<string> r = new AList<string>(2);
				r.AddItem(curr[0]);
				r.AddItem(value);
				return r;
			}
			curr.AddItem(value);
			return curr;
		}

		private Config.State GetState()
		{
			Config.State cur;
			Config.State upd;
			do
			{
				cur = state.Get();
				Config.State @base = GetBaseState();
				if (cur.baseState == @base)
				{
					return cur;
				}
				upd = new Config.State(cur.entryList, @base);
			}
			while (!state.CompareAndSet(cur, upd));
			return upd;
		}

		private Config.State GetBaseState()
		{
			return baseConfig != null ? baseConfig.GetState() : null;
		}

		/// <summary>Add or modify a configuration value.</summary>
		/// <remarks>
		/// Add or modify a configuration value. The parameters will result in a
		/// configuration entry like this.
		/// <pre>
		/// [section &quot;subsection&quot;]
		/// name = value
		/// </pre>
		/// </remarks>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		/// <param name="name">parameter name, e.g. "filemode"</param>
		/// <param name="value">parameter value</param>
		public virtual void SetInt(string section, string subsection, string name, int value
			)
		{
			SetLong(section, subsection, name, value);
		}

		/// <summary>Add or modify a configuration value.</summary>
		/// <remarks>
		/// Add or modify a configuration value. The parameters will result in a
		/// configuration entry like this.
		/// <pre>
		/// [section &quot;subsection&quot;]
		/// name = value
		/// </pre>
		/// </remarks>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		/// <param name="name">parameter name, e.g. "filemode"</param>
		/// <param name="value">parameter value</param>
		public virtual void SetLong(string section, string subsection, string name, long 
			value)
		{
			string s;
			if (value >= GiB && (value % GiB) == 0)
			{
				s = (value / GiB).ToString() + " g";
			}
			else
			{
				if (value >= MiB && (value % MiB) == 0)
				{
					s = (value / MiB).ToString() + " m";
				}
				else
				{
					if (value >= KiB && (value % KiB) == 0)
					{
						s = (value / KiB).ToString() + " k";
					}
					else
					{
						s = value.ToString();
					}
				}
			}
			SetString(section, subsection, name, s);
		}

		/// <summary>Add or modify a configuration value.</summary>
		/// <remarks>
		/// Add or modify a configuration value. The parameters will result in a
		/// configuration entry like this.
		/// <pre>
		/// [section &quot;subsection&quot;]
		/// name = value
		/// </pre>
		/// </remarks>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		/// <param name="name">parameter name, e.g. "filemode"</param>
		/// <param name="value">parameter value</param>
		public virtual void SetBoolean(string section, string subsection, string name, bool
			 value)
		{
			SetString(section, subsection, name, value ? "true" : "false");
		}

		/// <summary>Add or modify a configuration value.</summary>
		/// <remarks>
		/// Add or modify a configuration value. The parameters will result in a
		/// configuration entry like this.
		/// <pre>
		/// [section &quot;subsection&quot;]
		/// name = value
		/// </pre>
		/// </remarks>
		/// <?></?>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		/// <param name="name">parameter name, e.g. "filemode"</param>
		/// <param name="value">parameter value</param>
		public virtual void SetEnum<T>(string section, string subsection, string name, T 
			value)
		{
			string n = value.ToString().ToLower().Replace('_', ' ');
			SetString(section, subsection, name, n);
		}

		/// <summary>Add or modify a configuration value.</summary>
		/// <remarks>
		/// Add or modify a configuration value. The parameters will result in a
		/// configuration entry like this.
		/// <pre>
		/// [section &quot;subsection&quot;]
		/// name = value
		/// </pre>
		/// </remarks>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		/// <param name="name">parameter name, e.g. "filemode"</param>
		/// <param name="value">parameter value, e.g. "true"</param>
		public virtual void SetString(string section, string subsection, string name, string
			 value)
		{
			SetStringList(section, subsection, name, Sharpen.Collections.SingletonList(value)
				);
		}

		/// <summary>Remove a configuration value.</summary>
		/// <remarks>Remove a configuration value.</remarks>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		/// <param name="name">parameter name, e.g. "filemode"</param>
		public virtual void Unset(string section, string subsection, string name)
		{
			SetStringList(section, subsection, name, Sharpen.Collections.EmptyList<string>());
		}

		/// <summary>Remove all configuration values under a single section.</summary>
		/// <remarks>Remove all configuration values under a single section.</remarks>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		public virtual void UnsetSection(string section, string subsection)
		{
			Config.State src;
			Config.State res;
			do
			{
				src = state.Get();
				res = UnsetSection(src, section, subsection);
			}
			while (!state.CompareAndSet(src, res));
		}

		private Config.State UnsetSection(Config.State srcState, string section, string subsection
			)
		{
			int max = srcState.entryList.Count;
			AList<Config.Entry> r = new AList<Config.Entry>(max);
			bool lastWasMatch = false;
			foreach (Config.Entry e in srcState.entryList)
			{
				if (e.Match(section, subsection))
				{
					// Skip this record, it's for the section we are removing.
					lastWasMatch = true;
					continue;
				}
				if (lastWasMatch && e.section == null && e.subsection == null)
				{
					continue;
				}
				// skip this padding line in the section.
				r.AddItem(e);
			}
			return NewState(r);
		}

		/// <summary>Set a configuration value.</summary>
		/// <remarks>
		/// Set a configuration value.
		/// <pre>
		/// [section &quot;subsection&quot;]
		/// name = value
		/// </pre>
		/// </remarks>
		/// <param name="section">section name, e.g "branch"</param>
		/// <param name="subsection">optional subsection value, e.g. a branch name</param>
		/// <param name="name">parameter name, e.g. "filemode"</param>
		/// <param name="values">list of zero or more values for this key.</param>
		public virtual void SetStringList(string section, string subsection, string name, 
			IList<string> values)
		{
			Config.State src;
			Config.State res;
			do
			{
				src = state.Get();
				res = ReplaceStringList(src, section, subsection, name, values);
			}
			while (!state.CompareAndSet(src, res));
			if (NotifyUponTransientChanges())
			{
				FireConfigChangedEvent();
			}
		}

		private Config.State ReplaceStringList(Config.State srcState, string section, string
			 subsection, string name, IList<string> values)
		{
			IList<Config.Entry> entries = Copy(srcState, values);
			int entryIndex = 0;
			int valueIndex = 0;
			int insertPosition = -1;
			// Reset the first n Entry objects that match this input name.
			//
			while (entryIndex < entries.Count && valueIndex < values.Count)
			{
				Config.Entry e = entries[entryIndex];
				if (e.Match(section, subsection, name))
				{
					entries.Set(entryIndex, e.ForValue(values[valueIndex++]));
					insertPosition = entryIndex + 1;
				}
				entryIndex++;
			}
			// Remove any extra Entry objects that we no longer need.
			//
			if (valueIndex == values.Count && entryIndex < entries.Count)
			{
				while (entryIndex < entries.Count)
				{
					Config.Entry e = entries[entryIndex++];
					if (e.Match(section, subsection, name))
					{
						entries.Remove(--entryIndex);
					}
				}
			}
			// Insert new Entry objects for additional/new values.
			//
			if (valueIndex < values.Count && entryIndex == entries.Count)
			{
				if (insertPosition < 0)
				{
					// We didn't find a matching key above, but maybe there
					// is already a section available that matches. Insert
					// after the last key of that section.
					//
					insertPosition = FindSectionEnd(entries, section, subsection);
				}
				if (insertPosition < 0)
				{
					// We didn't find any matching section header for this key,
					// so we must create a new section header at the end.
					//
					Config.Entry e = new Config.Entry();
					e.section = section;
					e.subsection = subsection;
					entries.AddItem(e);
					insertPosition = entries.Count;
				}
				while (valueIndex < values.Count)
				{
					Config.Entry e = new Config.Entry();
					e.section = section;
					e.subsection = subsection;
					e.name = name;
					e.value = values[valueIndex++];
					entries.Add(insertPosition++, e);
				}
			}
			return NewState(entries);
		}

		private static IList<Config.Entry> Copy(Config.State src, IList<string> values)
		{
			// At worst we need to insert 1 line for each value, plus 1 line
			// for a new section header. Assume that and allocate the space.
			//
			int max = src.entryList.Count + values.Count + 1;
			AList<Config.Entry> r = new AList<Config.Entry>(max);
			Sharpen.Collections.AddAll(r, src.entryList);
			return r;
		}

		private static int FindSectionEnd(IList<Config.Entry> entries, string section, string
			 subsection)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				Config.Entry e = entries[i];
				if (e.Match(section, subsection, null))
				{
					i++;
					while (i < entries.Count)
					{
						e = entries[i];
						if (e.Match(section, subsection, e.name))
						{
							i++;
						}
						else
						{
							break;
						}
					}
					return i;
				}
			}
			return -1;
		}

		/// <returns>this configuration, formatted as a Git style text file.</returns>
		public virtual string ToText()
		{
			StringBuilder @out = new StringBuilder();
			foreach (Config.Entry e in state.Get().entryList)
			{
				if (e.prefix != null)
				{
					@out.Append(e.prefix);
				}
				if (e.section != null && e.name == null)
				{
					@out.Append('[');
					@out.Append(e.section);
					if (e.subsection != null)
					{
						@out.Append(' ');
						string escaped = EscapeValue(e.subsection);
						// make sure to avoid double quotes here
						bool quoted = escaped.StartsWith("\"") && escaped.EndsWith("\"");
						if (!quoted)
						{
							@out.Append('"');
						}
						@out.Append(escaped);
						if (!quoted)
						{
							@out.Append('"');
						}
					}
					@out.Append(']');
				}
				else
				{
					if (e.section != null && e.name != null)
					{
						if (e.prefix == null || string.Empty.Equals(e.prefix))
						{
							@out.Append('\t');
						}
						@out.Append(e.name);
						if (MAGIC_EMPTY_VALUE != e.value)
						{
							@out.Append(" =");
							if (e.value != null)
							{
								@out.Append(' ');
								@out.Append(EscapeValue(e.value));
							}
						}
						if (e.suffix != null)
						{
							@out.Append(' ');
						}
					}
				}
				if (e.suffix != null)
				{
					@out.Append(e.suffix);
				}
				@out.Append('\n');
			}
			return @out.ToString();
		}

		/// <summary>Clear this configuration and reset to the contents of the parsed string.
		/// 	</summary>
		/// <remarks>Clear this configuration and reset to the contents of the parsed string.
		/// 	</remarks>
		/// <param name="text">Git style text file listing configuration properties.</param>
		/// <exception cref="NGit.Errors.ConfigInvalidException">
		/// the text supplied is not formatted correctly. No changes were
		/// made to
		/// <code>this</code>
		/// .
		/// </exception>
		public virtual void FromText(string text)
		{
			IList<Config.Entry> newEntries = new AList<Config.Entry>();
			Config.StringReader @in = new Config.StringReader(text);
			Config.Entry last = null;
			Config.Entry e = new Config.Entry();
			for (; ; )
			{
				int input = @in.Read();
				if (-1 == input)
				{
					break;
				}
				char c = (char)input;
				if ('\n' == c)
				{
					// End of this entry.
					newEntries.AddItem(e);
					if (e.section != null)
					{
						last = e;
					}
					e = new Config.Entry();
				}
				else
				{
					if (e.suffix != null)
					{
						// Everything up until the end-of-line is in the suffix.
						e.suffix += c;
					}
					else
					{
						if (';' == c || '#' == c)
						{
							// The rest of this line is a comment; put into suffix.
							e.suffix = c.ToString();
						}
						else
						{
							if (e.section == null && char.IsWhiteSpace(c))
							{
								// Save the leading whitespace (if any).
								if (e.prefix == null)
								{
									e.prefix = string.Empty;
								}
								e.prefix += c;
							}
							else
							{
								if ('[' == c)
								{
									// This is a section header.
									e.section = ReadSectionName(@in);
									input = @in.Read();
									if ('"' == input)
									{
										e.subsection = ReadValue(@in, true, '"');
										input = @in.Read();
									}
									if (']' != input)
									{
										throw new ConfigInvalidException(JGitText.Get().badGroupHeader);
									}
									e.suffix = string.Empty;
								}
								else
								{
									if (last != null)
									{
										// Read a value.
										e.section = last.section;
										e.subsection = last.subsection;
										@in.Reset();
										e.name = ReadKeyName(@in);
										if (e.name.EndsWith("\n"))
										{
											e.name = Sharpen.Runtime.Substring(e.name, 0, e.name.Length - 1);
											e.value = MAGIC_EMPTY_VALUE;
										}
										else
										{
											e.value = ReadValue(@in, false, -1);
										}
									}
									else
									{
										throw new ConfigInvalidException(JGitText.Get().invalidLineInConfigFile);
									}
								}
							}
						}
					}
				}
			}
			state.Set(NewState(newEntries));
		}

		private Config.State NewState()
		{
			return new Config.State(Sharpen.Collections.EmptyList<Config.Entry>(), GetBaseState
				());
		}

		private Config.State NewState(IList<Config.Entry> entries)
		{
			return new Config.State(Sharpen.Collections.UnmodifiableList(entries), GetBaseState
				());
		}

		/// <summary>Clear the configuration file</summary>
		protected internal virtual void Clear()
		{
			state.Set(NewState());
		}

		/// <exception cref="NGit.Errors.ConfigInvalidException"></exception>
		private static string ReadSectionName(Config.StringReader @in)
		{
			StringBuilder name = new StringBuilder();
			for (; ; )
			{
				int c = @in.Read();
				if (c < 0)
				{
					throw new ConfigInvalidException(JGitText.Get().unexpectedEndOfConfigFile);
				}
				if (']' == c)
				{
					@in.Reset();
					break;
				}
				if (' ' == c || '\t' == c)
				{
					for (; ; )
					{
						c = @in.Read();
						if (c < 0)
						{
							throw new ConfigInvalidException(JGitText.Get().unexpectedEndOfConfigFile);
						}
						if ('"' == c)
						{
							@in.Reset();
							break;
						}
						if (' ' == c || '\t' == c)
						{
							continue;
						}
						// Skipped...
						throw new ConfigInvalidException(MessageFormat.Format(JGitText.Get().badSectionEntry
							, name));
					}
					break;
				}
				if (char.IsLetterOrDigit((char)c) || '.' == c || '-' == c)
				{
					name.Append((char)c);
				}
				else
				{
					throw new ConfigInvalidException(MessageFormat.Format(JGitText.Get().badSectionEntry
						, name));
				}
			}
			return name.ToString();
		}

		/// <exception cref="NGit.Errors.ConfigInvalidException"></exception>
		private static string ReadKeyName(Config.StringReader @in)
		{
			StringBuilder name = new StringBuilder();
			for (; ; )
			{
				int c = @in.Read();
				if (c < 0)
				{
					throw new ConfigInvalidException(JGitText.Get().unexpectedEndOfConfigFile);
				}
				if ('=' == c)
				{
					break;
				}
				if (' ' == c || '\t' == c)
				{
					for (; ; )
					{
						c = @in.Read();
						if (c < 0)
						{
							throw new ConfigInvalidException(JGitText.Get().unexpectedEndOfConfigFile);
						}
						if ('=' == c)
						{
							break;
						}
						if (';' == c || '#' == c || '\n' == c)
						{
							@in.Reset();
							break;
						}
						if (' ' == c || '\t' == c)
						{
							continue;
						}
						// Skipped...
						throw new ConfigInvalidException(JGitText.Get().badEntryDelimiter);
					}
					break;
				}
				if (char.IsLetterOrDigit((char)c) || c == '-')
				{
					// From the git-config man page:
					// The variable names are case-insensitive and only
					// alphanumeric characters and - are allowed.
					name.Append((char)c);
				}
				else
				{
					if ('\n' == c)
					{
						@in.Reset();
						name.Append((char)c);
						break;
					}
					else
					{
						throw new ConfigInvalidException(MessageFormat.Format(JGitText.Get().badEntryName
							, name));
					}
				}
			}
			return name.ToString();
		}

		/// <exception cref="NGit.Errors.ConfigInvalidException"></exception>
		private static string ReadValue(Config.StringReader @in, bool quote, int eol)
		{
			StringBuilder value = new StringBuilder();
			bool space = false;
			for (; ; )
			{
				int c = @in.Read();
				if (c < 0)
				{
					if (value.Length == 0)
					{
						throw new ConfigInvalidException(JGitText.Get().unexpectedEndOfConfigFile);
					}
					break;
				}
				if ('\n' == c)
				{
					if (quote)
					{
						throw new ConfigInvalidException(JGitText.Get().newlineInQuotesNotAllowed);
					}
					@in.Reset();
					break;
				}
				if (eol == c)
				{
					break;
				}
				if (!quote)
				{
					if (char.IsWhiteSpace((char)c))
					{
						space = true;
						continue;
					}
					if (';' == c || '#' == c)
					{
						@in.Reset();
						break;
					}
				}
				if (space)
				{
					if (value.Length > 0)
					{
						value.Append(' ');
					}
					space = false;
				}
				if ('\\' == c)
				{
					c = @in.Read();
					switch (c)
					{
						case -1:
						{
							throw new ConfigInvalidException(JGitText.Get().endOfFileInEscape);
						}

						case '\n':
						{
							continue;
							goto case 't';
						}

						case 't':
						{
							value.Append('\t');
							continue;
							goto case 'b';
						}

						case 'b':
						{
							value.Append('\b');
							continue;
							goto case 'n';
						}

						case 'n':
						{
							value.Append('\n');
							continue;
							goto case '\\';
						}

						case '\\':
						{
							value.Append('\\');
							continue;
							goto case '"';
						}

						case '"':
						{
							value.Append('"');
							continue;
							goto default;
						}

						default:
						{
							throw new ConfigInvalidException(MessageFormat.Format(JGitText.Get().badEscape, (
								(char)c)));
						}
					}
				}
				if ('"' == c)
				{
					quote = !quote;
					continue;
				}
				value.Append((char)c);
			}
			return value.Length > 0 ? value.ToString() : null;
		}

		/// <summary>Parses a section of the configuration into an application model object.</summary>
		/// <remarks>
		/// Parses a section of the configuration into an application model object.
		/// <p>
		/// Instances must implement hashCode and equals such that model objects can
		/// be cached by using the
		/// <code>SectionParser</code>
		/// as a key of a HashMap.
		/// <p>
		/// As the
		/// <code>SectionParser</code>
		/// itself is used as the key of the internal
		/// HashMap applications should be careful to ensure the SectionParser key
		/// does not retain unnecessary application state which may cause memory to
		/// be held longer than expected.
		/// </remarks>
		/// <?></?>
		public interface SectionParser<T>
		{
			/// <summary>Create a model object from a configuration.</summary>
			/// <remarks>Create a model object from a configuration.</remarks>
			/// <param name="cfg">the configuration to read values from.</param>
			/// <returns>the application model instance.</returns>
			T Parse(Config cfg);
		}

		private class SubsectionNames : Config.SectionParser<ICollection<string>>
		{
			private readonly string section;

			internal SubsectionNames(string sectionName)
			{
				section = sectionName;
			}

			public override int GetHashCode()
			{
				return section.GetHashCode();
			}

			public override bool Equals(object other)
			{
				if (other is Config.SubsectionNames)
				{
					return section.Equals(((Config.SubsectionNames)other).section);
				}
				return false;
			}

			public virtual ICollection<string> Parse(Config cfg)
			{
				ICollection<string> result = new HashSet<string>();
				while (cfg != null)
				{
					foreach (Config.Entry e in cfg.state.Get().entryList)
					{
						if (e.subsection != null && e.name == null && StringUtils.EqualsIgnoreCase(section
							, e.section))
						{
							result.AddItem(e.subsection);
						}
					}
					cfg = cfg.baseConfig;
				}
				return Sharpen.Collections.UnmodifiableSet(result);
			}
		}

		private class NamesInSection : Config.SectionParser<ICollection<string>>
		{
			private readonly string section;

			private readonly string subsection;

			internal NamesInSection(string sectionName, string subSectionName)
			{
				section = sectionName;
				subsection = subSectionName;
			}

			public override int GetHashCode()
			{
				int prime = 31;
				int result = 1;
				result = prime * result + section.GetHashCode();
				result = prime * result + ((subsection == null) ? 0 : subsection.GetHashCode());
				return result;
			}

			public override bool Equals(object obj)
			{
				if (this == obj)
				{
					return true;
				}
				if (obj == null)
				{
					return false;
				}
				if (GetType() != obj.GetType())
				{
					return false;
				}
				Config.NamesInSection other = (Config.NamesInSection)obj;
				if (!section.Equals(other.section))
				{
					return false;
				}
				if (subsection == null)
				{
					if (other.subsection != null)
					{
						return false;
					}
				}
				else
				{
					if (!subsection.Equals(other.subsection))
					{
						return false;
					}
				}
				return true;
			}

			public virtual ICollection<string> Parse(Config cfg)
			{
				ICollection<string> result = new HashSet<string>();
				while (cfg != null)
				{
					foreach (Config.Entry e in cfg.state.Get().entryList)
					{
						if (e.name != null && StringUtils.EqualsIgnoreCase(e.section, section))
						{
							if (subsection == null && e.subsection == null)
							{
								result.AddItem(StringUtils.ToLowerCase(e.name));
							}
							else
							{
								if (e.subsection != null && e.subsection.Equals(subsection))
								{
									result.AddItem(StringUtils.ToLowerCase(e.name));
								}
							}
						}
					}
					cfg = cfg.baseConfig;
				}
				return Sharpen.Collections.UnmodifiableSet(result);
			}
		}

		private class SectionNames : Config.SectionParser<ICollection<string>>
		{
			public virtual ICollection<string> Parse(Config cfg)
			{
				ICollection<string> result = new HashSet<string>();
				while (cfg != null)
				{
					foreach (Config.Entry e in cfg.state.Get().entryList)
					{
						if (e.section != null)
						{
							result.AddItem(StringUtils.ToLowerCase(e.section));
						}
					}
					cfg = cfg.baseConfig;
				}
				return Sharpen.Collections.UnmodifiableSet(result);
			}
		}

		private class State
		{
			internal readonly IList<Config.Entry> entryList;

			internal readonly IDictionary<object, object> cache;

			internal readonly Config.State baseState;

			internal State(IList<Config.Entry> entries, Config.State @base)
			{
				entryList = entries;
				cache = new ConcurrentHashMap<object, object>(16, 0.75f, 1);
				baseState = @base;
			}
		}

		/// <summary>The configuration file entry</summary>
		private class Entry
		{
			/// <summary>The text content before entry</summary>
			internal string prefix;

			/// <summary>The section name for the entry</summary>
			internal string section;

			/// <summary>Subsection name</summary>
			internal string subsection;

			/// <summary>The key name</summary>
			internal string name;

			/// <summary>The value</summary>
			internal string value;

			/// <summary>The text content after entry</summary>
			internal string suffix;

			internal virtual Config.Entry ForValue(string newValue)
			{
				Config.Entry e = new Config.Entry();
				e.prefix = prefix;
				e.section = section;
				e.subsection = subsection;
				e.name = name;
				e.value = newValue;
				e.suffix = suffix;
				return e;
			}

			internal virtual bool Match(string aSection, string aSubsection, string aKey)
			{
				return EqIgnoreCase(section, aSection) && EqSameCase(subsection, aSubsection) && 
					EqIgnoreCase(name, aKey);
			}

			internal virtual bool Match(string aSection, string aSubsection)
			{
				return EqIgnoreCase(section, aSection) && EqSameCase(subsection, aSubsection);
			}

			private static bool EqIgnoreCase(string a, string b)
			{
				if (a == null && b == null)
				{
					return true;
				}
				if (a == null || b == null)
				{
					return false;
				}
				return StringUtils.EqualsIgnoreCase(a, b);
			}

			private static bool EqSameCase(string a, string b)
			{
				if (a == null && b == null)
				{
					return true;
				}
				if (a == null || b == null)
				{
					return false;
				}
				return a.Equals(b);
			}
		}

		private class StringReader
		{
			private readonly char[] buf;

			private int pos;

			internal StringReader(string @in)
			{
				buf = @in.ToCharArray();
			}

			internal virtual int Read()
			{
				try
				{
					return buf[pos++];
				}
				catch (IndexOutOfRangeException)
				{
					pos = buf.Length;
					return -1;
				}
			}

			internal virtual void Reset()
			{
				pos--;
			}
		}
	}
}
