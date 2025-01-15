using System.Timers;

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
        public Guid NodeId { get; set; } = Guid.NewGuid();
        public Guid VoteForId { get; set; }

        public int ElectionTimeout { get; set; } // in ms
        public static System.Timers.Timer aTimer { get; set; }
        public int HeartbeatTimeout { get; } = 50; // in ms
        public bool Vote { get; set; }
        public INode[] OtherNodes { get; set; }

        public NodeState State { get; set; } = NodeState.Follower; // nodes start as followers

        public Node(bool Vote, Node[] OtherNodes)
        {
            aTimer = new System.Timers.Timer(HeartbeatTimeout);
            aTimer.Elapsed += TimeoutHasPassed;
            aTimer.AutoReset = true; // repeat this
            aTimer.Enabled = true; // start the timer

            this.Vote = Vote;
            this.OtherNodes = OtherNodes;
            this.ElectionTimeout = Random.Shared.Next(150, 300);
        }

        public void TimeoutHasPassed(Object source, ElapsedEventArgs e)
        {
            SendAppendEntriesRPC();
        }

        public void SendAppendEntriesRPC()
        {
            foreach (var node in OtherNodes)
            {
                node.RespondToAppendEntriesRPC();
            }
        }

        public bool RespondToAppendEntriesRPC()
        {
            return true; // simplest case for now
        }

        public void StartElection()
        {
            this.State = NodeState.Candidate;
            this.VoteForId = this.NodeId;
        }

    }
}
