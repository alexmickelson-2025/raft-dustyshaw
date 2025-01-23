using Raft;

namespace RaftGui;

public class SimulationNode : INode
{
    public readonly Node InnerNode;
    public SimulationNode(Node node)
    {
        this.InnerNode = node;
    }

	public System.Timers.Timer aTimer { get => ((INode)InnerNode).aTimer; set => ((INode)InnerNode).aTimer = value; }
	public int ElectionTimeout { get => ((INode)InnerNode).ElectionTimeout; set => ((INode)InnerNode).ElectionTimeout = value; }

	public int HeartbeatTimeout => ((INode)InnerNode).HeartbeatTimeout;

	public Guid LeaderId { get => ((INode)InnerNode).LeaderId; set => ((INode)InnerNode).LeaderId = value; }
	public int NetworkRequestDelay { get => ((INode)InnerNode).NetworkRequestDelay; set => ((INode)InnerNode).NetworkRequestDelay = value; }
	public Guid NodeId { get => ((INode)InnerNode).NodeId; set => ((INode)InnerNode).NodeId = value; }
	public INode[] OtherNodes { get => ((INode)InnerNode).OtherNodes; set => ((INode)InnerNode).OtherNodes = value; }
	public Node.NodeState State { get => ((INode)InnerNode).State; set => ((INode)InnerNode).State = value; }
	public int TermNumber { get => ((INode)InnerNode).TermNumber; set => ((INode)InnerNode).TermNumber = value; }
	public int VotedForTermNumber { get => ((INode)InnerNode).VotedForTermNumber; set => ((INode)InnerNode).VotedForTermNumber = value; }
	public Guid VoteForId { get => ((INode)InnerNode).VoteForId; set => ((INode)InnerNode).VoteForId = value; }
	public List<bool> votesRecieved { get => ((INode)InnerNode).votesRecieved; set => ((INode)InnerNode).votesRecieved = value; }
	public DateTime WhenTimerStarted { get => ((INode)InnerNode).WhenTimerStarted; set => ((INode)InnerNode).WhenTimerStarted = value; }
	public int LowerBoundElectionTime { get => ((INode)InnerNode).LowerBoundElectionTime; set => ((INode)InnerNode).LowerBoundElectionTime = value; }
	public int UpperBoundElectionTime { get => ((INode)InnerNode).UpperBoundElectionTime; set => ((INode)InnerNode).UpperBoundElectionTime = value; }

	public int CommitIndex { get => ((INode)InnerNode).CommitIndex; set => ((INode)InnerNode).CommitIndex = value; }
	public List<Entry> Entries { get => ((INode)InnerNode).Entries; set => ((INode)InnerNode).Entries = value; }

	public void BecomeLeader()
	{
		((INode)InnerNode).BecomeLeader();
	}

	public void DetermineElectionResults()
	{
		((INode)InnerNode).DetermineElectionResults();
	}

	public Task RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
	{
		Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.RecieveAVoteRequestFromCandidate(candidateId, lastLogTerm);
		});
		return Task.CompletedTask;

		//return ((INode)InnerNode).RecieveAVoteRequestFromCandidate(candidateId, lastLogTerm);
	}

	public void RecieveVoteResults(bool result, int termNumber)
	{
		((INode)InnerNode).RecieveVoteResults(result, termNumber);
	}

	public void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex)
	{
		((INode)InnerNode).RespondBackToLeader(response, myTermNumber, myCommitIndex);
	}

	public Task RecieveAppendEntriesRPC(Guid leaderId, int TermNumber, int CommitIndex, List<Entry> LeadersLog)
	{
		Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.RecieveAppendEntriesRPC(leaderId, TermNumber, CommitIndex, LeadersLog);
		});
		return Task.CompletedTask;

		//return ((INode)InnerNode).RespondToAppendEntriesRPC(leaderId, TermNumber);
	}

	public void SendAppendEntriesRPC()
	{
		((INode)InnerNode).SendAppendEntriesRPC();
	}

	public Task SendMyVoteToCandidate(Guid candidateId, bool result)
	{
		Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.SendMyVoteToCandidate(candidateId, result);
		});
		return Task.CompletedTask;
		//return ((INode)InnerNode).SendMyVoteToCandidate(candidateId, result);
	}

	public void SendVoteRequestRPCsToOtherNodes()
	{
		((INode)InnerNode).SendVoteRequestRPCsToOtherNodes();
	}

	public void StartElection()
	{
		((INode)InnerNode).StartElection();
	}

	public void TimeoutHasPassed()
	{
		((INode)InnerNode).TimeoutHasPassed();
	}

	public void TimeoutHasPassedForLeaders()
	{
		((INode)InnerNode).TimeoutHasPassedForLeaders();
	}
}
