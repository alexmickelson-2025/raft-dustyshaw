using Raft;
using System.Net.Http.Json;

public class HttpRpcNode
{
	public Guid NodeId { get; set; }
	public string Url { get; }
	private HttpClient client = new();

	public HttpRpcNode(string url)
	{
		NodeId = Guid.NewGuid();
		Url = url;
	}

	public async Task RequestAppendEntries(AppendEntriesRPC rpc)
	{
		try
		{
			await client.PostAsJsonAsync(Url + "/request/SendAppendEntriesRPC", rpc);
		}
		catch 
		{
			Console.WriteLine($"node {Url} is down");
		}
	}
}