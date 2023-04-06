using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace TestGameApi
{
    [TestClass]
    public class GamePlayers
    {
        [TestMethod]
        public async Task JoinGame()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            
            var actual = await http.PostAsJsonAsync(
                $"/games/{TestEnv.Guid5}/players",
                new Player() { PrivateId=TestEnv.Guid3, PublicId=TestEnv.Guid4 }
            );

            Assert.AreEqual(StatusCodes.Status201Created, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task JoinGameMultiplePlayers()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob,carol");
            await TestEnv.BobJoins   (http, TestEnv.Guid7);
            await TestEnv.PlayerJoins(http, TestEnv.Guid7, TestEnv.Guid5, TestEnv.Guid6);

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>(
                $"/games/{TestEnv.Guid7}/players"
            );

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(
                new [] { 
                    new PlayerDto(TestEnv.Guid2, "alice"),
                    new PlayerDto(TestEnv.Guid4, "bob"),
                    new PlayerDto(TestEnv.Guid6, "carol")
                },
                actual.ToArray()
            );
        }
        [TestMethod]
        public async Task JoinUnexistingGameFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            
            var actual = await http.PostAsJsonAsync(
                $"/games/{TestEnv.Guid7}/players",
                new Player()
                {
                    PrivateId = TestEnv.Guid3,
                    PublicId = TestEnv.Guid4
                }
            );

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task JoinGameWithPublicIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins(http, TestEnv.Guid5);

            var actual = await http.PostAsJsonAsync(
                $"/games/{TestEnv.Guid5}/players",
                new Player()
                {
                    PrivateId = TestEnv.Guid4,
                    PublicId = TestEnv.Guid4
                }
            );

            Assert.AreEqual(StatusCodes.Status401Unauthorized, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task JoinGameWithBadIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins(http, TestEnv.Guid5);

            var actual = await http.PostAsJsonAsync(
                $"/games/{TestEnv.Guid5}/players",
                new Player()
                {
                    PrivateId = TestEnv.Guid3,
                    PublicId = TestEnv.Guid7
                }
            );

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task JoinGameTwiceFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins(http, TestEnv.Guid5);

            var actual = await http.PostAsJsonAsync(
                $"/games/{TestEnv.Guid5}/players",
                new Player() {
                    PrivateId = TestEnv.Guid3,
                    PublicId  = TestEnv.Guid4
                }
            );

            Assert.AreEqual(StatusCodes.Status409Conflict, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task JoinGameWhenAlreadyInAnotherGameFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await http.PostAsJsonAsync("/games", new GameDto(Title: "g2", Creator: TestEnv.Guid3));

            await TestEnv.BobJoins(http, TestEnv.Guid6);

            var actual = await http.PostAsJsonAsync(
                $"/games/{TestEnv.Guid5}/players",
                new Player()
                {
                    PrivateId = TestEnv.Guid3,
                    PublicId = TestEnv.Guid4
                }
            );

            Assert.AreEqual(StatusCodes.Status409Conflict, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task LeaveGame()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins(http, TestEnv.Guid5);

            var actual = await http.DeleteAsync(
                $"/games/{TestEnv.Guid5}/players/{TestEnv.Guid4}?privateId={TestEnv.Guid3}"
            );

            Assert.AreEqual(StatusCodes.Status204NoContent, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task LeaveGameWhenTimedOut()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame(
                "g1",
                "alice,bob", TestEnv.NewStubClock(
                    TestEnv.Xmas2023_10h00am, // Alice created
                    TestEnv.Xmas2023_10h00am, // Bob created
                    TestEnv.Xmas2023_10h00am, // Alice creates G1
                    TestEnv.Xmas2023_10h30am
                )
            );

            var actual = await http.DeleteAsync(
                $"/games/{TestEnv.Guid5}/players/{TestEnv.Guid4}?privateId={TestEnv.Guid3}"
            );
            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task LeaveGameWithBadIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins(http, TestEnv.Guid5);

            var actual = await http.DeleteAsync(
                $"/games/{TestEnv.Guid5}/players/{TestEnv.Guid4}?privateId={TestEnv.Guid7}"
            );

            Assert.AreEqual(StatusCodes.Status401Unauthorized, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeleteGameWhenLastPlayerLeaves()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins     (http, TestEnv.Guid5);
            await TestEnv.AliceLeaveG1(http);

            var actual = await http.DeleteAsync($"/games/{TestEnv.Guid5}/players/{TestEnv.Guid4}?privateId={TestEnv.Guid3}");

            Assert.AreEqual(StatusCodes.Status204NoContent, (int)actual.StatusCode);
            CollectionAssert.AreEqual(
                Array.Empty<GameDto>(),
                (await http.GetFromJsonAsync<IEnumerable<GameDto>>("/games"))?.ToArray()
            );
        }
        [TestMethod]
        public async Task DeleteGameWhenAllPlayersDie()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame(
                "g1", 
                "alice,bob", TestEnv.NewStubClock(
                    TestEnv.Xmas2023_10h00am, // Alice created
                    TestEnv.Xmas2023_10h00am, // Bob created
                    TestEnv.Xmas2023_10h00am, // Alice creates G1
                    TestEnv.Xmas2023_10h30am
                )
            );

            var actual = await http.GetFromJsonAsync<IEnumerable<GameDto>>("/games");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(
                Array.Empty<GameDto>(),
                actual.ToArray()
            );
        }

        [TestMethod]
        public async Task KickPlayerAsCreator()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins(http, TestEnv.Guid5);

            var actual = await http.DeleteAsync(
                $"/games/{TestEnv.Guid5}/players/{TestEnv.Guid4}?privateId={TestEnv.Guid1}"
            );

            Assert.AreEqual(StatusCodes.Status204NoContent, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task KickPlayerWithNoCreatorCredentialsFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await TestEnv.BobJoins(http, TestEnv.Guid5);

            var actual = await http.DeleteAsync(
                $"/games/{TestEnv.Guid5}/players/{TestEnv.Guid4}"
            );

            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);
        }
    }
}
