using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
            
               private readonly DataContext _dbContext;
               private readonly ITokenService _tokenService;
               public AccountController(DataContext dataContext,ITokenService tokenService)
               {
                   _dbContext = dataContext;
                   _tokenService =tokenService;
               }

               [HttpPost("register")]
               public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto) 
               {
                   if(await UserExsists(registerDto.Username)) return BadRequest("username is taken");
                   using  var hmac= new HMACSHA512();

                   var user = new AppUser
                   {
                       UserName=registerDto.Username.ToLower(),
                       PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                       PasswordSalt =hmac.Key
                   };

                   _dbContext.Add(user);
                   await _dbContext.SaveChangesAsync();

                    return new UserDto{
                        Username = user.UserName,
                        Token=_tokenService.CreateToken(user)
                    };
               }

              [HttpPost("login")]
               public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
               {
                   var user = await _dbContext.Users.SingleOrDefaultAsync(x=>x.UserName==loginDto.Username.ToLower());
                   if(user == null)return Unauthorized("Invalid Username");
                   using var hmac= new HMACSHA512(user.PasswordSalt);
                   var computeHash= hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
                   for(int i=0;i<computeHash.Length;i++){
                       if(computeHash[i]!=user.PasswordHash[i]) return Unauthorized("Invalid Password");
                   }
                  return new UserDto{
                        Username = user.UserName,
                        Token=_tokenService.CreateToken(user)
                    };

               }

               private async Task<bool> UserExsists(string username)
               {
                   return await _dbContext.Users.AnyAsync(x=>x.UserName == username.ToLower());
               }
               
    }
}