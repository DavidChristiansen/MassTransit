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
namespace MassTransit.Pipeline.Sinks
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Routes messages to instances of subscribed components. A new instance of the component
	/// is created from the container for each message received.
	/// </summary>
	/// <typeparam name="TComponent">The component type to handle the message</typeparam>
	/// <typeparam name="TMessage">The message to handle</typeparam>
	public class SelectedComponentMessageSink<TComponent, TMessage> :
		IPipelineSink<TMessage>
		where TMessage : class
		where TComponent : class, Consumes<TMessage>.Selected
	{
		private readonly Func<TComponent> _getComponent;

		public SelectedComponentMessageSink(Func<TComponent> getComponent)
		{
			_getComponent = getComponent;
		}

		public void Dispose()
		{
		}

		public IEnumerable<Action<TMessage>> Enumerate(TMessage message)
		{
			TComponent component = _getComponent();

			try
			{
				if (component.Accept(message) == false)
					yield break;

				yield return component.Consume;
			}
			finally
			{
				// TODO I think Castle sucks, so make sure your lifecycle is such that castle is not tracking your component
			}
		}

		public bool Inspect(IPipelineInspector inspector)
		{
			return inspector.Inspect(this);
		}
	}
}