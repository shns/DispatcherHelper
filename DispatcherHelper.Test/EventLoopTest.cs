using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DispatcherHelper.Test
{
    [TestClass]
    public class EventLoopTest
    {
        [TestMethod]
        public void RunTest()
        {
            using (var loop = new EventLoop())
            {
                loop.Run();
                var expected = loop.Dispatcher.Thread.ManagedThreadId;
                var actual = 0;
                loop.Dispatcher.Invoke(
                    () =>
                    {
                        actual = Thread.CurrentThread.ManagedThreadId;
                    });
                Assert.AreEqual(expected, actual, "ワーカースレッドで処理を実行すること");
            }
        }

        [TestMethod]
        public void RunTwiceTest()
        {
            using (var loop = new EventLoop())
            {
                loop.Run();
                Assert.ThrowsException<InvalidOperationException>(
                    () =>
                    {
                        loop.Run();
                    },
                    "2回目以降のRunメソッド実行でInvalidOperationExceptionをスローすること");
            }
        }

        [TestMethod]
        public void DispatcherTest()
        {
            using (var loop = new EventLoop())
            {
                loop.Run();
                loop.Dispose();
                Assert.ThrowsException<ObjectDisposedException>(
                    () =>
                    {
                        var d = loop.Dispatcher;
                    },
                    "Dispose後のDispatcherプロパティアクセスでObjectDisposedExceptionをスローすること");
            }
        }

        [TestMethod]
        public void DisposeTest()
        {
            using (var loop = new EventLoop())
            {
            }
            Assert.IsTrue(true, "Run実行前にDispose可能なこと");
        }
    }
}
