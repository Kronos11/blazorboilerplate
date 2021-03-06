﻿using AutoMapper;
using BlazorBoilerplate.Server.Data;
using BlazorBoilerplate.Server.Middleware.Wrappers;
using BlazorBoilerplate.Server.Models;
using BlazorBoilerplate.Shared.Dto;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorBoilerplate.Server.Services
{
    public interface IUserProfileService
    {
        Task<ApiResponse> Get(Guid userId);
        Task<ApiResponse> Upsert(UserProfileDto userProfile);
        Task<string> GetLastPageVisited(string userName);
    }
    public class UserProfileService : IUserProfileService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _autoMapper;

        public UserProfileService(ApplicationDbContext db, IMapper autoMapper)
        {
            _db = db;
            _autoMapper = autoMapper;
        }

        public async Task<string> GetLastPageVisited(string userName)
        {
            string lastPageVisited = "/dashboard";
            var userProfile = await (from userProf in _db.UserProfiles
                                     join user in _db.Users on userProf.UserId equals user.Id
                                     where user.UserName == userName
                                     select userProf).FirstOrDefaultAsync();

            if (userProfile != null)
            {
                lastPageVisited = !String.IsNullOrEmpty(userProfile.LastPageVisited) ? userProfile.LastPageVisited : lastPageVisited;
            }

            return lastPageVisited;
        }

        public async Task<ApiResponse> Get(Guid userId)
        {
            var profileQuery = from userProf in _db.UserProfiles
                               where userProf.UserId == userId
                               select userProf;

            UserProfileDto userProfile = new UserProfileDto();
            if (!await profileQuery.AnyAsync())
            {
                userProfile = new UserProfileDto
                {
                    UserId = userId
                };
            }
            else
            {
                UserProfile profile = await profileQuery.FirstAsync();
                userProfile.Count = profile.Count;
                userProfile.IsNavOpen = profile.IsNavOpen;
                userProfile.LastPageVisited = profile.LastPageVisited;
                userProfile.IsNavMinified = profile.IsNavMinified;
                userProfile.UserId = userId;
            }

            return new ApiResponse(Status200OK, "Retrieved User Profile", userProfile);
        }

        public async Task<ApiResponse> Upsert(UserProfileDto userProfileDto)
        {
            try
            {
                var profileQuery = from prof in _db.UserProfiles where prof.UserId == userProfileDto.UserId select prof;
                if (profileQuery.Any())
                {
                    UserProfile profile = profileQuery.First();
                    //_autoMapper.Map<UserProfileDto, UserProfile>(userProfileDto, profile);

                    profile.Count = userProfileDto.Count;
                    profile.IsNavOpen = userProfileDto.IsNavOpen;
                    profile.LastPageVisited = userProfileDto.LastPageVisited;
                    profile.IsNavMinified = userProfileDto.IsNavMinified;
                    profile.LastUpdatedDate = DateTime.Now;
                    _db.UserProfiles.Update(profile);
                }
                else
                {
                    //TODO review automapper settings
                    //_autoMapper.Map<UserProfileDto, UserProfile>(userProfileDto, profile);

                    UserProfile profile = new UserProfile
                    {
                        UserId = userProfileDto.UserId,
                        Count = userProfileDto.Count,
                        IsNavOpen = userProfileDto.IsNavOpen,
                        LastPageVisited = userProfileDto.LastPageVisited,
                        IsNavMinified = userProfileDto.IsNavMinified,
                        LastUpdatedDate = DateTime.Now
                    };
                    _db.UserProfiles.Add(profile);
                }

                await _db.SaveChangesAsync();

                return new ApiResponse(Status200OK, "Updated User Profile");
            }
            catch (Exception ex)
            {
                string test = ex.Message;
                return new ApiResponse(Status400BadRequest, "Failed to Retrieve User Profile");
            }
        }
    }
}
