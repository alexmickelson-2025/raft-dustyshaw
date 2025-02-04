using System.Text.Json;
using Raft;
using Raft.DTOs;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:8080");

//var nodeId = Environment.GetEnvironmentVariable("NODE_ID") ?? throw new Exception("NODE_ID environment variable not set");
var otherNodesRaw = Environment.GetEnvironmentVariable("OTHER_NODES") ?? throw new Exception("OTHER_NODES environment variable not set");
var nodeIntervalScalarRaw = Environment.GetEnvironmentVariable("NODE_INTERVAL_SCALAR") ?? throw new Exception("NODE_INTERVAL_SCALAR environment variable not set");


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

INode[] otherNodes = otherNodesRaw
.Split(";")
.Select(x =>
{
	var parts = x.Split(",");
	return new HttpRpcNode(parts[0], Guid.Parse(parts[1]));
}).ToArray();

Console.WriteLine($"other nodes {JsonSerializer.Serialize(otherNodes)}" );

INode node = new Node(otherNodes, null);

app.MapPost("/RecieveAppendEntriesRPC", async (AppendEntriesRPC rpc) =>
{
	await node.RecieveAppendEntriesRPC(rpc);
});

app.MapPost("/RecieveAVoteRequestFromCandidate", async (VoteRequestFromCandidateRpc rpc) =>
{
	await node.RecieveAVoteRequestFromCandidate(rpc);
});

app.MapPost("/RecieveVoteResults", async (VoteFromFollowerRpc rpc) =>
{
	await node.RecieveVoteResults(rpc);
});

app.MapPost("/SendMyVoteToCandidate", async (VoteRpc rpc) =>
{
	await node.SendMyVoteToCandidate(rpc);
});

app.MapPost("/RespondBackToLeader", async (ResponseBackToLeader rpc) =>
{
	await node.RespondBackToLeader(rpc);
});



app.Run();