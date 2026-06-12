using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LogisticsApp.Api.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
    }
}
