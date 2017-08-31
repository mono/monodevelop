using System;
using Xunit;

namespace unit_testing_xunit_dotnetcore
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
