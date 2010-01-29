// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Tests.Load
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using Context;
	using Magnum;
	using Magnum.DateTimeExtensions;
	using Messages;

	public class LoadGenerator<TRequest, TResponse> :
		Consumes<TResponse>.All
		where TRequest : class, First
		where TResponse : class, First

	{
		private readonly Dictionary<Guid, CommandInstance> _commands = new Dictionary<Guid, CommandInstance>();
		private readonly AutoResetEvent _received = new AutoResetEvent(false);
		private int _responseCount;
		private int _unknownCommands;
		private IServiceBus _bus;

		public void Consume(TResponse message)
		{
			CommandInstance instance;
			lock (_commands)
				if (!_commands.TryGetValue(message.CorrelationId, out instance))
				{
					Interlocked.Increment(ref _unknownCommands);
					return;
				}

			instance.ResponseCreatedAt = message.CreatedAt;
			instance.ResponseReceivedAt = SystemUtil.UtcNow;
			instance.Worker = _bus.ConsumeContext(x => x.SourceAddress);

			Interlocked.Increment(ref _responseCount);

			_received.Set();
		}

		public void Run(IServiceBus bus, int iterations, Func<Guid, TRequest> generateRequest)
		{
			_bus = bus;

			using (bus.Subscribe(this).Disposable())
			{
				ThreadUtil.Sleep(2.Seconds());

				for (int i = 0; i < iterations; i++)
				{
					var commandInstance = new CommandInstance();
					lock (_commands)
						_commands.Add(commandInstance.Id, commandInstance);

					var command = generateRequest(commandInstance.Id);

					ThreadUtil.Sleep(5.Milliseconds());

					bus.Publish(command, x =>
						{
							x.SendResponseTo(bus.Endpoint);

							x.IfNoSubscribers<FirstCommand>(message => { throw new InvalidOperationException("No subscriptions were found (timing error?)"); });
						});
				}

				while (_received.WaitOne(5.Seconds(), true))
				{
				}
			}

			DisplayResults();
		}

		private void DisplayResults()
		{
            var sources = GetWorkerLoad();

			int sent = 0;
			int received = 0;

			TimeSpan totalDuration = TimeSpan.Zero;
			TimeSpan receiveDuration = TimeSpan.Zero;

			_commands.Values.Each(command =>
				{
					sent++;

					if (command.Worker != null)
					{
						received++;
						totalDuration += (command.ResponseReceivedAt - command.CreatedAt);
						receiveDuration += (command.ResponseCreatedAt - command.CreatedAt);
					}
				});

			Trace.WriteLine("Total Commands Sent = " + sent);
			Trace.WriteLine("Total Responses Received = " + received);
			Trace.WriteLine("Total Elapsed Time = " + totalDuration.TotalSeconds + "s");
			if (received > 0)
				Trace.WriteLine("Mean Roundtrip Time = " + (totalDuration.TotalMilliseconds/received).ToString("F0") + "ms");

			Trace.WriteLine("Receive Latency = " + receiveDuration.TotalSeconds + "s");
			if (received > 0)
				Trace.WriteLine("Mean Receive Latency = " + (receiveDuration.TotalMilliseconds/received).ToString("F0") + "ms");

			if (received > 0)
			{
				var query = _commands.Values.Select(x => x.ResponseReceivedAt - x.CreatedAt).OrderBy(x => x);

				int count = query.Count();

				int offset = Convert.ToInt32(count*0.95);

				TimeSpan value = query.Skip(offset).First();

				Trace.WriteLine("95th Percentile = " + value.TotalMilliseconds + "ms");
			}

			Trace.WriteLine("Workers Utilized");

			sources.Each(worker => Trace.WriteLine(worker.Key + ": " + worker.Value + " commands"));

			received.ShouldEqual(sent);
		}

        public Dictionary<Uri, int> GetWorkerLoad()
        {
            var sources = new Dictionary<Uri, int>();

            _commands.Values.Each(command =>
            {
                if (command.Worker != null)
                {
                    if (sources.ContainsKey(command.Worker))
                        sources[command.Worker] = sources[command.Worker] + 1;
                    else
                        sources.Add(command.Worker, 1);
                }
            });

            return sources;
        }
	}
}