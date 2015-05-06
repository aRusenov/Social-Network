namespace SocialNetwork.Services.Models.Users
{
    using SocialNetwork.Models;

    public class UserViewModelMinified
    {
        public string Id { get; set; }

        public string ProfileImageDataMinified { get; set; }

        public string Name { get; set; }

        public static UserViewModelMinified Create(ApplicationUser user)
        {
            return new UserViewModelMinified()
            {
                Id = user.Id,
                Name = user.Name,
                ProfileImageDataMinified = user.ProfileImageDataMinified
            };
        }
    }
}