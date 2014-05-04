using System.IO;
using Newtonsoft.Json;

namespace Habitat.Core.TestingLibrary
{
    public static class WebStreamHelpers
    {
        public static long WriteObjectToStream<T>(Stream stream, T bag)
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            using (var sw = new StreamWriter(stream))
            using (var writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, bag);
            }
            stream.Position = 0;
            return stream.Length;
        }

        public static T GetObjectDataFromResponseStream<T>(Stream stream)
        {
            stream.Position = 0;
            using (var sr = new StreamReader(stream))
            {
                var result = sr.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(result);
            }
        }
    }
}
