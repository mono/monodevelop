/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2007, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Globalization;
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    public class PersonIdent
    {
        public string Name { get; private set; }
        public string EmailAddress { get; private set; }

        /// <summary>
        /// Elapsed milliseconds since Epoch (1970.1.1 00:00:00 GMT)
        /// </summary>
        public long When { get; private set; }
        private readonly int tzOffset; // offset in minutes to UTC

        /// <summary>
        /// Creates new PersonIdent from config info in repository, with current time.
        /// This new PersonIdent gets the info from the default committer as available
        /// from the configuration.
        /// </summary>
        /// <param name="repo"></param>
        public PersonIdent(Repository repo)
        {
            RepositoryConfig config = repo.Config;
            Name = config.getCommitterName();
            EmailAddress = config.getCommitterEmail();
            When = SystemReader.getInstance().getCurrentTime();
            tzOffset = SystemReader.getInstance().getTimezone(When);
        }

        /// <summary>
        /// Copy a <seealso cref="PersonIdent"/>.
        /// </summary>
        /// <param name="pi">Original <seealso cref="PersonIdent"/>.</param>
        public PersonIdent(PersonIdent pi)
            : this(pi.Name, pi.EmailAddress)
        {
        }

        /// <summary>
        /// Construct a new <seealso cref="PersonIdent"/> with current time.
        /// </summary>
        /// <param name="name"> </param>
        /// <param name="emailAddress"></param>
        public PersonIdent(string name, string emailAddress)
        {
            Name = name;
            EmailAddress = emailAddress;
            When = SystemReader.getInstance().getCurrentTime();
            tzOffset = SystemReader.getInstance().getTimezone(When);
        }

        /// <summary>
        /// Copy a PersonIdent, but alter the clone's time stamp
        /// </summary>
        /// <param name="pi">Original <seealso cref="PersonIdent"/>.</param>
        /// <param name="when">Local date time in milliseconds (since Epoch).</param>
        /// <param name="tz">Time zone offset in minutes.</param>
        public PersonIdent(PersonIdent pi, DateTime when, int tz)
            : this(pi.Name, pi.EmailAddress, when, tz)
        {
        }

        /// <summary>
        /// Copy a <seealso cref="PersonIdent"/>, but alter the clone's time stamp
        /// </summary>
        /// <param name="pi">Original <seealso cref="PersonIdent"/>.</param>
        /// <param name="when">Local date time in milliseconds (since Epoch).</param>
        public PersonIdent(PersonIdent pi, DateTime when)
            : this(pi.Name, pi.EmailAddress, when.ToMillisecondsSinceEpoch(), pi.tzOffset)
        {
        }

        /// <summary>
        /// Construct a PersonIdent from simple data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="emailAddress"></param>
        /// <param name="when">Local date time in milliseconds (since Epoch).</param>
        /// <param name="tz">Time zone offset in minutes.</param>
        public PersonIdent(string name, string emailAddress, DateTime when, int tz)
            : this(name, emailAddress, when.ToMillisecondsSinceEpoch(), tz)
        {
        }

        /// <summary>
        /// Construct a <seealso cref="PersonIdent"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="emailAddress"> </param>
        /// <param name="when">Local date time in milliseconds (since Epoch).</param>
        /// <param name="tz">Time zone offset in minutes.</param>
        public PersonIdent(string name, string emailAddress, long when, int tz)
        {
            Name = name;
            EmailAddress = emailAddress;
            When = when;
            tzOffset = tz;
        }

        /// <summary>
        /// Copy a PersonIdent, but alter the clone's time stamp
        /// </summary>
        /// <param name="pi">Original <seealso cref="PersonIdent"/>.</param>
        /// <param name="when">Local date time in milliseconds (since Epoch).</param>
        /// <param name="tz">Time zone offset in minutes.</param>
        public PersonIdent(PersonIdent pi, long when, int tz)
            : this(pi.Name, pi.EmailAddress, when, tz)
        {
        }

        /// <summary>
        /// Construct a PersonIdent from a string with full name, email, time time
        /// zone string. The input string must be valid.
        /// </summary>
        /// <param name="str">A Git internal format author/committer string.</param>
        public PersonIdent(string str)
        {
            int lt = str.IndexOf('<');
            if (lt == -1)
            {
                throw new ArgumentException("Malformed PersonIdent string"
                        + " (no < was found): " + str);
            }

            int gt = str.IndexOf('>', lt);
            if (gt == -1)
            {
                throw new ArgumentException("Malformed PersonIdent string"
                        + " (no > was found): " + str);
            }

            int sp = str.IndexOf(' ', gt + 2);
            if (sp == -1)
            {
                When = 0;
                tzOffset = -1;
            }
            else
            {
                string tzHoursStr = str.Slice(sp + 1, sp + 4).Trim();
                int tzHours = tzHoursStr[0] == '+' ? int.Parse(tzHoursStr.Substring(1)) : int.Parse(tzHoursStr);

                int tzMins = int.Parse(str.Substring(sp + 4).Trim());
                When = long.Parse(str.Slice(gt + 1, sp).Trim()) * 1000;
                tzOffset = tzHours * 60 + tzMins;
            }

            Name = str.Slice(0, lt).Trim();
            EmailAddress = str.Slice(lt + 1, gt).Trim();
        }

        /// <summary>
        /// TimeZone offset in minutes
        /// </summary>
        public int TimeZoneOffset
        {
            get
            {
                return tzOffset;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return EmailAddress.GetHashCode() ^ (int)When;
            }
        }

        public override bool Equals(object obj)
        {
            var p = obj as PersonIdent;
            if (p == null)
                return false;

            return Name == p.Name
                && EmailAddress == p.EmailAddress
                && When == p.When;
        }

        /// <summary>
        /// Format for Git storage.
        /// </summary>
        /// <returns>A string in the git author format.</returns>
        public string ToExternalString()
        {
            var r = new StringBuilder();

            r.Append(Name);
            r.Append(" <");
            r.Append(EmailAddress);
            r.Append("> ");
            r.Append(When / 1000);
            r.Append(' ');
            appendTimezone(r);

            return r.ToString();
        }

        private void appendTimezone(StringBuilder r)
        {
            int offset = tzOffset;
            char sign;

            if (offset < 0)
            {
                sign = '-';
                offset *= -1;
            }
            else
            {
                sign = '+';
            }

            int offsetHours = offset / 60;
            int offsetMins = offset % 60;

            r.AppendFormat(CultureInfo.InvariantCulture, "{0}{1:D2}{2:D2}", sign, offsetHours, offsetMins);
        }

        public override string ToString()
        {
            var r = new StringBuilder();

            r.Append("PersonIdent[");
            r.Append(Name);
            r.Append(", ");
            r.Append(EmailAddress);
            r.Append(", ");
            r.Append(When.MillisToDateTimeOffset(tzOffset).ToIsoDateFormat());
            r.Append("]");

            return r.ToString();
        }
    }
}
