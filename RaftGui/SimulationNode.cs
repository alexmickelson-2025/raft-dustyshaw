using Raft;
using Raft.DTOs;
using System.Timers;

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

	public void RecieveVoteResults(bool result, int termNumber)
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).RecieveVoteResults(result, termNumber);
	}

	public void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex, Guid fNodeId)
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).RespondBackToLeader(response, myTermNumber, myCommitIndex, fNodeId);
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

	public Task SendMyVoteToCandidate(Guid candidateId, bool result)
	{
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.SendMyVoteToCandidate(candidateId, result);
		});
		return Task.CompletedTask;
	}
}
