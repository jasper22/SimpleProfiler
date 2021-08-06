using System;

using Xunit;

namespace ShugiShugi.Test
{
    public class UnitTest1
    {
        private CoreObject coreObject;

        public UnitTest1()
        {
            coreObject = new CoreObject();
        }

        [Fact]
        public void Test1()
        {
            coreObject.Function1();

            Assert.True(true);
        }

        [Fact]
        public void Test2()
        {
            coreObject.Function2();

            Assert.True(true);
        }

        [Fact]
        public void Test3()
        {
            coreObject.Function3();

            Assert.True(true);
        }
    }
}
