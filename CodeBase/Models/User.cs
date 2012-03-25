﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.Web.Security;

namespace CodeBase.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        public string Username { get; set; }
        public string Gravatar { get; set; }
        public DateTime? JoinDate { get; set; }

        [NotMapped]
        public MembershipUser MembershipUser { get; set; }
        [NotMapped]
        public IEnumerable<String> Roles {get;set;}



        public virtual ICollection<Article> Articles { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
        public virtual ICollection<Answer> Answers { get; set; }
        public virtual ICollection<Question> Questions { get; set; }
    }

}