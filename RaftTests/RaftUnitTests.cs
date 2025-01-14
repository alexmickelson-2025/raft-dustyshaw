using Raft;

namespace RaftTests
{
    public class RaftUnitTests
    {
        [Fact]
        public void OneNodeInSystemThenVoteIsYes()
        {
            // Arrange
            Node onlyNode = new Node(true, []);

            // Act
            onlyNode.SendVoteRequest();

            Assert.True(onlyNode.Vote);
        }

        // Testing #5
        [Fact]
        public void TestCase3_NodesStartAsFollowers()
        {
            // Arrange
            Node newNode = new Node(true, []);

            // Act
            var currentNodeState = newNode.State;

            // Assert
            Assert.Equal(Node.NodeState.Follower, currentNodeState);
        }
    }
}