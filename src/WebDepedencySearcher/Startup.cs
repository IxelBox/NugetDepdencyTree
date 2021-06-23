using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace WebDepedencySearcher
{
	public class Startup
	{
		readonly string _allowAllOrigins = "_allowAll";

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddCors(options =>
			{
				options.AddPolicy(name: _allowAllOrigins,
					builder =>
					{
						builder.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
					});
			});


			services.AddControllers().AddJsonOptions(opt =>
			{
				opt.JsonSerializerOptions.MaxDepth = 128;
				opt.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
			});
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebDepedencySearcher", Version = "v1" });
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebDepedencySearcher v1"));
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors(_allowAllOrigins);

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
