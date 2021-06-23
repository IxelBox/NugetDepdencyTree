namespace NugetGraph
{
	public class PackageFilter
	{
		public const string Section = "Filter";
		public string SearchTerm { get; set; }
		public string SearchResultRegex { get; set; }
		public string DependencyRegexFilter { get; set; }
	}
}