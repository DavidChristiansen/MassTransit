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
namespace MassTransit.Tests
{
	using System;
	using System.Collections.Generic;
	using Context;
	using Magnum.DateTimeExtensions;
	using Messages;
	using NUnit.Framework;
	using TestConsumers;
	using TextFixtures;

	[TestFixture]
	public class MessageContext_Specs :
		LoopbackLocalAndRemoteTestFixture
	{
		[Test]
		public void A_response_should_be_published_if_no_reply_address_is_specified()
		{
			PingMessage ping = new PingMessage();

			TestMessageConsumer<PongMessage> otherConsumer = new TestMessageConsumer<PongMessage>();
			RemoteBus.Subscribe(otherConsumer);

			TestCorrelatedConsumer<PongMessage, Guid> consumer = new TestCorrelatedConsumer<PongMessage, Guid>(ping.CorrelationId);
			LocalBus.Subscribe(consumer);

			FutureMessage<PongMessage> pong = new FutureMessage<PongMessage>();

			RemoteBus.Subscribe<PingMessage>(message =>
				{
					pong.Set(new PongMessage(message.CorrelationId));

					RemoteBus.Respond(pong.Message);
				});

			LocalBus.Publish(ping);

			Assert.IsTrue(pong.IsAvailable(3.Seconds()), "No pong generated");

			consumer.ShouldHaveReceivedMessage(pong.Message, 3.Seconds());
			otherConsumer.ShouldHaveReceivedMessage(pong.Message, 1.Seconds());
		}

		[Test]
		public void A_response_should_be_sent_directly_if_a_reply_address_is_specified()
		{
			PingMessage ping = new PingMessage();

			TestMessageConsumer<PongMessage> otherConsumer = new TestMessageConsumer<PongMessage>();
			RemoteBus.Subscribe(otherConsumer);

			TestCorrelatedConsumer<PongMessage, Guid> consumer = new TestCorrelatedConsumer<PongMessage, Guid>(ping.CorrelationId);
			LocalBus.Subscribe(consumer);

			FutureMessage<PongMessage> pong = new FutureMessage<PongMessage>();

			RemoteBus.Subscribe<PingMessage>(message =>
				{
					pong.Set(new PongMessage(message.CorrelationId));

					RemoteBus.Respond(pong.Message);
				});

			LocalBus.Publish(ping, context => context.SendResponseTo(LocalBus));

			Assert.IsTrue(pong.IsAvailable(3.Seconds()), "No pong generated");

			consumer.ShouldHaveReceivedMessage(pong.Message, 3.Seconds());
			otherConsumer.ShouldNotHaveReceivedMessage(pong.Message, 1.Seconds());
		}

		[Test]
		public void The_destination_address_should_pass()
		{
			FutureMessage<PingMessage> received = new FutureMessage<PingMessage>();

			LocalBus.Subscribe<PingMessage>(message =>
				{
					Assert.AreEqual(LocalBus.Endpoint.Uri, LocalBus.ConsumeContext(x => x.DestinationAddress));

					received.Set(message);
				});

			LocalBus.Publish(new PingMessage());

			Assert.IsTrue(received.IsAvailable(5.Seconds()), "No message was received");
		}

		[Test]
		public void The_fault_address_should_pass()
		{
			FutureMessage<PingMessage> received = new FutureMessage<PingMessage>();

			LocalBus.Subscribe<PingMessage>(message =>
				{
					Assert.AreEqual(LocalBus.Endpoint.Uri, LocalBus.ConsumeContext(x => x.FaultAddress));

					received.Set(message);
				});

			LocalBus.Publish(new PingMessage(), context => context.SendFaultTo(LocalBus));

			Assert.IsTrue(received.IsAvailable(5.Seconds()), "No message was received");
		}

		[Test]
		public void The_response_address_should_pass()
		{
			FutureMessage<PingMessage> received = new FutureMessage<PingMessage>();

			LocalBus.Subscribe<PingMessage>(message =>
				{
					Assert.AreEqual(LocalBus.Endpoint.Uri, LocalBus.ConsumeContext(x=>x.ResponseAddress));

					received.Set(message);
				});

			LocalBus.Publish(new PingMessage(), context => context.SendResponseTo(LocalBus));

			Assert.IsTrue(received.IsAvailable(5.Seconds()), "No message was received");
		}

		[Test]
		public void The_source_address_should_pass()
		{
			FutureMessage<PingMessage> received = new FutureMessage<PingMessage>();

			LocalBus.Subscribe<PingMessage>(message =>
				{
					Assert.AreEqual(LocalBus.Endpoint.Uri, LocalBus.ConsumeContext(x=>x.SourceAddress));

					received.Set(message);
				});

			LocalBus.Publish(new PingMessage());

			Assert.IsTrue(received.IsAvailable(5.Seconds()), "No message was received");
		}
	}

