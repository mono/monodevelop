// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Utilities;

    /// <summary>
    /// A string that participates in a text change.
    /// </summary>
    internal abstract class ChangeString
    {
        public static LiteralChangeString EmptyChangeString = new LiteralChangeString("");
        public const int LiteralStringThreshold = 2 * 1024;

        public abstract string Text { get; }
        public abstract int Length { get; }
        public abstract int ComputeLineBreakCount();
        public abstract char this[int position] { get; }
        public abstract string Substring(int startIndex, int length);

        internal abstract IStringRebuilder Content { get; }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InstalledUICulture, "{0}: {1}", this.Length, this.Text);
        }
    }

    internal class LiteralChangeString : ChangeString
    {
        private string text;

        public LiteralChangeString(string text)
        {
            this.text = text;
        }

        public override string Text
        {
            get { return this.text; }
        }

        public override int Length
        {
            get { return this.text.Length; }
        }

        public override char this[int position] 
        {
            get { return this.text[position]; }
        }

        public override string Substring(int startIndex, int length)
        {
            return this.text.Substring(startIndex, length);
        }

        public override int ComputeLineBreakCount()
        {
            return TextUtilities.ScanForLineCount(this.text);
        }

        internal override IStringRebuilder Content
        {
            get { return SimpleStringRebuilder.Create(this.text); }
        }
    }

    /// <summary>
    /// This class uses a StringRebuilder to store the contents of the ChangeString.
    /// </summary>
    /// <remarks>
    /// <para>There isn't an easy answer as to whether it is better to use a LiteralChangeString or a ReferenceChangeString to store a change
    /// string (even though the logic in the factories below is very simple).</para>
    /// 
    /// <para>A LiteralChangeString makes a copy of the text (and -- in the cases where the factories are being used to create a ChangeString
    /// from a SnapshotSpan -- this corresponds to text being deleted), taking O(length + log buffer length) time and forcing an allocation of O(length)
    /// (but since text is being deleted from the text buffer, the size of the text buffer could be reduced by O(length)).</para>
    /// 
    /// <para>A ReferenceChangeString creates a StringRebuilder that corresponds to a portion of the text in the snapshot (pointing to the same
    /// strings the snapshot does without reallocating them). This takes O(log buffer length) time and O(log string length) space in new allocations
    /// but it pins the strings used in the snapshot's piece table as long as the ChangeString is alive (which will be as long as the TextVersion is alive).</para>
    /// 
    /// <para>For buffers used by the output window (or any window that does not support undo), using ReferenceChangeStrings is probably a win
    /// for anything over 200 or so characters (the size where the StringRebuilder won't simply copy the string anyhow).</para>
    /// 
    /// <para>For buffers that support Undo, the trade-off is not as obvious. The TextVersions are effectively kept alive forever by the Undo manager,
    /// Using ReferenceChangeStrings will be faster for large strings and avoid some allocations, but risk keeping other strings pinned in memory. For
    /// example, suppose I delete 1k characters and 4k characters from a buffer represented as a single 5k string. The first deletion creates a literal
    /// change string (allocating 1k new characters), but the buffer doesn't release anything (since the entire 5k string is pinned by the snapshot).
    /// The second deletion creates a reference change string which pins the original 5k string, and the snapshot now releases the string (but this doesn't
    /// do anything since the string is pinned by the ReferenceChangeString).</para>
    /// </remarks>
    internal class ReferenceChangeString : ChangeString
    {
        internal IStringRebuilder builder;

        public static ChangeString CreateChangeString(ITextSnapshot snapshot, Span span)
        {
            if (span.Length == 0)
            {
                return ChangeString.EmptyChangeString;
            }
            else if (span.Length <= ChangeString.LiteralStringThreshold)
            {
                return new LiteralChangeString(snapshot.GetText(span));
            }
            else
            {
                return new ReferenceChangeString(new SnapshotSpan(snapshot, span));
            }
        }

        public static ChangeString CreateChangeString(SnapshotSpan span)
        {
            return ReferenceChangeString.CreateChangeString(span.Snapshot, span.Span);
        }

        public static ChangeString CreateChangeString(IList<SnapshotSpan> sourceSpans, Span selected)
        {
            if (selected.Length == 0)
            {
                return ChangeString.EmptyChangeString;
            }
            else if (selected.Length == 1)
            {
                return ReferenceChangeString.CreateChangeString(sourceSpans[selected.Start]);
            }
            else
            {
                IStringRebuilder builder = SimpleStringRebuilder.Create(String.Empty);
                for (int i = 0; (i < selected.Length); ++i)
                {
                    builder = builder.Insert(builder.Length, BufferFactoryService.StringRebuilderFromSnapshotSpan(sourceSpans[selected.Start + i]));
                }

                return ReferenceChangeString.CreateChangeString(builder);
            }
        }

        internal static ChangeString CreateChangeString(IStringRebuilder content)
        {
            if (content.Length == 0)
            {
                return ChangeString.EmptyChangeString;
            }
            else if (content.Length <= ChangeString.LiteralStringThreshold)
            {
                return new LiteralChangeString(content.GetText(new Span(0, content.Length)));
            }
            else
            {
                return new ReferenceChangeString(content);
            }
        }

        public ReferenceChangeString(SnapshotSpan span)
            : this(BufferFactoryService.StringRebuilderFromSnapshotSpan(span))
        {
        }

        public ReferenceChangeString(IStringRebuilder builder)
        {
            this.builder = builder;
        }

        public override string Text
        {
            get { return this.builder.GetText(new Span(0, this.builder.Length)); }
        }

        public override int Length
        {
            get { return this.builder.Length; }
        }

        public override char this[int position]
        {
            get { return this.builder[position]; }
        }

        public override string Substring(int startIndex, int length)
        {
            return this.builder.GetText(new Span(startIndex, length));
        }

        public override int ComputeLineBreakCount()
        {
            return this.builder.LineBreakCount;
        }

        internal override IStringRebuilder Content
        {
            get { return this.builder; }
        }
    }
}