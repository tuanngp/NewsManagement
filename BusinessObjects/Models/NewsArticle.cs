using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BusinessObjects.ValidationAttributes;

namespace BusinessObjects.Models;

public partial class NewsArticle
{
    [Required(ErrorMessage = "Article ID is required")]
    [StringLength(50)]
    [Display(Name = "Article ID")]
    public string? NewsArticleId { get; set; } = null!;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters")]
    [Display(Name = "Title")]
    public string? NewsTitle { get; set; }

    [Required(ErrorMessage = "Headline is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Headline must be between 10 and 500 characters")]
    public string Headline { get; set; } = null!;

    [Display(Name = "Created Date")]
    [DataType(DataType.DateTime)]
    public DateTime? CreatedDate { get; set; } = DateTime.Now;

    [Required(ErrorMessage = "Content is required")]
    [MinLength(100, ErrorMessage = "Content must be at least 100 characters")]
    [Display(Name = "Content")]
    public string? NewsContent { get; set; }

    [Display(Name = "Source")]
    [StringLength(200)]
    [Url(ErrorMessage = "Please enter a valid URL")]
    public string? NewsSource { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public short? CategoryId { get; set; }

    [Display(Name = "Status")]
    public bool? NewsStatus { get; set; }

    [Display(Name = "Created By")]
    public short? CreatedById { get; set; }

    [Display(Name = "Updated By")]
    public short? UpdatedById { get; set; }

    [Display(Name = "Modified Date")]
    [DataType(DataType.DateTime)]
    public DateTime? ModifiedDate { get; set; }

    [Required]
    [Display(Name = "Article Status")]
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    [Display(Name = "Approval Status")]
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    [Display(Name = "Approved By")]
    public short? ApprovedById { get; set; }

    [Display(Name = "Approval Date")]
    [DataType(DataType.DateTime)]
    public DateTime? ApprovedDate { get; set; }

    [Display(Name = "Publish Date")]
    [DataType(DataType.DateTime)]
    [FutureDate(ErrorMessage = "Publish date must be in the future")]
    public DateTime? PublishDate { get; set; }

    public virtual Category? Category { get; set; }

    public virtual SystemAccount? CreatedBy { get; set; }
    public virtual SystemAccount? ApprovedBy { get; set; }

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

public enum ArticleStatus
{
    Draft,
    Published,
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
}
