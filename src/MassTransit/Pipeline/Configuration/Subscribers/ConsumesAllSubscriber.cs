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
namespace MassTransit.Pipeline.Configuration.Subscribers
{
	using System;
	using Sinks;

	public class ConsumesAllSubscriber :
		ConsumesSubscriberBase<ConsumesAllSubscriber>
	{
		protected override Type InterfaceType
		{
			get { return typeof (Consumes<>.All); }
		}

		protected virtual UnsubscribeAction Connect<TMessage>(ISubscriberContext context, Consumes<TMessage>.All consumer)
			where TMessage : class
		{
			var sink = new InstanceMessageSink<TMessage>(message => consumer.Consume);

			return context.Pipeline.ConnectToRouter(sink, () => context.SubscribedTo<TMessage>());
		}

		protected virtual UnsubscribeAction Connect<TComponent, TMessage>(ISubscriberContext context)
			where TMessage : class
			where TComponent : class, Consumes<TMessage>.All
		{
			var sink = new ComponentMessageSink<TComponent, TMessage>(() => context.Builder.GetInstance<TComponent>());

			return context.Pipeline.ConnectToRouter(sink, () => context.SubscribedTo<TMessage>());
		}
	}
}