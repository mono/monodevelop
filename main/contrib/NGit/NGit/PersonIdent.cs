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
using System.Globalization;
using System.Text;
using NGit;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>A combination of a person identity and time in Git.</summary>
	/// <remarks>
	/// A combination of a person identity and time in Git.
	/// Git combines Name + email + time + time zone to specify who wrote or
	/// committed something.
	/// </remarks>
	public class PersonIdent
	{
		private readonly string name;

		private readonly string emailAddress;

		private readonly long when;

		private readonly int tzOffset;

		/// <summary>Creates new PersonIdent from config info in repository, with current time.
		/// 	</summary>
		/// <remarks>
		/// Creates new PersonIdent from config info in repository, with current time.
		/// This new PersonIdent gets the info from the default committer as available
		/// from the configuration.
		/// </remarks>
		/// <param name="repo"></param>
		public PersonIdent(Repository repo)
		{
			UserConfig config = repo.GetConfig().Get(UserConfig.KEY);
			name = config.GetCommitterName();
			emailAddress = config.GetCommitterEmail();
			when = SystemReader.GetInstance().GetCurrentTime();
			tzOffset = SystemReader.GetInstance().GetTimezone(when);
		}

		/// <summary>
		/// Copy a
		/// <see cref="PersonIdent">PersonIdent</see>
		/// .
		/// </summary>
		/// <param name="pi">
		/// Original
		/// <see cref="PersonIdent">PersonIdent</see>
		/// </param>
		public PersonIdent(NGit.PersonIdent pi) : this(pi.GetName(), pi.GetEmailAddress()
			)
		{
		}

		/// <summary>
		/// Construct a new
		/// <see cref="PersonIdent">PersonIdent</see>
		/// with current time.
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aEmailAddress"></param>
		public PersonIdent(string aName, string aEmailAddress)
		{
			name = aName;
			emailAddress = aEmailAddress;
			when = SystemReader.GetInstance().GetCurrentTime();
			tzOffset = SystemReader.GetInstance().GetTimezone(when);
		}

		/// <summary>Copy a PersonIdent, but alter the clone's time stamp</summary>
		/// <param name="pi">
		/// original
		/// <see cref="PersonIdent">PersonIdent</see>
		/// </param>
		/// <param name="when">local time</param>
		/// <param name="tz">time zone</param>
		public PersonIdent(NGit.PersonIdent pi, DateTime when, TimeZoneInfo tz) : this(pi
			.GetName(), pi.GetEmailAddress(), when, tz)
		{
		}

		/// <summary>
		/// Copy a
		/// <see cref="PersonIdent">PersonIdent</see>
		/// , but alter the clone's time stamp
		/// </summary>
		/// <param name="pi">
		/// original
		/// <see cref="PersonIdent">PersonIdent</see>
		/// </param>
		/// <param name="aWhen">local time</param>
		public PersonIdent(NGit.PersonIdent pi, DateTime aWhen)
		{
			name = pi.GetName();
			emailAddress = pi.GetEmailAddress();
			when = aWhen.GetTime();
			tzOffset = pi.tzOffset;
		}

		/// <summary>Construct a PersonIdent from simple data</summary>
		/// <param name="aName"></param>
		/// <param name="aEmailAddress"></param>
		/// <param name="aWhen">local time stamp</param>
		/// <param name="aTZ">time zone</param>
		public PersonIdent(string aName, string aEmailAddress, DateTime aWhen, TimeZoneInfo
			 aTZ)
		{
			name = aName;
			emailAddress = aEmailAddress;
			when = aWhen.GetTime();
			tzOffset = aTZ.GetOffset(when) / (60 * 1000);
		}

		/// <summary>
		/// Construct a
		/// <see cref="PersonIdent">PersonIdent</see>
		/// </summary>
		/// <param name="aName"></param>
		/// <param name="aEmailAddress"></param>
		/// <param name="aWhen">local time stamp</param>
		/// <param name="aTZ">time zone</param>
		public PersonIdent(string aName, string aEmailAddress, long aWhen, int aTZ)
		{
			name = aName;
			emailAddress = aEmailAddress;
			when = aWhen;
			tzOffset = aTZ;
		}

		/// <summary>Copy a PersonIdent, but alter the clone's time stamp</summary>
		/// <param name="pi">
		/// original
		/// <see cref="PersonIdent">PersonIdent</see>
		/// </param>
		/// <param name="aWhen">local time stamp</param>
		/// <param name="aTZ">time zone</param>
		public PersonIdent(NGit.PersonIdent pi, long aWhen, int aTZ)
		{
			name = pi.GetName();
			emailAddress = pi.GetEmailAddress();
			when = aWhen;
			tzOffset = aTZ;
		}

		/// <returns>Name of person</returns>
		public virtual string GetName()
		{
			return name;
		}

		/// <returns>email address of person</returns>
		public virtual string GetEmailAddress()
		{
			return emailAddress;
		}

		/// <returns>timestamp</returns>
		public virtual DateTime GetWhen()
		{
			return Sharpen.Extensions.CreateDate(when);
		}

		/// <returns>this person's declared time zone; null if time zone is unknown.</returns>
		public virtual TimeZoneInfo GetTimeZone()
		{
			StringBuilder tzId = new StringBuilder(8);
			tzId.Append("GMT");
			AppendTimezone(tzId);
			return Sharpen.Extensions.GetTimeZone(tzId.ToString());
		}

		/// <returns>
		/// this person's declared time zone as minutes east of UTC. If the
		/// timezone is to the west of UTC it is negative.
		/// </returns>
		public virtual int GetTimeZoneOffset()
		{
			return tzOffset;
		}

		public override int GetHashCode()
		{
			int hc = GetEmailAddress().GetHashCode();
			hc *= 31;
			hc += (int)(when / 1000L);
			return hc;
		}

		public override bool Equals(object o)
		{
			if (o is NGit.PersonIdent)
			{
				NGit.PersonIdent p = (NGit.PersonIdent)o;
				return GetName().Equals(p.GetName()) && GetEmailAddress().Equals(p.GetEmailAddress
					()) && when / 1000L == p.when / 1000L;
			}
			return false;
		}

		/// <summary>Format for Git storage.</summary>
		/// <remarks>Format for Git storage.</remarks>
		/// <returns>a string in the git author format</returns>
		public virtual string ToExternalString()
		{
			StringBuilder r = new StringBuilder();
			r.Append(GetName());
			r.Append(" <");
			r.Append(GetEmailAddress());
			r.Append("> ");
			r.Append(when / 1000);
			r.Append(' ');
			AppendTimezone(r);
			return r.ToString();
		}

		private void AppendTimezone(StringBuilder r)
		{
			int offset = tzOffset;
			char sign;
			int offsetHours;
			int offsetMins;
			if (offset < 0)
			{
				sign = '-';
				offset = -offset;
			}
			else
			{
				sign = '+';
			}
			offsetHours = offset / 60;
			offsetMins = offset % 60;
			r.Append(sign);
			if (offsetHours < 10)
			{
				r.Append('0');
			}
			r.Append(offsetHours);
			if (offsetMins < 10)
			{
				r.Append('0');
			}
			r.Append(offsetMins);
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			SimpleDateFormat dtfmt;
			dtfmt = new SimpleDateFormat("EEE MMM d HH:mm:ss yyyy Z", CultureInfo.InvariantCulture
				);
			dtfmt.SetTimeZone(GetTimeZone());
			r.Append("PersonIdent[");
			r.Append(GetName());
			r.Append(", ");
			r.Append(GetEmailAddress());
			r.Append(", ");
			r.Append(dtfmt.Format(Sharpen.Extensions.ValueOf(when)));
			r.Append("]");
			return r.ToString();
		}
	}
}
