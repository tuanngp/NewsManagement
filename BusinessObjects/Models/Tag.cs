using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Models;

public partial class Tag
{
    public int TagId { get; set; }

    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Tag name must be between 2 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9-_]+$", ErrorMessage = "Tag name can only contain letters, numbers, hyphens and underscores")]
    [Display(Name = "Tag Name")]
    public string? TagName { get; set; }

    [StringLength(200, ErrorMessage = "Note cannot exceed 200 characters")]
    public string? Note { get; set; }

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();
}
