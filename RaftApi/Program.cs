using Raft;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var logger = app.Services.GetService<ILogger<Program>>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// My code
HttpRpcNode n = new("");
Console.WriteLine($"HttpRpcNode Made {n.NodeId}");

app.MapPost("/request/SendAppendEntriesRPC", (int term, int prevLogIndex, List<Entry> entries, int commitIndex) =>
{
	Console.WriteLine("Sending stuff!");
	n.SendAppendEntriesRPC();
});



app.MapPost("/receive/RecieveAppendEntries", (AppendEntriesRPC rpc) =>
{
	//n.RecieveAppendEntriesRPC(rpc);
});



app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
