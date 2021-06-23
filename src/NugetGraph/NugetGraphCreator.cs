using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetGraph
{
	public class NugetGraphCreator
	{
		private readonly ILogger _logger;

		private readonly List<PackageSource> _packageSources;
		private readonly SourceCacheContext _sourceCache;

		public NugetGraphCreator(NugetInfos nugetData, ILogger logger)
		{
			_logger = logger;
			_packageSources = nugetData.Urls.Select(i =>
			{
				var source = new PackageSource(i);
				source.Credentials = PackageSourceCredential.FromUserInput(nameof(NugetGraphCreator),
					nugetData.UserName, nugetData.Password, false, "basic");
				return source;
			}).ToList();
			_sourceCache = new SourceCacheContext();
		}

		protected virtual async Task<IEnumerable<IPackageSearchMetadata>> SearchAsync(string searchTerm)
		{
			CancellationToken cancellationToken = CancellationToken.None;
			var result = new List<IPackageSearchMetadata>();
			foreach (var packageSource in _packageSources)
			{
				SourceRepository repository = Repository.Factory.GetCoreV3(packageSource);
				PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>(cancellationToken);
				SearchFilter searchFilter = new SearchFilter(includePrerelease: false);
				IEnumerable<IPackageSearchMetadata> searchResult = await resource.SearchAsync(
					searchTerm,
					searchFilter,
					skip: 0,
					take: 900,
					_logger,
					cancellationToken);
				result = result.Union(searchResult).ToList();
			}

			return result;
		}

		protected virtual async Task<SourcePackageDependencyInfo> GetDependencyAsync(PackageIdentity package)
		{

			CancellationToken cancellationToken = CancellationToken.None;
			foreach (var packageSource in _packageSources)
			{
				SourceRepository repository = Repository.Factory.GetCoreV3(packageSource);

				var dependencyResource = await repository.GetResourceAsync<DependencyInfoResource>(cancellationToken);
				var dependency = await dependencyResource.ResolvePackage(package, NuGetFramework.AnyFramework,
					_sourceCache, _logger, cancellationToken);

				if (dependency != null)
				{
					return dependency;
				}
			}

			return null;
		}

		protected IEnumerable<IPackageSearchMetadata> FilterSearchResult(string regexExpression, IEnumerable<IPackageSearchMetadata> filterItems)
		{
			var regex = new Regex(regexExpression, RegexOptions.Compiled | RegexOptions.IgnoreCase);

			foreach (var item in filterItems)
			{
				if (regex.IsMatch(item.Identity.Id))
					yield return item;
			}
		}

		protected async Task<NugetGraphNode> CreateGraphItemAsync(string packageId, VersionRange versionRange)
		{
			CancellationToken cancellationToken = CancellationToken.None;
			foreach (var packageSource in _packageSources)
			{
				SourceRepository repository = Repository.Factory.GetCoreV3(packageSource);
				var metadata = await repository.GetResourceAsync<PackageMetadataResource>(cancellationToken);
				var newMetadatas =
					(await metadata.GetMetadataAsync(new PackageIdentity(packageId, versionRange.MinVersion),
						_sourceCache, _logger, cancellationToken));
				if (newMetadatas != null)
				{
					return CreateGraphItem(newMetadatas);
				}
			}

			return null;
		}

		protected NugetGraphNode CreateGraphItem(IPackageSearchMetadata packageSearchMetadata)
		{
			if (packageSearchMetadata == null)
			{
				return null;
			}

			return new NugetGraphNode(packageSearchMetadata.Identity.Id,
				packageSearchMetadata.Identity.Version,
				packageSearchMetadata.Description,
				packageSearchMetadata.PackageDetailsUrl);
		}

		public async Task<IGraph<NugetGraphNode, NugetGraphEdge>> CreateGraphAsync(PackageFilter searchInstruction, int numberOfMaximalRecursiveDepthSearch)
		{
			var searchResult = await SearchAsync(searchInstruction.SearchTerm);
			searchResult = FilterSearchResult(searchInstruction.SearchResultRegex, searchResult);


			var graphBuilder = new GraphBuilder<NugetGraphNode, NugetGraphEdge>();
			graphBuilder.MergeNode += (o, args) =>
			{
				foreach (var newItemVersion in args.NewItem.Versions.Where(i => !args.OldItem.Versions.Contains(i)))
				{

					args.OldItem.Versions.Add(newItemVersion);
				}
			};

			var dependencyRegex = new Regex(searchInstruction.DependencyRegexFilter,
				RegexOptions.IgnoreCase | RegexOptions.Compiled);

			foreach (var package in searchResult)
			{
				await WriteDependenciesAsync(graphBuilder, dependencyRegex, CreateGraphItem(package), numberOfMaximalRecursiveDepthSearch, 1);
			}

			return graphBuilder.Build();

		}

		private async Task WriteDependenciesAsync(GraphBuilder<NugetGraphNode, NugetGraphEdge> graphBuilder, Regex dependencyRegex,
			NugetGraphNode packageWithDependency, int numberOfMaximalReursiveDeapthSearch, int currentDepth)
		{
			var dependency = await GetDependencyAsync(new PackageIdentity(packageWithDependency.PackageId, packageWithDependency.Versions.OrderBy(i => i.Version).First()));
			foreach (var dependencyPackage in (dependency?.Dependencies?.Where(i => dependencyRegex.IsMatch(i.Id)).ToList() ??
											  new List<PackageDependency>()).ToList())
			{
				if (currentDepth > numberOfMaximalReursiveDeapthSearch)
				{
					return;
				}

				var parent = packageWithDependency;
				if (!parent.Equals(null))
				{
					var children = await CreateGraphItemAsync(dependencyPackage.Id, dependencyPackage.VersionRange);
					if (!(children?.Equals(null) ?? true))
					{
						graphBuilder.AddChildConnection(parent, children,
							new NugetGraphEdge(dependencyPackage.VersionRange, parent, children));
						await WriteDependenciesAsync(graphBuilder, dependencyRegex, children, numberOfMaximalReursiveDeapthSearch, currentDepth + 1);
					}
					else
					{
						Console.WriteLine($"children package not found {dependencyPackage}");
					}
				}
				else
				{
					Console.WriteLine($"parent package not found {packageWithDependency.PackageId}");
				}
			}
		}
	}
}
