using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Models;

public partial class Category
{
    public short CategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Category name must be between 3 and 100 characters")]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = null!;

    [Required(ErrorMessage = "Category description is required")]
    [StringLength(500, ErrorMessage = "Category description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string CategoryDesciption { get; set; } = null!;

    [Display(Name = "Parent Category")]
    public short? ParentCategoryId { get; set; }

    [Display(Name = "Active Status")]
    public bool? IsActive { get; set; } = true;

    public virtual ICollection<Category> InverseParentCategory { get; set; } = new List<Category>();

    public virtual ICollection<NewsArticle> NewsArticles { get; set; } = new List<NewsArticle>();

    public virtual Category? ParentCategory { get; set; }
}
