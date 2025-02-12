var builder = DistributedApplication.CreateBuilder(args);


var api = builder.AddProject<Projects.CFW_EntityApi_TestApi>("test-api")
    .WithEnvironment("DbContextSetting__SqliteConnectionString", "Data Source=test-api.db")
    .WithExternalHttpEndpoints();


builder.AddNpmApp("test-ui", "../CFW.EntityApi.TestUI")
    .WithReference(api)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

builder.Build().Run();
