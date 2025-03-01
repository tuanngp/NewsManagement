using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BusinessObjects.Models;

public partial class NewsArticle
{
    public string? NewsArticleId { get; set; } = null!;

    [Required]
    public string? NewsTitle { get; set; }

    public string Headline { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    [Required]
    public string? NewsContent { get; set; }

    public string? NewsSource { get; set; }

    public short? CategoryId { get; set; }

    public bool? NewsStatus { get; set; }

    public short? CreatedById { get; set; }

    public short? UpdatedById { get; set; }

    public DateTime? ModifiedDate { get; set; }

    // Thêm trường cho Draft & Preview
    [Required]
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    // Thêm trường cho Workflow approval
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    public short? ApprovedById { get; set; }
    public DateTime? ApprovedDate { get; set; }

    // Thêm trường cho Scheduled publishing
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
