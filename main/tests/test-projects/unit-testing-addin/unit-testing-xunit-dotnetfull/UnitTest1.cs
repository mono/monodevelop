using System;
using Xunit;

namespace unittestingxunitdotnetfull
{
    public class UnitTest1
    {
        [Fact]
        public void Test1Sucess()
        {
            Assert.Equal(1, 1);
        }

        [Fact]
        public void Test1Failure()
        {
            Assert.Equal(1, 2);
        }
    }
}
