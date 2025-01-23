using NSubstitute;
using Raft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaftTests
{
	public class LogTests
	{
		// Testing #1
		[Fact]
		public void TestCase01_LeadersSendRPCToFollowersWhenRecieveAnEntry()
		{
			// when a leader receives a client command, the leader sends the
			// log entry in the next appendentries RPC to all nodes

			// Arrange
			Node n = new Node([], null, null);
			Entry l = new Entry("set a");
			n.Entries = [l];

			var follower = Substitute.For<INode>();

			n.OtherNodes = [follower];	

			// Act
			n.RecieveClientCommand(l.Command);

			// Assert
			follower.Received(1).RecieveAppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>());
		}


		// Testing #2
		[Fact]
		public void TestCase02_NodesRecieveCommands()
		{
			Node n = new Node([], null, null);
			Entry l = new Entry("set a");

			n.RecieveClientCommand(l.Command);


			Assert.True(n.Entries.Count() > 0);
			Assert.StrictEqual([l], n.Entries);	// node should contain the log
		}

		// Testing #3
		[Fact]
		public void TestCase03_NodesStartWithNoLogs()
		{
			// Arrange and Act
			Node n = new([], null, null);

			// Assert
			Assert.True(n.Entries.Count() == 0);
		}

		// Testing #6
		[Fact]
		public void TestCase06_CommittedIndexIsIncludedInAppendEntriesRPC()
		{
			// 6. Highest committed index from the leader is included in AppendEntries RPC's
			// Arrange
			var leader = new Node([], null, null);
			leader.BecomeLeader();

			var follower = Substitute.For<INode>();
			leader.OtherNodes = [follower];

			// Act
			leader.CommitIndex = 100;
			leader.SendAppendEntriesRPC();

			// assert
			// The follower should have recieved the leaders commit index (along with its id and term)
			follower.Received(1).RecieveAppendEntriesRPC(leader.NodeId, leader.TermNumber, leader.CommitIndex);
		}

		// Testing #10
		[Fact]
		public void TestCase10_FollowersAddEntriesToTheirLog()
		{
			// 10. given a follower receives an appendentries with log(s) it will add those entries to its personal log

			// arrange
			

			// act

			//assert
		}
	}
}
