using NSubstitute;
using Raft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
			follower.Received(1).RecieveAppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<List<Entry>>());
		}


		// Testing #2
		[Fact]
		public void TestCase02_NodesRecieveCommands()
		{
			Node n = new Node([], null, null);
			Entry l = new Entry("set a");

			n.RecieveClientCommand(l.Command);

			Assert.True(n.Entries.Count() > 0);
			Assert.Contains(l.Command, n.Entries.First().Command);
			//Assert.StrictEqual([l], n.Entries);	// node should contain the log
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

		// Testing #5
		[Fact]
		public void TestCase05_LeadersInitializeNextIndexForOtherNodes()
		{
			// leaders maintain an "nextIndex" for each follower that is the index
			// of the next log entry the leader will send to that follower

			// Leaders initialize their nextIndex[] array to have each node

			// arrange
			Node f1 = new Node([], null, null);
			Node f2 = new Node([], null, null);

			Node n = new Node([f1, f2], null, null);
			n.BecomeLeader();

			// act
			var result1 = n.nextIndexes.ContainsKey(f1.NodeId);
			var result2 = n.nextIndexes.ContainsKey(f2.NodeId);

			//assert
			Assert.True(result1);
			Assert.True(result2);
		}

		// Testing #5 (part 2)
		[Fact]
		public void TestCase05_LeadersInitializeNextIndexForOtherNodesOneGreaterThanLastLogIndex()
		{
			// Leaders initialize their nextIndex[] array to have the lastLogTerm + 1 of leader

			// arrange
			Node f1 = new Node([], null, null);

			Node n = new Node([f1], null, null);
			n.Entries = new List<Entry> { new Entry("set a"), new Entry("set b") };
			n.BecomeLeader();

			// act
			var result = n.nextIndexes.ContainsValue(2);	

			//assert
			Assert.True(result);
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
			follower.Received(1).RecieveAppendEntriesRPC(leader.NodeId, leader.TermNumber, leader.CommitIndex, Arg.Any<List<Entry>>());
		}

		// Testing #9
		[Fact]
		public void TestCase09_LeadersCommitEntriesByIncreasingTheirIndex()
		{
			//  the leader commits logs by incrementing its committed log index

			// arrange
			Node leader = new Node([], null, null);
			int indexBefore = leader.CommitIndex;

			// act
			leader.CommitEntry();

			// assert
			Assert.True(leader.CommitIndex - 1 == indexBefore);
		}

		// Testing #10
		[Fact]
		public async Task TestCase10_FollowersAddOneEntryToTheirLog()
		{
			// 10. given a follower receives an appendentries with log(s) it will add those entries to its personal log

			// arrange
			var f = new Node([], null, null);
			List<Entry> entries = new List<Entry>();
			Entry e = new Entry("set a");
			entries.Add(e);

			// act
			await f.RecieveAppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), entries);

			//assert
			Assert.True(f.Entries.Count() > 0);
			Assert.Contains(e, entries);
		}

		// Testing #10
		[Fact]
		public async Task TestCase10_FollowersAddMultipleEntriesToTheirLogInOrder()
		{
			// I want to make sure that the logs are appended in the order the follower recieved them.

			// arrange
			var f = new Node([], null, null);
			f.Entries = new List<Entry> { new Entry("set a") };

			List<Entry> entriesFromLeader = new List<Entry>();
			Entry e1 = new("set a");
			Entry e2 = new("set b");
			entriesFromLeader.Add(e1);
			entriesFromLeader.Add(e2);


			// act
			await f.RecieveAppendEntriesRPC(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>(), entriesFromLeader);

			//assert
			// Check the order
			var entriesList = f.Entries.ToList();
			Assert.Equal(entriesList.Last(), e2);  // Ensure e2 is the last one in the list
			Assert.Equal(entriesList[entriesList.Count - 2], e1);  // Ensure e is the one before the last one

		}


		// Testing #11
		[Fact]
		public async Task TestCase11_FollowersSendAResponseToLeaders()
		{
			//  a followers response to an appendentries includes the followers term number and log entry index

			// arrange
			var l = Substitute.For<INode>();

			Node  f = new([], null, null);
			f.OtherNodes = [l];

			// act
			await f.RecieveAppendEntriesRPC(l.NodeId, l.TermNumber, l.CommitIndex, new List<Entry>());

			// assert
			l.Received(1).RespondBackToLeader(Arg.Any<bool>(), f.TermNumber, f.CommitIndex);

		}

		// Testing #14
		[Fact]
		public async Task TestCase14_()
		{
			// 14.when a follower receives a heartbeat,
			// it increases its commitIndex to match the commit index of the heartbeat

			// arrange
			Node l = new([], null, null);
			l.CommitIndex = 100;
			Node f = new([], null, null);
			l.OtherNodes = [f];

			l.OtherNodes = [f];

			// act
			// follower recieves an empty heartbeat
			await f.RecieveAppendEntriesRPC(l.NodeId, l.TermNumber, l.CommitIndex, new List<Entry>());

			// assert
			Assert.True(f.CommitIndex == 100);	
		}
	}
}
