var builder = WebApplication.CreateBuilder(args);

// Swagger (Dev only)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // /swagger
}

app.UseHttpsRedirection();

// Optional: convenient home + health
app.MapGet("/", () => Results.Redirect("/swagger", false));
app.MapGet("/healthz", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }))
   .WithOpenApi();


app.Run();
