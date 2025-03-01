using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Models;

public partial class SystemAccount
{
    public short AccountId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(
        100,
        MinimumLength = 2,
        ErrorMessage = "Name must be between 2 and 100 characters"
    )]
    [Display(Name = "Name")]
    public string? AccountName { get; set; }

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [Display(Name = "Email")]
    public string? AccountEmail { get; set; }

    [Required(ErrorMessage = "Role is required")]
    [Range(0, 2, ErrorMessage = "Please select a valid role")]
    [Display(Name = "Role")]
    public int? AccountRole { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(
        100,
        MinimumLength = 6,
        ErrorMessage = "Password must be between 6 and 100 characters"
    )]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character"
    )]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? AccountPassword { get; set; }

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
}
