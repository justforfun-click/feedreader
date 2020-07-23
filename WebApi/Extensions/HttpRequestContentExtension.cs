using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FeedReader.WebApi.Extensions
{
    [Binding]
    class HttpRequestContentAttribute : Attribute
    {
        public Type Type { get; set; }

        public uint MaxLength { get; set; } = 100 * 1024;
    }

    class HttpRequestContentValueProvider : IValueProvider
    {
        private readonly HttpRequest _req;

        private readonly HttpRequestContentAttribute _attr;

        public Type Type => throw new NotImplementedException();

        public HttpRequestContentValueProvider(HttpRequest req, HttpRequestContentAttribute attr)
        {
            _req = req;
            _attr = attr;
        }

        public async Task<object> GetValueAsync()
        {
            if (_req.ContentLength > _attr.MaxLength)
            {
                throw new OverflowException($"Request body is too large, limitation: {_attr.MaxLength / 1024} KB");
            }

            using (var sr = new StreamReader(_req.Body, Encoding.UTF8))
            {
                var body = await sr.ReadToEndAsync();
                return JsonConvert.DeserializeObject(body, _attr.Type);
            }
        }

        public string ToInvokeString()
        {
            return null;
        }
    }

    class HttpRequestContentBinding : IBinding
    {
        private readonly HttpRequestContentAttribute _attr;

        public bool FromAttribute => false;

        public HttpRequestContentBinding(HttpRequestContentAttribute attr)
        {
            _attr = attr;
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
            throw new NotImplementedException();
        }

        public Task<IValueProvider> BindAsync(BindingContext context)
        {
            var req = context.BindingData.First(pair => pair.Value is HttpRequest).Value as HttpRequest;
            return Task.FromResult<IValueProvider>(new HttpRequestContentValueProvider(req, _attr));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }
    }

    class HttpRequestContentBindingProvider : IBindingProvider
    {
        public Task<IBinding> TryCreateAsync(BindingProviderContext context)
        {
            var attr = context.Parameter.GetCustomAttribute<HttpRequestContentAttribute>();
            return Task.FromResult<IBinding>(new HttpRequestContentBinding(attr));
        }
    }

    class HttpRequestContentExtension : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            context.AddBindingRule<HttpRequestContentAttribute>().Bind(new HttpRequestContentBindingProvider());
        }
    }
}
