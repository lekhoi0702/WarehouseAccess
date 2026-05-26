using Microsoft.EntityFrameworkCore;
using WarehouseAccessAPI.Data;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://10.0.111.118:5111");
const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddDbContext<WarehouseAccessDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection"));
});

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins("https://10.0.111.118:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthorization();
app.MapControllers();
app.Run();
