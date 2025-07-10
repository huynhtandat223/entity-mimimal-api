using CFW.HangfireExtentions.Filters;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace CFW.HangfireExtentions;

public static class HangfireExtentions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services
        , ConfigurationManager configuration, IHostEnvironment env)
    {
        services.AddSingleton(s =>
        {
            var conn = s.GetService<Func<DbConnection>>();
            var storage = new SqlServerStorage(conn);
            return storage;
        });

        //services.AddSingleton<Func<DbConnection>>(s =>
        //{
        //    return () =>
        //    {
        //        var connectionString = configuration.GetConnectionString(AppSettings.Database.CONNECTION_STRING_KEY) ?? string.Empty;
        //        SqlConnection conn = new SqlConnection(connectionString);
        //        if (!env.IsDevelopment() && !connectionString.ToLower().Contains("password="))
        //        {
        //            var defaultAzureCredential = s.GetRequiredService<DefaultAzureCredential>();
        //            var token = defaultAzureCredential.GetTokenString();

        //            conn.AccessToken = token;
        //        }

        //        return conn;
        //    };
        //});

        services.AddHangfire((s, c) => c
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings(s =>
            {
                s.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            })
            .UseSqlServerStorage(s.GetRequiredService<Func<DbConnection>>()
            , new SqlServerStorageOptions
            {
                QueuePollInterval = TimeSpan.FromSeconds(30)
            })
            .UseActivator(ActivatorUtilities.CreateInstance<BackgroundJobActivator>(s))
            .UseFilter(new AutomaticRetryAttribute { Attempts = 0 })
            .UseFilter(ActivatorUtilities.CreateInstance<BackgroundTaskStateHandler>(s))
            .UseFilter(ActivatorUtilities.CreateInstance<InterceptorFilter>(s)));
        //.UseTagsWithSql(new Hangfire.Tags.TagsOptions
        //{
        //    Clean = Hangfire.Tags.Clean.None,
        //    TagsListStyle = Hangfire.Tags.TagsListStyle.Dropdown
        //}));

        //services.AddHangfireServer((s, o) =>
        //{
        //    o.ServerName = Constants.SyncAzRepositoryQueue;
        //    o.Queues = new[] { Constants.SyncAzRepositoryQueue };
        //    o.WorkerCount = 1;
        //    o.SchedulePollingInterval = TimeSpan.FromMinutes(1);
        //    o.HeartbeatInterval = TimeSpan.FromMinutes(2);
        //});

        //services.AddHangfireServer((s, o) =>
        //{
        //    o.ServerName = Constants.SyncAzResourceQueue;
        //    o.Queues = new[] { Constants.SyncAzResourceQueue };
        //    o.WorkerCount = 3;
        //    o.SchedulePollingInterval = TimeSpan.FromMinutes(1);
        //    o.HeartbeatInterval = TimeSpan.FromMinutes(2);
        //});

        services.AddHangfireServer((s, o) =>
        {
            o.ServerName = "Default server";
            o.WorkerCount = 3;
            o.SchedulePollingInterval = TimeSpan.FromSeconds(15);
            o.HeartbeatInterval = TimeSpan.FromMinutes(2);
        });

        return services;
    }

    public static WebApplication UseBackgroundJobs(this WebApplication app)
    {
        var storage = app.Services.GetService<SqlServerStorage>();
        if (storage is null)
            return app;

        storage.GetConnection().RemoveTimedOutServers(new TimeSpan(0, 0, 10));

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            DisplayStorageConnectionString = false,
            Authorization = new[] { new HangfireDashboardAuthorizationFilter() },
            StatsPollingInterval = 20_000
        });

        return app;
    }
}
