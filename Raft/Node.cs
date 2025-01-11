namespace Raft
{
    public class Node
    {
        public int ElectionTimeout { get; set; } 
        public bool Vote {  get; set; }
        public Node[] OtherNodes { get; set; }

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
