using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Web.Validators.HotelRooms;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Services.AddProblemDetails();

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Detail = "One or more validation errors occurred",
                    Instance = context.HttpContext.Request.Path
                };

                return new BadRequestObjectResult(problemDetails);
            };
        });

    builder.Services.AddValidatorsFromAssemblyContaining<SearchAvailableRequestValidator>();

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<IHotelService, HotelService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IHotelRoomsService, HotelRoomsService>();
    builder.Services.AddScoped<IBookingsService, BookingService>();

    var app = builder.Build();

    // Configure exception handler
    app.UseExceptionHandler(exceptionHandlerApp =>
    {
        exceptionHandlerApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
            var exception = exceptionHandlerFeature?.Error;

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred",
                Detail = app.Environment.IsDevelopment()
                    ? exception?.Message
                    : "An unexpected error occurred. Please try again later.",
                Instance = context.Request.Path
            };

            // TODO: Log

            await context.Response.WriteAsJsonAsync(problemDetails);
        });
    });
    
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    
    app.MapControllers();
    
    if (app.Environment.IsProduction())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }