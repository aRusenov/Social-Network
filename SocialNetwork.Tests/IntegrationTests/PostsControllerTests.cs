﻿namespace SocialNetwork.Tests.IntegrationTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SocialNetwork.Common;
    using SocialNetwork.Data;
    using SocialNetwork.Services.Models.Likes;
    using SocialNetwork.Services.Models.Posts;

    [TestClass]
    public class PostsControllerTests : BaseIntegrationTest
    {
        [TestMethod]
        public void GetPostShouldReturnInfoAboutPost()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var post = this.Data.Posts.All().First();

            var getResponse = this.httpClient.GetAsync(
                string.Format("api/posts/{0}", post.Id)).Result;

            var responseData = getResponse.Content.ReadAsAsync<PostViewModel>().Result;

            Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.AreEqual(post.Id, responseData.Id);
        }

        [TestMethod]
        public void EditPostShouldModifyPostAndReturnData()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var post = this.Data.Posts.All().First();
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("content", "new content")
            });

            var putResponse = this.httpClient.PutAsync(
                string.Format("api/posts/{0}", post.Id), formData).Result;
            var responseData = putResponse.Content.ReadAsStringAsync().Result.ToJson<string, object>();

            this.Data = new SocialNetworkData();

            Assert.AreEqual(HttpStatusCode.OK, putResponse.StatusCode);
            Assert.AreEqual(responseData["content"],
                this.Data.Posts.GetById(post.Id).Content);
        }

        [TestMethod]
        public void DeletePostShouldRemovePostFromDatabase()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var post = this.Data.Posts.All().First();

            var deleteResponse = this.httpClient.DeleteAsync(
                string.Format("api/posts/{0}", post.Id)).Result;

            Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode);

            this.Data = new SocialNetworkData();
            var samePost = this.Data.Posts.GetById(post.Id);

            Assert.IsNull(samePost);
        }

        [TestMethod]
        public void GetDetailedLikesShouldReturnDataAboutAllLikes()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var likedPost = this.Data.Posts.All()
                .First(p => p.Likes.Count > 0);

            var getLikesResponse = this.httpClient.GetAsync(
                string.Format("api/posts/{0}/likes", likedPost.Id)).Result;

            Assert.AreEqual(HttpStatusCode.OK, getLikesResponse.StatusCode);

            var responseData = getLikesResponse.Content
                .ReadAsAsync<ICollection<DetailedPostLikesViewModel>>().Result;

            foreach (var postLike in responseData)
            {
                Assert.IsNotNull(postLike.Name);
                Assert.IsNotNull(postLike.PostId);
                Assert.IsNotNull(postLike.UserId);
                Assert.IsNotNull(postLike.Username);
            }
        }

        [TestMethod]
        public void LikingUnlikedPostOnFriendWallShouldReturn200Ok()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var unlikedPostOnFriendWall = this.Data.Posts.All()
                .FirstOrDefault(p => p.Content == "Friend wall");

            var likeResponse = this.httpClient.PostAsync(string.Format("api/posts/{0}/likes", unlikedPostOnFriendWall.Id), null).Result;

            Assert.AreEqual(HttpStatusCode.OK, likeResponse.StatusCode);
        }

        [TestMethod]
        public void LikingUnlikedPostByFriendOnNonFriendWallShouldReturn200Ok()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var unlikedPostOnFriendWall = this.Data.Posts.All()
                .FirstOrDefault(p => p.Content == "Other wall");

            var likeResponse = this.httpClient.PostAsync(
                string.Format("api/posts/{0}/likes", unlikedPostOnFriendWall.Id), null).Result;

            Assert.AreEqual(HttpStatusCode.OK, likeResponse.StatusCode);
        }

        [TestMethod]
        public void LikingUnlikedPostByNonFriendOnNonFriendWallShouldReturnBadRequest()
        {
            this.Login(SeededUserUsername, SeededUserPassword);

            var unlikedPostOnFriendWall = this.Data.Posts.All()
                .FirstOrDefault(p => p.Content == "Restricted wall");

            var likeResponse = this.httpClient.PostAsync(string.Format("api/posts/{0}/likes", unlikedPostOnFriendWall.Id), null).Result;

            Assert.AreEqual(HttpStatusCode.BadRequest, likeResponse.StatusCode);
        }

        [TestMethod]
        public void LikingUnlikedPostShouldIncrementLikes()
        {
            var loginResponse = this.Login(SeededUserUsername, SeededUserPassword);
            var username = loginResponse.Content.ReadAsStringAsync().Result.ToJson()["userName"];

            var unlikedOwnPost = this.Data.Users.All()
                .First(u => u.UserName == username)
                .WallPosts.First(p => p.Likes.Count == 0);

            int postLikesCount = unlikedOwnPost.Likes.Count;

            var likeResponse = this.httpClient.PostAsync(string.Format("api/posts/{0}/likes", unlikedOwnPost.Id), null).Result;

            Assert.AreEqual(HttpStatusCode.OK, likeResponse.StatusCode);

            this.Data = new SocialNetworkData();
            Assert.AreEqual(postLikesCount + 1, this.Data.Posts.GetById(unlikedOwnPost.Id).Likes.Count);
        }

        [TestMethod]
        public void UnlikingLikedPostShouldDecrementLikes()
        {
            var loginResponse = this.Login(SeededUserUsername, SeededUserPassword);
            var username = loginResponse.Content.ReadAsStringAsync().Result.ToJson()["userName"];

            var unlikedOwnPost = this.Data.Users.All()
                .First(u => u.UserName == username)
                .WallPosts.First(p => p.Likes
                    .Any(l => l.User.UserName == username));

            int postLikesCount = unlikedOwnPost.Likes.Count;

            var unlikeResponse = this.httpClient.DeleteAsync(string.Format("api/posts/{0}/likes", unlikedOwnPost.Id)).Result;

            Assert.AreEqual(HttpStatusCode.OK, unlikeResponse.StatusCode);

            this.Data = new SocialNetworkData();
            Assert.AreEqual(postLikesCount - 1, this.Data.Posts.GetById(unlikedOwnPost.Id).Likes.Count);
        }
    }
}