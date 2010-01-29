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
namespace MassTransit.Distributor
{
	using System;
	using System.Linq;
	using Context;
	using Magnum;
	using Magnum.Actors.Schedulers;
	using Magnum.DateTimeExtensions;
	using Magnum.Threading;
	using Messages;

	public class Distributor<T> :
		IDistributor<T>,
		Consumes<T>.Selected
		where T : class
	{
		private readonly IEndpointFactory _endpointFactory;
		private readonly IWorkerSelectionStrategy<T> _selectionStrategy;
		private readonly ReaderWriterLockedDictionary<Uri, WorkerDetails> _workers = new ReaderWriterLockedDictionary<Uri, WorkerDetails>();
		private ThreadPoolScheduler _threadPoolScheduler;
		private UnsubscribeAction _unsubscribeAction = () => false;
        private readonly int _pingTimeout = (int)1.Minutes().TotalMilliseconds;
		private IServiceBus _bus;

		public Distributor(IEndpointFactory endpointFactory, IWorkerSelectionStrategy<T> workerSelectionStrategy)
		{
			_endpointFactory = endpointFactory;
			_selectionStrategy = workerSelectionStrategy;
		}

		public Distributor(IEndpointFactory endpointFactory)
			:
				this(endpointFactory, new DefaultWorkerSelectionStrategy<T>())
		{
		}

		public void Consume(T message)
		{
			WorkerDetails worker = _selectionStrategy.GetAvailableWorkers(_workers.Values, message).FirstOrDefault();
			if (worker == null)
			{
				_bus.Context<IConsumeContext>(x => x.RetryLater());
				return;
			}

			worker.Add();

			IEndpoint endpoint = _endpointFactory.GetEndpoint(worker.DataUri);

			var distributed = new Distributed<T>(message, _bus.ConsumeContext(x => x.ResponseAddress));

			endpoint.Send(distributed);
		}

		public bool Accept(T message)
		{
			return _selectionStrategy.GetAvailableWorkers(_workers.Values, message).Count() > 0;
		}

		public void Dispose()
		{
		}

		public void Start(IServiceBus bus)
		{
			_bus = bus;

			_unsubscribeAction = bus.Subscribe<WorkerAvailable<T>>(Consume);

			// don't plan to unsubscribe this since it's an important thing
			bus.Subscribe(this);

			_threadPoolScheduler = new ThreadPoolScheduler();

			_threadPoolScheduler.Schedule(_pingTimeout, _pingTimeout, PingWorkers);
		}

		public void Stop()
		{
			_threadPoolScheduler.Dispose();

			_workers.Clear();

			_unsubscribeAction();
		}

		public void Consume(WorkerAvailable<T> message)
		{
			WorkerDetails worker = _workers.Retrieve(message.ControlUri, () =>
				{
					return new WorkerDetails
						{
							ControlUri = message.ControlUri,
							DataUri = message.DataUri,
							InProgress = message.InProgress,
							InProgressLimit = message.InProgressLimit,
							Pending = message.Pending,
							PendingLimit = message.PendingLimit,
							LastUpdate = message.Updated,
						};
				});

			worker.UpdateInProgress(message.InProgress, message.InProgressLimit, message.Pending, message.PendingLimit, message.Updated);
		}

		private void PingWorkers()
		{
			_workers.Values
				.Where(x => x.LastUpdate < SystemUtil.UtcNow.Subtract(_pingTimeout.Milliseconds()))
				.ToList()
				.ForEach(x => { _endpointFactory.GetEndpoint(x.ControlUri).Send(new PingWorker()); });
		}
	}
}