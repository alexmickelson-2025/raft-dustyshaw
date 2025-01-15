using Raft;

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
    public bool Vote { get => ((INode)InnerNode).Vote; set => ((INode)InnerNode).Vote = value; }

    public void AskForVotesFromOtherNodes()
    {
        ((INode)InnerNode).AskForVotesFromOtherNodes();
    }

    public bool RecieveAVoteRequestFromCandidate(Guid candidateId, int lastLogTerm)
    {
        return ((INode)InnerNode).RecieveAVoteRequestFromCandidate(candidateId, lastLogTerm);
    }

    public void RespondToAppendEntriesRPC()
    {
        ((INode)InnerNode).RespondToAppendEntriesRPC();
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
