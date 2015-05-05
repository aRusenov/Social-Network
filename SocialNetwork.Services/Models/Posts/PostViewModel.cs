﻿namespace SocialNetwork.Services.Models.Posts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using SocialNetwork.Services.Models.Comments;
    using SocialNetwork.Models;

    public class PostViewModel
    {
        public int Id { get; set; }

        public string AuthorId { get; set; }

        public string AuthorUsername { get; set; }

        public string AuthorProfileImage { get; set; }

        public string WallOwnerId { get; set; }

        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int LikesCount { get; set; }

        public bool Liked { get; set; }

        public int TotalCommentsCount { get; set; }

        public IEnumerable<CommentViewModel> Comments { get; set; }

        public static PostViewModel Create(Post p, ApplicationUser currentUser)
        {
            return new PostViewModel()
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                AuthorUsername = p.Author.UserName,
                AuthorProfileImage = p.Author.ProfileImageDataMinified,
                WallOwnerId = p.WallOwnerId,
                Content = p.Content,
                Date = p.Date,
                LikesCount = p.Likes.Count,
                Liked = p.Likes
                    .Any(l => l.UserId == currentUser.Id),
                TotalCommentsCount = p.Comments.Count,
                Comments = p.Comments
                    .Take(3)
                    .Select(c => CommentViewModel.Create(c, currentUser))
            };
        }
    }
}