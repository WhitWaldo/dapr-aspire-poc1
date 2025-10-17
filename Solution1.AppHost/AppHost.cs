using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache").WithDbGate();
var daprState = builder.AddDaprStateStore("statestore");

const int rabbitPort = 5672;
var username = builder.AddParameter("uesrname", "guest", true);
var password = builder.AddParameter("password", "guest", true);
var rabbitmq = builder.AddRabbitMQ("rabbitmq", username, password, rabbitPort).WithManagementPlugin();

// var connectionString = $"amqp://{await username.Resource.GetValueAsync(CancellationToken.None)}:{await password.Resource.GetValueAsync(CancellationToken.None)}@localhost:{rabbitPort}";
// var daprPubSub = builder.AddDaprPubSub("pubsub-rabbit")
// 	.WithMetadata("connectionString", connectionString)
// 	.WaitFor(rabbitmq);
//Console.WriteLine($"[Aspire] Dapr PubSub connection string: {connectionString}");

builder.AddProject<Projects.Web>("web")
	.WithHttpHealthCheck("/health")
	.WithReference(cache)
	.WithReference(rabbitmq)
	.WithDaprSidecar(b =>
	{
		b.WithOptions(new DaprSidecarOptions
			{
				ResourcesPaths = ["../.dapr/components"],
				AppHealthCheckPath = "/health",
				EnableAppHealthCheck = true,
				AppId = "web",
				AppPort = 5124,
				DaprGrpcPort = 50001,
				DaprHttpPort = 3500,
				MetricsPort = 9090
			})
			//.WithReference(daprPubSub)
			.WithReference(daprState);
	})
	.WaitFor(rabbitmq);

builder.Build().Run();
