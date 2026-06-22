using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pinula.Shared.Models;

public partial class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipeId { get; set; }

    public Guid UserId { get; set; }

    public string? Text { get; set; }

    public short? Rating { get; set; }

    public DateTime? CreatedAt { get; set; }

    public Guid? ParentCommentId { get; set; }

    public bool IsApproved { get; set; } = true;

    public bool IsDeleted { get; set; }

    public bool IsEdited { get; set; }

    public string LanguageCode { get; set; } = string.Empty;

    public DateTime? EditedAt { get; set; }

    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    [JsonIgnore]
    public virtual Comment? ParentComment { get; set; }
    [JsonIgnore]
    public virtual Recipe Recipe { get; set; } = null!;
    [JsonIgnore]
    public virtual User User { get; set; } = null!;
}
