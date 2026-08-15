using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Sygnia.Application;
using Sygnia.Infrastructure;
using Sygnia.Presentation;
using Sygnia.Presentation.Services;
using Grpc.AspNetCore.Web;

const string ServiceName = "Sygnia.Presentation";

var builder = WebApplication.CreateBuilder(args);

// Serilog -> Seq. Structured logging is the app's only logging path: this replaces the
// default provider entirely rather than running alongside it.
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", ServiceName)
    .WriteTo.Console()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

// OpenTelemetry tracing -> Jaeger via OTLP (Jaeger's all-in-one image ingests OTLP natively;
// the dedicated Jaeger exporter package is deprecated in favour of this).
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(otlp => otlp.Endpoint =
            new Uri(builder.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317")));

// Add services to the container.
builder.Services.AddGrpc(options => options.Interceptors.Add<ErrorInterceptor>());
builder.Services.AddCors(o => o.AddPolicy("frontend", p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")));
builder.Services.Register(); // Sygnia.Application: MediatR, validators, logging pipeline
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("SygniaCash")
        ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SygniaCash'."));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();
app.UseCors("frontend");
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.MapGrpcService<GreeterService>().EnableGrpcWeb();
app.MapGrpcService<MovementGrpcService>().EnableGrpcWeb();
app.MapGrpcService<AccountGrpcService>().EnableGrpcWeb();
app.MapGrpcService<UserGrpcService>().EnableGrpcWeb();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
