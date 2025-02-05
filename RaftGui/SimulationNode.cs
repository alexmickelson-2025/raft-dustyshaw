using Raft;
using Raft.DTOs;

namespace RaftGui;

public class SimulationNode : INode
{
    public readonly Node InnerNode;
    public bool IsRunning { get; set; } = true;
    public int NetworkRequestDelay { get; set; } = 0;
	public Guid NodeId { get => ((INode)InnerNode).NodeId; set => ((INode)InnerNode).NodeId = value; }

	public SimulationNode(Node node)
    {
        this.InnerNode = node;
    }

	public Task RecieveAVoteRequestFromCandidate(VoteRequestFromCandidateRpc rpc)
	{
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.RecieveAVoteRequestFromCandidate(rpc);
		});
		return Task.CompletedTask;
	}

	public Task RespondBackToLeader(ResponseBackToLeader rpc)
	{
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }
		Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.RespondBackToLeader(rpc);
		});
		return Task.CompletedTask;
	}

	public Task RecieveAppendEntriesRPC(AppendEntriesRPC rpc)
	{
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.RecieveAppendEntriesRPC(rpc);
		});
		return Task.CompletedTask;
	}

	public Task SendMyVoteToCandidate(VoteRpc rpc)
	{
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.SendMyVoteToCandidate(rpc);
		});
		return Task.CompletedTask;
	}

	public Task RecieveVoteResults(VoteFromFollowerRpc vote)
	{
		if (!IsRunning)
		{
			return Task.CompletedTask;
		}
		return ((INode)InnerNode).RecieveVoteResults(vote);
	}
}
