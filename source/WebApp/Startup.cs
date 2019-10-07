using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace SharpLab.WebApp {
    public class Startup : Server.Startup {
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            base.Configure(app, env);

            if (!env.IsDevelopment())
                app.UseHttpsRedirection();

            app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = { "index.html" } });

            app.UseStaticFiles(new StaticFileOptions {
                OnPrepareResponse = context => {
                    if (context.File.Name == "app.min.js" || context.File.Name == "app.min.css")
                        context.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=31536000,immutable";
                }
            });
        }
    }
}
