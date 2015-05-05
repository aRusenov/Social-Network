namespace SocialNetwork.Services.Models.Likes
{
    using SocialNetwork.Models;

    public class LikeViewModel
    {
        public static object SelectPostLikeData(PostLike postLike)
        {
            return new
            {
                userId = postLike.UserId,
                postId = postLike.PostId
            };
        }

        public static object SelectCommentLikeData(CommentLike commentLike)
        {
            return new
            {
                userId = commentLike.UserId,
                commentId = commentLike.CommentId
            };
        }
    }
}