	[TestFixture]
	public class When_publishing_a_message_with_no_consumers :
		LoopbackLocalAndRemoteTestFixture
	{
		[Test]
		public void The_method_should_be_called_to_notify_the_caller()
		{
			var ping = new PingMessage();

			bool noConsumers = false;

			LocalBus.Publish(ping, x =>
				{
					x.IfNoSubscribers<PingMessage>(message =>
						{
							Assert.IsInstanceOfType(typeof (PingMessage), message);
							noConsumers = true;
						});
				});

			Assert.IsTrue(noConsumers, "There should have been no consumers");
		}

		[Test]
		public void The_method_should_not_carry_over_the_subsequent_calls()
		{
			var ping = new PingMessage();

			int hitCount = 0;

			LocalBus.Publish(ping, x => x.IfNoSubscribers<PingMessage>(message => hitCount++));
			LocalBus.Publish(ping);

			Assert.AreEqual(1, hitCount, "There should have been no consumers");
		}
	}

	[TestFixture]
	public class When_publishing_a_message_with_an_each_consumer_action_specified :
		LoopbackLocalAndRemoteTestFixture
	{
		[Test]
		public void The_method_should_not_be_called_when_there_are_no_subscribers()
		{
			var ping = new PingMessage();

			List<Uri> consumers = new List<Uri>();

			LocalBus.Publish(ping, x =>
				{
					x.ForEachSubscriber<PingMessage>((message,consumer) => consumers.Add(consumer.Uri));
				});

			Assert.AreEqual(0, consumers.Count);
		}

		[Test]
		public void The_method_should_be_called_for_each_destination_endpoint()
		{
			LocalBus.Subscribe<PingMessage>(x => { });

			var ping = new PingMessage();

			List<Uri> consumers = new List<Uri>();

			LocalBus.Publish(ping, x =>
				{
					x.ForEachSubscriber<PingMessage>((message,endpoint) => consumers.Add(endpoint.Uri));
				});

			Assert.AreEqual(1, consumers.Count);
			Assert.AreEqual(LocalBus.Endpoint.Uri, consumers[0]);
		}

		[Test]
		public void The_method_should_not_carry_over_to_the_next_call_context()
		{
			var ping = new PingMessage();

			List<Uri> consumers = new List<Uri>();

			LocalBus.Publish(ping, x =>
				{
					x.ForEachSubscriber<PingMessage>((message,endpoint) => consumers.Add(endpoint.Uri));
				});

			LocalBus.Subscribe<PingMessage>(x => { });

			LocalBus.Publish(ping);

			Assert.AreEqual(0, consumers.Count);
		}
		
		[Test]
		public void The_method_should_be_called_for_each_destination_endpoint_when_there_are_multiple()
		{
			LocalBus.Subscribe<PingMessage>(x => { });
			RemoteBus.Subscribe<PingMessage>(x => { });

			var ping = new PingMessage();

			List<Uri> consumers = new List<Uri>();

			LocalBus.Publish(ping, x =>
				{
					x.ForEachSubscriber<PingMessage>((message,endpoint) => consumers.Add(endpoint.Uri));
				});

			Assert.AreEqual(2, consumers.Count);
			Assert.IsTrue(consumers.Contains(LocalBus.Endpoint.Uri));
			Assert.IsTrue(consumers.Contains(RemoteBus.Endpoint.Uri));
		}
	}

}