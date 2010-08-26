/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Mykola Nikishov <mn@mn.com.ua>
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{
	/// <summary>
	/// This URI like construct used for referencing Git archives over the net, as
	/// well as locally stored archives. The most important difference compared to
	/// RFC 2396 URI's is that no URI encoding/decoding ever takes place. A space or
	/// any special character is written as-is.
	/// </summary>
	public class URIish
	{
		private static readonly Regex FullUri =
			new Regex("^(?:([a-z][a-z0-9+-]+)://(?:([^/]+?)(?::([^/]+?))?@)?(?:([^/]+?))?(?::(\\d+))?)?((?:[A-Za-z]:)?/.+)$");

		private static readonly Regex ScpUri = new Regex("^(?:([^@]+?)@)?([^:]+?):(.+)$");

		public string Scheme { get; private set; }
		public string Path { get; protected set; }
		public string User { get; private set; }
		public string Pass { get; private set; }
		public int Port { get; private set; }
		public string Host { get; private set; }

		/// <summary>
		/// Construct a URIish from a standard URL.
		/// </summary>
		/// <param name="u">The source URL to convert from.</param>
		public URIish(Uri u)
		{
			if (u == null)
				throw new ArgumentNullException ("u");
			Scheme = u.Scheme;
			Path = u.AbsolutePath;
			Port = u.Port;
			Host = u.Host;

			string ui = u.UserInfo;
			if (ui != null)
			{
				int d = ui.IndexOf(':');
				User = d < 0 ? ui : ui.Slice(0, d);
				Pass = d < 0 ? null : ui.Substring(d + 1);
			}
		}

		/// <summary>
		/// Parse and construct an <see cref="URIish"/> from a string
		/// </summary>
		/// <param name="s"></param>
		public URIish(string s)
		{

            // If the string passes relative paths such as .\dir1 or ..\dir1,
            // get the absolute path for future processing.
            if (system().getOperatingSystem() == GitSharp.Core.PlatformType.Windows)
            {
                try
                {
                    if (!System.IO.Path.IsPathRooted(s))
                        s = System.IO.Path.GetFullPath(s);
                } 
			    catch (NotSupportedException) {}
			    catch (ArgumentException) {}
            }

			s = s.Replace('\\', '/');
			Match matcher = FullUri.Match(s);
			Port = -1;
			if (matcher.Success)
			{
				Scheme = matcher.Groups[1].Value;
				Scheme = Scheme.Length == 0 ? null : Scheme;
				User = matcher.Groups[2].Value;
				User = User.Length == 0 ? null : User;
				Pass = matcher.Groups[3].Value;
				Pass = Pass.Length == 0 ? null : Pass;
				Host = matcher.Groups[4].Value;
				Host = Host.Length == 0 ? null : Host;
				if (matcher.Groups[5].Success)
				{
					Port = int.Parse(matcher.Groups[5].Value);
				}
				Path = matcher.Groups[6].Value;
				if (Path.Length >= 3 && Path[0] == '/' && Path[2] == ':' && (Path[1] >= 'A' && Path[1] <= 'Z' || Path[1] >= 'a' && Path[1] <= 'z'))
				{
					Path = Path.Substring(1);
				}
			}
			else
			{
				matcher = ScpUri.Match(s);
				if (matcher.Success)
				{
					User = matcher.Groups[1].Value;
					User = User.Length == 0 ? null : User;
					Host = matcher.Groups[2].Value;
					Host = Host.Length == 0 ? null : Host;
					Path = matcher.Groups[3].Value;
					Path = Path.Length == 0 ? null : Path;
				}
				else
				{
					throw new UriFormatException("Cannot parse Git URI-ish (" + s + ")");
				}
			}
		}

		/// <summary>
		/// Create an empty, non-configured URI.
		/// </summary>
		public URIish()
		{
			Port = -1;
		}

		private URIish(URIish u)
		{
			Scheme = u.Scheme;
			Path = u.Path;
			User = u.User;
			Pass = u.Pass;
			Port = u.Port;
			Host = u.Host;
		}

		/// <summary>
		/// Returns true if this URI references a repository on another system.
		/// </summary>
		public bool IsRemote
		{
			get { return Host != null; }
		}

		/// <summary>
		/// Return a new URI matching this one, but with a different host.
		/// </summary>
		/// <param name="n">the new value for host.</param>
		/// <returns>a new URI with the updated value.</returns>
		public URIish SetHost(string n)
		{
			return new URIish(this) { Host = n };
		}

		/// <summary>
		/// Return a new URI matching this one, but with a different scheme.
		/// </summary>
		/// <param name="n">the new value for scheme.</param>
		/// <returns>a new URI with the updated value.</returns>
		public URIish SetScheme(string n)
		{
			return new URIish(this) { Scheme = n };
		}

		/// <summary>
		/// Return a new URI matching this one, but with a different path.
		/// </summary>
		/// <param name="n">the new value for path.</param>
		/// <returns>a new URI with the updated value.</returns>
		public URIish SetPath(string n)
		{
			return new URIish(this) { Path = n };
		}

		/// <summary>
		/// Return a new URI matching this one, but with a different user.
		/// </summary>
		/// <param name="n">the new value for user.</param>
		/// <returns>a new URI with the updated value.</returns>
		public URIish SetUser(string n)
		{
			return new URIish(this) { User = n };
		}

		/// <summary>
		/// Return a new URI matching this one, but with a different password.
		/// </summary>
		/// <param name="n">the new value for password.</param>
		/// <returns>A new URI with the updated value.</returns>
		public URIish SetPass(string n)
		{
			return new URIish(this) { Pass = n };
		}

		/// <summary>
		/// Return a new URI matching this one, but with a different port.
		/// </summary>
		/// <param name="n">The new value for port.</param>
		/// <returns>A new URI with the updated value.</returns>
		public URIish SetPort(int n)
		{
			return new URIish(this) { Port = (n > 0 ? n : -1) };
		}

		public override int GetHashCode()
		{
			int hc = 0;

			if (Scheme != null)
			{
				hc = hc * 31 + Scheme.GetHashCode();
			}

			if (User != null)
			{
				hc = hc * 31 + User.GetHashCode();
			}

			if (Pass != null)
			{
				hc = hc * 31 + Pass.GetHashCode();
			}

			if (Host != null)
			{
				hc = hc * 31 + Host.GetHashCode();
			}

			if (Port > 0)
			{
				hc = hc * 31 + Port;
			}

			if (Path != null)
			{
				hc = hc * 31 + Path.GetHashCode();
			}

			return hc;
		}

		private static bool Eq(string a, string b)
		{
			if (a == b) return true;
			if (a == null || b == null) return false;
			return a.Equals(b);
		}

		public override bool Equals(object obj)
		{
			URIish b = (obj as URIish);
			if (b == null) return false;

			if (!Eq(Scheme, b.Scheme)) return false;
			if (!Eq(User, b.User)) return false;
			if (!Eq(Pass, b.Pass)) return false;
			if (!Eq(Host, b.Host)) return false;
			if (Port != b.Port) return false;
			if (!Eq(Path, b.Path)) return false;

			return true;
		}

		/// <summary>
		/// Obtain the string form of the URI, with the password included.
		/// </summary>
		/// <returns>The URI, including its password field, if any.</returns>
		public string ToPrivateString()
		{
			return Format(true);
		}

		public override string ToString()
		{
			return Format(false);
		}

		private string Format(bool includePassword)
		{
			var r = new StringBuilder();
			if (Scheme != null)
			{
				r.Append(Scheme);
				r.Append("://");
			}

			if (User != null)
			{
				r.Append(User);
				if (includePassword && Pass != null)
				{
					r.Append(':');
					r.Append(Pass);
				}
			}

			if (Host != null)
			{
				if (User != null)
				{
					r.Append('@');
				}

				r.Append(Host);

				if (Scheme != null && Port > 0)
				{
					r.Append(':');
					r.Append(Port);
				}
			}

			if (Path != null)
			{
				if (Scheme != null)
				{
					if (!Path.StartsWith("/"))
					{
						r.Append('/');
					}
				}
				else if (Host != null)
				{
					r.Append(':');
				}
				r.Append(Path);
			}

			return r.ToString();
		}

	    /// <summary>
	    ///   Get the "humanish" part of the path. Some examples of a 'humanish' part for a full path:
	    /// </summary>
	    /// <example>
	    ///   /path/to/repo.git -> repo
	    /// </example>
	    /// <example>
	    ///   /path/to/repo.git/ -> repo
	    /// </example>
	    /// <example>
	    ///   /path/to/repo/.git -> repo
	    /// </example>
	    /// <example>
	    ///   /path/to/repo/ -> repo
	    /// </example>
	    /// <example>
	    ///   /path//to -> an empty string
	    /// </example>
	    /// <returns>
	    ///   the "humanish" part of the path. May be an empty string. Never null.</returns>
	    public string getHumanishName()
	    {
	        if (string.IsNullOrEmpty(Path))
	        {
	            throw new InvalidOperationException("Path is either null or empty.");
	        }

	        string[] elements = Path.Split('/');
	        if (elements.Length == 0)
	        {
	            throw new InvalidOperationException();
	        }

            // In order to match Java Split behavior (http://java.sun.com/j2se/1.4.2/docs/api/java/lang/String.html#split(java.lang.String)
	        string[] elements2 = RemoveTrailingEmptyStringElements(elements);

            if (elements2.Length == 0)
            {
                throw new InvalidOperationException();
            }
            
            string result = elements2[elements2.Length - 1];
            if (Constants.DOT_GIT.Equals(result))
	        {
	            result = elements2[elements2.Length - 2];
	        }
	        else if (result.EndsWith(Constants.DOT_GIT_EXT))
	        {
                result = result.Slice(0, result.Length - Constants.DOT_GIT_EXT.Length);
	        }

	        return result;
	    }

	    private static string[] RemoveTrailingEmptyStringElements(string[] elements)
	    {
	        var trimmedElements = new List<string>();

	        for (int i = elements.Length - 1; i > -1; i--)
	        {
                if (elements[i] == string.Empty)
	            {
	                continue;
	            }
	        
                trimmedElements.AddRange(elements.Take(i + 1));
                break;
            }

	        return trimmedElements.ToArray();
	    }

	    private SystemReader system()
        {
            return SystemReader.getInstance();
        }
	}
}
