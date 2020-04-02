using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Terradue.WebService.Ogc.Common
{
    public class XmlContent : HttpContent
    {
        private readonly MemoryStream _Stream = new MemoryStream();

        public XmlContent(XmlDocument document)
        {
            document.Save(_Stream);
            _Stream.Position = 0;
            Headers.ContentType = new MediaTypeHeaderValue("application/xml");
        }

        public XmlContent(object obj, string type = "application/xml")
        {
            //  Serialize result object
            XmlSerializer serializer = obj.GetType().GetSerializer();
            serializer.Serialize(_Stream, obj);
            _Stream.Position = 0;
            Headers.ContentType = new MediaTypeHeaderValue(type);
        }

        protected override Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
        {

            _Stream.CopyTo(stream);

            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _Stream.Length;
            return true;
        }
    }
}
