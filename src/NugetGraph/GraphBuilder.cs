using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using NuGet.Protocol.Core.Types;

namespace NugetGraph
{
	public class GraphBuilder<TNode, TEdge>
	where TEdge : IEdge<TNode>
	{
		private readonly HashSet<TNode> _nodeList = new HashSet<TNode>();

		private readonly HashSet<Tuple<TNode, TNode, TEdge>> _childConnections =
			new HashSet<Tuple<TNode, TNode, TEdge>>();

		public void AddNode(TNode node)
		{
			if (!_nodeList.Contains(node))
				_nodeList.Add(node);
			else
			{
				var existingNode = _nodeList.FirstOrDefault(i => i.Equals(node));
				MergeNode?.Invoke(this, new GraphItemMergeEventArgs<TNode>(existingNode, node));
			}
		}

		public event EventHandler<GraphItemMergeEventArgs<TNode>> MergeNode;
		public event EventHandler<GraphItemMergeEventArgs<Tuple<TNode, TNode, TEdge>>> MergeEdge;

		public void AddChildConnection(TNode parentNode, TNode childrenNode, TEdge edgeData)
		{
			AddNode(parentNode);
			AddNode(childrenNode);

			var edge = new Tuple<TNode, TNode, TEdge>(parentNode, childrenNode, edgeData);
			if (!_childConnections.Contains(edge))
			{
				_childConnections.Add(edge);
			}
			else
			{
				var existingEdge = _childConnections.FirstOrDefault(i => edge.Equals(i));
				MergeEdge?.Invoke(this, new GraphItemMergeEventArgs<Tuple<TNode, TNode, TEdge>>(existingEdge, edge));
			}
		}

		public IGraph<TNode, TEdge> Build()
		{
			return new AdjacencyMatrixGraph<TNode, TEdge>(_nodeList, _childConnections);
		}
	}


