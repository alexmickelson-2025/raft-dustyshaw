using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Raft
{
    public class NodeData
    {
        public Guid NodeId { get; set; }
        public Node.NodeState State { get; set; }
        public int ElectionTimeout { get; set; }
        public int Term { get; set; }
        public Guid LeaderId { get; set; }
        public int CommitIndex { get; set; }
        public List<Entry> Entries { get; set; }
        public int NodeInteralScalar { get; set; }
        public DateTime WhenTimeStarted { get; set; }
        public string? Url { get; set; }
        public bool IsRunning { get; set; }
        public List<Entry> StateMachine { get; set; }
    }
}
