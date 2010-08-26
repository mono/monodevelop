/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
    /// <summary>
    /// Describes how refs in one repository copy into another repository.
    /// <para />
    /// A ref specification provides matching support and limited rules to rewrite a
    /// reference in one repository to another reference in another repository.
    /// </summary>
    public class RefSpec
    {
        /// <summary>
        /// Suffix for wildcard ref spec component, that indicate matching all refs
        /// with specified prefix.                                                 
        /// </summary>
        public const string WILDCARD_SUFFIX = "/*";

        /// <summary>
        /// Check whether provided string is a wildcard ref spec component.
        /// </summary>
        /// <param name="s">ref spec component - string to test. Can be null.</param>
        /// <returns>true if provided string is a wildcard ref spec component.</returns>
        public static bool IsWildcard(string s)
        {
            return s != null && s.EndsWith(WILDCARD_SUFFIX);
        }

        /// <summary>
        /// Check if this specification wants to forcefully update the destination.
        /// <para/>
        /// Returns true if this specification asks for updates without merge tests.
        /// </summary>
        public bool Force { get; private set; }

        /// <summary>
        /// Check if this specification is actually a wildcard pattern.
        /// <para/>
        /// If this is a wildcard pattern then the source and destination names
        /// returned by <see cref="Source"/> and <see cref="Destination"/> will not
        /// be actual ref names, but instead will be patterns.
        /// <para/>
        /// Returns true if this specification could match more than one ref.</summary>
        public bool Wildcard { get; private set; }

        /// <summary>
        /// Get the source ref description.
        /// <para/>
        /// During a fetch this is the name of the ref on the remote repository we
        /// are fetching from. During a push this is the name of the ref on the local
        /// repository we are pushing out from.
        /// <para/>
        /// Returns name (or wildcard pattern) to match the source ref.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Get the destination ref description.
        /// <para/>
        /// During a fetch this is the local tracking branch that will be updated
        /// with the new ObjectId after fetching is complete. During a push this is
        /// the remote ref that will be updated by the remote's receive-pack process.
        /// <para/>
        /// If null during a fetch no tracking branch should be updated and the
        /// ObjectId should be stored transiently in order to prepare a merge.
        /// <para/>
        /// If null during a push, use <see cref="Source"/> instead.
        /// <para/>
        /// Returns name (or wildcard) pattern to match the destination ref.
        /// </summary>
        public string Destination { get; private set; }

        /// <summary>
        /// Construct an empty RefSpec.
        /// <para/>
        /// A newly created empty RefSpec is not suitable for use in most
        /// applications, as at least one field must be set to match a source name.
        /// </summary>
        public RefSpec()
        {
            Force = false;
            Wildcard = false;
            Source = Constants.HEAD;
            Destination = null;
        }

        public RefSpec(string source, string destination)
            : this()
        {
            Source = source;
            Destination = destination;
        }

        /// <summary>
        /// Parse a ref specification for use during transport operations.
        /// <para/>
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
        /// </summary>
        /// <param name="spec">string describing the specification.</param>
        public RefSpec(string spec)
        {
            string s = spec;
            if (s.StartsWith("+"))
            {
                Force = true;
                s = s.Substring(1);
            }

            int c = s.LastIndexOf(':');
            if (c == 0)
            {
                s = s.Substring(1);
                if (IsWildcard(s))
                {
                    throw new ArgumentException("Invalid Wildcards " + spec);
                }
                Destination = s;
            }
            else if (c > 0)
            {
                Source = s.Slice(0, c);
                Destination = s.Substring(c + 1);
                if (IsWildcard(Source) && IsWildcard(Destination))
                {
                    Wildcard = true;
                }
                else if (IsWildcard(Source) || IsWildcard(Destination))
                {
                    throw new ArgumentException("Invalid Wildcards " + spec);
                }
            }
            else
            {
                if (IsWildcard(s))
                {
                    throw new ArgumentException("Invalid Wildcards " + spec);
                }
                Source = s;
            }
        }

        private RefSpec(RefSpec p)
        {
            Force = p.Force;
            Wildcard = p.Wildcard;
            Source = p.Source;
            Destination = p.Destination;
        }

        /// <summary>
        /// Create a new RefSpec with a different force update setting.
        /// </summary>
        /// <param name="force">new value for force update in the returned instance.</param>
        /// <returns>a new RefSpec with force update as specified.</returns>
        public RefSpec SetForce(bool force)
        {
            return new RefSpec(this) { Force = force };
        }

        /// <summary>
        /// Create a new RefSpec with a different source name setting.
        /// </summary>
        /// <param name="source">new value for source in the returned instance.</param>
        /// <returns>a new RefSpec with source as specified.</returns>
        public RefSpec SetSource(string source)
        {
            var r = new RefSpec(this);
            r.Source = source;
            if (IsWildcard(r.Source) && r.Destination == null)
                throw new InvalidOperationException("Destination is not a wildcard.");
            if (IsWildcard(r.Source) != IsWildcard(r.Destination))
                throw new InvalidOperationException("Source/Destination must match.");
            return r;
        }

        /// <summary>
        /// Create a new RefSpec with a different destination name setting.
        /// </summary>
        /// <param name="destination">new value for destination in the returned instance.</param>
        /// <returns>a new RefSpec with destination as specified.</returns>
        public RefSpec SetDestination(string destination)
        {
            RefSpec r = new RefSpec(this);
            r.Destination = destination;

            if (IsWildcard(r.Destination) && r.Source == null)
            {
                throw new InvalidOperationException("Source is not a wildcard.");
            }
            if (IsWildcard(r.Source) != IsWildcard(r.Destination))
            {
                throw new InvalidOperationException("Source/Destination must match.");
            }
            return r;
        }

        /// <summary>
        /// Create a new RefSpec with a different source/destination name setting.
        /// </summary>
        /// <param name="source">new value for source in the returned instance.</param>
        /// <param name="destination">new value for destination in the returned instance.</param>
        /// <returns>a new RefSpec with destination as specified.</returns>
        public RefSpec SetSourceDestination(string source, string destination)
        {
            if (IsWildcard(source) != IsWildcard(destination))
            {
                throw new ArgumentException("Source/Destination must match.");
            }

            return new RefSpec(this) { Wildcard = IsWildcard(source), Source = source, Destination = destination };
        }

        /// <summary>
        /// Does this specification's source description match the ref name?
        /// </summary>
        /// <param name="r">ref name that should be tested.</param>
        /// <returns>true if the names match; false otherwise.</returns>
        public bool MatchSource(string r)
        {
            return match(r, Source);
        }

        /// <summary>
        /// Does this specification's source description match the ref?
        /// </summary>
        /// <param name="r">ref whose name should be tested.</param>
        /// <returns>true if the names match; false otherwise.</returns>
        public bool MatchSource(Ref r)
        {
            return match(r.Name, Source);
        }

        /// <summary>
        /// Does this specification's destination description match the ref name?
        /// </summary>
        /// <param name="r">ref name that should be tested.</param>
        /// <returns>true if the names match; false otherwise.</returns>
        public bool MatchDestination(string r)
        {
            return match(r, Destination);
        }

        /// <summary>
        /// Does this specification's destination description match the ref?
        /// </summary>
        /// <param name="r">ref whose name should be tested.</param>
        /// <returns>true if the names match; false otherwise.</returns>
        public bool MatchDestination(Ref r)
        {
            return match(r.Name, Destination);
        }

        /// <summary>
        /// Expand this specification to exactly match a ref name.
        /// <para/>
        /// Callers must first verify the passed ref name matches this specification,
        /// otherwise expansion results may be unpredictable.
        /// </summary>
        /// <param name="r">
        /// a ref name that matched our source specification. Could be a
        /// wildcard also.
        /// </param>
        /// <returns>
        /// a new specification expanded from provided ref name. Result
        /// specification is wildcard if and only if provided ref name is
        /// wildcard.
        /// </returns>
        public RefSpec ExpandFromSource(string r)
        {
            return Wildcard ? new RefSpec(this).expandFromSourceImp(r) : this;
        }

        private RefSpec expandFromSourceImp(string name)
        {
            string psrc = Source, pdst = Destination;
            Wildcard = false;
            Source = name;
            Destination = pdst.Slice(0, pdst.Length - 1) + name.Substring(psrc.Length - 1);
            return this;
        }

        /// <summary>
        /// Expand this specification to exactly match a ref.
        /// <para/>
        /// Callers must first verify the passed ref matches this specification,
        /// otherwise expansion results may be unpredictable.
        /// </summary>
        /// <param name="r">
        /// a ref that matched our source specification. Could be a
        /// wildcard also.
        /// </param>
        /// <returns>
        /// a new specification expanded from provided ref name. Result
        /// specification is wildcard if and only if provided ref name is
        /// wildcard.
        /// </returns>
        public RefSpec ExpandFromSource(Ref r)
        {
            return ExpandFromSource(r.Name);
        }

        /// <summary>
        /// Expand this specification to exactly match a ref name.
        /// <para/>
        /// Callers must first verify the passed ref name matches this specification,
        /// otherwise expansion results may be unpredictable.
        /// </summary>
        /// <param name="r">
        /// a ref name that matched our destination specification. Could
        /// be a wildcard also.
        /// </param>
        /// <returns>
        /// a new specification expanded from provided ref name. Result
        /// specification is wildcard if and only if provided ref name is
        /// wildcard.
        /// </returns>
        public RefSpec ExpandFromDestination(string r)
        {
            return Wildcard ? new RefSpec(this).expandFromDstImp(r) : this;
        }

        private RefSpec expandFromDstImp(string name)
        {
            string psrc = Source, pdst = Destination;
            Wildcard = false;
            Source = psrc.Slice(0, psrc.Length - 1) + name.Substring(pdst.Length - 1);
            Destination = name;
            return this;
        }

        /// <summary>
        /// Expand this specification to exactly match a ref.
        /// <para/>
        /// Callers must first verify the passed ref matches this specification,
        /// otherwise expansion results may be unpredictable.
        /// </summary>
        /// <param name="r">a ref that matched our destination specification.</param>
        /// <returns>
        /// a new specification expanded from provided ref name. Result
        /// specification is wildcard if and only if provided ref name is
        /// wildcard.
        /// </returns>
        public RefSpec ExpandFromDestination(Ref r)
        {
            return ExpandFromDestination(r.Name);
        }

        private bool match(string refName, string s)
        {
            if (s == null)
            {
                return false;
            }

            if (Wildcard)
            {
                return refName.StartsWith(s.Slice(0, s.Length - 1));
            }

            return refName.Equals(s);
        }

        public override int GetHashCode()
        {
            int hc = 0;
            if (Source != null)
                hc = hc * 31 + Source.GetHashCode();
            if (Destination != null)
                hc = hc * 31 + Destination.GetHashCode();
            return hc;
        }

        public override bool Equals(object obj)
        {
            var b = (obj as RefSpec);
            if (b == null)
                return false;

            if (Force != b.Force) return false;
            if (Wildcard != b.Wildcard) return false;
            if (!eq(Source, b.Source)) return false;
            if (!eq(Destination, b.Destination)) return false;
            return true;
        }
        
        private static bool eq(string a, string b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }
        
        public override string ToString()
        {
            var r = new StringBuilder();
            if (Force)
                r.Append('+');
            if (Source != null)
                r.Append(Source);
            if (Destination != null)
            {
                r.Append(':');
                r.Append(Destination);
            }
            return r.ToString();
        }
    }
}