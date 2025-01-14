namespace Raft
{
    public class Node
    {
        public enum NodeState
        {
            Follower,
            Candidate,
            Leader
        }

        public int ElectionTimeout { get; set; } // in ms
        public bool Vote { get; set; }
        public Node[] OtherNodes { get; set; }

        public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

        public Node(bool Vote, Node[] OtherNodes)
        {
            this.Vote = Vote;
            this.OtherNodes = OtherNodes;
        }

        public async void SendVoteRequest()
        {
            // Send vote
        }
    }
}
