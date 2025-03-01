var builder = DistributedApplication.CreateBuilder(args);


var api = builder.AddProject<Projects.CFW_EntityApi_TestApi>("test-api")
    .WithEnvironment("DbContextSetting__SqliteConnectionString", "Data Source=test-api.db")
    .WithExternalHttpEndpoints();

var auto = builder.AddProject<Projects.CFW_Automation>("cfw-automation")
    .WithEnvironment("DatabaseSetting__SqliteConnectionString", "Data Source=auto-api.db");

builder.AddNpmApp("test-ui", "../CFW.EntityApi.TestUI")
    .WithReference(auto)
    .WithEnvironment("BROWSER", "none")
    .WithHttpEndpoint(env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();


builder.Build().Run();
