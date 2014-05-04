using System;
using System.IO;
using SystemInterface.IO;

namespace Habitat.Core
{
    /// <summary>
    /// Extensions to SystemWrapper's StreamWrap class.
    /// </summary>
    /// <remarks>
    /// TODO: create pull request for this in SystemWrapper project if possible
    /// </remarks>
    public static class StreamWrapExtension
    {
        /// <summary>
        /// An extension to copy a Stream to the StreamInstance of IStreamWrap.
        /// </summary>
        /// <param name="streamWrap">output stream to copy to.</param>
        /// <param name="stream">input stream to copy from.</param>
        public static void CopyFromStream(this IStream streamWrap, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException();
            }
            if (streamWrap == null)
            {
                throw new ArgumentNullException();
            }

            stream.CopyTo(streamWrap.StreamInstance);
        }
    }
}
