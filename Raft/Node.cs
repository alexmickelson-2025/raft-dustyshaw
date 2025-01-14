namespace Raft
{
    public class Node : INode
    {
        public enum NodeState
        {
            Follower,
            Candidate,
            Leader
        }

        public int ElectionTimeout { get; set; } // in ms
        public bool Vote { get; set; }
        public INode[] OtherNodes { get; set; }

        public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

        public Node(bool Vote, Node[] OtherNodes)
        {
            this.Vote = Vote;
            this.OtherNodes = OtherNodes;
            this.ElectionTimeout = Random.Shared.Next(150, 300);
        }

        public bool SendAppendEntriesRPC()
        {
            foreach (var node in OtherNodes)
            {
                node.RespondToAppendEntriesRPC();
            }
            return true;
        }

        public bool RespondToAppendEntriesRPC()
        {
            return true; // simplest case for now
        }
    }
}
