using System;
using System.IO;
using System.Text;
using Habitat.Core;
using OpenRasta.Codecs;
using OpenRasta.TypeSystem;
using OpenRasta.Web;
using StructureMap;
using Newtonsoft.Json;

namespace Habitat.Server.Data.Codecs
{
    /// <summary>
    /// Codec for serializing Config objects to response stream
    /// and deserializing from request stream
    /// </summary>
    public class ConfigCodec : IMediaTypeReader, IMediaTypeWriter
    {
        /// <summary>
        /// StructureMap container to use with the codec
        /// </summary>
        private readonly IContainer _container;

        /// <summary>
        /// Logger instance to use
        /// </summary>
        //private readonly ILog _log;

        /// <summary>
        /// Injected configuration object (part of IMediaTypeReader, IMediaTypeWriter)
        /// </summary>
        public object Configuration { get; set; }

        /// <summary>
        /// Constructs an instance of ConfigCodec
        /// </summary>
        /// <param name="container">StructureMap container to use with the codec</param>
        public ConfigCodec(IContainer container)
        {
            _container = container;
            //_log = _container.GetInstance<ILog>();
        }

        /// <summary>
        /// Writes the config entity to the response stream.
        /// </summary>
        /// <param name="entity">Config object to write (returned from handler method)</param>
        /// <param name="response">Response entity</param>
        /// <param name="codecParameters">Not used</param>
        public void WriteTo(object entity, IHttpEntity response, string[] codecParameters)
        {
            try
            {
                //_log.Debug("Entering ConfigCodec.WriteTo()");

                var data = JsonConvert.SerializeObject(entity);

                var encodedData = Encoding.UTF8.GetBytes(data);
                using (var ms = new MemoryStream(encodedData))
                {
                    ms.CopyTo(response.Stream);
                }
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Reads the request stream and returns an instance of IJsonEntity of ConfigRoot
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="destinationType">Not used</param>
        /// <param name="destinationName">Not used</param>
        /// <returns></returns>
        public object ReadFrom(IHttpEntity request, IType destinationType, string destinationName)
        {
            try
            {
                //_log.Debug("Entering ConfigCodec.ReadFrom()");

                string requestBody;
                using (var sr = new StreamReader(request.Stream))
                {
                    requestBody = sr.ReadToEnd();
                }

                var configEntity = JsonConvert.DeserializeObject<ConfigRoot>(requestBody);

                return configEntity;
            }
            catch (Exception ex)
            {
                //_log.Error(ex);
                throw;
            }
        }
    }
}