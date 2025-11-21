using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;


namespace OnionSharp.Tor
{
    public delegate DateTime LifetimeResolver(string identity);
    public record HttpClientHandlerConfiguration
    {
        public static readonly HttpClientHandlerConfiguration Default = new();
        public int MaxAttempts { get; init; } = 3;
        public TimeSpan TimeBeforeRetringAfterTooManyRequests { get; init; } = TimeSpan.FromSeconds(2);
        public TimeSpan TimeBeforeRetringAfterNetworkError { get; init; } = TimeSpan.FromSeconds(3);
        public TimeSpan TimeBeforeRetringAfterServerError { get; init; } = TimeSpan.FromSeconds(2);
    }

    public class HttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClientHandlerConfiguration _httpHandlerConfig;
        private readonly ConcurrentDictionary<string, DateTime> _expirationDatetimes = new();
        private readonly ConcurrentDictionary<string, HttpClientHandler> _httpClientHandlers = new();
        private readonly ConcurrentBag<LifetimeResolver> _lifetimeResolvers = new();

        public HttpClientFactory(HttpClientHandlerConfiguration? httpHandlerConfig = null)
        {
            _httpHandlerConfig = httpHandlerConfig ?? HttpClientHandlerConfiguration.Default;
            AddLifetimeResolver(identity => identity.StartsWith("long-live")
                ? DateTime.MaxValue
                : DateTime.UtcNow.AddHours(6));
        }

        public HttpClient CreateClient(string name)
        {
            CheckForExpirations();
            var httpClientHandler = _httpClientHandlers.GetOrAdd(name, CreateHttpClientHandler);
            return new HttpClient(httpClientHandler, false);
        }

        public void AddLifetimeResolver(LifetimeResolver resolver)
        {
            _lifetimeResolvers.Add(resolver);
        }

        private void CheckForExpirations()
        {
            var expiredHandlers = _expirationDatetimes.Where(x => x.Value < DateTime.UtcNow).Select(x => x.Key).ToArray();
            foreach (var handlerName in expiredHandlers)
            {
                if (_httpClientHandlers.TryRemove(handlerName, out var handler))
                {
                    handler.Dispose();
                }
            }
        }

        protected virtual HttpClientHandler CreateHttpClientHandler(string name)
        {
            SetExpirationDate(name);
            var handler = new RetryHttpClientHandler(name,
                handlerName =>
                {
                    _httpClientHandlers.TryRemove(handlerName, out _);
                    _expirationDatetimes.TryRemove(handlerName, out _);
                }, _httpHandlerConfig);

            return handler;
        }

        private void SetExpirationDate(string name)
        {
            var expirationTime = _lifetimeResolvers.Min(resolve => resolve(name));
            _expirationDatetimes.AddOrUpdate(name, expirationTime, (_, _) => expirationTime);
        }
    }

    public class OnionHttpClientFactory(Uri proxyUri, HttpClientHandlerConfiguration? configurator = null)
        : HttpClientFactory(configurator)
    {
        protected override HttpClientHandler CreateHttpClientHandler(string name)
        {
            var credentials = new NetworkCredential(name, name);
            var webProxy = new WebProxy(proxyUri, BypassOnLocal: false, [], Credentials: credentials);
            var handler = base.CreateHttpClientHandler(name);
            handler.Proxy = webProxy;
            return handler;
        }
    }
    public class NotifyHttpClientHandler(string name, Action<string> disposedCallback) : HttpClientHandler
    {
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            disposedCallback(name);
        }
    }

    public class RetryHttpClientHandler(string name, Action<string> disposedCallback, HttpClientHandlerConfiguration config) : NotifyHttpClientHandler(name, disposedCallback)
    {
        internal HttpClientHandlerConfiguration Config = config;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var attempt = 0;
            while (attempt < Config.MaxAttempts)
            {
                try
                {
                    var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.RequestTimeout:
                        case HttpStatusCode.BadGateway:
                        case HttpStatusCode.ServiceUnavailable:
                            await Task.Delay(Config.TimeBeforeRetringAfterServerError, cancellationToken).ConfigureAwait(false);
                            continue;
                        case HttpStatusCode.TooManyRequests:
                            // Be nice with third-party server overwhelmed by request from Tor exit nodes
                            await Task.Delay(Config.TimeBeforeRetringAfterTooManyRequests, cancellationToken).ConfigureAwait(false);
                            continue;

                        default:
                            // Not something we can retry, return the response as is
                            return response;
                    }
                }
                catch (Exception ex)
                {
                    if (!ShouldRetry(ex))
                    {
                        throw;
                    }
                    await Task.Delay(Config.TimeBeforeRetringAfterNetworkError, cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    attempt++;
                }
            }

            throw new HttpRequestException($"Failed to make http request '{request.RequestUri}' after 3 attempts.");
        }

        private static bool ShouldRetry(Exception ex) =>
            ex switch
            {
                SocketException => true,
                HttpRequestException
                {
                    HttpRequestError:
                    HttpRequestError.ConnectionError or
                    HttpRequestError.ProxyTunnelError or
                    HttpRequestError.SecureConnectionError or
                    HttpRequestError.NameResolutionError or
                    HttpRequestError.InvalidResponse or
                    HttpRequestError.ResponseEnded
                } => true,
                { InnerException: var inner } when ShouldRetry(inner) => true,
                _ => false
            };
    }
}
