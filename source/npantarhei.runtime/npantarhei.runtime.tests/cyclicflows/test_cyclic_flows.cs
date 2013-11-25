using System;
using NUnit.Framework;
using npantarhei.runtime.contract;
using npantarhei.runtime.messagetypes;
using npantarhei.runtime.operations;

namespace npantarhei.runtime.tests.cyclicflows
{
	[TestFixture]
	public class test_cyclic_flows
	{
		internal const int maxCycles = 5000;
		
		[Test]
		public void test_cyclic_flow_with_async_EBC ()
		{
			Console.WriteLine("test started for {0} flow cycles...", maxCycles);

            var config = new FlowRuntimeConfiguration()
                                .AddStreamsFrom(@"
                                                    /
                                                    .in, cyclic.run
                                                    cyclic.continue, cyclic.run
                                                    cyclic.stopOn, .out
                                                    ")
                                .AddEventBasedComponent("cyclic", new CyclicFlow());

            using(var fr = new FlowRuntime(config))
            {
                fr.UnhandledException += Console.WriteLine;

                fr.Process(".in", 1);

                IMessage result = null;
                Assert.IsTrue(fr.WaitForResult(5000, _ => result = _), 
				              "result not received within 5 seconds");
                Assert.AreEqual(".out", result.Port.Fullname);
                Assert.AreEqual(maxCycles, (int)result.Data);
            }
		}
		
		class CyclicFlow
	    {			
			[AsyncMethod]
	        public void Run(int counter)
	        {
	            if (counter < maxCycles) {
					Console.WriteLine("cycle {0}", counter);
	                Continue(counter+1);
				}
	            else
	                StopOn(maxCycles);
	        }
	
	        public event Action<int> StopOn;
	        public event Action<int> Continue;
	    }

	
		[Test]
		public void test_cyclic_flow_with_sync_action_fails ()
		{
			Console.WriteLine("test started for {0} flow cycles...", maxCycles);

            var config = new FlowRuntimeConfiguration()
                                .AddStreamsFrom(@"
                                                    /
                                                    .in, cyclic.in
                                                    cyclic.out0, cyclic.in
                                                    cyclic.out1, .stopped
                                                    ")
                                .AddAction<int, int, int>("cyclic", 
					                            (counter, continueCycling, stopOn) =>
					                            {
													if (counter < maxCycles) 
													{
														Console.WriteLine("cycle {0}", counter);
														continueCycling(counter+1);
													}
													else{
														stopOn(maxCycles);
													}
												}
								);

            using(var fr = new FlowRuntime(config))
            {
                fr.UnhandledException += Console.WriteLine;

                fr.Process(".in", 1);

                Assert.IsFalse(fr.WaitForResult(5000), "StackOverflow exception expected but not thrown");
            }
		}

		[Test]
		public void test_cyclic_flow_with_async_action ()
		{
			Console.WriteLine("test started for {0} flow cycles...", maxCycles);

            var config = new FlowRuntimeConfiguration()
                                .AddStreamsFrom(@"
                                                    /
                                                    .in, cyclic.in
                                                    cyclic.out0, cyclic.in
                                                    cyclic.out1, .stopped
                                                    ")
                                .AddAction<int, int, int>("cyclic", 
					                            (counter, continueCycling, stopOn) =>
					                            {
													if (counter < maxCycles) 
													{
														Console.WriteLine("cycle {0}", counter);
														continueCycling(counter+1);
													}
													else{
														stopOn(maxCycles);
													}
												}
								).MakeAsync();

            using(var fr = new FlowRuntime(config))
            {
                fr.UnhandledException += Console.WriteLine;

                fr.Process(".in", 1);

                IMessage result = null;
                Assert.IsTrue(fr.WaitForResult(5000, _ => result = _), 
				              "result not received with 5 seconds");
                Assert.AreEqual(".stopped", result.Port.Fullname);
                Assert.AreEqual(maxCycles, (int)result.Data);
            }
		}

	}
	
}

