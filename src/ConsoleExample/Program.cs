using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Microsoft.Extensions.Options;
using NugetGraph;

namespace ConsoleApp4
{
	class Program
	{



		static async Task Main(string[] args)
		{
			var main = new StartupService();

			main.ConfigureService();

			//main.ConfigureService(serviceCollection);
			//var container = serviceCollection

			//var tmp = container.GetService<IConfiguration>();
			//var filter = tmp.GetSection(PackageFilter.Section).Get<PackageFilter>();
			await main.StartAsync();


			//ILogger logger = NullLogger.Instance;
			//CancellationToken cancellationToken = CancellationToken.None;
			//CancellationToken cancellationToken2 = CancellationToken.None;
			//var packageSource = new PackageSource("https://pkgs.dev.azure.com/ZEISSgroup-SMT/_packaging/DevShared/nuget/v3/index.json");
			//packageSource.Credentials = PackageSourceCredential.FromUserInput("nugetDepdencyTest", "adrian.boehmichen.ext@zeiss.com", "kuw6mthtpyqb7sgxmzhqf3wbj2u5wh5e5uyoyvb5hdzb6gp5s6nq", false, "basic");
			//SourceRepository repository = Repository.Factory.GetCoreV3(packageSource);
			//PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>();
			//SearchFilter searchFilter = new SearchFilter(includePrerelease: false);
			//IEnumerable<IPackageSearchMetadata> results = await resource.SearchAsync(
			//	"SMT.Minos.Business.Plugins.SpecCheck.Ui.Wpf",
			//	searchFilter,
			//	skip: 0,
			//	take: 500,
			//	logger,
			//	cancellationToken);

			//foreach (IPackageSearchMetadata result in results)
			//{
			//	Console.WriteLine($"Found package {result.Identity.Id} {result.Identity.Version} {result.Description}");
			//	var depdencyResource = await repository.GetResourceAsync<DependencyInfoResource>();
			//	var depdencies = await depdencyResource.ResolvePackages(result.Identity.Id, NuGetFramework.AnyFramework,
			//		new NullSourceCacheContext(), logger, cancellationToken2);
			//	foreach (var dep in depdencies)
			//	{
			//		Console.WriteLine($"\tDepdencies: {string.Join(", ", dep.Dependencies.Select(i => i.Id))}");
			//	}
			//	//		Console.WriteLine(depdencies.Id+"!!!!!!!!!!!!!!!!!!!!");
			//	//		Console.WriteLine($"\tDepdencies: {string.Join(", ", depdencies.Dependencies.Select(i => i.Id))}");
			//}

			////	PackageArchiveReader
		}
	}
}
