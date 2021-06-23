using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NuGet.Common;
using NugetGraph;

namespace WebDepedencySearcher.Controllers
{
	public record Node
	{
		public Node(string packageId, List<string> versions, string description, string url, List<Edge> childrens)
		{
			Childrens = childrens;
			PackageId = packageId;
			Versions = versions;
			Description = description;
			Url = url;
		}

		public string PackageId { get; set; }
		public List<string> Versions { get; set; }
		public string Description { get; set; }
		public string Url { get; set; }

		public List<Edge> Childrens { get; set; }
	}

	public record Edge
	{
		public Edge(Node endpoint, string version) => (Endpoint, Version) = (endpoint, version);

		public Node Endpoint { get; set; }
		public string Version { get; set; }
	}

	[ApiController]
	[Route("api/v1/[controller]")]
	public class NugetDepdencyController : ControllerBase
	{
		private readonly IConfiguration _configuration;

		public NugetDepdencyController(IConfiguration configuration)
		{
			_configuration = configuration;

		}

		[HttpGet()]
		public async Task<IEnumerable<Node>> Get([FromQuery]PackageFilter filter, [FromQuery] int depth)
		{
			var graphBuilder = new NugetGraphCreator(
				_configuration.GetSection(NugetInfos.Section).Get<NugetInfos>(),
				new NullLogger());
			var graph =
			 await graphBuilder.CreateGraphAsync(filter, depth);



			return graph.RootNodes().Select(CreateNode).ToList();
		}

		private Node CreateNode(IGraphNode<NugetGraphNode, NugetGraphEdge> i)
		{
			{
				return new Node(i.Node.PackageId, i.Node.Versions.Select(v => v.ToNormalizedString()).ToList(),
					i.Node.Description ?? "", i.Node.PackageUrl?.AbsoluteUri ?? "", GetChildrens(i));
			}
		}

		private List<Edge> GetChildrens(IGraphNode<NugetGraphNode, NugetGraphEdge> node)
		{
			var result = new List<Edge>();
			foreach (var child in node.ChildrenConnections())
			{
				var edge = new Edge(CreateNode(child.End), child.Data.VersionRange.ToNormalizedString());
				result.Add(edge);
			}

			return result;
		}
	}
}
