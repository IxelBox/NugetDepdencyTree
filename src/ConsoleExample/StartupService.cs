using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NugetGraph;

namespace ConsoleApp4
{
	public class StartupService
	{
		ServiceProvider serviceProvider;

		public void ConfigureService()
		{
			var serviceCollection = new ServiceCollection();
			IConfigurationBuilder configBuilder = new ConfigurationBuilder();
			foreach (var configFile in this.ConfigurationFiles)
			{
				configBuilder.AddJsonFile(configFile);
			}

			serviceCollection.AddSingleton<IConfiguration>(configBuilder.Build());
			serviceCollection.AddLogging();
			serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions() { ValidateOnBuild = true });
		}

		public string[] ConfigurationFiles => new[]{
			"appsettings.json"
		};

		public async Task StartAsync()
		{
			var configuration = serviceProvider.GetService<IConfiguration>();

			var graphBuilder = new NugetGraphCreator(
				configuration.GetSection(NugetInfos.Section).Get<NugetInfos>(),
				new NullLogger());
			var graph = await graphBuilder.CreateGraphAsync(configuration.GetSection(PackageFilter.Section).Get<PackageFilter>(), 10);

			GenerateHTML(graph);

			//var sb = new StringBuilder();

			//sb.AppendLine("digraph G {");
			//sb.AppendLine("rankdir=BT");
			//foreach (var node in graph.AllNodes())
			//{
			//	sb.AppendLine($"{node.Index} [label=\"{node.Node.PackageId}\"]");
			//}

			//sb.AppendLine();
			//var parentHashSet = new HashSet<Tuple<int, int>>();
			//foreach (var leaf in graph.LeafNodes())
			//{
			//	int depth = 0;
			//	AddParentConnection(leaf, sb, ref depth, parentHashSet);
			//}

			//sb.AppendLine("}");
			//await File.WriteAllTextAsync("ParentGraph.dot", sb.ToString());


			//sb = new StringBuilder();

			//sb.AppendLine("digraph G {");
			//sb.AppendLine("rankdir=BT");
			//foreach (var node in graph.AllNodes())
			//{
			//	sb.AppendLine($"{node.Index} [label=\"{node.Node.PackageId}\"]");
			//}

			//sb.AppendLine();
			//parentHashSet.Clear();
			//foreach (var leaf in graph.RootNodes())
			//{
			//	int depth = 0;
			//	AddChildConnection(leaf, sb, ref depth, parentHashSet);
			//}

			//sb.AppendLine("}");
			//await File.WriteAllTextAsync("ChildGraph.dot", sb.ToString());
		}

		private static void AddParentConnection(IGraphNode<NugetGraphNode, NugetGraphEdge> node, StringBuilder sb, ref int depth, HashSet<Tuple<int, int>> parentHashSet)
		{
			var oldDepth = depth;
			depth += 1;
			foreach (var parent in node.ParentConnections())
			{
				var tuple = Tuple.Create(parent.End.Index, node.Index);
				if (!parentHashSet.Contains(tuple))
				{
					sb.AppendLine($"{parent.End.Index} -> {node.Index}");
					parentHashSet.Add(tuple);
				}

				AddParentConnection(parent.End, sb, ref depth, parentHashSet);
				depth = oldDepth;
			}
		}
		private static void AddChildConnection(IGraphNode<NugetGraphNode, NugetGraphEdge> node, StringBuilder sb, ref int depth, HashSet<Tuple<int, int>> parentHashSet)
		{
			var oldDepth = depth;
			depth += 1;
			foreach (var parent in node.ChildrenConnections())
			{
				var tuple = Tuple.Create(node.Index, parent.End.Index);
				if (!parentHashSet.Contains(tuple))
				{
					sb.AppendLine($"{node.Index} -> {parent.End.Index}");
					parentHashSet.Add(tuple);
				}

				AddChildConnection(parent.End, sb, ref depth, parentHashSet);
				depth = oldDepth;
			}
		}

