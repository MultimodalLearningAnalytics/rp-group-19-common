// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.Psi.Interop.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Format serializer/deserializer for JSON.
    /// </summary>
    public class JsonWrapper : IFormatSerializer, IPersistentFormatSerializer, IFormatDeserializer, IPersistentFormatDeserializer
    {
        private JsonWrapper()
        {
            MessageSemaphore = new Semaphore(1, 1);
            ArraySemaphore = new Semaphore(1, 1);
            lastMessTime = DateTime.MinValue;
            lastArrayTime = DateTime.MinValue;
        }

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        public static JsonWrapper LavInstance { get; } = new JsonWrapper();
        public static JsonWrapper EpInstance { get; } = new JsonWrapper();
        
        private Semaphore MessageSemaphore { get; set; }
        private Semaphore ArraySemaphore { get; set; }
        private DateTime lastMessTime { get; set; }
        private DateTime lastArrayTime { get; set; }

        /// <inheritdoc />
        public (byte[], int, int) SerializeMessage(dynamic message, DateTime originatingTime)
        {
            // { originatingTime = ..., message = ... }
            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { originatingTime, message }));
            return (bytes, 0, bytes.Length);
        }

        /// <inheritdoc />
        public (dynamic, DateTime) DeserializeMessage(byte[] payload, int index, int count)
        {
            string str = Encoding.UTF8.GetString(payload, index, count);
            if (str.Contains("[")) {
                if (ArraySemaphore.WaitOne(75)) {
                    (dynamic mess, DateTime ts) = Deserializer(str, index, count);

                    if (ts > lastArrayTime)
                    {
                        lastArrayTime = ts;

                        ArraySemaphore.Release();
                        return (mess, ts);
                    }
                    else {

                        Console.WriteLine("[JSONWRAPPER] >> Ignoring array message because ts was smaller then last");
                        lastArrayTime = lastArrayTime.AddMilliseconds(50);

                        ArraySemaphore.Release();
                        return ((dynamic) "", lastArrayTime);
                    }
                } else {
                    Console.WriteLine("[JSONWRAPPER] >> Ignoring array message because semaphore timed out");
                    lastArrayTime = lastArrayTime.AddMilliseconds(50);
                    return ((dynamic) "", lastArrayTime);
                }
            } else {
                if (MessageSemaphore.WaitOne(75))
                {
                    (dynamic mess, DateTime ts) = Deserializer(str, index, count);

                    if (ts > lastMessTime)
                    {
                        lastMessTime = ts;

                        MessageSemaphore.Release();
                        return (mess, ts);

                    }
                    else {
                        Console.WriteLine("[JSONWRAPPER] >> Ignoring frequency message because ts was smaller then last");
                        lastMessTime = lastMessTime.AddMilliseconds(50);

                        MessageSemaphore.Release();
                        return ((dynamic) 0.0, lastMessTime);
                    }
                    
                }
                else
                {
                    Console.WriteLine("[JSONWRAPPER] >> Ignoring frequency message because semaphore timed out");
                    lastMessTime = lastMessTime.AddMilliseconds(50);
                    return ((dynamic) 0.0, lastMessTime);
                }
            }
        }

        public (dynamic, DateTime) Deserializer(string payload, int index, int count) {
            try
            {
                var tok = JsonConvert.DeserializeObject<JToken>(payload);
                var originatingTime = tok["originatingTime"].Value<DateTime>();
                var msg = this.JObjectToDynamic(tok["message"]);
                return (msg, originatingTime);
            }
            catch (Exception err)
            {
                Console.WriteLine(count);
                Console.WriteLine("DESERIALIZE ERROR: " + err);
                (dynamic mess, DateTime ts) = DeserializeOnCrash(payload, index, count);
                Console.WriteLine($"Recovered: ({mess} at {ts}");
                return (mess, ts);
            }
        }


        private (dynamic, DateTime) DeserializeOnCrash(string payload, int index, int count) {
            bool arr = false;
            if (payload.Contains("["))
            {
                int i = payload.LastIndexOf(",");
                payload = payload.Remove(i);
                payload = $"{payload}]\" }}";
                arr = true;
            }
            else {
                int i = payload.IndexOf(", \"message\"");
                payload = $"{payload.Remove(i)} }}";
            }

            var tok = JsonConvert.DeserializeObject<JToken>(payload);
            var originatingTime = tok["originatingTime"].Value<DateTime>();

            if (arr) {
                var msg = (string) this.JObjectToDynamic(tok["message"]);

                short[] jarr = JArray.Parse(msg).ToObject<short[]>();
                Array.Resize(ref jarr, MLAConfig.AUDIO_BLOCK_SIZE);

                string output = $"[{string.Join(",", jarr)}]";
                return ((dynamic) output, originatingTime);
            }
            return ((dynamic) 0.0, originatingTime);
            
        }

        /// <inheritdoc />
        public dynamic PersistHeader(dynamic message, Stream stream)
        {
            // persisted form as array [<message>,<message>,...]
            stream.WriteByte((byte)'[');
            return null;
        }

        /// <inheritdoc />
        public void PersistRecord(dynamic message, DateTime originatingTime, bool first, Stream stream, dynamic state)
        {
            (byte[], int, int) msg = this.SerializeMessage(message, originatingTime);
            if (!first)
            {
                // commas *before* all but first record in persisted form as array [<message>,<message>,...]
                stream.WriteByte((byte)',');
            }

            stream.Write(msg.Item1, msg.Item2, msg.Item3);
        }

        /// <inheritdoc />
        public void PersistFooter(Stream stream, dynamic state)
        {
            // close array in persisted form [<message>,<message>,...]
            stream.WriteByte((byte)']');
        }

        /// <inheritdoc />
        public IEnumerable<(dynamic, DateTime)> DeserializeRecords(Stream stream)
        {
            var reader = new JsonTextReader(new StreamReader(stream));
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var obj = this.JObjectToDynamic(JObject.Load(reader));
                    yield return (obj.message, obj.originatingTime);
                }
            }
        }

        private dynamic JObjectToDynamic(JToken tok)
        {
            switch (tok.Type)
            {
                case JTokenType.Object:
                    var dict = new ExpandoObject() as IDictionary<string, dynamic>;
                    tok.Children<JProperty>().ToList().ForEach(p => dict.Add(p.Name, this.JObjectToDynamic(p.Value)));
                    return dict;
                case JTokenType.Array:
                    return tok.Children<JValue>().Select(this.JObjectToDynamic).ToArray();
                case JTokenType.String:
                    return tok.Value<string>();
                case JTokenType.Float:
                    return tok.Value<double>();
                case JTokenType.Integer:
                    return tok.Value<int>();
                case JTokenType.Boolean:
                    return tok.Value<bool>();
                case JTokenType.Null:
                    return null;
                case JTokenType.Bytes:
                    return tok.Value<byte[]>();
                case JTokenType.Date:
                    return tok.Value<DateTime>();
                case JTokenType.Guid:
                    return tok.Value<Guid>();
                case JTokenType.TimeSpan:
                    return tok.Value<TimeSpan>();
                case JTokenType.Uri:
                    return tok.Value<Uri>();
                default: throw new ArgumentException($"Unexpected JTokenType: {tok}");
            }
        }
    }
}
