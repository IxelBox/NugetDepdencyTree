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

			await main.StartAsync();

		}
	}
}
