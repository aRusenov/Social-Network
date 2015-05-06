using System;
using SocialNetwork.Services.Models.Users;

namespace SocialNetwork.Tests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SocialNetwork.Data;
    using SocialNetwork.Common;
    using SocialNetwork.Services.Models.Posts;
    using SocialNetwork.Tests.Models;

    [TestClass]
    public class UserControllerTests : BaseIntegrationTest
    {
        [TestMethod]
        public void LoginShouldReturnAuthTokenWith200Ok()
        {
            var httpResponse = this.Login(SeededUserUsername, SeededUserPassword);
            var responseValues = httpResponse.Content.ReadAsAsync<UserSessionModel>().Result;

            Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
            Assert.IsNotNull(responseValues.Access_Token);
        }

        [TestMethod]
        public void RegisterWithValidDataShouldReturnAccessTokenWith200Ok()
        {
            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", RegisterTestUsername),
                new KeyValuePair<string, string>("password", "pitona"),
                new KeyValuePair<string, string>("confirmPassword", "pitona"),
                new KeyValuePair<string, string>("name", "Mitio Pishtova"),
                new KeyValuePair<string, string>("email", "mm@aha.bg"),
                new KeyValuePair<string, string>("phone", "+359 9315 17238")
            });

            var httpResponse = this.httpClient.PostAsync(ApiEndpoints.UserRegister, loginData).Result;

            var responseValues = httpResponse.Content.ReadAsAsync<UserSessionModel>().Result;

            Assert.AreEqual(HttpStatusCode.OK, httpResponse.StatusCode);
            Assert.IsNotNull(responseValues.Access_Token);
        }

        [TestMethod]
        public void LogoutShouldReturn200Ok()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var logoutResponse = this.httpClient.PostAsync(ApiEndpoints.UserLogout, null).Result;

            Assert.AreEqual(HttpStatusCode.OK, logoutResponse.StatusCode);
        }

        [TestMethod]
        public async Task GetUserPreviewShouldReturnDataAboutUserWhenLogged()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var getUserResponse = await this.httpClient.GetAsync(string.Format("api/users/{0}/preview", SeededUserUsername));

            var getUserResponseString = getUserResponse.Content.ReadAsStringAsync().Result;
            var getUserResponseData = getUserResponseString.ToJson();

            Assert.AreEqual(HttpStatusCode.OK, getUserResponse.StatusCode);
            Assert.IsNotNull(getUserResponseData["id"]);
            Assert.IsNotNull(getUserResponseData["userName"]);
            Assert.IsNotNull(getUserResponseData["name"]);
            Assert.IsNotNull(getUserResponseData["isFriend"]);
            Assert.IsNotNull(getUserResponseData["hasPendingRequest"]);
            Assert.IsTrue(getUserResponseData.ContainsKey("profileImage"));
        }

        [TestMethod]
        public void GetUserShouldReturn401UnauthorizedWhenNotLogged()
        {
            var getUserResponse = this.httpClient.GetAsync(string.Format(ApiEndpoints.UserPreview, SeededUserUsername)).Result;

            Assert.AreEqual(HttpStatusCode.Unauthorized, getUserResponse.StatusCode);
        }

        [TestMethod]
        public void GetWallShouldReturnConsecutivePagesWithNonRepeatingPosts()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var user = this.Data.Users.All()
                .First(u => u.WallPosts.Count > 10);

            const int pageSize = 5;
            int wallPostCount = user.WallPosts.Count();
            int? startId = null;

            var postIds = new List<int>();

            int pageCount = (wallPostCount + pageSize - 1) / pageSize;
            for (int i = 0; i < pageCount; i++)
            {
                var getWallResponse = this.httpClient.GetAsync(string.Format(
                    "api/users/{0}/wall?pageSize={1}&startPostId={2}",
                    user.UserName, pageSize, startId)).Result;

                var responseData = getWallResponse.Content
                    .ReadAsAsync<IEnumerable<PostViewModel>>().Result;

                foreach (var post in responseData)
                {
                    Assert.IsNotNull(post.Id);

                    postIds.Add(post.Id);
                }

                startId = responseData.LastOrDefault() == null ? null : (int?)responseData.Last().Id;
            }

            CollectionAssert.AllItemsAreUnique(postIds);
            Assert.AreEqual(postIds.Count, wallPostCount);
        }

        [TestMethod]
        public void GetWallShouldReturnPostDataAndAuthorDataAndTop3Comments()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var user = this.Data.Users.All()
                .First(u => u.WallPosts.Count > 2);

            var getWallResponse = this.httpClient.GetAsync(string.Format(
                     "api/users/{0}/wall?pageSize=0", user.UserName)).Result;

            Assert.AreEqual(HttpStatusCode.OK, getWallResponse.StatusCode);

            var responseData = getWallResponse.Content
                .ReadAsAsync<IEnumerable<PostViewModel>>().Result;

            foreach (var post in responseData)
            {
                // Post data
                Assert.IsNotNull(post.Id);
                Assert.IsNotNull(post.AuthorId);
                Assert.IsNotNull(post.AuthorProfileImage);
                Assert.IsNotNull(post.AuthorUsername);
                Assert.IsNotNull(post.PostContent);
                Assert.IsNotNull(post.Date);
                Assert.IsNotNull(post.LikesCount);
                Assert.IsNotNull(post.Liked);

                // Comments data
                Assert.IsNotNull(post.TotalCommentsCount);
                Assert.IsNotNull(post.Comments.Count() <= 3);

                foreach (var comment in post.Comments)
                {
                    Assert.IsNotNull(comment.Id);
                    Assert.IsNotNull(comment.CommentContent);
                    Assert.IsNotNull(comment.AuthorId);
                    Assert.IsNotNull(comment.AuthorProfileImage);
                    Assert.IsNotNull(comment.AuthorUsername);
                    Assert.IsNotNull(comment.LikesCount);
                    Assert.IsNotNull(comment.Liked);
                }
            }
        }

        [TestMethod]
        public void GetAllFriendsOfFriendShouldReturnFriends()
        {
            var loginResponse = this.Login(SeededUserUsername, SeededUserPassword);
            var username = loginResponse.Content.ReadAsStringAsync().Result.ToJson()["userName"];

            var friend = this.Data.Users.All()
                .First(u => u.Friends
                    .Any(fr => fr.UserName == username));

            var getResponse = this.httpClient.GetAsync(
                string.Format("api/users/{0}/friends", friend.UserName)).Result;

            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [TestMethod]
        public void GetAllFriendsOfNonFriendShouldReturnBadRequest()
        {
            var loginResponse = this.Login(SeededUserUsername, SeededUserPassword);
            var username = loginResponse.Content.ReadAsStringAsync().Result.ToJson()["userName"];

            var friend = this.Data.Users.All()
                .First(u => u.Friends
                    .All(fr => fr.UserName != username));

            var getResponse = this.httpClient.GetAsync(
                string.Format("api/users/{0}/friends", friend.UserName)).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, getResponse.StatusCode);
        }

        [TestMethod]
        public void SearchingPartOfNamesReturnUsersWhoseNamesContainThatString()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var usersWithJInNamesCount = this.Data.Users.All()
                .Count(u => u.Name.ToLower().Contains("j"));

            var getResponse = this.httpClient.GetAsync(
                string.Format("api/users/search?searchTerm={0}", "J")).Result;

            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);

            var responseData = getResponse.Content
                  .ReadAsAsync<IEnumerable<UserViewModelMinified>>().Result;
            Assert.AreEqual(usersWithJInNamesCount, responseData.Count());
        }
    }
}
