namespace Pinula.Shared.DTOs
{
    public class PostCommentResponse
    {
        public decimal? NewAverageRating { get; set; }
        public int? NewUsersRatedCount { get; set; }
        public CommentPreview NewComment { get; set; } = null!;
    }
}
