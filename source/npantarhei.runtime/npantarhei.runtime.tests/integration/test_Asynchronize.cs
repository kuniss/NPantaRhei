﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using npantarhei.runtime.contract;
using npantarhei.runtime.messagetypes;

namespace npantarhei.runtime.tests.integration
{
    [TestFixture]
    public class test_Asynchronize
    {
        [Test]
        public void Run_on_separate_thread()
        {
            using (var sut = new FlowRuntime())
            {
                sut.AddStream(new Stream(".in", "asyncNop"));
                sut.AddStream(new Stream("asyncNop", ".out"));

                var cont = new FlowOperationContainer();

                long asyncThreadId = 0;
                cont.AddFunc<string, string>("asyncNop", _ =>
                                                             {
                                                                 asyncThreadId = Thread.CurrentThread.GetHashCode();
                                                                 return _;
                                                             }).MakeAsync();
                sut.AddOperations(cont.Operations);

                IMessage result = null;
                long runtimeThreadId = 0;
                var are = new AutoResetEvent(false);
                sut.Result += _ =>
                                  {
                                      runtimeThreadId = Thread.CurrentThread.GetHashCode();
                                      result = _; 
                                      are.Set();
                                  };

                sut.Process(new Message(".in", "hello"));

                Assert.IsTrue(are.WaitOne(1000));
                Assert.AreEqual("hello", result.Data.ToString());
                Assert.AreNotEqual(runtimeThreadId, asyncThreadId);
            }
        }

        [Test]
        public void Catch_exception_from_background()
        {
            using (var sut = new FlowRuntime())
            {
                sut.AddStream(new Stream(".in", "throw"));

                var cont = new FlowOperationContainer();
                cont.AddAction<string>("throw", (string _) => { throw new ApplicationException("xxx"); }).MakeAsync();
                sut.AddOperations(cont.Operations);

                FlowRuntimeException ex = null;
                var are = new AutoResetEvent(false);
                sut.UnhandledException += _ =>
                                                {
                                                    ex = _;
                                                    are.Set();
                                                };

                sut.Process(new Message(".in", "hello"));

                Assert.IsTrue(are.WaitOne(1000));
                Assert.AreEqual("xxx", ex.InnerException.Message);
            }
        }
    }
}
