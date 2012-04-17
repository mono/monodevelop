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
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit
{
	internal class ConfigSnapshot
	{
		internal readonly IList<ConfigLine> entryList;

		internal readonly IDictionary<object, object> cache;

		internal readonly NGit.ConfigSnapshot baseState;

		internal volatile IList<ConfigLine> sorted;

		internal volatile ConfigSnapshot.SectionNames names;

		internal ConfigSnapshot(IList<ConfigLine> entries, NGit.ConfigSnapshot @base)
		{
			entryList = entries;
			cache = new ConcurrentHashMap<object, object>(16, 0.75f, 1);
			baseState = @base;
		}

		internal virtual ICollection<string> GetSections()
		{
			return Names().sections;
		}

		internal virtual ICollection<string> GetSubsections(string section)
		{
			IDictionary<string, ICollection<string>> m = Names().subsections;
			ICollection<string> r = m.Get(section);
			if (r == null)
			{
				r = m.Get(StringUtils.ToLowerCase(section));
			}
			if (r == null)
			{
				return Sharpen.Collections.EmptySet<string>();
			}
			return Sharpen.Collections.UnmodifiableSet(r);
		}

		internal virtual ICollection<string> GetNames(string section, string subsection)
		{
			IList<ConfigLine> s = Sorted();
			int idx = Find(s, section, subsection, string.Empty);
			if (idx < 0)
			{
				idx = -(idx + 1);
			}
			IDictionary<string, string> m = new LinkedHashMap<string, string>();
			while (idx < s.Count)
			{
				ConfigLine e = s[idx++];
				if (!e.Match(section, subsection))
				{
					break;
				}
				if (e.name == null)
				{
					continue;
				}
				string l = StringUtils.ToLowerCase(e.name);
				if (!m.ContainsKey(l))
				{
					m.Put(l, e.name);
				}
			}
			return new ConfigSnapshot.CaseFoldingSet(m);
		}

		internal virtual string[] Get(string section, string subsection, string name)
		{
			IList<ConfigLine> s = Sorted();
			int idx = Find(s, section, subsection, name);
			if (idx < 0)
			{
				return null;
			}
			int end = End(s, idx, section, subsection, name);
			string[] r = new string[end - idx];
			for (int i = 0; idx < end; )
			{
				r[i++] = s[idx++].value;
			}
			return r;
		}

		private int Find(IList<ConfigLine> s, string s1, string s2, string name)
		{
			int low = 0;
			int high = s.Count;
			while (low < high)
			{
				int mid = (int)(((uint)(low + high)) >> 1);
				ConfigLine e = s[mid];
				int cmp = Compare2(s1, s2, name, e.section, e.subsection, e.name);
				if (cmp < 0)
				{
					high = mid;
				}
				else
				{
					if (cmp == 0)
					{
						return First(s, mid, s1, s2, name);
					}
					else
					{
						low = mid + 1;
					}
				}
			}
			return -(low + 1);
		}

		private int First(IList<ConfigLine> s, int i, string s1, string s2, string n)
		{
			while (0 < i)
			{
				if (s[i - 1].Match(s1, s2, n))
				{
					i--;
				}
				else
				{
					return i;
				}
			}
			return i;
		}

		private int End(IList<ConfigLine> s, int i, string s1, string s2, string n)
		{
			while (i < s.Count)
			{
				if (s[i].Match(s1, s2, n))
				{
					i++;
				}
				else
				{
					return i;
				}
			}
			return i;
		}

		private IList<ConfigLine> Sorted()
		{
			IList<ConfigLine> r = sorted;
			if (r == null)
			{
				sorted = r = Sort(entryList);
			}
			return r;
		}

		private static IList<ConfigLine> Sort(IList<ConfigLine> @in)
		{
			IList<ConfigLine> sorted = new AList<ConfigLine>(@in.Count);
			foreach (ConfigLine line in @in)
			{
				if (line.section != null && line.name != null)
				{
					sorted.AddItem(line);
				}
			}
			sorted.Sort(new ConfigSnapshot.LineComparator());
			return sorted;
		}

		private static int Compare2(string aSection, string aSubsection, string aName, string
			 bSection, string bSubsection, string bName)
		{
			int c = StringUtils.CompareIgnoreCase(aSection, bSection);
			if (c != 0)
			{
				return c;
			}
			if (aSubsection == null && bSubsection != null)
			{
				return -1;
			}
			if (aSubsection != null && bSubsection == null)
			{
				return 1;
			}
			if (aSubsection != null)
			{
				c = StringUtils.CompareWithCase(aSubsection, bSubsection);
				if (c != 0)
				{
					return c;
				}
			}
			return StringUtils.CompareIgnoreCase(aName, bName);
		}

		private class LineComparator : IComparer<ConfigLine>
		{
			public virtual int Compare(ConfigLine a, ConfigLine b)
			{
				var value = Compare2(a.section, a.subsection, a.name, b.section, b.subsection, b.name);
				return value != 0 ? value : string.CompareOrdinal (a.value, b.value);
			}
		}

		private ConfigSnapshot.SectionNames Names()
		{
			ConfigSnapshot.SectionNames n = names;
			if (n == null)
			{
				names = n = new ConfigSnapshot.SectionNames(this);
			}
			return n;
		}

		internal class SectionNames
		{
			internal readonly ConfigSnapshot.CaseFoldingSet sections;

			internal readonly IDictionary<string, ICollection<string>> subsections;

			internal SectionNames(ConfigSnapshot cfg)
			{
				IDictionary<string, string> sec = new LinkedHashMap<string, string>();
				IDictionary<string, ICollection<string>> sub = new Dictionary<string, ICollection
					<string>>();
				while (cfg != null)
				{
					foreach (ConfigLine e in cfg.entryList)
					{
						if (e.section == null)
						{
							continue;
						}
						string l1 = StringUtils.ToLowerCase(e.section);
						if (!sec.ContainsKey(l1))
						{
							sec.Put(l1, e.section);
						}
						if (e.subsection == null)
						{
							continue;
						}
						ICollection<string> m = sub.Get(l1);
						if (m == null)
						{
							m = new LinkedHashSet<string>();
							sub.Put(l1, m);
						}
						m.AddItem(e.subsection);
					}
					cfg = cfg.baseState;
				}
				sections = new ConfigSnapshot.CaseFoldingSet(sec);
				subsections = sub;
			}
		}

		internal class CaseFoldingSet : AbstractSet<string>
		{
			private readonly IDictionary<string, string> names;

			internal CaseFoldingSet(IDictionary<string, string> names)
			{
				this.names = names;
			}

			public override bool Contains(object needle)
			{
				if (needle is string)
				{
					string n = (string)needle;
					return names.ContainsKey(n) || names.ContainsKey(StringUtils.ToLowerCase(n));
				}
				return false;
			}

			public override Sharpen.Iterator<string> Iterator()
			{
				Sharpen.Iterator<string> i = names.Values.Iterator();
				return new _Iterator_276(i);
			}

			private sealed class _Iterator_276 : Sharpen.Iterator<string>
			{
				public _Iterator_276(Sharpen.Iterator<string> i)
				{
					this.i = i;
				}

				public override bool HasNext()
				{
					return i.HasNext();
				}

				public override string Next()
				{
					return i.Next();
				}

				public override void Remove()
				{
					throw new NotSupportedException();
				}

				private readonly Sharpen.Iterator<string> i;
			}

			public override int Count
			{
				get
				{
					return names.Count;
				}
			}
		}
	}
}
