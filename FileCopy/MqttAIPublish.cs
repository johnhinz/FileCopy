using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileCopy
{
    public class MqttAIPublish : IPublishDetections<MqttClientPublishResult>
    {
        private readonly ILogger<MqttAIPublish> _logger;
        private readonly string _server;
        private readonly string _clientName;
        private readonly string _regexPattern;
        private readonly int _position;
        private readonly string _queueName;

        public MqttAIPublish(ILogger<MqttAIPublish> logger, string server, string clientName, string regexPattern, int position, string queueName)
        {
            if (string.IsNullOrEmpty(server)) throw new ArgumentNullException("server");
            if (string.IsNullOrEmpty(regexPattern)) throw new ArgumentNullException("regexPattern");
            if (position < 0) throw new ArgumentOutOfRangeException("position");

            _logger = logger;
            _server = server;
            _clientName = clientName;
            _regexPattern = regexPattern;
            _position = position;
            _queueName = queueName;
        }
        public Task<MqttClientPublishResult> PublishAsync<TPrediction>(TPrediction message, string source, CancellationToken token)
        {
            _logger.LogInformation($"MqttAIPublish called for {source}");
            var factory = new MqttFactory();
            using (var mqttClient = factory.CreateMqttClient())
            {
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(_clientName)
                    .WithTcpServer(_server)
                    .WithCleanSession()
                    .Build();

                mqttClient.ConnectAsync(options, CancellationToken.None).Wait();

                string[] topicName = Regex.Split(source, _regexPattern);
                if (!(topicName.Length > _position))
                {
                    throw new ArgumentOutOfRangeException("Sub-topic name cannot be determined.");
                }
                return mqttClient.PublishAsync(
                                new MqttApplicationMessageBuilder()
                                        .WithTopic($"{_queueName}/{topicName[_position]}")
                                        .WithPayload(JsonSerializer.Serialize(message))
                                        .Build(),
                                 CancellationToken.None);
            }
        }
    }
}
