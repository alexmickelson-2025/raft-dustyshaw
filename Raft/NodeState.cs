namespace Raft
{
    public partial class Node
    {
        public enum NodeState
        {
            Follower,
            Candidate,
            Leader
        }
    }
}