		private void GenerateHTML(IGraph<NugetGraphNode, NugetGraphEdge> graph)
		{
			var htmlFile = "graph.html";
			if (File.Exists(htmlFile))
				File.Delete(htmlFile);

			File.WriteAllText(htmlFile, HTMLTemplateStart);

			var parentHashSet = new HashSet<Tuple<int, int>>();
			var sb = new StringBuilder();
			sb.AppendLine(GenerateGraphStart());

			graph.LeafNodes().SelectMany(i => i.ParentConnections());

			foreach (var leaf in graph.LeafNodes())
			{
				PaintNode(leaf);

				//int depth = 0;
				//AddParentNodes(leaf, sb, ref depth, parentHashSet);
			}
			GenerateNewGraph();

			PaintDepth(graph.LeafNodes());

			sb.AppendLine(GenerateGraphEnd());

			File.AppendAllText(htmlFile, HTMLTemplateEnd);

			void PaintDepth(IEnumerable<IGraphNode<NugetGraphNode, NugetGraphEdge>> nodes)
			{

				bool hasChange = false;
				var nodeWithParent = nodes.Select(n => (Node: n, Parents: n.ParentConnections())).ToList();
				foreach (var node in nodeWithParent)
				{
					foreach (var parent in node.Parents)
					{
						var tuple = Tuple.Create(parent.End.Index, node.Node.Index);
						if (!parentHashSet.Contains(tuple))
						{
							sb.Append(AddNode(node.Node));
							sb.Append(AddNode(parent.End));

							sb.AppendLine($"{parent.End.Index} -> {node.Node.Index}");
							parentHashSet.Add(tuple);
							hasChange = true;
						}
					}
				}

				if (hasChange)
				{
					GenerateNewGraph();
					PaintDepth(nodeWithParent.SelectMany(n => n.Parents.Select(p=> p.End)));
				}

			}

			void PaintNode(IGraphNode<NugetGraphNode, NugetGraphEdge> node) => sb.Append(AddNode(node));

			void AddParentNodes(IGraphNode<NugetGraphNode, NugetGraphEdge> node, StringBuilder sb, ref int depth, HashSet<Tuple<int, int>> parentHashSet)
			{
				var oldDepth = depth;
				depth += 1;
				bool hasChange = false;
				foreach (var parent in node.ParentConnections())
				{
					var tuple = Tuple.Create(parent.End.Index, node.Index);
					if (!parentHashSet.Contains(tuple))
					{
						sb.Append(AddNode(node));
						sb.Append(AddNode(parent.End));

						sb.AppendLine($"{parent.End.Index} -> {node.Index}");
						parentHashSet.Add(tuple);
						hasChange = true;
					}

					AddParentNodes(parent.End, sb, ref depth, parentHashSet);
					depth = oldDepth;
				}

				if (hasChange)
					GenerateNewGraph();
			}

			void GenerateNewGraph()
			{
				var oldGraph = sb.ToString();
				sb.AppendLine(GenerateGraphEnd());

				File.AppendAllText(htmlFile, sb.ToString());

				sb.Clear();
				sb.AppendLine(oldGraph);
			}

			string AddNode(IGraphNode<NugetGraphNode, NugetGraphEdge> node) => !NodeIsKnown(node.Index) ? $"{GenerateNodeText(node)}{Environment.NewLine}" : string.Empty;

			bool NodeIsKnown(int nodeId) => parentHashSet.SelectMany(i => new[] { i.Item1, i.Item2 }).Contains(nodeId);

			string GenerateGraphStart() => "[\nString.raw`digraph {";

			string GenerateGraphEnd() => "}`\n],";

			string GenerateNodeText(IGraphNode<NugetGraphNode, NugetGraphEdge> node)
				=> $"{node.Index} [label=\"{node.Node.PackageId}\"]";
		}

		/// <summary>
		/// HTML Template start
		/// </summary>
		private const string HTMLTemplateStart =
			@"<!DOCTYPE html>
<meta charset=""utf-8"">
<body>
<script src=""https://unpkg.com/d3@5.0.0/dist/d3.min.js""></script>
<script src=""https://unpkg.com/@hpcc-js/wasm@1.6.0/dist/index.min.js"" type=""application/javascript/""></script>
<script src=""https://unpkg.com/d3-graphviz@3.2.0/build/d3-graphviz.js""></script>
<div id=""graph"" style=""text-align: center;""></div>
<script>

var dotIndex = 0;
var graphviz = d3.select(""#graph"").graphviz()
    .transition(function () {
        return d3.transition(""main"")
            .ease(d3.easeLinear)
            .delay(250)
            .duration(250);
    })
    //.logEvents(true)
    .on(""initEnd"", render);

function updateValue(e) {
  innerDiv.textContent = e.target.value;
  dotIndex = e.target.value;
  render();
}

function render() {
    var dotLines = dots[dotIndex];
    var dot = dotLines[0];
    graphviz
        .renderDot(dot);
        
}

var dots = [
";

		/// <summary>
		/// HTML Template end
		/// </summary>
		private const string HTMLTemplateEnd = @"
];


let div = document.createElement('div');
let innerDiv = document.createElement('div');
		innerDiv.id = ""val""
    innerDiv.textContent = 0;
let slider = document.createElement('input');
    slider.id = ""depth"";
    slider.type = 'range';
    slider.min = 0;
    slider.max = dots.length -1;
    slider.value = 0;
    slider.step = 1;
    
slider.addEventListener('input', updateValue);

  div.appendChild(slider);
  div.appendChild(innerDiv);
  document.body.prepend(div); 
</script>
";
	}
}
