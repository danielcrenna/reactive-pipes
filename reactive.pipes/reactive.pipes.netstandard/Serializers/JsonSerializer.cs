using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace reactive.pipes.Serializers
{
    public class JsonSerializer : ISerializer
    {
        private readonly Newtonsoft.Json.JsonSerializer _serializer;

        public JsonSerializer()
        {
            _serializer = new Newtonsoft.Json.JsonSerializer();
        }

        public void Dispose() { }

        public Stream SerializeToStream<T>(T message)
        {
            var s = new MemoryStream();
            using (StreamWriter sw = new StreamWriter(s, Encoding.UTF8, 4096, leaveOpen: true))
            {
                using (JsonTextWriter tw = new JsonTextWriter(sw))
                {
                    _serializer.Serialize(tw, message);
                    tw.Flush();
                }
            }
            return s;
        }

        public void SerializeToStream<T>(Stream stream, T message)
        {
            using (StreamWriter sw = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true))
            {
                using (JsonTextWriter tw = new JsonTextWriter(sw))
                {
                    _serializer.Serialize(tw, message);
                    tw.Flush();
                }
            }
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            using (var sr = new StreamReader(stream, Encoding.UTF8, false, 4096, leaveOpen: true))
            {
                using (var tr = new JsonTextReader(sr))
                {
                    return _serializer.Deserialize<T>(tr);
                }
            }
        }
    }
}