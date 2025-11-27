using System.Net;
using NBitcoin;


namespace OnionSharp.Helpers
{
    public static class UriHelpers
    {
        public static string ToUriString(this EndPoint endpoint, string schema)
           => $"{schema}://{endpoint.ToEndpointString()}";

        public static Uri ToUri(this EndPoint endpoint, string schema)
            => new(endpoint.ToUriString(schema));
    }
}
