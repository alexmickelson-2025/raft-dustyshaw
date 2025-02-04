using Raft;
using Raft.DTOs;
using System.Net.Http.Json;

public class HttpRpcNode : INode
{
	public Guid NodeId { get; set; }
	public string Url { get; }
	private HttpClient client = new();

	public HttpRpcNode(string url, Guid guid)
	{
		Console.WriteLine($"---- New Node Made {url} and guid {guid}");
		NodeId = guid;
		Url = url;
	}
	public async Task RequestAppendEntries(AppendEntriesRPC rpc)
	{
		try
		{
			Console.WriteLine($"---- Calling  RequestAppendEntries({rpc})");
			await client.PostAsJsonAsync(Url + "/RequestAppendEntries", rpc);
		}
		catch (Exception e)
		{
			Console.WriteLine($"---- node {NodeId} is down - RequestAppendEntries - {e.Message}");
		}
	}

    public async Task RecieveAVoteRequestFromCandidate(VoteRequestFromCandidateRpc rpc)
    {
       try
		{
			Console.WriteLine($"---- Calling  RecieveAVoteRequestFromCandidate({rpc})");
			await client.PostAsJsonAsync(Url + "/RecieveAVoteRequestFromCandidate", rpc);
		}
		catch (Exception e)
		{
			Console.WriteLine($"---- node {NodeId} is down - RecieveAVoteRequestFromCandidate - {e.Message}");	// failing
		}
    }

    public async Task RecieveAppendEntriesRPC(AppendEntriesRPC rpc)
    {
        try
		{
			Console.WriteLine($"---- Calling  RecieveAppendEntriesRPC({rpc})");
			await client.PostAsJsonAsync(Url + "/RecieveAppendEntriesRPC", rpc);
		}
		catch 
		{
			Console.WriteLine($"---- node {NodeId} is down - RecieveAppendEntriesRPC");
		}
    }

	public async Task RecieveVoteResults(VoteFromFollowerRpc vote)
	{
		try
		{
			Console.WriteLine($"---- Calling  RecieveVoteResults({vote})");
			await client.PostAsJsonAsync(Url + "/RecieveVoteResults", vote);
		}
		catch
		{
			Console.WriteLine($"---- node {NodeId} is down - RecieveVoteResults");
		}
	}
	
	public async Task RespondBackToLeader(ResponseBackToLeader rpc)
	{
		try
		{
			Console.WriteLine($"Calling  RespondBackToLeader({rpc})");
			await client.PostAsJsonAsync(Url + "/RespondBackToLeader", rpc);
		}
		catch
		{
			Console.WriteLine($"---- node {NodeId} is down - RespondBackToLeader");
		}
	}

    public async Task SendMyVoteToCandidate(VoteRpc vote)
    {
        try
		{
			Console.WriteLine($"---- Calling  SendMyVoteToCandidate({vote})");
			await client.PostAsJsonAsync(Url + "/SendMyVoteToCandidate", vote);
		}
		catch
		{
			Console.WriteLine($"---- node {NodeId} is down - SendMyVoteToCandidate");
		}
    }
}