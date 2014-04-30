using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using OpenRasta.Codecs;
using OpenRasta.Web;
using ProTeck.Core.Log;
using StructureMap;

namespace Habitat.Server.Data.Codecs
{
    /// <summary>
    /// Codec for serializing list of Config objects (list of strings)
    /// </summary>
    public class ConfigListCodec : IMediaTypeWriter
    {
        /// <summary>
        /// StructureMap container to use with the codec
        /// </summary>
        private readonly IContainer _container;

        /// <summary>
        /// Logger instance to use
        /// </summary>
        private readonly ILog _log;

        /// <summary>
        /// Injected configuration object (part of IMediaTypeReader, IMediaTypeWriter)
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Constructs an instance of ConfigListCodec
        /// </summary>
        /// <param name="container">StructureMap container to use with the codec</param>
        public ConfigListCodec(IContainer container)
        {
            _container = container;
            _log = _container.GetInstance<ILog>();
        }

        /// <summary>
        /// Writes the list of strings to the response stream.
        /// </summary>
        /// <param name="entity">Config object to write (returned from handler method)</param>
        /// <param name="response">Response entity</param>
        /// <param name="codecParameters">Not used</param>
        public void WriteTo(object entity, IHttpEntity response, string[] codecParameters)
        {
            try
            {
                _log.Debug("Entering ConfigListCodec.WriteTo()");

                var data = JsonConvert.SerializeObject(entity);

                var encodedData = Encoding.UTF8.GetBytes(data);
                using (var ms = new MemoryStream(encodedData))
                {
                    ms.CopyTo(response.Stream);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                throw;
            }
        }
    }
}