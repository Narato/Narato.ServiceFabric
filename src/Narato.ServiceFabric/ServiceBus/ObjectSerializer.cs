using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace Narato.ServiceFabric.ServiceBus
{
    public static class ObjectSerializer
    {
        public static byte[] Serialize(object objectToSerialize)
        {
            var serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, objectToSerialize);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static T Deserialize<T>(byte[] data) where T : class
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return JsonSerializer.Create().Deserialize(reader, typeof(T)) as T;
        }
    }
}
