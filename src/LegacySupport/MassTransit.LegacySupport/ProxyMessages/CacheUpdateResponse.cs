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
namespace MassTransit.ServiceBus.Subscriptions.Messages
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class CacheUpdateResponse
    {
        List<Subscription> _subscriptions;

        //xml serialization
        public CacheUpdateResponse()
        {
            _subscriptions = new List<Subscription>();
        }

        public CacheUpdateResponse(IEnumerable<Subscription> subscriptions)
        {
            _subscriptions = new List<Subscription>(subscriptions);
        }

        public List<Subscription> Subscriptions
        {
            get { return _subscriptions; }
            set { _subscriptions = value;}
        }
    }
}