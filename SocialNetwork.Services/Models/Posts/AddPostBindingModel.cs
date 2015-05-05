namespace SocialNetwork.Services.Models.Posts
{
    using System.ComponentModel.DataAnnotations;

    public class AddPostBindingModel
    {
        [Required]
        public string Content { get; set; }
    }
}