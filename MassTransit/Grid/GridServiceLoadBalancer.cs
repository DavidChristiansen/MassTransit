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
namespace MassTransit.Grid
{
	using Paxos;
	using Saga;

	public class GridServiceLoadBalancer
	{
		private ISagaRepository<Acceptor<AvailableGridServiceNode>> _acceptors;
		private IServiceBus _bus;
		private IServiceBus _controlBus;
		private ISagaRepository<Learner<AvailableGridServiceNode>> _listeners;
		private UnsubscribeAction _unsubscribe = () => true;

		public GridServiceLoadBalancer(ISagaRepository<Acceptor<AvailableGridServiceNode>> acceptors,
		                               ISagaRepository<Learner<AvailableGridServiceNode>> listeners)
		{
			_acceptors = acceptors;
			_listeners = listeners;
		}

		public void Start(IServiceBus bus)
		{
			_bus = bus;
			_controlBus = bus.ControlBus;

			_unsubscribe += _controlBus.Subscribe<Acceptor<AvailableGridServiceNode>>();
			_unsubscribe += _controlBus.Subscribe<Learner<AvailableGridServiceNode>>();
		}


		public void Stop()
		{
			_unsubscribe();

			_controlBus = null;
			_bus = null;
		}
	}
}