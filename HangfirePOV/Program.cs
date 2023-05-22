using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHangfire(config => config
    .UseSqlServerStorage("<my db connection>"));

builder.Services.AddHangfireServer();

builder.Services.AddSingleton<IMyService, MyService>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"));

app.UseHangfireDashboard();

// Map routes.
app.MapControllers();

app.Run();

public interface IMyService
{
    public void LongRunningMethod();
}

public class MyService : IMyService
{
    public void LongRunningMethod()
    {
        Console.WriteLine("Long running task started...");
        // Your long-running task here...
        System.Threading.Thread.Sleep(5000);
        Console.WriteLine("Long running task finished...");
    }
}

[ApiController]
[Route("[controller]")]
public class JobsController : ControllerBase
{
    [HttpPost("startjob")]
    public IActionResult StartJob([FromServices] IMyService myService)
    {
        var jobId = BackgroundJob.Enqueue(() => myService.LongRunningMethod());

        return Ok(new { JobId = jobId });
    }

    [HttpPost("startrecurringjob")]
    public IActionResult StartRecurringJob([FromServices] IMyService myService)
    {
        var jobId = Guid.NewGuid().ToString();
        RecurringJob.AddOrUpdate(jobId, () => myService.LongRunningMethod(), Cron.Minutely);

        return Ok(new { Message = "Recurring job started!", JobId = jobId });
    }

    [HttpPost("stoprecurringjob/{id}")]
    public IActionResult StopRecurringJob(string id)
    {
        RecurringJob.RemoveIfExists(id);
        return Ok($"Recurring job {id} stopped");
    }


}