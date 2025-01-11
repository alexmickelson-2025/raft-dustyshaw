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
    }
}