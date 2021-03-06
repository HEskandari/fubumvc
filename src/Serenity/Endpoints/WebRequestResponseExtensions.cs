using System.Diagnostics;
using System.Net;
using System.Text;
using FubuCore;

namespace Serenity.Endpoints
{
    public static class WebRequestResponseExtensions
    {
        public static HttpResponse ToHttpCall(this WebRequest request)
        {
            var result = request.BeginGetResponse(r => { }, null);
            try
            {
                var response = request.EndGetResponse(result).As<HttpWebResponse>();
                return new HttpResponse(response);
            }
            catch (WebException e)
            {
                var errorResponse = new HttpResponse(e.Response.As<HttpWebResponse>());
                Debug.WriteLine(errorResponse.ToString());

                return errorResponse;
            }
        }

        public static void WriteText(this WebRequest request, string content)
        {
            request.ContentLength = content.Length;
            var stream = request.GetRequestStream();

            var array = Encoding.Default.GetBytes(content);
            stream.Write(array, 0, array.Length);
            stream.Close();
        }
    }
}