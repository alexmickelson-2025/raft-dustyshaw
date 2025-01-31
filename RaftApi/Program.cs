using Raft;
using Raft.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	//app.UseSwagger();
	//app.UseSwaggerUI();
}

app.UseHttpsRedirection();


// My code
Console.WriteLine("----- STARTING SIMULATION");
HttpRpcNode n1 = new("http://node1:8080");

app.MapPost("/SendAppendEntriesRPC", async (int term, int prevLogIndex, List<Entry> entries, int commitIndex) =>
{
	await n1.SendAppendEntriesRPC();
});

app.MapPost("/RecieveAppendEntries", async (AppendEntriesRPC rpc) =>
{
	await n1.RecieveAppendEntriesRPC(rpc);
});

app.MapPost("/RecieveAVoteRequestFromCandidate", async (VoteRequestFromCandidateRpc rpc) =>
{
	await n1.RecieveAVoteRequestFromCandidate(rpc);
});


app.MapPost("/RecieveVoteResults", (bool result, int termNumber) =>
{
	n1.RecieveVoteResults(result, termNumber);
});




app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
	public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
