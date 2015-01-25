using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace RestEasy
{
    public class RestClient
    {

        public bool IsInDeignModel
        {
            get
            {
                return Windows.ApplicationModel.DesignMode.DesignModeEnabled;
            }
        }

        private static string ComputeMd5(string str)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

        public async Task<T> GetAsync<T>(string requestUri, Dictionary<string, string> headers = null)
        {
            string key = ComputeMd5(requestUri);

            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                if (await StorageHelper.FileExistsAsync(key))
                {
                    var result = await StorageHelper.ReadFileAsync(key);
                    return JsonConvert.DeserializeObject<T>(result);
                }
            }

            var response = await FetchLiveData(requestUri, headers);

            if (Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                SaveResponseToDisk(key, response);
            }

            return JsonConvert.DeserializeObject<T>(response.Trim());
        }

        private static async Task<string> FetchLiveData(string requestUri, Dictionary<string, string> headers)
        {
            var httpClient = new HttpClient().AddHeaders(headers);
            HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(requestUri);
            string response = await httpResponseMessage.Content.ReadAsStringAsync();
            return response;
        }

        private static void SaveResponseToDisk(string key, string response)
        {
            StorageHelper.WriteFileFireAndForget(key, response.Trim(), StorageHelper.StorageStrategies.Local);
        }

        public async Task<T> PostAsync<T>(string requestUri, Dictionary<string, string> headers = null, Dictionary<string, string> parameters = null)
        {
            return await PostAsync<T>(requestUri, headers, parameters != null ? FormatPostParameters(parameters) : null);
        }

        public async Task<T> PostAsync<T>(string requestUri, Dictionary<string, string> headers = null, string content = null)
        {
            var client = new HttpClient().AddHeaders(headers);

            HttpContent httpContent = new StringContent(content ?? "");
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            HttpResponseMessage httpResponseMessage = await client.PostAsync(requestUri, httpContent);
            string response = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(response);
        }

        protected string FormatPostParameters(Dictionary<string, string> parameters)
        {
            var result = "";
            int count = 0;
            foreach (var parameter in parameters)
            {
                if (count > 0)
                {
                    result = result + "&";
                }
                result = result + parameter.Key + "=" + parameter.Value;
                count++;
            }
            return result;
        }
    }

    public static class HttpClientExtensions
    {
        public static HttpClient AddHeaders(this HttpClient httpClient, Dictionary<string, string> headers)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            return httpClient;
        }
    }
}
