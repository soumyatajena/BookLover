using Microsoft.AspNetCore.Mvc;
using API.Data;
using API.Entities;
using System.Security.Cryptography;
using System.Text;
using API.DTOs;
using Microsoft.EntityFrameworkCore;


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController:BaseApiController
    {
        private readonly DataContext context;
        public AccountController(DataContext context)
        {
            this.context=context;
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register(RegisterDto registeredDto)
        {
            //using - disposal of this object implicitly once class's work is done
            //when a class is called with Using statement - it calls the dispose() which implements IDisposable interface

            if(await UserExists(registeredDto.UserName))
            {
                return BadRequest("Username is taken");
            }
            
            using var hmac=new HMACSHA512();
            var user = new User
            {
                UserName=registeredDto.UserName.ToLower(),
                PasswordHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(registeredDto.Password)),
                PasswordSalt=hmac.Key
            };

            this.context.Users.Add(user);
            await this.context.SaveChangesAsync();
            return user;

        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login(LoginDto loginDto)
        {
            var user= await this.context.Users
                                .SingleOrDefaultAsync(x=>x.UserName==loginDto.UserName);
            if(user ==null)
            return Unauthorized("Invalid Username");

            using var hmac=new HMACSHA512(user.PasswordSalt);
            var computedHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            for(int i=0;i<computedHash.Length;i++)
            {
                if(computedHash[i]!=user.PasswordHash[i])
                {
                     return Unauthorized("Invalid Password");
                }
            }
            return user;

        }
        private async Task<bool> UserExists(string userName)
        {
            return await this.context.Users.AnyAsync(x=>x.UserName==userName.ToLower());

        }
    }
}