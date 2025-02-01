using Raft;
using Raft.DTOs;
using System.Net.Http.Json;

public class HttpRpcNode : INode
{
	public Guid NodeId { get; set; }
	public string Url { get; }
	private HttpClient client = new();

	public HttpRpcNode(string url)
	{
		Console.WriteLine($"New Node Made {url}");
		NodeId = Guid.NewGuid();
		Url = url;
	}

	public async Task SendAppendEntriesRPC()
	{
		Console.WriteLine("something happened");
	}

	public async Task RequestAppendEntries(AppendEntriesRPC rpc)
	{
		try
		{
			Console.WriteLine("Calling Stuff from the NOde!");
			await client.PostAsJsonAsync(Url + "/RequestAppendEntries", rpc);
		}
		catch 
		{
			Console.WriteLine($"node {Url} is down");
		}
	}

    public async Task RecieveAVoteRequestFromCandidate(VoteRequestFromCandidateRpc rpc)
    {
       try
		{
			Console.WriteLine($"Calling  RecieveAVoteRequestFromCandidate({rpc})");
			await client.PostAsJsonAsync(Url + "/RecieveAVoteRequestFromCandidate", rpc);
		}
		catch 
		{
			Console.WriteLine($"node {Url} is down");
		}
    }

    public Task SendMyVoteToCandidate(Guid candidateId, bool result)
    {
        throw new NotImplementedException();
    }

    public Task RecieveAppendEntriesRPC(AppendEntriesRPC rpc)
    {
        throw new NotImplementedException();
    }

    public void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex, Guid fNodeId)
    {
        throw new NotImplementedException();
    }

	public async Task RecieveVoteResults(VoteFromFollowerRpc vote)
	{
		try
		{
			Console.WriteLine($"Calling  RecieveVoteResults({vote})");
			await client.PostAsJsonAsync(Url + "/RecieveVoteResults", vote);
		}
		catch
		{
			Console.WriteLine($"node {Url} is down");
		}
	}
}