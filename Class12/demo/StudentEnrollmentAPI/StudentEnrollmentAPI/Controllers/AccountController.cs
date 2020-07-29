﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StudentEnrollmentAPI.Models;
using StudentEnrollmentAPI.Models.DTO;

namespace StudentEnrollmentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]


    public class AccountController : ControllerBase
    {
        private UserManager<ApplicationUser> _userManager;
        private SignInManager<ApplicationUser> _signInManager;
        private IConfiguration _config;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = configuration;
        }
        // api/account/register
        [HttpPost, Route("register")]
        public async Task<IActionResult> Register(RegisterDTO register)
        {
            ApplicationUser user = new ApplicationUser()
            {
                Email = register.Email,
                UserName = register.Email,
                FirstName = register.FirstName,
                LastName = register.LastName
            };

            // create the user. 
            var result = await _userManager.CreateAsync(user, register.Password);

            if (result.Succeeded)
            {

                if(user.Email == _config["PrincipalSeed"])
                {
                  await  _userManager.AddToRoleAsync(user, ApplicationRoles.Principal);
                }
                // sign the user in if it was successful. 
                await _signInManager.SignInAsync(user, false);

                return Ok();
            }

            return BadRequest("Invalid Registeration");


            // do something to put this into the database. 
        }

        [HttpPost, Route("Login")]
        public async Task<IActionResult> Login(LoginDTO login)
        {
            var result = await _signInManager.PasswordSignInAsync(login.Email, login.Password, false, false);
            if (result.Succeeded)
            {

                // look the user up
                var user = await _userManager.FindByEmailAsync(login.Email);

                var token = CreateToken(user);

                // make them a token based on their account

                // Send that JWT token abck to the user

                // log the user in

                return Ok(new
                {
                    jwt = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }

            return BadRequest("invalid attempt");


        }

        [HttpPost, Route("assign/role")]
        [Authorize(Roles = ApplicationRoles.Principal)]
        public async Task AssignRoleToUser(AssignRoleDTO assignment)
        {

            var user = await _userManager.FindByEmailAsync(assignment.Email);

            // validation here to confirm the role is valid
            //string role = "";
            //if (assignment.Role.ToUpper() == "ADVISOR")
            //{
            //    role = ApplicationRoles.Advisor;
            //}

            await _userManager.AddToRoleAsync(user, assignment.Role);
        }

        private JwtSecurityToken CreateToken(ApplicationUser user)
        {
            // Token requires pieces of information called "claims"
            // Person/User is the principle
            // A principle can have many forms of identity
            // an identity contains many claims
            // a claim is a single statement about the user

            var authClaims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("UserId", user.Id)
            };
            var token = AuthenticateToken(authClaims);

            return token;
        }


        private JwtSecurityToken AuthenticateToken(Claim[] claims)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWTKey"]));

            var token = new JwtSecurityToken(
                issuer: _config["JWTIssuer"],
                audience: _config["JWTIssuer"],
                expires: DateTime.UtcNow.AddHours(24),
                claims: claims,
                signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                );

            return token;

        }


    }
}
