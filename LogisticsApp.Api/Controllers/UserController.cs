using LogisticsApp.Api.Data;
using LogisticsApp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class UserController : ControllerBase
    {
        private readonly LogisticsContext _context;

        public UserController(LogisticsContext context)
        {
            _context = context;
        }

        // GET: api/user
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            
            var users = await _context.Users
                .Select(u => new { u.Id, u.FullName, u.Email, u.Role })
                .ToListAsync();

            return Ok(users);
        }

        // POST: api/user
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User newUser)
        {
            if (await _context.Users.AnyAsync(u => u.Email == newUser.Email))
            {
                return BadRequest(new { message = "A user with this email already exists." });
            }

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User created successfully.", userId = newUser.Id });
        }

        // DELETE: api/user/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User access revoked." });
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            
            user.FullName = updatedUser.FullName;
            user.Email = updatedUser.Email;
            user.Role = updatedUser.Role;

            await _context.SaveChangesAsync();

            return Ok(new { message = "User profile updated successfully." });
        }



      
        public class UserUpdateDto
        {
            public string FullName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }
    }
}