namespace SocialNetwork.Services.Models
{
    using System.ComponentModel.DataAnnotations;

    public class EditPostBindingModel
    {
        public int Id { get; set; }

        [Required]
        [MinLength(2)]
        public string Content { get; set; }
    }
}