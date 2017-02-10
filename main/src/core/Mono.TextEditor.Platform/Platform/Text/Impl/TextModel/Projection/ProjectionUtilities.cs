namespace Microsoft.VisualStudio.Text.Projection.Implementation
{
    internal struct ProjectionLineInfo
    {
        public int lineNumber;
        public int start;
        public int end;             // excluding line break
        public int lineBreakLength;
        public bool startComplete;
        public bool endComplete;

        //public override string ToString()
        //{
        //    System.Text.StringBuilder sb = new System.Text.StringBuilder("<");
        //    sb.Append("line# ");
        //    sb.Append(lineNumber);
        //    sb.Append(" start ");
        //    sb.Append(start);
        //    sb.Append(startComplete ? "!" : "?");
        //    sb.Append(" end ");
        //    sb.Append(end);
        //    sb.Append(endComplete ? "!" : "?");
        //    sb.Append(" lbl ");
        //    sb.Append(lineBreakLength);
        //    sb.Append(">");
        //    return sb.ToString();
        //}
    }

    internal enum ProjectionLineCalculationState
    {
        /// <summary>
        /// We are searching for the map node containing the requested position.
        /// </summary>
        Primary,

        /// <summary>
        /// The primary node has been found, but it did not contain the line break signifying the
        /// end of the line, so we look to the right to discover the tail of the line.
        /// </summary>
        Append,

        /// <summary>
        /// The primary node has been found, but it did not contain the line break signifying the
        /// end of the previous line, so we look to the left to discover the head of the line.
        /// </summary>
        Prepend,

        /// <summary>
        /// The primary node has been found, but it contained neither the line break signifying the
        /// end of the previous line nor the line break signifying the end of the current line, so we
        /// look both to the left and right to discover the head and tail of the line.
        /// </summary>
        Bipend
    }
}
