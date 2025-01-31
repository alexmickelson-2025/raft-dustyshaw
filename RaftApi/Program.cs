using Raft;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// My code
Node n = new([], null);

app.MapPost("/request/SendAppendEntriesRPC", (int term, int prevLogIndex, List<Entry> entries, int commitIndex) =>
{
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
