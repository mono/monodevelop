/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Thad Hughes <thadh@thad.corp.google.com>
 * Copyright (C) 2009, JetBrains s.r.o.
 * Copyright (C) 2009, Google, Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core
{
    /// <summary>
    /// Git style <code>.config</code>, <code>.gitconfig</code>, <code>.gitmodules</code> file.
    /// </summary>
    public class Config
    {
        private static readonly string[] EmptyStringArray = new string[0];

        private const long GiB = 1024 * MiB;
        private const long KiB = 1024;
        private const long MiB = 1024 * KiB;

        ///	<summary>
        /// Immutable current state of the configuration data.
        ///	<para />
        /// This state is copy-on-write. It should always contain an immutable list
        /// of the configuration keys/values.
        /// </summary>
        internal AtomicReference<State> _state;

        /// Magic value indicating a missing entry.
        ///	This value is tested for reference equality in some contexts, so we
        ///	must ensure it is a special copy of the empty string.  It also must
        ///	be treated like the empty string.
        private static readonly string MagicEmptyValue = string.Empty;

        private readonly Config _baseConfig;

        /// <summary>
        /// Create a configuration with no default fallback.
        /// </summary>
        public Config()
            : this(null)
        {
        }

        ///	<summary>
        /// Create an empty configuration with a fallback for missing keys.
        ///	</summary>
        ///	<param name="defaultConfig">
        ///	the base configuration to be consulted when a key is missing
        /// from this configuration instance.
        /// </param>
        public Config(Config defaultConfig)
        {
            _baseConfig = defaultConfig;
            _state = new AtomicReference<State>(newState());
        }

        ///	<summary>
        /// Escape the value before saving
        /// </summary>
        /// <param name="x">The value to escape.</param>
        ///	<returns>The escaped value.</returns>
        private static string EscapeValue(string x)
        {
            bool inquote = false;
            int lineStart = 0;
            var r = new StringBuilder(x.Length);

            for (int k = 0; k < x.Length; k++)
            {
                char c = x[k];
                switch (c)
                {
                    case '\n':
                        if (inquote)
                        {
                            r.Append('"');
                            inquote = false;
                        }
                        r.Append("\\n\\\n");
                        lineStart = r.Length;
                        break;

                    case '\t':
                        r.Append("\\t");
                        break;

                    case '\b':
                        r.Append("\\b");
                        break;

                    case '\\':
                        r.Append("\\\\");
                        break;

                    case '"':
                        r.Append("\\\"");
                        break;

                    case ';':
                    case '#':
                        if (!inquote)
                        {
                            r.Insert(lineStart, '"');
                            inquote = true;
                        }
                        r.Append(c);
                        break;

                    case ' ':
                        if (!inquote && r.Length > 0 && r[r.Length - 1] == ' ')
                        {
                            r.Insert(lineStart, '"');
                            inquote = true;
                        }
                        r.Append(' ');
                        break;

                    default:
                        r.Append(c);
                        break;
                }
            }

            if (inquote)
            {
                r.Append('"');
            }

            return r.ToString();
        }

        ///	<summary>
        /// Obtain an integer value from the configuration.
        ///	</summary>
        ///	<param name="section">Section the key is grouped within.</param>
        ///	<param name="name">Name of the key to get.</param>
        ///	<param name="defaultValue">
        ///	Default value to return if no value was present.
        /// </param>
        ///	<returns>
        /// An integer value from the configuration, or <paramref name="defaultValue"/>.
        /// </returns>
        public int getInt(string section, string name, int defaultValue)
        {
            return getInt(section, null, name, defaultValue);
        }

        ///	<summary>
        /// Obtain an integer value from the configuration.
        ///	</summary>
        ///	<param name="section">Section the key is grouped within.</param>
        ///	<param name="subsection">
        /// Subsection name, such a remote or branch name.
        /// </param>
        ///	<param name="name">Name of the key to get.</param>
        ///	<param name="defaultValue">
        /// Default value to return if no value was present. </param>
        ///	<returns>
        /// An integer value from the configuration, or <paramref name="defaultValue"/>.
        /// </returns>
        public int getInt(string section, string subsection, string name, int defaultValue)
        {
            long val = getLong(section, subsection, name, defaultValue);
            if (int.MinValue <= val && val <= int.MaxValue)
                return (int)val;
            throw new ArgumentException("Integer value " + section + "." + name + " out of range");
        }

        ///	<summary>
        /// Obtain an integer value from the configuration.
        /// </summary>
        ///	<param name="section">Section the key is grouped within.</param>
        ///	<param name="subsection">
        /// Subsection name, such a remote or branch name.
        /// </param>
        ///	<param name="name">Name of the key to get.</param>
        ///	<param name="defaultValue">
        /// Default value to return if no value was present.
        /// </param>
        ///	<returns>
        /// An integer value from the configuration, or <paramref name="defaultValue"/>.
        /// </returns>
        public long getLong(string section, string subsection, string name, long defaultValue)
        {
            string str = getString(section, subsection, name);
            if (str == null)
                return defaultValue;

            string n = str.Trim();
            if (n.Length == 0)
                return defaultValue;

            long mul = 1;
            switch (StringUtils.toLowerCase(n[n.Length - 1]))
            {
                case 'g':
                    mul = GiB;
                    break;

                case 'm':
                    mul = MiB;
                    break;

                case 'k':
                    mul = KiB;
                    break;
            }

            if (mul > 1)
                n = n.Slice(0, n.Length - 1).Trim();

            if (n.Length == 0)
                return defaultValue;

            try
            {
                return mul * long.Parse(n);
            }
            catch (FormatException nfe)
            {
                throw new ArgumentException("Invalid long value: " + section + "." + name + "=" + str, nfe);
            }
        }

        ///	<summary>
        /// Get a boolean value from the git config.
        /// </summary>
        /// <param name="section">Section the key is grouped within.</param>
        ///	<param name="name">Name of the key to get.</param>
        ///	<param name="defaultValue">
        /// Default value to return if no value was present.
        /// </param>
        ///	<returns>
        /// True if any value or <paramref name="defaultValue"/> is true, false 
        /// for missing or explicit false.
        /// </returns>
        public bool getBoolean(string section, string name, bool defaultValue)
        {
            return getBoolean(section, null, name, defaultValue);
        }

        ///	<summary>
        /// Get a boolean value from the git config.
        /// </summary>
        ///	<param name="section">Section the key is grouped within.</param>
        ///	<param name="subsection">
        /// Subsection name, such a remote or branch name.
        /// </param>
        ///	<param name="name">Name of the key to get.</param>
        ///	<param name="defaultValue">
        /// Default value to return if no value was present.
        /// </param>
        ///	<returns>
        /// True if any value or defaultValue is true, false for missing or
        /// explicit false.
        /// </returns>
        public bool getBoolean(string section, string subsection, string name, bool defaultValue)
        {
            string n = getRawString(section, subsection, name);
            if (n == null)
                return defaultValue;

            if (MagicEmptyValue == n)
            {
                return true;
            }

            try
            {
                return StringUtils.toBoolean(n);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("Invalid boolean value: " + section + "." + name + "=" + n, e);
            }

        }

        ///	<summary>
        /// Get string value.
        ///	</summary>
        ///	<param name="section">The section.</param>
        ///	<param name="subsection">The subsection for the value.</param>
        ///	<param name="name">The key name.</param>
        ///	<returns>A <see cref="String"/> value from git config.</returns>
        public string getString(string section, string subsection, string name)
        {
            return getRawString(section, subsection, name);
        }

        ///	<summary>
        /// Get a list of string values
        ///	<para />
        /// If this instance was created with a base, the base's values are returned
        /// first (if any).
        /// </summary>
        /// <param name="section">The section.</param>
        ///	<param name="subsection">The subsection for the value.</param>
        ///	<param name="name">The key name.</param>
        ///	<returns>Array of zero or more values from the configuration.</returns>
        public string[] getStringList(string section, string subsection, string name)
        {
            string[] baseList = _baseConfig != null ? _baseConfig.getStringList(section, subsection, name) : EmptyStringArray;

            List<string> lst = getRawStringList(section, subsection, name);
            if (lst != null)
            {
                var res = new string[baseList.Length + lst.Count];
                int idx = baseList.Length;

                Array.Copy(baseList, 0, res, 0, idx);

                foreach (string val in lst)
                {
                    res[idx++] = val;
                }

                return res;
            }

            return baseList;
        }

        ///	<param name="section">Section to search for. </param>
        ///	<returns> set of all subsections of specified section within this
        /// configuration and its base configuration; may be empty if no
        /// subsection exists.
        /// </returns>
        public IList<string> getSubsections(string section)
        {
            return get(new SubsectionNames(section));
        }

        ///	<summary>
        /// Obtain a handle to a parsed set of configuration values.
        /// </summary>
        ///	<param name="parser">
        /// Parser which can create the model if it is not already
        /// available in this configuration file. The parser is also used
        /// as the key into a cache and must obey the hashCode and equals
        /// contract in order to reuse a parsed model.
        /// </param>
        ///	<returns>
        /// The parsed object instance, which is cached inside this config.
        /// </returns>
        /// <typeparam name="T">Type of configuration model to return.</typeparam>
        public T get<T>(SectionParser<T> parser)
        {
            State myState = getState();
            T obj = (T)myState.Cache.get<object, object>(parser);
            if (Equals(obj, default(T)))
            {
                obj = (T)parser.parse(this);
                myState.Cache.put<object, object>(parser, obj);
            }
            return obj;
        }

        /// <summary>
        /// Remove a cached configuration object.
        ///	<para />
        /// If the associated configuration object has not yet been cached, this
        /// method has no effect.
        /// </summary>
        ///	<param name="parser">Parser used to obtain the configuration object.</param>
        ///	<seealso cref="get{T}"/>
        public void uncache<T>(SectionParser<T> parser)
        {
            _state.get().Cache.Remove(parser);
        }

        private string getRawString(string section, string subsection, string name)
        {
            List<string> lst = getRawStringList(section, subsection, name);

            if (lst != null && lst.Count > 0)
                return lst[0];

            if (_baseConfig != null)
                return _baseConfig.getRawString(section, subsection, name);

            return null;
        }

        private List<string> getRawStringList(string section, string subsection, string name)
        {
            List<string> r = null;
            foreach (Entry e in _state.get().EntryList)
            {
                if (e.match(section, subsection, name))
                    r = add(r, e.value);
            }

            return r;
        }

        private static List<string> add(List<string> curr, string value)
        {
            if (curr == null)
                return new List<string> { value };

            curr.Add(value);
            return curr;
        }

        internal State getState()
        {
            State cur, upd;
            do
            {
                cur = _state.get();
                State @base = getBaseState();
                if (cur.baseState == @base)
                    return cur;
                upd = new State(cur.EntryList, @base);
            } while (!_state.compareAndSet(cur, upd));
            return upd;
        }

        private State getBaseState()
        {
            return _baseConfig != null ? _baseConfig.getState() : null;
        }

        /// <summary>
        /// Add or modify a configuration value. The parameters will result in a
        /// configuration entry like this.
        /// <para />
        /// <pre>
        /// [section &quot;subsection&quot;]
        /// name = value
        /// </pre>
        ///	</summary>
        ///	<param name="section">Section name, e.g "branch"</param>
        ///	<param name="subsection">Optional subsection value, e.g. a branch name.</param>
        ///	<param name="name">Parameter name, e.g. "filemode".</param>
        ///	<param name="value">Parameter value.</param>
        public void setInt(string section, string subsection, string name, int value)
        {
            setLong(section, subsection, name, value);
        }

        ///	<summary>
        /// Add or modify a configuration value. The parameters will result in a
        ///	configuration entry like this.
        /// <para />
        /// <pre>
        /// [section &quot;subsection&quot;]
        /// name = value
        /// </pre>
        ///	</summary>
        ///	<param name="section">Section name, e.g "branch"</param>
        ///	<param name="subsection">Optional subsection value, e.g. a branch name.</param>
        ///	<param name="name">Parameter name, e.g. "filemode".</param>
        ///	<param name="value">Parameter value.</param>
        public void setLong(string section, string subsection, string name, long value)
        {
            string s;
            if (value >= GiB && (value % GiB) == 0)
                s = (value / GiB) + " g";
            else if (value >= MiB && (value % MiB) == 0)
                s = (value / MiB) + " m";
            else if (value >= KiB && (value % KiB) == 0)
                s = (value / KiB) + " k";
            else
                s = value.ToString();

            setString(section, subsection, name, s);
        }

        ///	<summary>
        /// Add or modify a configuration value. The parameters will result in a
        ///	configuration entry like this.
        /// <para />
        /// <pre>
        /// [section &quot;subsection&quot;]
        /// name = value
        /// </pre>
        ///	</summary>
        ///	<param name="section">Section name, e.g "branch"</param>
        ///	<param name="subsection">Optional subsection value, e.g. a branch name.</param>
        ///	<param name="name">Parameter name, e.g. "filemode".</param>
        ///	<param name="value">Parameter value.</param>
        public void setBoolean(string section, string subsection, string name, bool value)
        {
            setString(section, subsection, name, value ? "true" : "false");
        }

        ///	<summary>
        /// Add or modify a configuration value. The parameters will result in a
        ///	configuration entry like this.
        /// <para />
        /// <pre>
        /// [section &quot;subsection&quot;]
        /// name = value
        /// </pre>
        ///	</summary>
        ///	<param name="section">Section name, e.g "branch"</param>
        ///	<param name="subsection">Optional subsection value, e.g. a branch name.</param>
        ///	<param name="name">Parameter name, e.g. "filemode".</param>
        ///	<param name="value">Parameter value.</param>
        public void setString(string section, string subsection, string name, string value)
        {
            setStringList(section, subsection, name, new List<string> { value });
        }

        ///	<summary>
        /// Remove a configuration value.
        /// </summary>
        /// <param name="section">Section name, e.g "branch".</param>
        /// <param name="subsection">Optional subsection value, e.g. a branch name.</param>
        /// <param name="name">Parameter name, e.g. "filemode".</param>
        public void unset(string section, string subsection, string name)
        {
            setStringList(section, subsection, name, new List<string>());
        }

        /// <summary>
        /// Remove all configuration values under a single section.
        /// </summary>
        /// <param name="section">section name, e.g "branch"</param>
        /// <param name="subsection">optional subsection value, e.g. a branch name</param>
        public void unsetSection(string section, string subsection)
        {
            State src, res;
            do
            {
                src = _state.get();
                res = unsetSection(src, section, subsection);
            } while (!_state.compareAndSet(src, res));
        }

        private State unsetSection(State srcState, string section,
                string subsection)
        {
            int max = srcState.EntryList.Count;
            var r = new List<Entry>(max);

            bool lastWasMatch = false;
            foreach (Entry e in srcState.EntryList)
            {
                if (e.match(section, subsection))
                {
                    // Skip this record, it's for the section we are removing.
                    lastWasMatch = true;
                    continue;
                }

                if (lastWasMatch && e.section == null && e.subsection == null)
                    continue; // skip this padding line in the section.
                r.Add(e);
            }

            return newState(r);
        }

        ///	<summary>
        /// Set a configuration value.
        /// <para />
        /// <pre>
        /// [section &quot;subsection&quot;]
        /// name = value
        /// </pre>
        ///	</summary><param name="section">Section name, e.g "branch".</param>
        ///	<param name="subsection">Optional subsection value, e.g. a branch name.</param>
        /// <param name="name">Parameter name, e.g. "filemode".</param>
        /// <param name="values">List of zero or more values for this key.</param>
        public void setStringList(string section, string subsection, string name, List<string> values)
        {
            State src, res;
            do
            {
                src = _state.get();
                res = replaceStringList(src, section, subsection, name, values);
            } while (!_state.compareAndSet(src, res));
        }

        private State replaceStringList(State srcState, string section, string subsection, string name, IList<string> values)
        {
            List<Entry> entries = copy(srcState, values);
            int entryIndex = 0;
            int valueIndex = 0;
            int insertPosition = -1;

            // Reset the first n Entry objects that match this input name.
            //
            while (entryIndex < entries.Count && valueIndex < values.Count)
            {
                Entry e = entries[entryIndex];
                if (e.match(section, subsection, name))
                {
                    entries[entryIndex] = e.forValue(values[valueIndex++]);
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
                    Entry e = entries[entryIndex++];
                    if (e.match(section, subsection, name))
                    {
                        entries.RemoveAt(--entryIndex);
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
                    insertPosition = findSectionEnd(entries, section, subsection);
                }

                if (insertPosition < 0)
                {
                    // We didn't find any matching section header for this key,
                    // so we must create a new section header at the end.
                    //
                    var e = new Entry { section = section, subsection = subsection };
                    entries.Add(e);
                    insertPosition = entries.Count;
                }

                while (valueIndex < values.Count)
                {
                    var e = new Entry
                                {
                                    section = section,
                                    subsection = subsection,
                                    name = name,
                                    value = values[valueIndex++]
                                };

                    entries.Insert(insertPosition++, e);
                }
            }

            return newState(entries);
        }

        private static List<Entry> copy(State src, ICollection<string> values)
        {
            // At worst we need to insert 1 line for each value, plus 1 line
            // for a new section header. Assume that and allocate the space.
            //
            int max = src.EntryList.Count + values.Count + 1;
            var r = new List<Entry>(max);
            r.AddRange(src.EntryList);
            return r;
        }

        private static int findSectionEnd(IList<Entry> entries, string section, string subsection)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e.match(section, subsection, null))
                {
                    i++;
                    while (i < entries.Count)
                    {
                        e = entries[i];
                        if (e.match(section, subsection, e.name))
                            i++;
                        else
                            break;
                    }
                    return i;
                }
            }
            return -1;
        }

        ///	<returns>
        /// This configuration, formatted as a Git style text file.
        /// </returns>
        public string toText()
        {
            var o = new StringBuilder();
            foreach (Entry e in _state.get().EntryList)
            {
                if (e.prefix != null)
                    o.Append(e.prefix);
                if (e.section != null && e.name == null)
                {
                    o.Append('[');
                    o.Append(e.section);

                    if (e.subsection != null)
                    {
                        o.Append(' ');
                        o.Append('"');
                        o.Append(EscapeValue(e.subsection));
                        o.Append('"');
                    }

                    o.Append(']');
                }
                else if (e.section != null && e.name != null)
                {
                    if (e.prefix == null || string.Empty.Equals(e.prefix))
                    {
                        o.Append('\t');
                    }

                    o.Append(e.name);

                    if (MagicEmptyValue != e.value)
                    {
                        o.Append(" =");
                        if (e.value != null)
                        {
                            o.Append(" ");
                            o.Append(EscapeValue(e.value));
                        }
                    }

                    if (e.suffix != null)
                    {
                        o.Append(' ');
                    }
                }

                if (e.suffix != null)
                    o.Append(e.suffix);
                {
                    o.Append('\n');
                }
            }
            return o.ToString();
        }

        /// <summary>
        /// Clear this configuration and reset to the contents of the parsed string.
        ///	</summary>
        ///	<param name="text">
        /// Git style text file listing configuration properties.
        /// </param>
        ///	<exception cref="ConfigInvalidException">
        /// The text supplied is not formatted correctly. No changes were
        /// made to this.</exception>
        public void fromText(string text)
        {
            var newEntries = new List<Entry>();
            var i = new ConfigReader(text);
            Entry last = null;
            var e = new Entry();

            while (true)
            {
                int input = i.Read();
                if (-1 == input)
                {
                    break;
                }

                var c = (char)input;
                if ('\n' == c)
                {
                    newEntries.Add(e);
                    if (e.section != null)
                        last = e;
                    e = new Entry();
                }
                else if (e.suffix != null)
                {
                    // Everything up until the end-of-line is in the suffix.
                    e.suffix += c;
                }
                else if (';' == c || '#' == c)
                {
                    // The rest of this line is a comment; put into suffix.
                    e.suffix = new string(c, 1);
                }
                else if (e.section == null && char.IsWhiteSpace(c))
                {
                    // Save the leading whitespace (if any).
                    if (e.prefix == null)
                    {
                        e.prefix = string.Empty;
                    }
                    e.prefix += c;
                }
                else if ('[' == c)
                {
                    // This is a section header.
                    e.section = readSectionName(i);
                    input = i.Read();
                    if ('"' == input)
                    {
                        e.subsection = ReadValue(i, true, '"');
                        input = i.Read();
                    }

                    if (']' != input)
                    {
                        throw new ConfigInvalidException("Bad group header");
                    }

                    e.suffix = string.Empty;
                }
                else if (last != null)
                {
                    // Read a value.
                    e.section = last.section;
                    e.subsection = last.subsection;
                    i.Reset();
                    e.name = readKeyName(i);
                    if (e.name.EndsWith("\n"))
                    {
                        e.name = e.name.Slice(0, e.name.Length - 1);
                        e.value = MagicEmptyValue;
                    }
                    else
                    {
                        e.value = ReadValue(i, false, -1);
                    }
                }
                else
                {
                    throw new ConfigInvalidException("Invalid line in config file");
                }
            }

            _state.set(newState(newEntries));
        }

        private static string ReadValue(ConfigReader i, bool quote, int eol)
        {
            var value = new StringBuilder();
            bool space = false;
            for (; ; )
            {
                int c = i.Read();
                if (c < 0)
                {
                    if (value.Length == 0)
                        throw new ConfigInvalidException("Unexpected end of config file");
                    break;
                }

                if ('\n' == c)
                {
                    if (quote)
                        throw new ConfigInvalidException("Newline in quotes not allowed");
                    i.Reset();
                    break;
                }

                if (eol == c)
                    break;

                if (!quote)
                {
                    if (char.IsWhiteSpace((char)c))
                    {
                        space = true;
                        continue;
                    }

                    if (';' == c || '#' == c)
                    {
                        i.Reset();
                        break;
                    }
                }

                if (space)
                {
                    if (value.Length > 0)
                        value.Append(' ');
                    space = false;
                }

                if ('\\' == c)
                {
                    c = i.Read();
                    switch (c)
                    {
                        case -1:
                            throw new ConfigInvalidException("End of file in escape");

                        case '\n':
                            continue;

                        case 't':
                            value.Append('\t');
                            continue;

                        case 'b':
                            value.Append('\b');
                            continue;

                        case 'n':
                            value.Append('\n');
                            continue;

                        case '\\':
                            value.Append('\\');
                            continue;

                        case '"':
                            value.Append('"');
                            continue;

                        default:
                            throw new ConfigInvalidException("Bad escape: " + ((char)c));
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

        private State newState()
        {
            return new State(new List<Entry>(), getBaseState());
        }

        private State newState(List<Entry> entries)
        {
            return new State(entries, getBaseState());
        }

        protected void clear()
        {
            _state.set(newState());
        }

        private static string readSectionName(ConfigReader i)
        {
            var name = new StringBuilder();
            for (; ; )
            {
                int c = i.Read();
                if (c < 0)
                    throw new ConfigInvalidException("Unexpected end of config file");

                if (']' == c)
                {
                    i.Reset();
                    break;
                }

                if (' ' == c || '\t' == c)
                {
                    for (; ; )
                    {
                        c = i.Read();
                        if (c < 0)
                            throw new ConfigInvalidException("Unexpected end of config file");

                        if ('"' == c)
                        {
                            i.Reset();
                            break;
                        }

                        if (' ' == c || '\t' == c)
                        {
                            continue;
                        }
                        throw new ConfigInvalidException("Bad section entry: " + name);
                    }
                    break;
                }

                if (char.IsLetterOrDigit((char)c) || '.' == c || '-' == c)
                    name.Append((char)c);
                else
                    throw new ConfigInvalidException("Bad section entry: " + name);
            }
            return name.ToString();
        }

        private static string readKeyName(ConfigReader i)
        {
			var name = new StringBuilder();
			
            for (; ; )
            {
                int c = i.Read();
                if (c < 0)
                    throw new ConfigInvalidException("Unexpected end of config file");

                if ('=' == c)
                    break;

                if (' ' == c || '\t' == c)
                {
                    for (; ; )
                    {
                        c = i.Read();
                        if (c < 0)
                            throw new ConfigInvalidException("Unexpected end of config file");

                        if ('=' == c)
                            break;

                        if (';' == c || '#' == c || '\n' == c)
                        {
                            i.Reset();
                            break;
                        }

                        if (' ' == c || '\t' == c)
                            continue;
                        throw new ConfigInvalidException("Bad entry delimiter");
                    }
                    break;
                }

                if (char.IsLetterOrDigit((char)c) || c == '-')
                {
                    name.Append((char)c);
                }
                else if ('\n' == c)
                {
                    i.Reset();
                    name.Append((char)c);
                    break;
                }
                else
                    throw new ConfigInvalidException("Bad entry name: " + name);
            }

            return name.ToString();
        }

        #region Nested type: ConfigReader

        private class ConfigReader
        {
            private readonly string data;
            private readonly int len;
            private int position;

            public ConfigReader(string text)
            {
                data = text;
                len = data.Length;
            }

            public int Read()
            {
                int ret = -1;
                if (position < len)
                {
                    ret = data[position];
                    position++;
                }
                return ret;
            }

            public void Reset()
            {
                // no idea what the java pendant actually does..
                //position = 0;
                --position;
            }
        }

        #endregion

        #region Nested type: Entry

        /// <summary>
        /// The configuration file entry.
        /// </summary>
        internal class Entry
        {
            /// <summary>
            /// The key name.
            /// </summary>
            public string name;

            /// <summary>
            /// The text content before entry.
            /// </summary>
            public string prefix;

            /// <summary>
            /// The section name for the entry.
            /// </summary>
            public string section;

            /// <summary>
            /// Subsection name.
            /// </summary>
            public string subsection;

            /// <summary>
            /// The text content after entry.
            /// </summary>
            public string suffix;

            /// <summary>
            /// The value
            /// </summary>
            public string value;

            public Entry forValue(string newValue)
            {
                var e = new Entry
                            {
                                prefix = prefix,
                                section = section,
                                subsection = subsection,
                                name = name,
                                value = newValue,
                                suffix = suffix
                            };
                return e;
            }

            public bool match(string aSection, string aSubsection, string aKey)
            {
                return eqIgnoreCase(section, aSection)
                       && eqSameCase(subsection, aSubsection)
                       && eqIgnoreCase(name, aKey);
            }

            public bool match(string aSection, string aSubsection)
            {
                return eqIgnoreCase(section, aSection)
                        && eqSameCase(subsection, aSubsection);
            }
            private static bool eqIgnoreCase(string a, string b)
            {
                if (a == null && b == null)
                    return true;
                if (a == null || b == null)
                    return false;
                return StringUtils.equalsIgnoreCase(a, b);
            }

            private static bool eqSameCase(string a, string b)
            {
                if (a == null && b == null)
                    return true;
                if (a == null || b == null)
                    return false;
                return a.Equals(b);
            }
        }

        #endregion

        #region Nested type: SectionParser

        ///	<summary>
        /// Parses a section of the configuration into an application model object.
        ///	<para />
        ///	Instances must implement hashCode and equals such that model objects can
        ///	be cached by using the <see cref="SectionParser{T}"/> as a key of a
        /// Dictionary.
        ///	<para />
        ///	As the <see cref="SectionParser{T}"/> itself is used as the key of the internal
        ///	Dictionary applications should be careful to ensure the SectionParser key
        ///	does not retain unnecessary application state which may cause memory to
        ///	be held longer than expected.
        ///	</summary>
        /// <typeparam name="T">type of the application model created by the parser.</typeparam>
        public interface SectionParser<T>
        {
            ///	<summary>
            /// Create a model object from a configuration.
            ///	</summary>
            ///	<param name="cfg">
            ///	The configuration to read values from.
            /// </param>
            ///	<returns>The application model instance.</returns>
            T parse(Config cfg);
        }

        #endregion

        #region Nested type: State

        internal class State
        {
            public readonly State baseState;
            public readonly Dictionary<object, object> Cache;
            public readonly List<Entry> EntryList;

            public State(List<Entry> entries, State @base)
            {
                EntryList = entries;
                Cache = new Dictionary<object, object>();
                baseState = @base;
            }
        }

        #endregion

        #region Nested type: SubsectionNames

        private class SubsectionNames : SectionParser<IList<string>>
        {
            private readonly string section;

            public SubsectionNames(string sectionName)
            {
                section = sectionName;
            }

            #region SectionParser<List<string>> Members

            public IList<string> parse(Config cfg)
            {
                var result = new List<string>();
                while (cfg != null)
                {
                    foreach (Entry e in cfg._state.get().EntryList)
                    {
                        if (e.subsection != null && e.name == null && StringUtils.equalsIgnoreCase(section, e.section))
                            result.Add(e.subsection);
                    }
                    cfg = cfg._baseConfig;
                }
                return result.AsReadOnly();
            }

            #endregion

            public override int GetHashCode()
            {
                return section.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                SubsectionNames oSub = (obj as SubsectionNames);
                if (oSub != null)
                    return section.Equals(oSub.section);
                return false;
            }
        }

        #endregion
    }
}