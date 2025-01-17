﻿using Raft;

namespace RaftGui;

public class SimulationNode : INode
{
    public readonly Node InnerNode;
    public SimulationNode(Node node)
    {
        this.InnerNode = node;
    }

    public int ElectionTimeout { get => ((INode)InnerNode).ElectionTimeout; set => ((INode)InnerNode).ElectionTimeout = value; }
    public INode[] OtherNodes { get => ((INode)InnerNode).OtherNodes; set => ((INode)InnerNode).OtherNodes = value; }
    public Node.NodeState State { get => ((INode)InnerNode).State; set => ((INode)InnerNode).State = value; }
	public int TermNumber { get => ((INode)InnerNode).TermNumber; set => ((INode)InnerNode).TermNumber = value; }
	public Guid LeaderId { get => ((INode)InnerNode).LeaderId; set => ((INode)InnerNode).LeaderId = value; }
	public DateTime WhenTimerStarted { get => ((INode)InnerNode).WhenTimerStarted; set => ((INode)InnerNode).WhenTimerStarted = value; }
	public System.Timers.Timer aTimer { get => ((INode)InnerNode).aTimer; set => ((INode)InnerNode).aTimer = value; }

	public void SendVoteRequestRPCsToOtherNodes()
    {
        ((INode)InnerNode).SendVoteRequestRPCsToOtherNodes();
    }

    public bool RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
    {
        return ((INode)InnerNode).RecieveAVoteRequestFromCandidate(candidateId, lastLogTerm);
    }

    public void RespondToAppendEntriesRPC(Guid leaderId, int termNumber)
    {
        ((INode)InnerNode).RespondToAppendEntriesRPC(leaderId, termNumber);
    }

    public void SendAppendEntriesRPC()
    {
        ((INode)InnerNode).SendAppendEntriesRPC();
    }

    public void StartElection()
    {
        ((INode)InnerNode).StartElection();
    }
}
