using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Refit;
using Snackable.PresentationApi.BackgroundJobs;
using Snackable.PresentationApi.Db;
using Snackable.PresentationApi.ProcessingApi;

namespace Snackable.PresentationApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<SnackableDbContext>(opt => opt.UseInMemoryDatabase(SnackableDbContext.DbName));
            services.AddControllers().AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Snackable.PresentationApi", Version = "v1"});
            });

            var processingApiUri = Configuration.GetValue<string>("Snackable:ProcessingUri");
            services
                .AddRefitClient<IProcessingApiClient>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(processingApiUri));

            services.AddHostedService<AllFilesSynchronizer>();
            services.AddHostedService<ProcessingFilesSynchronizer>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Snackable.PresentationApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}