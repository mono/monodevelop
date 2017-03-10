//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal abstract class BaseStringRebuilder : StringRebuilder
    {

        protected BaseStringRebuilder(int length, int lineBreakCount, int depth)
            : base(length, lineBreakCount, depth)
        {
        }

        #region Private
        private StringRebuilder Assemble(Span left, Span right)
        {
            if (left.Length == 0)
                return this.Substring(right);
            else if (right.Length == 0)
                return this.Substring(left);
            else if (left.Length + right.Length == this.Length)
                return this;
            else
                return BinaryStringRebuilder.Create(this.Substring(left), this.Substring(right));
        }

        private StringRebuilder Assemble(Span left, StringRebuilder text, Span right)
        {
            if (text.Length == 0)
                return Assemble(left, right);
            else if (left.Length == 0)
                return (right.Length == 0) ? text : BinaryStringRebuilder.Create(text, this.Substring(right));
            else if (right.Length == 0)
                return BinaryStringRebuilder.Create(this.Substring(left), text);
            else if (left.Length < right.Length)
                return BinaryStringRebuilder.Create(BinaryStringRebuilder.Create(this.Substring(left), text),
                                                    this.Substring(right));
            else
                return BinaryStringRebuilder.Create(this.Substring(left),
                                                    BinaryStringRebuilder.Create(text, this.Substring(right)));
        }
        #endregion

        #region StringRebuilder Members
        public override char[] ToCharArray(int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex");

            if ((length < 0) || (startIndex + length > this.Length) || (startIndex + length < 0))
                throw new ArgumentOutOfRangeException("length");

            char[] copy = new char[length];
            this.CopyTo(startIndex, copy, 0, length);

            return copy;
        }

        public override StringRebuilder Delete(Span span)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");

            return this.Assemble(Span.FromBounds(0, span.Start), Span.FromBounds(span.End, this.Length));
        }

        public override StringRebuilder Replace(Span span, string text)
        {
            return this.Replace(span, SimpleStringRebuilder.Create(text));
        }

        public override StringRebuilder Replace(Span span, StringRebuilder text)
        {
            if (span.End > this.Length)
                throw new ArgumentOutOfRangeException("span");
            if (text == null)
                throw new ArgumentNullException("text");

            return this.Assemble(Span.FromBounds(0, span.Start), text, Span.FromBounds(span.End, this.Length));
        }

        public override StringRebuilder Append(string text)
        {
            return this.Insert(this.Length, text);
        }

        public override StringRebuilder Append(StringRebuilder text)
        {
            return this.Insert(this.Length, text);
        }

        public override StringRebuilder Insert(int position, string text)
        {
            return this.Insert(position, SimpleStringRebuilder.Create(text));
        }

        public override StringRebuilder Insert(int position, StringRebuilder text)
        {
            if ((position < 0) || (position > this.Length))
                throw new ArgumentOutOfRangeException("position");
            if (text == null)
                throw new ArgumentNullException("text");

            return this.Assemble(Span.FromBounds(0, position), text, Span.FromBounds(position, this.Length));
        }

        public override StringRebuilder Insert(int position, ITextStorage storage)
        {
            return this.Insert(position, SimpleStringRebuilder.Create(storage));
        }
        #endregion
    }
}
