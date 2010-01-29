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
namespace MassTransit.Tests.Serialization
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Magnum.DateTimeExtensions;
    using MassTransit.Internal;
    using MassTransit.Serialization;
    using Messages;
    using NUnit.Framework;

    [TestFixture]
    public class MessageSerialization_Specs :
		SerializationSpecificationBase
    {
        [SetUp]
        public void SetupContext()
        {
        	_message = new SerializationTestMessage
        		{
        			DecimalValue = 123.45m,
        			LongValue = 098123213,
        			BoolValue = true,
        			ByteValue = 127,
        			IntValue = 123,
        			DateTimeValue = new DateTime(2008, 9, 8, 7, 6, 5, 4),
        			TimeSpanValue = 30.Seconds(),
                    GuidValue = Guid.NewGuid(),
                    StringValue = "Chris's Sample Code",
                    DoubleValue = 1823.172,
					MaybeMoney = 567.89m,
                };
        }

        private SerializationTestMessage _message;

		// well crap, the built-in serializer from .NET doesn't support TimeSpan
        [Test, Explicit]
        public void The_xml_serializer_should_be_awesome()
        {
            byte[] serializedMessageData;

            var serializer = new DotNotXmlMessageSerializer();

            using (MemoryStream output = new MemoryStream())
            {
                serializer.Serialize(output, _message, _sendContext);

                serializedMessageData = output.ToArray();

                Trace.WriteLine(Encoding.UTF8.GetString(serializedMessageData));
            }

            using (MemoryStream input = new MemoryStream(serializedMessageData))
            {
                SerializationTestMessage receivedMessage = serializer.Deserialize(input, _receiveContext) as SerializationTestMessage;

                Assert.AreEqual(_message, receivedMessage);
            }
        }

        [Test]
        public void The_xml_message_serializer_should_rock_the_house()
        {
            byte[] serializedMessageData;

            var serializer = new XmlMessageSerializer();

			_sendContext.SetSourceAddress("msmq://localhost/queue_name");
			_sendContext.SetDestinationAddress("msmq://remotehost/queue_name");
			_sendContext.SetResponseAddress("msmq://localhost/response_queue");
			_sendContext.SetFaultAddress("msmq://localhost/fault_queue");
			_sendContext.SetRetryCount(7);

            using (MemoryStream output = new MemoryStream())
            {
                serializer.Serialize(output, _message, _sendContext);

                serializedMessageData = output.ToArray();

                Trace.WriteLine(Encoding.UTF8.GetString(serializedMessageData));
            }

            using (MemoryStream input = new MemoryStream(serializedMessageData))
            {
                SerializationTestMessage receivedMessage = serializer.Deserialize(input, _receiveContext) as SerializationTestMessage;

                Assert.AreEqual(_message, receivedMessage);
            }
        }

        [Test]
        public void The_binary_formatter_should_make_mouths_happy()
        {
            byte[] serializedMessageData;

            IMessageSerializer serializer = new BinaryMessageSerializer();

            using (MemoryStream output = new MemoryStream())
            {
                serializer.Serialize(output, _message, _sendContext);

                serializedMessageData = output.ToArray();
            }

            using (MemoryStream input = new MemoryStream(serializedMessageData))
            {
                SerializationTestMessage receivedMessage = serializer.Deserialize(input, _receiveContext) as SerializationTestMessage;

                Assert.AreEqual(_message, receivedMessage);
            }
        }
    }
}