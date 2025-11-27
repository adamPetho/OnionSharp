# OnionSharp
Implement Tor easily into your C# projects.

# DISCLAIMER
The Tor implementation was completely taken out of [WalletWasabi](https://github.com/WalletWasabi/WalletWasabi). 
The code was stripped of its WalletWasabi related components to serve as a general Tor implementation.
I do not own this code and I'm not the intellectual owner of the code. Check WalletWasabi commits for that.
The project was created for learning purposes only.

# Key Features
- Use TorSettings class to customize your Tor instance.
- Use OnionHttpClientFactory to create TorHttpClient.
- Use TorHttpClient to send requests.
- Use it on client and/or server side.

# How to use
```cs
// You can use the EnvironmentHelpers to grab the data dir or insert your own path/of/data/directory. 
var dataDir = EnvironmentHelpers.GetDataDir("Sample-Project");

// Create your Tor Settings
var torSetting = new TorSettings(dataDir,
			distributionFolderPath: EnvironmentHelpers.GetFullBaseDirectory(),
			terminateOnExit: true,
			TorMode.Enabled,
			socksPort: 37155,
			controlPort: 37156);

services.AddSingleton(torSetting);

// Register your HttpClientFactory and use it across your app
services.AddSingleton<IHttpClientFactory>(s => new OnionHttpClientFactory(torSetting.SocksEndpoint.ToUri("socks5")));


// OR in a simple console app, you can:
// Create the OnionHttpFactory
var onionClientFactory = new OnionHttpClientFactory(torSetting.SocksEndpoint.ToUri("socks5"));

// Create Http Clients 
var myHttpClient = onionClientFactory.CreateClient("name-of-your-client");

// Send requests to onion address (for example)
await blockstreamClient.GetAsync("http://explorerzydxu5ecjrkwceayqybizmpjjznk5izmitf2modhcusuqlid.onion/api/mempool/recent");
```
