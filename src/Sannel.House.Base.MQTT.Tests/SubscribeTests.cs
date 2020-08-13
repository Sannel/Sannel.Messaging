/* Copyright 2020-2020 Sannel Software, L.L.C.
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at
      http://www.apache.org/licenses/LICENSE-2.0
   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/

using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using Sannel.House.Base.MQTT.Tests.Access;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sannel.House.Base.MQTT.Tests
{
	public class SubscribeTests
	{
		[Fact]
		public async Task SubscribeTest()
		{
			string expectedTopic = null;

			var subscribeCount = 0;

			var client = new Moq.Mock<IMqttClient>();
			client.Setup(i => i.SubscribeAsync(
								It.IsAny<MqttClientSubscribeOptions>(),
								It.IsAny<System.Threading.CancellationToken>()))
				.Callback<MqttClientSubscribeOptions, CancellationToken>((options, token) =>
				{
					Assert.Single(options.TopicFilters);
					var topic = options.TopicFilters[0];
					Assert.Equal(expectedTopic, expectedTopic);
					subscribeCount++;
				});

			var logger = new Mock<ILogger<MqttService>>();

			var service = new MqttServiceAccess(client.Object, "topic1", new MqttClientOptions(), logger.Object);

			expectedTopic = "test/test1";

			var called = 0;

			var sendPayload = new
			{
				DeviceId = 1,
				Name = "testName1"
			};

			service.Subscribe("test/test1", (topic, payload) =>
			{
				called++;
				Assert.Equal(expectedTopic, topic);
				Assert.Equal(JsonSerializer.Serialize(sendPayload), payload);
			});

			Assert.Equal(1, subscribeCount);

			await service.HandleApplicationMessageReceivedAsync(
				new MQTTnet.MqttApplicationMessageReceivedEventArgs("1", new MQTTnet.MqttApplicationMessage()
				{
					Topic = expectedTopic,
					Payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sendPayload))
				})
			);

			Assert.Equal(1, called);
		}

		[Fact]
		public async Task SubscribeTwoTopicsTest()
		{
			var expectedTopic1 = "topic1";
			var expectedTopic2 = "topic2";

			var topic1Count = 0;
			var topic2Count = 0;

			var client = new Moq.Mock<IMqttClient>();
			client.Setup(i => i.SubscribeAsync(
								It.IsAny<MqttClientSubscribeOptions>(),
								It.IsAny<System.Threading.CancellationToken>()))
				.Callback<MqttClientSubscribeOptions, CancellationToken>((options, token) =>
				{
					Assert.Single(options.TopicFilters);
					var topic = options.TopicFilters[0].Topic;
					if (topic == expectedTopic1)
					{
						topic1Count++;
					}
					else if (topic == expectedTopic2)
					{
						topic2Count++;
					}
					else
					{
						Assert.True(false, "Unexpected Topic sent");
					}
				});

			var logger = new Mock<ILogger<MqttService>>();

			var service = new MqttServiceAccess(client.Object, "topic1", new MqttClientOptions(), logger.Object);


			var topic1Called = 0;
			var topic2Called = 0;

			object sendPayload = new
			{
				DeviceId = 1,
				Name = "testName1"
			};

			service.Subscribe(expectedTopic1, (topic, payload) =>
			{
				topic1Called++;
				Assert.Equal(expectedTopic1, topic);
				Assert.Equal(JsonSerializer.Serialize(sendPayload), payload);
			});

			service.Subscribe(expectedTopic2, (topic, payload) =>
			{
				topic2Called++;
				Assert.Equal(expectedTopic2, topic);
				Assert.Equal(JsonSerializer.Serialize(sendPayload), payload);
			});

			Assert.Equal(1, topic1Count);
			Assert.Equal(1, topic2Count);

			await service.HandleApplicationMessageReceivedAsync(
				new MQTTnet.MqttApplicationMessageReceivedEventArgs("1", new MQTTnet.MqttApplicationMessage()
				{
					Topic = expectedTopic1,
					Payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sendPayload))
				})
			);

			Assert.Equal(1, topic1Called);
			Assert.Equal(0, topic2Called);

			sendPayload = new
			{
				PayloadId=3,
				CertId=4
			};
			await service.HandleApplicationMessageReceivedAsync(
				new MQTTnet.MqttApplicationMessageReceivedEventArgs("1", new MQTTnet.MqttApplicationMessage()
				{
					Topic = expectedTopic2,
					Payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(sendPayload))
				})
			);

			Assert.Equal(1, topic1Called);
			Assert.Equal(1, topic2Called);
		}
	}
}