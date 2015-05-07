namespace SocialNetwork.Services.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;

    using SocialNetwork.Common;
    using SocialNetwork.Data;
    using SocialNetwork.Models;
    using SocialNetwork.Services.Models.Posts;
    using SocialNetwork.Services.Models.Users;
    using SocialNetwork.Services.UserSessionUtils;
    using ChangePasswordBindingModel = SocialNetwork.Services.Models.ChangePasswordBindingModel;

    [SessionAuthorize]
    [RoutePrefix("api/me")]
    public class ProfileController : BaseApiController
    {
        private readonly ApplicationUserManager userManager;

        public ProfileController()
        {
            this.userManager = new ApplicationUserManager(
                new UserStore<ApplicationUser>(new ApplicationDbContext()));
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return this.userManager;
            }
        }

        [HttpGet]
        [Route]
        public IHttpActionResult GetProfileInfo()
        {
            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest("Invalid session token.");
            }

            var user = this.SocialNetworkData.Users.GetById(userId);

            return this.Ok(new
            {
                userName = user.UserName,
                name = user.Name,
                gender = user.Gender.ToString(),
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                image = user.ProfileImageData,
                friendsCount = user.Friends.Count
            });
        }

        [HttpPut]
        [Route]
        public IHttpActionResult EditProfileInfo(EditUserBindingModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            // Validate the current user exists in the database
            var currentUserId = this.User.Identity.GetUserId();
            var currentUser = this.SocialNetworkData.Users.All()
                .FirstOrDefault(u => u.Id == currentUserId);
            if (currentUser == null)
            {
                return this.BadRequest("Invalid user token.");
            }

            var emailHolder = this.SocialNetworkData.Users.All()
                .FirstOrDefault(u => u.Email == model.Email);
            if (emailHolder != null && emailHolder.Id != currentUserId)
            {
                return this.BadRequest("Email is already taken.");
            }

            if (!this.ValidateImageSize(model.ProfileImageData, ProfileImageKilobyteLimit))
            {
                return this.BadRequest(string.Format("Profile image size should be less than {0}kb.", ProfileImageKilobyteLimit));
            }

            if (!this.ValidateImageSize(model.CoverImageData, CoverImageKilobyteLimit))
            {
                return this.BadRequest(string.Format("Cover image size should be less than {0}kb.", CoverImageKilobyteLimit));
            }

            currentUser.Name = model.Name;
            currentUser.Email = model.Email;
            currentUser.PhoneNumber = model.PhoneNumber;
            currentUser.Gender = model.Gender;
            currentUser.ProfileImageData = model.ProfileImageData;

            var minifiedDataUrl = ImageUtility.Resize(model.ProfileImageData, 100, 100);
            currentUser.ProfileImageDataMinified = minifiedDataUrl;

            currentUser.CoverImageData = model.CoverImageData;

            this.SocialNetworkData.SaveChanges();

            return this.Ok(new
            {
                message = "User profile edited successfully."
            });
        }

        [HttpPut]
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangeUserPassword(ChangePasswordBindingModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            if (this.User.Identity.GetUserName() == "admin")
            {
                return this.BadRequest("Password change for user 'admin' is not allowed!");
            }

            var result = await this.UserManager.ChangePasswordAsync(
                this.User.Identity.GetUserId(), model.OldPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                return this.GetErrorResult(result);
            }

            return this.Ok(
                new
                {
                    message = "Password successfully changed.",
                }
            );
        }

        [HttpGet]
        [Route("friends")]
        public IHttpActionResult GetFriends()
        {
            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest();
            }

            var user = this.SocialNetworkData.Users.GetById(userId);
            var friends = user.Friends
                .OrderBy(fr => fr.Name)
                .Select(fr => new
                {
                    id = fr.Id,
                    userName = fr.UserName,
                    name = fr.Name,
                    image = fr.ProfileImageData
                });

            return this.Ok(friends);
        }

        [HttpGet]
        [Route("feed")]
        public IHttpActionResult GetNewsFeed([FromUri]NewsFeedBindingModel feedModel)
        {
            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest();
            }

            var user = this.SocialNetworkData.Users.GetById(userId);
            var candidatePosts = this.SocialNetworkData.Posts.All()
                .Where(p => p.Author.Friends.Any(fr => fr.Id == userId) || 
                    p.WallOwner.Friends.Any(fr => fr.Id == userId))
                .OrderByDescending(p => p.Date)
                .AsEnumerable();

            if (feedModel.StartPostId.HasValue)
            {
                candidatePosts = candidatePosts
                    .SkipWhile(p => p.Id != feedModel.StartPostId)
                    .Skip(1);
            }

            var pagePosts = candidatePosts
                .Take(feedModel.PageSize)
                .Select(p => PostViewModel.Create(p, user));

            if (pagePosts.Any())
            {
                return this.Ok(Enumerable.Empty<string>());
            }

            return this.Ok(pagePosts);
        }

        [HttpGet]
        [Route("requests")]
        public IHttpActionResult GetFriendRequests()
        {
            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest();
            }

            var user = this.SocialNetworkData.Users.GetById(userId);
            var friendRequests = user.FriendRequests
                .OrderBy(r => (int) r.Status)
                .Select(r => new
                {
                    id = r.Id,
                    status = r.Status,
                    user = new
                    {
                        id = r.From.Id,
                        userName = r.From.UserName,
                        name = r.From.Name,
                        image = r.From.ProfileImageData
                    }
                });

            return this.Ok(friendRequests);
        }

        [HttpPut]
        [Route("requests/{requestId}")]
        public IHttpActionResult ChangeRequestStatus(int requestId, [FromUri] string status)
        {
            var request = this.SocialNetworkData.FriendRequests.GetById(requestId);
            if (request == null)
            {
                return this.NotFound();
            }

            if (request.Status != FriendRequestStatus.Pending)
            {
                return this.BadRequest("Request status is already resolved.");
            }

            var userId = this.User.Identity.GetUserId();
            if (userId == null)
            {
                return this.BadRequest("Invalid session token.");
            }

            var user = this.SocialNetworkData.Users.GetById(userId);
            if (!user.FriendRequests.Contains(request))
            {
                return this.BadRequest("Friend request belongs to different user.");
            }

            if (status == "approved")
            {
                request.Status = FriendRequestStatus.Approved;
                user.Friends.Add(request.From);
                request.From.Friends.Add(user);
            }
            else if (status == "rejected")
            {
                request.Status = FriendRequestStatus.Rejected;
            }
            else
            {
                return this.BadRequest("Invalid friend request status.");
            }

            this.SocialNetworkData.SaveChanges();

            return this.Ok(new
            {
                message = string.Format("Friend request successfully {0}.", status)
            });
        }

        [HttpPost]
        [Route("requests/{username}")]
        public IHttpActionResult SendFriendRequest(string username)
        {
            var recipient = this.SocialNetworkData.Users.All()
                .FirstOrDefault(u => u.UserName == username);
            if (recipient == null)
            {
                return this.NotFound();
            }

            var loggedUserId = this.User.Identity.GetUserId();
            if (loggedUserId == null)
            {
                return this.BadRequest("Invalid session token.");
            }

            var loggedUser = this.SocialNetworkData.Users.GetById(loggedUserId);
            if (username == loggedUser.UserName)
            {
                return this.BadRequest("Cannot send request to self.");
            }

            bool isAlreadyFriend = loggedUser.Friends
                .Any(fr => fr.UserName == recipient.UserName);
            if (isAlreadyFriend)
            {
                return this.BadRequest("User is already in friends.");
            }

            bool hasReceivedRequest = loggedUser.FriendRequests
                .Any(r => r.FromId == recipient.Id && r.Status == FriendRequestStatus.Pending);
            bool hasSentRequest = recipient.FriendRequests
                .Any(r => r.FromId == loggedUser.Id && r.Status == FriendRequestStatus.Pending);
            if (hasSentRequest || hasReceivedRequest)
            {
                return this.BadRequest("A pending request already exists.");
            }

            var friendRequest = new FriendRequest()
            {
                From = loggedUser,
                To = recipient
            };

            recipient.FriendRequests.Add(friendRequest);
            this.SocialNetworkData.SaveChanges();

            return this.Ok();
        }
    }
}