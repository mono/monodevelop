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
using System.Text;
using NGit;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Describes how refs in one repository copy into another repository.</summary>
	/// <remarks>
	/// Describes how refs in one repository copy into another repository.
	/// <p>
	/// A ref specification provides matching support and limited rules to rewrite a
	/// reference in one repository to another reference in another repository.
	/// </remarks>
	[System.Serializable]
	public class RefSpec
	{
		private const long serialVersionUID = 1L;

		/// <summary>
		/// Suffix for wildcard ref spec component, that indicate matching all refs
		/// with specified prefix.
		/// </summary>
		/// <remarks>
		/// Suffix for wildcard ref spec component, that indicate matching all refs
		/// with specified prefix.
		/// </remarks>
		public static readonly string WILDCARD_SUFFIX = "/*";

		/// <summary>Check whether provided string is a wildcard ref spec component.</summary>
		/// <remarks>Check whether provided string is a wildcard ref spec component.</remarks>
		/// <param name="s">ref spec component - string to test. Can be null.</param>
		/// <returns>true if provided string is a wildcard ref spec component.</returns>
		public static bool IsWildcard(string s)
		{
			return s != null && s.EndsWith(WILDCARD_SUFFIX);
		}

		/// <summary>Does this specification ask for forced updated (rewind/reset)?</summary>
		private bool force;

		/// <summary>Is this specification actually a wildcard match?</summary>
		private bool wildcard;

		/// <summary>Name of the ref(s) we would copy from.</summary>
		/// <remarks>Name of the ref(s) we would copy from.</remarks>
		private string srcName;

		/// <summary>Name of the ref(s) we would copy into.</summary>
		/// <remarks>Name of the ref(s) we would copy into.</remarks>
		private string dstName;

		/// <summary>Construct an empty RefSpec.</summary>
		/// <remarks>
		/// Construct an empty RefSpec.
		/// <p>
		/// A newly created empty RefSpec is not suitable for use in most
		/// applications, as at least one field must be set to match a source name.
		/// </remarks>
		public RefSpec()
		{
			force = false;
			wildcard = false;
			srcName = Constants.HEAD;
			dstName = null;
		}

		/// <summary>Parse a ref specification for use during transport operations.</summary>
		/// <remarks>
		/// Parse a ref specification for use during transport operations.
		/// <p>
		/// Specifications are typically one of the following forms:
		/// <ul>
		/// <li><code>refs/head/master</code></li>
		/// <li><code>refs/head/master:refs/remotes/origin/master</code></li>
		/// <li><code>refs/head/*:refs/remotes/origin/*</code></li>
		/// <li><code>+refs/head/master</code></li>
		/// <li><code>+refs/head/master:refs/remotes/origin/master</code></li>
		/// <li><code>+refs/head/*:refs/remotes/origin/*</code></li>
		/// <li><code>:refs/head/master</code></li>
		/// </ul>
		/// </remarks>
		/// <param name="spec">string describing the specification.</param>
		/// <exception cref="System.ArgumentException">the specification is invalid.</exception>
		public RefSpec(string spec)
		{
			string s = spec;
			if (s.StartsWith("+"))
			{
				force = true;
				s = Sharpen.Runtime.Substring(s, 1);
			}
			int c = s.LastIndexOf(':');
			if (c == 0)
			{
				s = Sharpen.Runtime.Substring(s, 1);
				if (IsWildcard(s))
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidWildcards, 
						spec));
				}
				dstName = s;
			}
			else
			{
				if (c > 0)
				{
					srcName = Sharpen.Runtime.Substring(s, 0, c);
					dstName = Sharpen.Runtime.Substring(s, c + 1);
					if (IsWildcard(srcName) && IsWildcard(dstName))
					{
						wildcard = true;
					}
					else
					{
						if (IsWildcard(srcName) || IsWildcard(dstName))
						{
							throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidWildcards, 
								spec));
						}
					}
				}
				else
				{
					if (IsWildcard(s))
					{
						throw new ArgumentException(MessageFormat.Format(JGitText.Get().invalidWildcards, 
							spec));
					}
					srcName = s;
				}
			}
		}

		private RefSpec(NGit.Transport.RefSpec p)
		{
			force = p.IsForceUpdate();
			wildcard = p.IsWildcard();
			srcName = p.GetSource();
			dstName = p.GetDestination();
		}

		/// <summary>Check if this specification wants to forcefully update the destination.</summary>
		/// <remarks>Check if this specification wants to forcefully update the destination.</remarks>
		/// <returns>true if this specification asks for updates without merge tests.</returns>
		public virtual bool IsForceUpdate()
		{
			return force;
		}

		/// <summary>Create a new RefSpec with a different force update setting.</summary>
		/// <remarks>Create a new RefSpec with a different force update setting.</remarks>
		/// <param name="forceUpdate">new value for force update in the returned instance.</param>
		/// <returns>a new RefSpec with force update as specified.</returns>
		public virtual NGit.Transport.RefSpec SetForceUpdate(bool forceUpdate)
		{
			NGit.Transport.RefSpec r = new NGit.Transport.RefSpec(this);
			r.force = forceUpdate;
			return r;
		}

		/// <summary>Check if this specification is actually a wildcard pattern.</summary>
		/// <remarks>
		/// Check if this specification is actually a wildcard pattern.
		/// <p>
		/// If this is a wildcard pattern then the source and destination names
		/// returned by
		/// <see cref="GetSource()">GetSource()</see>
		/// and
		/// <see cref="GetDestination()">GetDestination()</see>
		/// will not
		/// be actual ref names, but instead will be patterns.
		/// </remarks>
		/// <returns>true if this specification could match more than one ref.</returns>
		public virtual bool IsWildcard()
		{
			return wildcard;
		}

		/// <summary>Get the source ref description.</summary>
		/// <remarks>
		/// Get the source ref description.
		/// <p>
		/// During a fetch this is the name of the ref on the remote repository we
		/// are fetching from. During a push this is the name of the ref on the local
		/// repository we are pushing out from.
		/// </remarks>
		/// <returns>name (or wildcard pattern) to match the source ref.</returns>
		public virtual string GetSource()
		{
			return srcName;
		}

		/// <summary>Create a new RefSpec with a different source name setting.</summary>
		/// <remarks>Create a new RefSpec with a different source name setting.</remarks>
		/// <param name="source">new value for source in the returned instance.</param>
		/// <returns>a new RefSpec with source as specified.</returns>
		/// <exception cref="System.InvalidOperationException">
		/// There is already a destination configured, and the wildcard
		/// status of the existing destination disagrees with the
		/// wildcard status of the new source.
		/// </exception>
		public virtual NGit.Transport.RefSpec SetSource(string source)
		{
			NGit.Transport.RefSpec r = new NGit.Transport.RefSpec(this);
			r.srcName = source;
			if (IsWildcard(r.srcName) && r.dstName == null)
			{
				throw new InvalidOperationException(JGitText.Get().destinationIsNotAWildcard);
			}
			if (IsWildcard(r.srcName) != IsWildcard(r.dstName))
			{
				throw new InvalidOperationException(JGitText.Get().sourceDestinationMustMatch);
			}
			return r;
		}

		/// <summary>Get the destination ref description.</summary>
		/// <remarks>
		/// Get the destination ref description.
		/// <p>
		/// During a fetch this is the local tracking branch that will be updated
		/// with the new ObjectId after fetching is complete. During a push this is
		/// the remote ref that will be updated by the remote's receive-pack process.
		/// <p>
		/// If null during a fetch no tracking branch should be updated and the
		/// ObjectId should be stored transiently in order to prepare a merge.
		/// <p>
		/// If null during a push, use
		/// <see cref="GetSource()">GetSource()</see>
		/// instead.
		/// </remarks>
		/// <returns>name (or wildcard) pattern to match the destination ref.</returns>
		public virtual string GetDestination()
		{
			return dstName;
		}

		/// <summary>Create a new RefSpec with a different destination name setting.</summary>
		/// <remarks>Create a new RefSpec with a different destination name setting.</remarks>
		/// <param name="destination">new value for destination in the returned instance.</param>
		/// <returns>a new RefSpec with destination as specified.</returns>
		/// <exception cref="System.InvalidOperationException">
		/// There is already a source configured, and the wildcard status
		/// of the existing source disagrees with the wildcard status of
		/// the new destination.
		/// </exception>
		public virtual NGit.Transport.RefSpec SetDestination(string destination)
		{
			NGit.Transport.RefSpec r = new NGit.Transport.RefSpec(this);
			r.dstName = destination;
			if (IsWildcard(r.dstName) && r.srcName == null)
			{
				throw new InvalidOperationException(JGitText.Get().sourceIsNotAWildcard);
			}
			if (IsWildcard(r.srcName) != IsWildcard(r.dstName))
			{
				throw new InvalidOperationException(JGitText.Get().sourceDestinationMustMatch);
			}
			return r;
		}

		/// <summary>Create a new RefSpec with a different source/destination name setting.</summary>
		/// <remarks>Create a new RefSpec with a different source/destination name setting.</remarks>
		/// <param name="source">new value for source in the returned instance.</param>
		/// <param name="destination">new value for destination in the returned instance.</param>
		/// <returns>a new RefSpec with destination as specified.</returns>
		/// <exception cref="System.ArgumentException">
		/// The wildcard status of the new source disagrees with the
		/// wildcard status of the new destination.
		/// </exception>
		public virtual NGit.Transport.RefSpec SetSourceDestination(string source, string 
			destination)
		{
			if (IsWildcard(source) != IsWildcard(destination))
			{
				throw new InvalidOperationException(JGitText.Get().sourceDestinationMustMatch);
			}
			NGit.Transport.RefSpec r = new NGit.Transport.RefSpec(this);
			r.wildcard = IsWildcard(source);
			r.srcName = source;
			r.dstName = destination;
			return r;
		}

		/// <summary>Does this specification's source description match the ref name?</summary>
		/// <param name="r">ref name that should be tested.</param>
		/// <returns>true if the names match; false otherwise.</returns>
		public virtual bool MatchSource(string r)
		{
			return Match(r, GetSource());
		}

		/// <summary>Does this specification's source description match the ref?</summary>
		/// <param name="r">ref whose name should be tested.</param>
		/// <returns>true if the names match; false otherwise.</returns>
		public virtual bool MatchSource(Ref r)
		{
			return Match(r.GetName(), GetSource());
		}

		/// <summary>Does this specification's destination description match the ref name?</summary>
		/// <param name="r">ref name that should be tested.</param>
		/// <returns>true if the names match; false otherwise.</returns>
		public virtual bool MatchDestination(string r)
		{
			return Match(r, GetDestination());
		}

		/// <summary>Does this specification's destination description match the ref?</summary>
		/// <param name="r">ref whose name should be tested.</param>
		/// <returns>true if the names match; false otherwise.</returns>
		public virtual bool MatchDestination(Ref r)
		{
			return Match(r.GetName(), GetDestination());
		}

		/// <summary>Expand this specification to exactly match a ref name.</summary>
		/// <remarks>
		/// Expand this specification to exactly match a ref name.
		/// <p>
		/// Callers must first verify the passed ref name matches this specification,
		/// otherwise expansion results may be unpredictable.
		/// </remarks>
		/// <param name="r">
		/// a ref name that matched our source specification. Could be a
		/// wildcard also.
		/// </param>
		/// <returns>
		/// a new specification expanded from provided ref name. Result
		/// specification is wildcard if and only if provided ref name is
		/// wildcard.
		/// </returns>
		public virtual NGit.Transport.RefSpec ExpandFromSource(string r)
		{
			return IsWildcard() ? new NGit.Transport.RefSpec(this).ExpandFromSourceImp(r) : this;
		}

		private NGit.Transport.RefSpec ExpandFromSourceImp(string name)
		{
			string psrc = srcName;
			string pdst = dstName;
			wildcard = false;
			srcName = name;
			dstName = Sharpen.Runtime.Substring(pdst, 0, pdst.Length - 1) + Sharpen.Runtime.Substring
				(name, psrc.Length - 1);
			return this;
		}

		/// <summary>Expand this specification to exactly match a ref.</summary>
		/// <remarks>
		/// Expand this specification to exactly match a ref.
		/// <p>
		/// Callers must first verify the passed ref matches this specification,
		/// otherwise expansion results may be unpredictable.
		/// </remarks>
		/// <param name="r">
		/// a ref that matched our source specification. Could be a
		/// wildcard also.
		/// </param>
		/// <returns>
		/// a new specification expanded from provided ref name. Result
		/// specification is wildcard if and only if provided ref name is
		/// wildcard.
		/// </returns>
		public virtual NGit.Transport.RefSpec ExpandFromSource(Ref r)
		{
			return ExpandFromSource(r.GetName());
		}

		/// <summary>Expand this specification to exactly match a ref name.</summary>
		/// <remarks>
		/// Expand this specification to exactly match a ref name.
		/// <p>
		/// Callers must first verify the passed ref name matches this specification,
		/// otherwise expansion results may be unpredictable.
		/// </remarks>
		/// <param name="r">
		/// a ref name that matched our destination specification. Could
		/// be a wildcard also.
		/// </param>
		/// <returns>
		/// a new specification expanded from provided ref name. Result
		/// specification is wildcard if and only if provided ref name is
		/// wildcard.
		/// </returns>
		public virtual NGit.Transport.RefSpec ExpandFromDestination(string r)
		{
			return IsWildcard() ? new NGit.Transport.RefSpec(this).ExpandFromDstImp(r) : this;
		}

		private NGit.Transport.RefSpec ExpandFromDstImp(string name)
		{
			string psrc = srcName;
			string pdst = dstName;
			wildcard = false;
			srcName = Sharpen.Runtime.Substring(psrc, 0, psrc.Length - 1) + Sharpen.Runtime.Substring
				(name, pdst.Length - 1);
			dstName = name;
			return this;
		}

		/// <summary>Expand this specification to exactly match a ref.</summary>
		/// <remarks>
		/// Expand this specification to exactly match a ref.
		/// <p>
		/// Callers must first verify the passed ref matches this specification,
		/// otherwise expansion results may be unpredictable.
		/// </remarks>
		/// <param name="r">a ref that matched our destination specification.</param>
		/// <returns>
		/// a new specification expanded from provided ref name. Result
		/// specification is wildcard if and only if provided ref name is
		/// wildcard.
		/// </returns>
		public virtual NGit.Transport.RefSpec ExpandFromDestination(Ref r)
		{
			return ExpandFromDestination(r.GetName());
		}

		private bool Match(string refName, string s)
		{
			if (s == null)
			{
				return false;
			}
			if (IsWildcard())
			{
				return refName.StartsWith(Sharpen.Runtime.Substring(s, 0, s.Length - 1));
			}
			return refName.Equals(s);
		}

		public override int GetHashCode()
		{
			int hc = 0;
			if (GetSource() != null)
			{
				hc = hc * 31 + GetSource().GetHashCode();
			}
			if (GetDestination() != null)
			{
				hc = hc * 31 + GetDestination().GetHashCode();
			}
			return hc;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is NGit.Transport.RefSpec))
			{
				return false;
			}
			NGit.Transport.RefSpec b = (NGit.Transport.RefSpec)obj;
			if (IsForceUpdate() != b.IsForceUpdate())
			{
				return false;
			}
			if (IsWildcard() != b.IsWildcard())
			{
				return false;
			}
			if (!Eq(GetSource(), b.GetSource()))
			{
				return false;
			}
			if (!Eq(GetDestination(), b.GetDestination()))
			{
				return false;
			}
			return true;
		}

		private static bool Eq(string a, string b)
		{
			if (a == b)
			{
				return true;
			}
			if (a == null || b == null)
			{
				return false;
			}
			return a.Equals(b);
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			if (IsForceUpdate())
			{
				r.Append('+');
			}
			if (GetSource() != null)
			{
				r.Append(GetSource());
			}
			if (GetDestination() != null)
			{
				r.Append(':');
				r.Append(GetDestination());
			}
			return r.ToString();
		}
	}
}
