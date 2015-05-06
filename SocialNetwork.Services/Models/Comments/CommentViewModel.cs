namespace SocialNetwork.Services.Models.Comments
{
    using System;
    using System.Linq;

    using SocialNetwork.Models;

    public class CommentViewModel
    {
        public int Id { get; set; }

        public string AuthorId { get; set; }

        public string AuthorUsername { get; set; }

        public string AuthorProfileImage { get; set; }

        public int LikesCount { get; set; }

        public string CommentContent { get; set; }

        public DateTime Date { get; set; }

        public bool Liked { get; set; }

        public static CommentViewModel Create(Comment c, ApplicationUser currentUser)
        {
            return new CommentViewModel()
            {
                Id = c.Id,
                AuthorId = c.AuthorId,
                AuthorUsername = c.Author.UserName,
                AuthorProfileImage =  c.Author.ProfileImageDataMinified,
                LikesCount = c.Likes.Count,
                CommentContent = c.Content,
                Date = c.Date,
                Liked = c.Likes
                    .Any(l => l.UserId == currentUser.Id)
            };
        }
    }
}