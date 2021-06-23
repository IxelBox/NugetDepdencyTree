using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace NugetGraph
{
	public class NugetGraphNode : IEquatable<NugetGraphNode>, ICloneable
	{
		public NugetGraphNode(string packageId, NuGetVersion initialVersion, string description, Uri packageUrl)
		{
			PackageId = packageId;
			Versions = new List<NuGetVersion>();
			Versions.Add(initialVersion);
			Description = description;
			PackageUrl = packageUrl;
		}

		private NugetGraphNode(string packageId, IEnumerable<NuGetVersion> versions, string description, Uri packageUrl)
		{
			PackageId = packageId;
			Versions = versions.ToList();
			Description = description;
			PackageUrl = packageUrl;
		}

		public bool Equals(NugetGraphNode other)
		{
			return PackageId.Equals(other?.PackageId);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((NugetGraphNode)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (PackageId != null ? PackageId.GetHashCode() : 0);
				return hashCode;
			}
		}

		public string PackageId { get; }
		public ICollection<NuGetVersion> Versions { get; }
		public string Description { get; set; }
		public Uri PackageUrl { get; set; }


		public static bool operator ==(NugetGraphNode item1, NugetGraphNode item2)
		{
			if(ReferenceEquals(item1, null) && ReferenceEquals(item2, null))
			{
				return true;
			}
			return item1?.Equals(item2) ?? false;
		}

		public static bool operator !=(NugetGraphNode item1, NugetGraphNode item2)
		{
			if(ReferenceEquals(item1, null) && ReferenceEquals(item2, null))
			{
				return false;
			}
			return !item1?.Equals(item2) ?? true;
		}

		public override string ToString()
		{
			return $"{nameof(PackageId)}: {PackageId}, {nameof(Description)}: {Description}";
		}

		public object Clone()
		{
			return new NugetGraphNode(PackageId, Versions.Select(i => NuGetVersion.Parse(i.ToNormalizedString())), Description, PackageUrl);
		}
	}

	public class NugetGraphEdge : IEdge<NugetGraphNode>, IEquatable<NugetGraphEdge>
	{
		public NugetGraphEdge(VersionRange versionRange, NugetGraphNode parent, NugetGraphNode children)
		{
			VersionRange = versionRange;
			Parent = parent;
			Children = children;
		}

		public VersionRange VersionRange { get; }

		public NugetGraphNode Parent { get; }
		public NugetGraphNode Children { get; }

		public bool Equals(NugetGraphEdge other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(VersionRange, other.VersionRange) && Equals(Parent, other.Parent) && Equals(Children, other.Children);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((NugetGraphEdge)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (VersionRange != null ? VersionRange.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Parent != null ? Parent.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Children != null ? Children.GetHashCode() : 0);
				return hashCode;
			}
		}

		public object Clone()
		{
			return new NugetGraphEdge(NuGet.Versioning.VersionRange.Parse(VersionRange.ToNormalizedString()), Parent, Children);
		}
	}
}
