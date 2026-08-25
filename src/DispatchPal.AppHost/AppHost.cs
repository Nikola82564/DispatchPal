var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder
    .AddPostgres("postgres")
    .WithDataVolume();

var dispatchPalDatabase = postgres
    .AddDatabase(
        name: "dispatchpal-database",
        databaseName: "dispatchpal");

var rabbitMqUserName = builder.AddParameter(
    name: "rabbitmq-username",
    value: "dispatchpal");

var rabbitMqPassword = builder.AddParameter(
    name: "rabbitmq-password",
    secret: true);


var rabbitMq = builder
    .AddRabbitMQ(
        name: "rabbitmq",
        userName: rabbitMqUserName,
        password: rabbitMqPassword)
    .WithManagementPlugin()
    .WithDataVolume("dispatchpal-aspire-rabbitmq-data");

var api = builder
    .AddProject<Projects.DispatchPal_Api>("api")
    .WithReference(
        dispatchPalDatabase,
        connectionName: "Postgres")
    .WithReference(rabbitMq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["RabbitMq__HostName"] =
            rabbitMq.Resource.PrimaryEndpoint.Property(
                EndpointProperty.Host);

        context.EnvironmentVariables["RabbitMq__Port"] =
            rabbitMq.Resource.PrimaryEndpoint.Property(
                EndpointProperty.Port);

        context.EnvironmentVariables["RabbitMq__UserName"] =
            rabbitMq.Resource.UserNameParameter;

        context.EnvironmentVariables["RabbitMq__Password"] =
            rabbitMq.Resource.PasswordParameter;
    })
    .WaitFor(dispatchPalDatabase)
    .WaitFor(rabbitMq);

var processing = builder
    .AddProject<Projects.DispatchPal_Processing>("processing")
    .WithReference(rabbitMq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["RabbitMq__HostName"] =
            rabbitMq.Resource.PrimaryEndpoint.Property(
                EndpointProperty.Host);

        context.EnvironmentVariables["RabbitMq__Port"] =
            rabbitMq.Resource.PrimaryEndpoint.Property(
                EndpointProperty.Port);

        context.EnvironmentVariables["RabbitMq__UserName"] =
            rabbitMq.Resource.UserNameParameter;

        context.EnvironmentVariables["RabbitMq__Password"] =
            rabbitMq.Resource.PasswordParameter;
    })
    .WaitFor(rabbitMq);

var notification = builder
    .AddProject<Projects.DispatchPal_Notification>("notification")
    .WithReference(rabbitMq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["RabbitMq__HostName"] =
            rabbitMq.Resource.PrimaryEndpoint.Property(
                EndpointProperty.Host);

        context.EnvironmentVariables["RabbitMq__Port"] =
            rabbitMq.Resource.PrimaryEndpoint.Property(
                EndpointProperty.Port);

        context.EnvironmentVariables["RabbitMq__UserName"] =
            rabbitMq.Resource.UserNameParameter;

        context.EnvironmentVariables["RabbitMq__Password"] =
            rabbitMq.Resource.PasswordParameter;
    })
    .WaitFor(rabbitMq);

var web = builder
    .AddJavaScriptApp(
        name: "web",
        appDirectory: "../DispatchPal.Web",
        runScriptName: "start")
    .WithReference(api)
    .WithHttpEndpoint(
        port: 4200,
        targetPort: 4200,
        isProxied: false)
    .WaitFor(api);

builder.Build().Run();