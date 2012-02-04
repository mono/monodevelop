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
using System.Globalization;
using NGit.Nls;
using Sharpen;

namespace NGit.Nls
{
	/// <summary>Global cache of translation bundles.</summary>
	/// <remarks>
	/// Global cache of translation bundles.
	/// <p>
	/// Every translation bundle will be cached here when it gets loaded for the
	/// first time from a thread. Another lookup for the same translation bundle
	/// (same locale and type) from the same or a different thread will return the
	/// cached one.
	/// <p>
	/// Note that NLS instances maintain per-thread Map of loaded translation
	/// bundles. Once a thread accesses a translation bundle it will keep reference
	/// to it and will not call
	/// <see cref="LookupBundle{T}(System.Globalization.CultureInfo, System.Type{T})">LookupBundle&lt;T&gt;(System.Globalization.CultureInfo, System.Type&lt;T&gt;)
	/// 	</see>
	/// again for the
	/// same translation bundle as long as its locale doesn't change.
	/// </remarks>
	internal class GlobalBundleCache
	{
		private static readonly IDictionary<CultureInfo, IDictionary<Type, TranslationBundle
			>> cachedBundles = new Dictionary<CultureInfo, IDictionary<Type, TranslationBundle
			>>();

		/// <summary>Looks up for a translation bundle in the global cache.</summary>
		/// <remarks>
		/// Looks up for a translation bundle in the global cache. If found returns
		/// the cached bundle. If not found creates a new instance puts it into the
		/// cache and returns it.
		/// </remarks>
		/// <?></?>
		/// <param name="locale">the preferred locale</param>
		/// <param name="type">required bundle type</param>
		/// <returns>an instance of the required bundle type</returns>
		/// <exception>
		/// TranslationBundleLoadingException
		/// see
		/// <see cref="TranslationBundle.Load(System.Globalization.CultureInfo)">TranslationBundle.Load(System.Globalization.CultureInfo)
		/// 	</see>
		/// </exception>
		/// <exception>
		/// TranslationStringMissingException
		/// see
		/// <see cref="TranslationBundle.Load(System.Globalization.CultureInfo)">TranslationBundle.Load(System.Globalization.CultureInfo)
		/// 	</see>
		/// </exception>
		internal static T LookupBundle<T>(CultureInfo locale) where T:TranslationBundle
		{
			System.Type type = typeof(T);
			lock (typeof(GlobalBundleCache))
			{
				try
				{
					IDictionary<Type, TranslationBundle> bundles = cachedBundles.Get(locale);
					if (bundles == null)
					{
						bundles = new Dictionary<Type, TranslationBundle>();
						cachedBundles.Put(locale, bundles);
					}
					TranslationBundle bundle = bundles.Get(type);
					if (bundle == null)
					{
						bundle = (TranslationBundle) System.Activator.CreateInstance(type);
						bundle.Load(locale);
						bundles.Put(type, bundle);
					}
					return (T)bundle;
				}
				catch (InstantiationException e)
				{
					throw new Error(e);
				}
				catch (MemberAccessException e)
				{
					throw new Error(e);
				}
			}
		}
	}
}
