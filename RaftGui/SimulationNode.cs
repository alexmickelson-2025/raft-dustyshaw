using Raft;
using System.Timers;

namespace RaftGui;

public class SimulationNode : INode
{
    public readonly Node InnerNode;
	public int NetworkRequestDelay { get; set; }
	public bool IsRunning { get; set; } = true;
	public Guid NodeId { get => ((INode)InnerNode).NodeId; set => ((INode)InnerNode).NodeId = value; }

	public SimulationNode(Node node)
    {
        this.InnerNode = node;
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

	// As a candidate, I need to send out a vote request to other nodes...
	public Task SendVoteRequestRPCsToOtherNodes()
	{
		if (!IsRunning)
		{
			return Task.CompletedTask;
		}
		Task.Delay(NetworkRequestDelay).ContinueWith(async (_previousTask) =>
		{
			await InnerNode.SendVoteRequestRPCsToOtherNodes();
		});
		return Task.CompletedTask;
		//((INode)InnerNode).SendVoteRequestRPCsToOtherNodes();
	}

	//public void StartElection()
	//{
	//	if (!IsRunning)
	//	{
	//		return;
	//	}
	//	((INode)InnerNode).StartElection();
	//}

	//public void TimeoutHasPassed()
	//{
	//	if (!IsRunning)
	//	{
	//		return;
	//	}
	//	   ((INode)InnerNode).TimeoutHasPassed();
	//}

	//public void TimeoutHasPassedForLeaders()
	//{
	//	((INode)InnerNode).TimeoutHasPassedForLeaders();
	//}

	//public List<Entry> CalculateEntriesToSend(INode node)
	//{
	//       if (!IsRunning)
	//       {
	//           return new List<Entry>();
	//       }
	//       return ((INode)InnerNode).CalculateEntriesToSend(node);
	//}

	//public void PauseNode()
	//{
	//    ((INode)InnerNode).PauseNode();
	//}

	//public void UnpauseNode()
	//{
	//    ((INode)InnerNode).UnpauseNode();
	//}
}
