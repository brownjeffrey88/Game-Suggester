using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GSMVC.Models
{
    public class Request
    {
        [Required(ErrorMessage = "You have to enter a board game geek username")]
        [Display(Name = "BGG Username")]
        public string username { get; set; }
        [Display(Name = "remember me")]
        public bool remember { get; set; }
        [Display(Name = "Player count")]
        [Range(1, 100, ErrorMessage = "you need 1 or more players")]
        public int? players { get; set; }
        [Display(Name = "maximum play time")]
        [Range(1, 100000000000, ErrorMessage = "you need to enter a proper minimum play time")]
        public int? playTime { get; set; }
        [Display(Name = "weight/complexity")]
        [Range(0.00, 5.00, ErrorMessage = "you need to enter a weight between 0.00 and 5.00")]
        public float? weight { get; set; }
        [Display(Name = "minimum rating")]
        [Range(0.00, 10.00, ErrorMessage = "you need to enter a rating between 0 and 10")]
        public float? rating { get; set; }
        [Display(Name = "new or unplayed games only?")]
        public bool unplayed { get; set; }

    }
}
