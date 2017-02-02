using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Differencing.Implementation
{
    /// <summary>
    /// A difference collection that is based off an existing set of differences, but with a new set of matches.
    /// </summary>
    class FinessedDifferenceCollection : IDifferenceCollection<string>
    {
        /// <summary>
        /// Given a set of line difference, finesse the results to produce something likely closer to what the user
        /// actually changed.  This means:
        /// 1) Transpose diffs forward and backward:
        ///  a)If the user has inserted a new method, say, the diff tends to be off a line or two, e.g. the first few
        ///    lines of the new doc comment may show up as matching the first few lines of the next method's doc comment, or
        ///    vice versa.  This slides the diff around until it appears to match up with probable "blocks" in the source file.
        ///
        ///  b)If the user has ignore (trim) whitespace on, the computed, greedy "match" may be worse than an equally
        ///    correct match with the diff transposed backward.  For example, if you have something like:
        ///
        ///    1|  {
        ///    2|    {
        ///    3|      foo ()
        ///    4|      {
        ///    5|      }
        ///    6|      bar ()
        ///    7|      {
        ///    8|      }
        ///    9|    }
        ///    0|  }
        ///    
        ///    ...and you delete the two methods, the ignore (trim) whitespace diff may be greedy about matching the
        ///    close braces.  Instead of showing lines 3-8 as being deleted, it may show lines 3-4, 6-7, and 8-9 being
        ///    deleted, matching with the close brackets on lines 5 and 7.  Even worse, since matches are chosen from
        ///    the right (after) file, the "matches" on lines 5 and 7 will be at the indent level of 9 and 10, shown
        ///    in the middle of the lines 3-8 block that is all indented at the same level.
        /// </summary>
        public static IDifferenceCollection<string> FinesseLineDifferences(IDifferenceCollection<string> lineDifferences, IList<string> leftLines, IList<string> rightLines)
        {
            if (lineDifferences.Differences.Count == 0)
                return lineDifferences;

            // Assume there will be approximately the same number of matches after we're done.
            List<Match> matches = new List<Match>(lineDifferences.Differences.Count + 1);

            int lastDiffOffset = 0;

            for (int i = 0; i < lineDifferences.Differences.Count; i++)
            {
                var diff = lineDifferences.Differences[i];

                Match before = diff.Before;
                if (lastDiffOffset != 0 && before != null)
                    before = new Match(MoveStartPoint(before.Left, lastDiffOffset),
                                       MoveStartPoint(before.Right, lastDiffOffset));

                Match after = diff.After;
                int thisDiffOffset = 0;

                // #1a) Try to transpose the diff forward
                while (after != null && after.Length > 0)
                {
                    // See how far we can transpose this diff forward (towards the end of the file)
                    int offset = TransposeDiffForward(after, diff, lineDifferences.LeftSequence, lineDifferences.RightSequence, leftLines, rightLines);

                    // If we didn't move it at all, we're done.
                    if (offset == 0)
                        break;

                    thisDiffOffset += offset;

                    // Offset our after match by the amount the diff chunk moved forwards
                    after = MoveStartPoint(after, offset);

                    // If the diff chunk just ate the entire match sequence, then combine this
                    // diff with the next diff, and use the next diff's After as our new after match.
                    if (after.Length == 0)
                    {
                        if (i < lineDifferences.Differences.Count - 1)
                        {
                            i++;
                            after = lineDifferences.Differences[i].After ?? new Match(new Span(leftLines.Count, 0), new Span(rightLines.Count, 0));
                            
                            // The offset (for the next round) resets, since we're moving on to the next diff here.
                            thisDiffOffset = 0;
                        }
                        else
                        {
                            after = new Match(new Span(leftLines.Count, 0), new Span(rightLines.Count, 0));
                        }
                    }

                    // If we didn't have a "before" before, we have one now.
                    if (before == null)
                        before = new Match(new Span(0, 0), new Span(0, 0));

                    // Grow the before match
                    before = MoveEndPoint(before, offset);

                    // Update the current diff to include the lines it ate from the after match (and
                    // possibly the next diff chunk as well, if we detected that the entire match
                    // was eaten above).
                    diff = new Difference(Span.FromBounds(before.Left.End, after.Left.Start),
                                          Span.FromBounds(before.Right.End, after.Right.Start),
                                          MaybeMatch(before),
                                          MaybeMatch(after));
                }

                // #1b) Try to transpose the diff backward.
                while (before != null && before.Length > 0)
                {
                    // See how far we can transpose this diff backwards (towards the start of the file)
                    int offset = TransposeDiffBackward(before, diff, lineDifferences.LeftSequence, lineDifferences.RightSequence, leftLines, rightLines);

                    // If we didn't move it at all, we're done.
                    if (offset == 0)
                        break;

                    thisDiffOffset += offset;

                    // Offset our before match by the amount the diff chunk moved backwards
                    before = MoveEndPoint(before, offset);

                    // If the diff chunk just ate the entire match sequence, then skip our before Match
                    // back to the previous Match.
                    if (before.Length == 0)
                    {
                        if (matches.Count > 0)
                        {
                            before = matches[matches.Count - 1];
                            matches.RemoveAt(matches.Count - 1);
                        }
                        else
                        {
                            before = new Match(new Span(0, 0), new Span(0, 0));
                        }
                    }

                    // If we didn't have an "after" before, we have one now.
                    if (after == null)
                        after = new Match(new Span(leftLines.Count, 0), new Span(rightLines.Count, 0));

                    // Grow the after match
                    after = MoveStartPoint(after, offset);

                    // Update the current diff to include the lines it ate from the before match (and
                    // possibly the previous diff chunk as well, if we detected that the entire match
                    // was eaten above).
                    diff = new Difference(Span.FromBounds(before.Left.End, after.Left.Start),
                                          Span.FromBounds(before.Right.End, after.Right.Start),
                                          MaybeMatch(before),
                                          MaybeMatch(after));
                }

                if (before != null && before.Length > 0)
                {
                    matches.Add(before);
                }

                if (i == lineDifferences.Differences.Count - 1 &&
                    after != null && after.Length > 0)
                {
                    matches.Add(after);
                }

                lastDiffOffset = thisDiffOffset;
            }

            return new FinessedDifferenceCollection(lineDifferences, matches);
        }

        #region Static helpers

        /// <summary>
        /// Return the given match, but only if it is non-null and non-empty. Otherwise, return <c>null</c>.
        /// </summary>
        static Match MaybeMatch(Match match)
        {
            if (match == null || match.Length == 0)
                return null;

            return match;
        }

        static Span MoveStartPoint(Span span, int offset)
        {
            return Span.FromBounds(span.Start + offset, span.End);
        }

        static Match MoveStartPoint(Match match, int offset)
        {
            return new Match(MoveStartPoint(match.Left, offset), MoveStartPoint(match.Right, offset));
        }

        static Span MoveEndPoint(Span span, int offset)
        {
            return Span.FromBounds(span.Start, span.End + offset);
        }

        static Match MoveEndPoint(Match match, int offset)
        {
            return new Match(MoveEndPoint(match.Left, offset), MoveEndPoint(match.Right, offset));
        }

        /// <summary>
        /// Transpose the given diff spans forward, returning the distance moved.
        /// </summary>
        static int TransposeDiffForward(Match after, Difference diff, IList<string> leftSequence, IList<string> rightSequence,
                                                                      IList<string> leftLines, IList<string> rightLines)
        {
            int distanceMoved = 0;

            while (distanceMoved < after.Length)
            {
                string firstLeftDiffLine = leftSequence[diff.Left.Start + distanceMoved];
                string firstRightDiffLine = rightSequence[diff.Right.Start + distanceMoved];

                string firstMatchLine = leftSequence[after.Left.Start + distanceMoved];

                // To continue, the first line in both the left/right sides of the diff has to match 
                if (!string.Equals(firstLeftDiffLine, firstMatchLine, StringComparison.Ordinal) ||
                    !string.Equals(firstRightDiffLine, firstMatchLine, StringComparison.Ordinal))
                    break;

                // Check to see if the existing match start is a filtered match, and the new match we'd be creating (at diff.Left/Right.Start)
                // would be a non-filtered match

                var leftMatchLine = leftLines[after.Left.Start + distanceMoved];
                var rightMatchLine = rightLines[after.Right.Start + distanceMoved];

                var leftDiffLine = leftLines[diff.Left.Start + distanceMoved];
                var rightDiffLine = rightLines[diff.Right.Start + distanceMoved];

                bool existingMatchIsExact = string.Equals(leftMatchLine, rightMatchLine, StringComparison.Ordinal);
                bool newMatchIsExact = string.Equals(leftDiffLine, rightDiffLine, StringComparison.Ordinal);

                bool preferExistingMatch = existingMatchIsExact && !newMatchIsExact;
                bool preferNewMatch = newMatchIsExact && !existingMatchIsExact;

                if (preferExistingMatch)
                    break;

                if (!preferNewMatch && IsCommonBlockStart(firstMatchLine))
                    break;

                distanceMoved++;
            }

            return distanceMoved;
        }

        /// <summary>
        /// Transpose the given diff spans backward, returning the distance moved.
        /// </summary>
        static int TransposeDiffBackward(Match before, Difference diff, IList<string> leftSequence, IList<string> rightSequence,
                                                                        IList<string> leftLines, IList<string> rightLines)
        {
            int distanceMoved = 0;

            while (distanceMoved < before.Length)
            {
                string lastLeftDiffLine = leftSequence[diff.Left.End - 1 - distanceMoved];
                string lastRightDiffLine = rightSequence[diff.Right.End - 1 - distanceMoved];

                string lastMatchLine = leftSequence[before.Left.End - 1 - distanceMoved];

                // To continue, the last line in both the left/right sides of the diff has to match 
                if (!string.Equals(lastLeftDiffLine, lastMatchLine, StringComparison.Ordinal) ||
                    !string.Equals(lastRightDiffLine, lastMatchLine, StringComparison.Ordinal))
                    break;

                // Check to see if the existing match start is a filtered match, and the new match we'd be creating (at diff.Left/Right.Start)
                // would be a non-filtered match

                var leftMatchLine = leftLines[before.Left.End - 1 - distanceMoved];
                var rightMatchLine = rightLines[before.Right.End - 1 - distanceMoved];

                var leftDiffLine = leftLines[diff.Left.End - 1 - distanceMoved];
                var rightDiffLine = rightLines[diff.Right.End - 1 - distanceMoved];

                bool existingMatchIsExact = string.Equals(leftMatchLine, rightMatchLine, StringComparison.Ordinal);

                bool newMatchIsExact = string.Equals(leftDiffLine, rightDiffLine, StringComparison.Ordinal);

                bool preferExistingMatch = existingMatchIsExact && !newMatchIsExact;
                bool preferNewMatch = newMatchIsExact && !existingMatchIsExact;

                if (preferExistingMatch)
                    break;

                if (!preferNewMatch && IsCommonBlockEnd(lastMatchLine))
                    break;

                distanceMoved++;
            }

            // Since we're going backward, negate the distance moved (for the offset)
            return -distanceMoved;
        }

        internal static bool IsCommonBlockStart(string line)
        {
            string trimmed = line.Trim();
            // Unlike moving backwards, we allow the block to move
            // forwards through blank lines, but this is predicated on the knowledge that
            // we transpose forward *first*, and thus will stop when transposing the block
            // backward.  If the order is ever changed, this line should be commented back in.
            //
            //if (trimmed.Length == 0)
            //    return true;

            // C-style blocks?
            if (trimmed.EndsWith("{", StringComparison.Ordinal) ||
                trimmed.StartsWith("{", StringComparison.Ordinal))
                return true;

            return false;
        }

        internal static bool IsCommonBlockEnd(string line)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                return true;

            // C-style blocks?
            if (trimmed.EndsWith("}", StringComparison.Ordinal) ||
                trimmed.StartsWith("}", StringComparison.Ordinal))
                return true;

            // XML/HTML-style closing tag
            if (trimmed.StartsWith("</", StringComparison.Ordinal) ||
                trimmed.EndsWith("/>", StringComparison.Ordinal))
                return true;

            return false;
        }

        static IList<Difference> GenerateDifferences(int leftCount, int rightCount, IList<Match> newMatches)
        {
            if (newMatches.Count == 0)
            {
                return new List<Difference>() { new Difference(new Span(0, leftCount), new Span(0, rightCount), null, null) };
            }

            IList<Difference> differences = new List<Difference>(newMatches.Count - 1);

            // Diff before the first match
            Match firstMatch = newMatches[0];
            if (firstMatch.Left.Start != 0 || firstMatch.Right.Start != 0)
            {
                Span left = Span.FromBounds(0, firstMatch.Left.Start);
                Span right = Span.FromBounds(0, firstMatch.Right.Start);
                differences.Add(new Difference(left, right, null, firstMatch));
            }

            // Diffs in between matches
            {
                Match before = null;
                Match after = null;

                foreach (var match in newMatches)
                {
                    before = after;
                    after = match;

                    // Diff with both a before and after
                    if (before != null)
                    {
                        Span left = Span.FromBounds(before.Left.End, after.Left.Start);
                        Span right = Span.FromBounds(before.Right.End, after.Right.Start);
                        differences.Add(new Difference(left, right, before, after));
                    }
                }
            }

            // Diff after the last match
            Match lastMatch = newMatches[newMatches.Count - 1];
            if (lastMatch.Left.End < leftCount || lastMatch.Right.End < rightCount)
            {
                Span left = Span.FromBounds(lastMatch.Left.End, leftCount);
                Span right = Span.FromBounds(lastMatch.Right.End, rightCount);
                differences.Add(new Difference(left, right, lastMatch, null));
            }

            return differences;
        }

        #endregion

        private FinessedDifferenceCollection(IDifferenceCollection<string> original, IList<Match> newMatches)
        {
            LeftSequence = original.LeftSequence;
            RightSequence = original.RightSequence;

            Differences = GenerateDifferences(LeftSequence.Count, RightSequence.Count, newMatches);
        }

        public IList<Difference> Differences { get; private set; }

        public IList<string> LeftSequence { get; private set; }

        public IEnumerable<Tuple<int, int>> MatchSequence
        {
            get
            {
                if (Differences.Count == 0)
                    yield break;

                foreach (var diff in Differences)
                {
                    if (diff.Before != null)
                    {
                        foreach (var pair in diff.Before)
                            yield return pair;
                    }
                }

                var lastMatch = Differences[Differences.Count - 1].After;
                if (lastMatch != null)
                {
                    foreach (var pair in lastMatch)
                        yield return pair;
                }
            }
        }

        public IList<string> RightSequence { get; private set; }

        public IEnumerator<Difference> GetEnumerator()
        {
            return Differences.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Differences.GetEnumerator();
        }
    }

}
