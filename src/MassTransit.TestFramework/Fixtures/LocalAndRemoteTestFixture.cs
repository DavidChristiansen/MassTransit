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
namespace MassTransit.TestFramework.Fixtures
{
	using System;
	using Configuration;
	using NUnit.Framework;
	using Services.Subscriptions;

	[TestFixture]
	public class LocalAndRemoteTestFixture<TEndpoint> :
		EndpointTestFixture<TEndpoint>
		where TEndpoint : IEndpoint
	{
		[TestFixtureSetUp]
		public void LocalAndRemoteTestFixtureSetup()
		{
			SetupSubscriptionService();

			if (LocalUri == null)
				throw new ArgumentException("The LocalUri property must be specified as part of the test fixture.");

			if (RemoteUri == null)
				throw new ArgumentException("The RemoteUri property must be specified as part of the test fixture.");

			LocalBus = SetupServiceBus(LocalUri);
			RemoteBus = SetupServiceBus(RemoteUri);
		}

		[TestFixtureTearDown]
		public void LocalAndRemoteTestFixtureTeardown()
		{
			LocalBus = null;
			RemoteBus = null;
			SubscriptionService = null;
		}

		protected Uri LocalUri { get; set; }
		protected Uri RemoteUri { get; set; }

		protected IServiceBus LocalBus { get; private set; }
		protected IServiceBus RemoteBus { get; private set; }
		protected ISubscriptionService SubscriptionService { get; private set; }

		protected override void ConfigureServiceBus(Uri uri, IServiceBusConfigurator configurator)
		{
			base.ConfigureServiceBus(uri, configurator);

			configurator.AddService<SubscriptionPublisher>();
			configurator.AddService<SubscriptionConsumer>();
		}

		private void SetupSubscriptionService()
		{
			SubscriptionService = new LocalSubscriptionService();

			ObjectBuilder.Add(SubscriptionService);
			ObjectBuilder.Construct(() => new SubscriptionPublisher(SubscriptionService));
			ObjectBuilder.Construct(() => new SubscriptionConsumer(SubscriptionService, EndpointFactory));
		}
	}
}