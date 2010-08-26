/*
 * Copyright (C) 2008, Florian Köberle <florianskarten@web.de>
 * Copyright (C) 2009, Adriano Machado <adriano.m.machado@hotmail.com>
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

using System.Text.RegularExpressions;
using GitSharp.Core.Exceptions;

namespace GitSharp.Core.FnMatch
{
    using System.Collections.Generic;

    internal sealed class GroupHead : AbstractHead
    {
        private readonly List<ICharacterPattern> _characterClasses;

        private static readonly Regex RegexPattern = new Regex("([^-][-][^-]|\\[[.:=].*?[.:=]\\])", RegexOptions.Compiled);

        private readonly bool _inverse;

        internal GroupHead(string pattern, string wholePattern)
            : base(false)
        {
            _characterClasses = new List<ICharacterPattern>();

            _inverse = pattern.StartsWith("!");
            if (_inverse)
            {
                pattern = pattern.Substring(1);
            }

            Match match = RegexPattern.Match(pattern);

            while (match.Success)
            {
                string characterClass = match.Groups[0].Value;

                if (characterClass.Length == 3 && characterClass[1] == '-')
                {
                    char start = characterClass[0];
                    char end = characterClass[2];
                    _characterClasses.Add(new CharacterRange(start, end));
                }
                else if (characterClass.Equals("[:alnum:]"))
                {
                    _characterClasses.Add(LetterPattern.Instance);
                    _characterClasses.Add(DigitPattern.Instance);
                }
                else if (characterClass.Equals("[:alpha:]"))
                {
                    _characterClasses.Add(LetterPattern.Instance);
                }
                else if (characterClass.Equals("[:blank:]"))
                {
                    _characterClasses.Add(new OneCharacterPattern(' '));
                    _characterClasses.Add(new OneCharacterPattern('\t'));
                }
                else if (characterClass.Equals("[:cntrl:]"))
                {
                    _characterClasses.Add(new CharacterRange('\u0000', '\u001F'));
                    _characterClasses.Add(new OneCharacterPattern('\u007F'));
                }
                else if (characterClass.Equals("[:digit:]"))
                {
                    _characterClasses.Add(DigitPattern.Instance);
                }
                else if (characterClass.Equals("[:graph:]"))
                {
                    _characterClasses.Add(new CharacterRange('\u0021', '\u007E'));
                    _characterClasses.Add(LetterPattern.Instance);
                    _characterClasses.Add(DigitPattern.Instance);
                }
                else if (characterClass.Equals("[:lower:]"))
                {
                    _characterClasses.Add(LowerPattern.Instance);
                }
                else if (characterClass.Equals("[:print:]"))
                {
                    _characterClasses.Add(new CharacterRange('\u0020', '\u007E'));
                    _characterClasses.Add(LetterPattern.Instance);
                    _characterClasses.Add(DigitPattern.Instance);
                }
                else if (characterClass.Equals("[:punct:]"))
                {
                    _characterClasses.Add(PunctPattern.Instance);
                }
                else if (characterClass.Equals("[:space:]"))
                {
                    _characterClasses.Add(WhitespacePattern.Instance);
                }
                else if (characterClass.Equals("[:upper:]"))
                {
                    _characterClasses.Add(UpperPattern.Instance);
                }
                else if (characterClass.Equals("[:xdigit:]"))
                {
                    _characterClasses.Add(new CharacterRange('0', '9'));
                    _characterClasses.Add(new CharacterRange('a', 'f'));
                    _characterClasses.Add(new CharacterRange('A', 'F'));
                }
                else if (characterClass.Equals("[:word:]"))
                {
                    _characterClasses.Add(new OneCharacterPattern('_'));
                    _characterClasses.Add(LetterPattern.Instance);
                    _characterClasses.Add(DigitPattern.Instance);
                }
                else
                {
                    string message = string.Format("The character class \"{0}\" is not supported.", characterClass);
                    throw new InvalidPatternException(message, wholePattern);
                }

                pattern = pattern.Replace(characterClass, string.Empty);
                match = RegexPattern.Match(pattern);
            }

            // pattern contains now no ranges
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                _characterClasses.Add(new OneCharacterPattern(c));
            }
        }

        protected internal override bool matches(char c)
        {
            foreach (ICharacterPattern pattern in _characterClasses)
            {
                if (pattern.Matches(c))
                {
                    return !_inverse;
                }
            }
            return _inverse;
        }

        private interface ICharacterPattern
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="c">The character to test</param>
            /// <returns>Returns true if the character matches a pattern.</returns>
            bool Matches(char c);
        }

        private sealed class CharacterRange : ICharacterPattern
        {
            private readonly char _start;
            private readonly char _end;

            internal CharacterRange(char start, char end)
            {
                _start = start;
                _end = end;
            }

            public bool Matches(char c)
            {
                return _start <= c && c <= _end;
            }
        }

        private sealed class DigitPattern : ICharacterPattern
        {
            internal static readonly DigitPattern Instance = new DigitPattern();

            public bool Matches(char c)
            {
                return char.IsDigit(c);
            }
        }

        private sealed class LetterPattern : ICharacterPattern
        {
            internal static readonly LetterPattern Instance = new LetterPattern();

            public bool Matches(char c)
            {
                return char.IsLetter(c);
            }
        }

        private sealed class LowerPattern : ICharacterPattern
        {
            internal static readonly LowerPattern Instance = new LowerPattern();

            public bool Matches(char c)
            {
                return char.IsLower(c);
            }
        }

        private sealed class UpperPattern : ICharacterPattern
        {
            internal static readonly UpperPattern Instance = new UpperPattern();

            public bool Matches(char c)
            {
                return char.IsUpper(c);
            }
        }

        private sealed class WhitespacePattern : ICharacterPattern
        {
            internal static readonly WhitespacePattern Instance = new WhitespacePattern();

            public bool Matches(char c)
            {
                return char.IsWhiteSpace(c);
            }
        }

        private sealed class OneCharacterPattern : ICharacterPattern
        {
            private readonly char _expectedCharacter;

            internal OneCharacterPattern(char c)
            {
                _expectedCharacter = c;
            }

            public bool Matches(char c)
            {
                return _expectedCharacter == c;
            }
        }

        private sealed class PunctPattern : ICharacterPattern
        {
            internal static readonly PunctPattern Instance = new PunctPattern();

            private static readonly string PunctCharacters = "-!\"#$%&'()*+,./:;<=>?@[\\]_`{|}~";

            public bool Matches(char c)
            {
                return PunctCharacters.IndexOf(c) != -1;
            }
        }
    }
}
