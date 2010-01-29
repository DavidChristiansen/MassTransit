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
namespace MassTransit
{
    using System;
    using Context;
    using Pipeline;
    using Saga;

	/// <summary>
	/// The action to call to unsubscribe a previously subscribed consumer
	/// </summary>
	/// <returns></returns>
	public delegate bool UnsubscribeAction();

	/// <summary>
	/// The action to call to unregister a previously registered component
	/// </summary>
	/// <returns></returns>
	public delegate bool UnregisterAction();

    /// <summary>
    /// The base service bus interface
    /// </summary>
    public interface IServiceBus :
        IDisposable
    {
        /// <summary>
        /// The endpoint from which messages are received
        /// </summary>
        IEndpoint Endpoint { get; }

        /// <summary>
        /// The poison endpoint associated with this instance where messages that cannot be processed are sent
        /// </summary>
        IEndpoint PoisonEndpoint { get; }

        /// <summary>
        /// Adds a message handler to the service bus for handling a specific type of message
        /// </summary>
        /// <typeparam name="T">The message type to handle, often inferred from the callback specified</typeparam>
        /// <param name="callback">The callback to invoke when messages of the specified type arrive on the service bus</param>
		UnsubscribeAction Subscribe<T>(Action<T> callback) where T : class;

        /// <summary>
        /// Adds a message handler to the service bus for handling a specific type of message
        /// </summary>
        /// <typeparam name="T">The message type to handle, often inferred from the callback specified</typeparam>
        /// <param name="callback">The callback to invoke when messages of the specified type arrive on the service bus</param>
        /// <param name="condition">A condition predicate to filter which messages are handled by the callback</param>
		UnsubscribeAction Subscribe<T>(Action<T> callback, Predicate<T> condition) where T : class;

        /// <summary>
        /// Connects any consumers for the component to the message dispatcher
        /// </summary>
        /// <typeparam name="T">The consumer type</typeparam>
        /// <param name="consumer">The component</param>
		UnsubscribeAction Subscribe<T>(T consumer) where T : class;

    	/// <summary>
        /// Adds a component to the dispatcher that will be created on demand to handle messages
        /// </summary>
        /// <typeparam name="TConsumer">The type of the component to add</typeparam>
		UnsubscribeAction Subscribe<TConsumer>() where TConsumer : class;

        /// <summary>
        /// Adds a component to the dispatcher that will be created on demand to handle messages
        /// </summary>
        /// <param name="consumerType">The type of component to add</param>
		UnsubscribeAction Subscribe(Type consumerType);


		/// <summary>
		/// Subscribe to a message that has a consumer that is retrieved from the specified expression
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="getConsumerAction"></param>
		/// <returns></returns>
    	UnsubscribeAction SubscribeConsumer<T>(Func<T,Action<T>> getConsumerAction) where T : class;


    	/// <summary>
        /// Publishes a message to all subscribed consumers for the message type
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="message">The messages to be published</param>
        void Publish<T>(T message) where T : class;

    	void Publish<T>(T message, Action<IPublishContext> contextAction) where T : class;

    	/// <summary>
		/// Returns the service for the requested interface if it was registered with the service bus
		/// </summary>
		/// <typeparam name="TService"></typeparam>
		/// <returns></returns>
    	TService GetService<TService>();

		/// <summary>
		/// Returns a value from the specified context, using the current thread context as the key index
		/// </summary>
		/// <typeparam name="TContext">The type of context being requested</typeparam>
		/// <typeparam name="TResult">The type of the property being returned</typeparam>
		/// <param name="accessor">The accessor method to return the value</param>
		/// <returns>The value returned by the accessor function</returns>
		TResult Context<TContext, TResult>(Func<TContext, TResult> accessor);

		/// <summary>
		/// Calls the action with the requested context interface
		/// </summary>
		/// <typeparam name="TContext"></typeparam>
		/// <param name="action"></param>
		void Context<TContext>(Action<TContext> action);

		IMessagePipeline OutboundPipeline { get; }

		IMessagePipeline InboundPipeline { get; }

    	IServiceBus ControlBus { get; }
    }

	public delegate Action<T> GetConsumerAction<T>(T message);
}