	public class AdjacencyMatrixGraph<TNode, TEdge> : IGraph<TNode, TEdge>
	where TEdge : IEdge<TNode>
	{
		private MatrixHelper _matrix;



		public AdjacencyMatrixGraph(ISet<TNode> nodes, ISet<Tuple<TNode, TNode, TEdge>> childEdges)
		{
			_matrix = new MatrixHelper(nodes, childEdges);
		}



		private class MatrixHelper
		{
			private readonly Dictionary<TNode, int> _nodeToIndex;
			private IList<TEdge>[,] _childEdges;

			public MatrixHelper(ISet<TNode> nodes, ISet<Tuple<TNode, TNode, TEdge>> childEdges)
			{
				_nodeToIndex = nodes.Select((i, index) => new { i, index })
					.ToDictionary(i => i.i, i => i.index);
				_childEdges = new IList<TEdge>[nodes.Count, nodes.Count];
				//_parentEdges = new IList<TEdge>[nodes.Count, nodes.Count];

				foreach (var childEdge in childEdges)
				{
					var childEdgeList = _childEdges[_nodeToIndex[childEdge.Item1], _nodeToIndex[childEdge.Item2]];
					if (childEdgeList == null)
					{
						_childEdges[_nodeToIndex[childEdge.Item1], _nodeToIndex[childEdge.Item2]] = new List<TEdge>();
					}
					_childEdges[_nodeToIndex[childEdge.Item1], _nodeToIndex[childEdge.Item2]].Add(childEdge.Item3);

				}
			}

			public IEnumerable<IGraphNode<TNode, TEdge>> GetRoots()
			{
				foreach (var parentNode in _nodeToIndex)
				{
					var isRoot = true;
					foreach (var childNode in _nodeToIndex)
					{
						if (childNode.Value == parentNode.Value) continue;

						if ((_childEdges[childNode.Value, parentNode.Value]?.Count ?? 0) != 0)
						{
							isRoot = false;
							break;
						}
					}

					if (isRoot) yield return new GraphNodeImpl(this, parentNode.Key);
				}
			}

			public IEnumerable<IGraphNode<TNode, TEdge>> GetLeafs()
			{
				foreach (var childNode in _nodeToIndex)
				{
					var isLeaf = true;
					foreach (var parentNode in _nodeToIndex)
					{
						if (childNode.Value == parentNode.Value) continue;

						if ((_childEdges[childNode.Value, parentNode.Value]?.Count ?? 0) != 0)
						{
							isLeaf = false;
							break;
						}
					}

					if (isLeaf) yield return new GraphNodeImpl(this, childNode.Key);
				}
			}

			public IEnumerable<IGraphNode<TNode, TEdge>> GetAllNodes()
			{
				return _nodeToIndex.Select(node => new GraphNodeImpl(this, node.Key));
			}

			private class GraphNodeImpl : IGraphNode<TNode, TEdge>
			{
				private readonly MatrixHelper _matrixHelper;

				public GraphNodeImpl(MatrixHelper matrixHelper, TNode parent)
				{
					_matrixHelper = matrixHelper;
					Node = parent;
					Index = matrixHelper._nodeToIndex[parent];
				}

				public int Index { get; }
				public TNode Node { get; }

				public IEnumerable<INodeConnection<TNode, TEdge>> ChildrenConnections()
				{
					var childCons = new List<NodeConnectionImpl>();
					foreach (var edgeNode in _matrixHelper._nodeToIndex)
					{
						var tmp = _matrixHelper._childEdges[_matrixHelper._nodeToIndex[Node], edgeNode.Value];
						childCons.AddRange(tmp?.Select(
							i =>
							{
								return new NodeConnectionImpl(new GraphNodeImpl(_matrixHelper, i.Children), i);
							}) ?? new List<NodeConnectionImpl>());
					}

					return childCons;

				}

				public IEnumerable<INodeConnection<TNode, TEdge>> ParentConnections()
				{
					var parentCons = new List<NodeConnectionImpl>();
					foreach (var edgeNode in _matrixHelper._nodeToIndex)
					{
						var tmp2 = _matrixHelper._childEdges[edgeNode.Value, _matrixHelper._nodeToIndex[Node]];
						parentCons.AddRange(tmp2?.Select(i =>
							{
								return new NodeConnectionImpl(new GraphNodeImpl(_matrixHelper, i.Parent), i);
							}
						) ?? new List<NodeConnectionImpl>());
					}

					return parentCons;
				}

				private class NodeConnectionImpl : INodeConnection<TNode, TEdge>
				{
					public NodeConnectionImpl(IGraphNode<TNode, TEdge> end, TEdge data)
					{
						End = end;
						Data = data;
					}

					public IGraphNode<TNode, TEdge> End { get; set; }
					public TEdge Data { get; set; }
				}
			}
		}

		public IEnumerable<IGraphNode<TNode, TEdge>> LeafNodes()
		{
			return _matrix.GetLeafs();
		}

		public IEnumerable<IGraphNode<TNode, TEdge>> RootNodes()
		{
			return _matrix.GetRoots();
		}

		public IEnumerable<IGraphNode<TNode, TEdge>> AllNodes()
		{
			return _matrix.GetAllNodes();
		}
	}

	public interface IGraphNode<TNode, TEdge>
		where TEdge : IEdge<TNode>
	{
		int Index { get; }
		TNode Node { get; }
		IEnumerable<INodeConnection<TNode, TEdge>> ChildrenConnections();
		IEnumerable<INodeConnection<TNode, TEdge>> ParentConnections();

	}

	public interface INodeConnection<TNode, TEdge>
		where TEdge : IEdge<TNode>
	{
		IGraphNode<TNode, TEdge> End { get; set; }
		TEdge Data { get; set; }
	}


	public interface IGraph<TNode, TEdge>
	where TEdge : IEdge<TNode>
	{
		IEnumerable<IGraphNode<TNode, TEdge>> LeafNodes();
		IEnumerable<IGraphNode<TNode, TEdge>> RootNodes();
		IEnumerable<IGraphNode<TNode, TEdge>> AllNodes();
	}

	public interface IEdge<TNode> : ICloneable
	{
		TNode Parent { get; }
		TNode Children { get; }

	}

	public class GraphItemMergeEventArgs<T> : EventArgs
	{
		public GraphItemMergeEventArgs(T oldItem, T newItem)
		{
			OldItem = oldItem;
			NewItem = newItem;
		}

		public T OldItem { get; set; }
		public T NewItem { get; set; }
	}
}
