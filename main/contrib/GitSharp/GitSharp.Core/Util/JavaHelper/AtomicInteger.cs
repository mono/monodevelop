namespace GitSharp.Core.Util.JavaHelper
{
    public class AtomicInteger : AtomicValue<int>
    {
        public AtomicInteger(int init)
            : base(init)
        {
        }

        public AtomicInteger()
        {
        }

        protected override int InnerAdd(int value, int delta)
        {
            return value + delta;
        }

        protected override int One
        {
            get { return 1; }
        }

        protected override int MinusOne
        {
            get { return -1; }
        }
    }

    public class AtomicLong : AtomicValue<long>
    {
        public AtomicLong(int init)
            : base(init)
        {
        }

        public AtomicLong()
        {
        }

        protected override long InnerAdd(long value, long delta)
        {
            return value + delta;
        }

        protected override long One
        {
            get { return 1; }
        }

        protected override long MinusOne
        {
            get { return -1; }
        }
    }
}


