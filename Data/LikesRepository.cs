using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Data
{
    public class LikesRepository : ILikesRepository
    {
        private readonly DataContext _dataContext;

        public LikesRepository(DataContext dataContext)
        {
            _dataContext = dataContext;
        }
        public async Task<UserLike> GetUserLike(int sourceUserId, int likedUserId)
        {
            return await _dataContext.Likes.FindAsync(sourceUserId, likedUserId);
        }

        public async Task<PagedList<LikeDto>> GetUserLikes(LikesParams likesParams)
        {
            var users = _dataContext.Users.OrderBy(u => u.UserName).AsQueryable();
            var likes = _dataContext.Likes.AsQueryable();

            if(likesParams.Predicate=="liked")
            {
                likes = likes.Where(like => like.SourceUserId == likesParams.UserId);
                users = likes.Select(lik => lik.LikedUser);
            }

            if (likesParams.Predicate == "likedBy")
            {
                likes = likes.Where(like => like.LikedUserId == likesParams.UserId);
                users = likes.Select(lik => lik.SourceUser);
            }

           var likedUsers= users.Select(user => new LikeDto
            {
                Username=user.UserName,
                KnownAs=user.KnownAs,
                Age=user.DateofBirth.CalculateAge(),
                PhotoUrl=user.Photos.FirstOrDefault(p=>p.IsMain).Url,
                City=user.City,
                Id=user.Id
            });

            return await PagedList<LikeDto>.CreateAsync(likedUsers, likesParams.PageNumber, likesParams.PageSize);
        }

        public async Task<AppUser> GetUserwithLikes(int userId)
        {
            return await _dataContext.Users
                 .Include(x => x.LikedUsers)
                 .FirstOrDefaultAsync(x => x.Id == userId);
        }
    }
}
