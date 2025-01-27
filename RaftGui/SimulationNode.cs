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
    public bool IsRunning { get => ((INode)InnerNode).IsRunning; set => ((INode)InnerNode).IsRunning = value; }

    public void BecomeLeader()
	{
		if (!IsRunning)
		{
			return;
		}
		((INode)InnerNode).BecomeLeader();
	}

	public void DetermineElectionResults()
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).DetermineElectionResults();
	}

	public Task RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
	{
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.RecieveAVoteRequestFromCandidate(candidateId, lastLogTerm);
		});
		return Task.CompletedTask;

		//return ((INode)InnerNode).RecieveAVoteRequestFromCandidate(candidateId, lastLogTerm);
	}

	public void RecieveVoteResults(bool result, int termNumber)
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).RecieveVoteResults(result, termNumber);
	}

	public void RespondBackToLeader(bool response, int myTermNumber, int myCommitIndex)
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).RespondBackToLeader(response, myTermNumber, myCommitIndex);
	}

	public Task RecieveAppendEntriesRPC(int LeadersTermNumber, Guid leaderId, int prevLogIndex, List<Entry> LeadersLog, int leaderCommit)
	{
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }
        Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.RecieveAppendEntriesRPC(LeadersTermNumber, leaderId, prevLogIndex, LeadersLog, leaderCommit);
		});
		return Task.CompletedTask;

		//return ((INode)InnerNode).RespondToAppendEntriesRPC(leaderId, TermNumber);
	}

	public void SendAppendEntriesRPC()
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).SendAppendEntriesRPC();
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
		//return ((INode)InnerNode).SendMyVoteToCandidate(candidateId, result);
	}

	public void SendVoteRequestRPCsToOtherNodes()
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).SendVoteRequestRPCsToOtherNodes();
	}

	public void StartElection()
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).StartElection();
	}

	public void TimeoutHasPassed()
	{
        if (!IsRunning)
        {
            return;
        }
        ((INode)InnerNode).TimeoutHasPassed();
	}

	public void TimeoutHasPassedForLeaders()
	{
		((INode)InnerNode).TimeoutHasPassedForLeaders();
	}

	public List<Entry> CalculateEntriesToSend(INode node)
	{
        if (!IsRunning)
        {
            return new List<Entry>();
        }
        return ((INode)InnerNode).CalculateEntriesToSend(node);
	}

    public void PauseNode()
    {
        ((INode)InnerNode).PauseNode();
    }

    public void UnpauseNode()
    {
        ((INode)InnerNode).UnpauseNode();
    }
}
