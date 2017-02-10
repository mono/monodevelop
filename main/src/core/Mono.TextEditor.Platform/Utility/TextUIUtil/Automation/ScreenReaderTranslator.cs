// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Utilities.Automation
{
    using System;
    using System.Text;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.Utilities;

    using Strings = Microsoft.VisualStudio.Text.Utilities.Strings;

    /// <summary>
    /// The purpose of this class is to translate textual values into strings that are better understood
    /// by screen readers. For instance, if a portion of code is being sent to a screen reader, we'd like
    /// all the periods to be pronounced as "period" instead of inducing a short pause at the end of a 
    /// sentence.
    /// </summary>
    internal static class ScreenReaderTranslator
    {

        /// <summary>
        /// A list including characters that we'd like to replace for any kind of text document
        /// such that screen readers can properly read the content to the user. These replacements
        /// correspond to replacements we would like to perform on a document with <see cref="IContentType"/> of
        /// text.
        /// </summary>
        private static readonly List<Tuple<string, string>> _languageCharacters = 
            new List<Tuple<string, string>> 
            {
                new Tuple<string, string>("'", Strings.SingleQuote),
                new Tuple<string, string>("\"", Strings.DoubleQuote),
                new Tuple<string, string>("?", Strings.QuestionMark),
                new Tuple<string, string>(",", Strings.Comma)
            };

        /// <summary>
        /// This list includes all the replacements we would like to perform given the contents of the buffer
        /// correspond to any programming language.
        /// </summary>
        private static readonly List<Tuple<string, string>> _programmingCharacters =
            new List<Tuple<string, string>> 
            {
                new Tuple<string, string>(":", Strings.Colon),
                new Tuple<string, string>(".", Strings.Period),
                new Tuple<string, string>("\\", Strings.Backslash),
                new Tuple<string, string>("/", Strings.Slash),
                new Tuple<string, string>("{", Strings.LeftCurlyBrace),
                new Tuple<string, string>("}", Strings.RightCurlyBrace),
                new Tuple<string, string>("(", Strings.LeftParenthesis),
                new Tuple<string, string>(")", Strings.RightParenthesis),
                new Tuple<string, string>("[", Strings.LeftSquareBracket),
                new Tuple<string, string>("]", Strings.RightSquareBracket),
                new Tuple<string, string>("<", Strings.LeftAngledBracket),
                new Tuple<string, string>(">", Strings.RightAngledBracket),
                new Tuple<string, string>(";", Strings.Semicolon)
            };

        /// <summary>
        /// Returns a string friendly for screen readers. The returned string will have textual values
        /// for special characters such as , or :
        /// </summary>
        /// <remarks>
        /// The result of this call will be dependent of the <see cref="IContentType"/> of the buffer
        /// to which the passed <see cref="SnapshotSpan"/> belongs. For example, if the content type
        /// is a programming language, then even simple language constructs such as . (dot) will also
        /// be replaced by their textual representation.
        /// </remarks>
        public static string Translate(SnapshotSpan input, IContentType contentType)
        {
            StringBuilder result = new StringBuilder(input.GetText());
            if (contentType.IsOfType("code"))
            {
                result = ReplaceLanguageCharacters(result);
                result = ReplaceProgrammingCharacters(result);
            }
            else if (contentType.IsOfType("text"))
            {
                result = ReplaceLanguageCharacters(result);
            }
            return result.ToString();
        }

        /// <summary>
        /// Replaces basic language characters in the provided source with their textual representation.
        /// </summary>
        private static StringBuilder ReplaceLanguageCharacters(StringBuilder source)
        {
            Replace(_languageCharacters, source);
            return source;
        }

        /// <summary>
        /// Replaces programming language constructs in the source with their textual representation.
        /// </summary>
        private static StringBuilder ReplaceProgrammingCharacters(StringBuilder source)
        {
            Replace(_programmingCharacters, source);
            return source;
        }

        /// <summary>
        /// Replaces patterns (provided in replacementList) with their textual
        /// representation in the provided source.
        /// </summary>
        private static void Replace(IList<Tuple<string, string>> replacementList, StringBuilder source)
        {
            foreach (Tuple<string, string> replacement in replacementList)
            {
                source = source.Replace(replacement.Item1, " " + replacement.Item2 + " ");
            }
        }
    }
}
