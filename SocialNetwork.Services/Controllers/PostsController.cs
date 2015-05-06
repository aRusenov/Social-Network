using System;

namespace SocialNetwork.Services.Controllers
{
    using System.Linq;
    using System.Web.Http;

    using Microsoft.AspNet.Identity;

    using SocialNetwork.Models;
    using SocialNetwork.Services.Models;
    using SocialNetwork.Services.Models.Likes;
    using SocialNetwork.Services.Models.Posts;
    using SocialNetwork.Services.UserSessionUtils;

    [SessionAuthorize]
    [RoutePrefix("api/Posts")]
    public class PostsController : BaseApiController
    {
        [HttpPost]
        [Route]
        public IHttpActionResult PostOnWall(AddPostBindingModel postModel)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var wallOwner = this.SocialNetworkData.Users.All()
                .FirstOrDefault(u => u.Id == postModel.UserId);
            if (wallOwner == null)
            {
                return this.NotFound();
            }

            var loggedUserId = this.User.Identity.GetUserId();

            var loggedUser = this.SocialNetworkData.Users
                .GetById(loggedUserId);

            if (loggedUserId == null)
            {
                return this.BadRequest("Invalid session token.");
            }

            if (wallOwner.Id != loggedUserId)
            {
                var isFriendOfWallOwner = wallOwner.Friends
                   .Any(fr => fr.Id == loggedUserId);
                if (!isFriendOfWallOwner)
                {
                    return this.BadRequest("Only friends can post on user's wall.");
                }
            }

            var newPost = new Post()
            {
                Content = postModel.PostContent,
                WallOwner = wallOwner,
                WallOwnerId = wallOwner.Id,
                Author = loggedUser,
                AuthorId = loggedUserId,
                Date = DateTime.Now
            };

            this.SocialNetworkData.Posts.Add(newPost);
            this.SocialNetworkData.SaveChanges();

            return this.Ok(new
            {
                message = "Post successfully added.",
                post = PostViewModel.Create(newPost, loggedUser)
            });
        }

        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult Get(int id)
        {
            var existingPost = this.SocialNetworkData.Posts.All()
                .FirstOrDefault(p => p.Id == id);

            if (existingPost == null)
            {
                return this.NotFound();
            }

            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest("Invalid session token.");
            }

            var user = this.SocialNetworkData.Users.GetById(userId);

            var post = PostViewModel.Create(existingPost, user);

            return this.Ok(post);
        }

        [HttpPut]
        [Route("{id}")]
        public IHttpActionResult Put(int id, EditPostBindingModel post)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var existingPost = this.SocialNetworkData.Posts
                .All()
                .FirstOrDefault(p => p.Id == id);

            if (existingPost == null)
            {
                return this.NotFound();
            }

            existingPost.Content = post.PostContent;
            this.SocialNetworkData.Posts.SaveChanges();

            post.Id = existingPost.Id;
            return this.Ok(new
            {
                id,
                content = post.PostContent
            });
        }

        [HttpDelete]
        [Route("{id}")]
        public IHttpActionResult Delete(int id)
        {
            var existingPost = this.SocialNetworkData.Posts.All()
                .FirstOrDefault(p => p.Id == id);

            if (existingPost == null)
            {
                return this.NotFound();
            }

            this.SocialNetworkData.Posts.Delete(existingPost);
            this.SocialNetworkData.Posts.SaveChanges();

            return this.Ok();
        }

        [HttpGet]
        [Route("{id}/likes")]
        public IHttpActionResult GetLikes(int id)
        {
            var existingPost = this.SocialNetworkData.Posts.GetById(id);
            if (existingPost == null)
            {
                return this.NotFound();
            }

            var postLikes = existingPost.Likes
                .Select(l => new DetailedPostLikesViewModel()
                {
                    UserId = l.UserId,
                    Name = l.User.Name,
                    Username = l.User.UserName,
                    ProfileImage = l.User.ProfileImageDataMinified,
                    PostId = l.PostId
                });
 
            return this.Ok(postLikes);
        }

        [HttpGet]
        [Route("{id}/likes/preview")]
        public IHttpActionResult GetLikesPreview(int id)
        {
            var existingPost = this.SocialNetworkData.Posts.GetById(id);
            if (existingPost == null)
            {
                return this.NotFound();
            }

            var postLikes = existingPost.Likes
                .Take(10)
                .Select(LikeViewModel.SelectPostLikeData);

            return this.Ok(new
            {
                totalLikeCount = existingPost.Likes.Count,
                postLikes
            });
        }

        [HttpPost]
        [Route("{id}/likes")]
        public IHttpActionResult PostLike(int id)
        {
            var existingPost = this.SocialNetworkData.Posts.GetById(id);
            if (existingPost == null)
            {
                return this.NotFound();
            }

            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest("Invalid session token.");
            }

            var user = this.SocialNetworkData.Users.GetById(userId);

            if (!this.HasAccessToPost(user, existingPost))
            {
                return this.BadRequest("Cannot like this post.");
            }

            bool hasAlreadyLiked = existingPost.Likes.Any(l => l.UserId == userId);
            if (hasAlreadyLiked)
            {
                return this.BadRequest("Post is already liked.");
            }

            this.SocialNetworkData.PostLikes.Add(new PostLike()
            {
                PostId = existingPost.Id,
                UserId = userId
            });

            this.SocialNetworkData.SaveChanges();

            return this.Ok(new
            {
                postId = existingPost.Id,
                likesCount = existingPost.Likes.Count,
                liked = true
            });
        }

        [HttpDelete]
        [Route("{id}/likes")]
        public IHttpActionResult DeleteLike(int id)
        {
            var existingPost = this.SocialNetworkData.Posts.GetById(id);
            if (existingPost == null)
            {
                return this.NotFound();
            }

            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest("Invalid session token.");
            }

            var user = this.SocialNetworkData.Users.GetById(userId);

            if (!this.HasAccessToPost(user, existingPost))
            {
                return this.BadRequest("Cannot like this post.");
            }

            var postLike = existingPost.Likes
                .FirstOrDefault(l => l.UserId == userId);
            if (postLike == null)
            {
                return this.BadRequest("Post has no like.");
            }

            this.SocialNetworkData.PostLikes.Delete(postLike);
            this.SocialNetworkData.SaveChanges();

            return this.Ok(new
            {
                postId = existingPost.Id,
                likesCount = existingPost.Likes.Count,
                liked = false
            });
        }
    }
}