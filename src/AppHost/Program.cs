var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql");

var database = sql.AddDatabase("MyProjectDb");

builder.AddProject<Projects.Web>("web")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
